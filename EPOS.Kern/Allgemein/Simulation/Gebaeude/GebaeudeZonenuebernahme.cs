using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>„Gebäude als eine Zone übernehmen"</b> (Stufe G3; Mehrzonenkonzept 5.1,
    /// Softwarearchitektur W1) — die Kernfunktion hinter dem Knopf, den eine spätere Dialogwelle
    /// in den Reiter „Hülle und Rechenmodell" setzt. Sie bildet aus der Gebäudezeile einen
    /// <see cref="GebaeudeZonensatz"/> mit Bauteilen <b>ohne Schichten</b>, der die fünf
    /// U/A-Gruppen des Klassenwegs wiedergibt. Hier entsteht nur der Kern-Datensatz; die
    /// Abbildung auf neue Tabellenzeilen macht <see cref="GebaeudeZonenabbildung.AlsZoneModel"/>,
    /// geschrieben wird über <c>GebaeudeZonenCtrl.SpeichernJeGebaeude</c>. Ohne Datenbank, ohne
    /// Zustand.
    ///
    /// <para><b>Die Zerlegung</b> (Mehrzonenkonzept 3.6, 4.3):</para>
    /// <list type="bullet">
    /// <item>Außenwand und Sonstiges je in vier Viertel Nord/Ost/Süd/West — Neigung 90°, Azimut
    /// 0°/90°/180°/270° in der Datenbankkonvention (0° = Nord, im Uhrzeigersinn), U-Wert der
    /// Gruppe, je ein Viertel der Fläche.</item>
    /// <item>Dach waagerecht (Neigung 0°, ohne Azimut), Grundfläche als Bodenplatte (Neigung
    /// 180°, ohne Azimut) mit der Randbedingung aus <c>Grundflaeche_Randbedingung</c>:
    /// ERDREICH (oder leer) → Erdreich, KELLER → unbeheizter Raum, AUSSENLUFT → Außenluft.</item>
    /// <item>Fenster je Richtung aus den Fensterflächen Süd/Ost/West/Nord — Ost und West mit
    /// derselben Auflösung wie der Klassenweg
    /// (<see cref="GebaeudeVorbereitung.FensterflaechenOstWest"/>), senkrecht, U-Wert der
    /// Fenster; g-Wert, Rahmenanteil und Verschattung bleiben NaN = Gebäudewert, so dass ein
    /// späterer Wechsel am Gebäude weiter wirkt.</item>
    /// <item>Σψ·L des Gebäudes (die drei Wärmebrückenpaare) zu gleichen Teilen auf die vier
    /// Wandviertel; ohne Außenwand auf die Viertel von Sonstiges, ohne beide aufs Dach, sonst
    /// auf die Bodenplatte — Σψ·L geht im Bauteilweg über die Bauteile in R_ext ein.</item>
    /// </list>
    /// <para>Eine Gruppe ohne Fläche ergibt kein Bauteil (ein Bauteil braucht eine Fläche größer
    /// null). Geprüft wird nicht hier, sondern im Bauteilweg — mit denselben Grenzen wie für
    /// jedes andere Bauteil.</para>
    ///
    /// <para><b>Die Hochrechnung</b> (Anwenderentscheid vom 25.09.2026 „Hochrechnen“): Der
    /// Klassenweg rechnet den Katalogbau und multipliziert seine Reihe mit dem Faktor der
    /// Fassade nach (E8: bei einer Flächenangabe Projektfläche / Nutzfläche, bei einer
    /// Verbrauchsangabe aus der Verhältnisrechnung des Kataloglaufs). Die Übernahme rechnet die
    /// Hülle mit GENAU diesem Faktor auf die Projektfläche hoch — alle Bauteilflächen und ψ·L mal
    /// Faktor, die Zone trägt die Nutzfläche Faktor × Nutzfläche des Gebäudes —, und der Bauteilweg
    /// rechnet danach ohne Nachmultiplikation dasselbe Ergebnis: Das RC-Modell ist linear in
    /// Leitwerten, Kapazitäten und Lasten, und über den Flächenschlüssel folgen Speichermasse,
    /// Luftvolumen, innere Gewinne und f_IW·A_f der Zonenfläche. Den Faktor liefert die Fassade
    /// selbst (<c>GebaeudeBedarfCtrl.Hochrechnungsfaktor</c>), er wird hier nicht nachgerechnet.
    /// Faktor 1 ist der Grenzfall darunter.</para>
    ///
    /// <para><b>Der Grenzfall:</b> Mit diesem Satz rechnet der Bauteilweg dieselben Reihen wie
    /// der Klassenweg, bis auf die Reihenfolge der Summen (Nachweis
    /// <c>GebaeudeBauteilwegLaufTests</c>). Zwei benannte Ausnahmen, beide nur mit Schalter
    /// „Strahlung auf Außenbauteile": eine Grundfläche an Außenluft rechnet im Bauteilweg mit
    /// ihrer Neigung (Sichtfaktor 0, reflektierte Einstrahlung), der Klassenweg ohne
    /// Strahlungsterm; und die Außenwand bekommt je Viertel die Strahlung ihrer Richtung statt
    /// des Mittels der vier Fassaden — der Mittelwert ist derselbe, weil der kurzwellige Term
    /// linear in der Einstrahlung ist.</para>
    /// </summary>
    internal static class GebaeudeZonenuebernahme
    {
        /// <summary>Die Bezeichnung der übernommenen Zone.</summary>
        internal const string ZONE_BEZEICHNUNG = "Gebäude";

        /// <summary>Azimut Nord in der Datenbankkonvention [°].</summary>
        internal const double AZIMUT_NORD = 0.0;
        /// <summary>Azimut Ost in der Datenbankkonvention [°].</summary>
        internal const double AZIMUT_OST = 90.0;
        /// <summary>Azimut Süd in der Datenbankkonvention [°].</summary>
        internal const double AZIMUT_SUED = 180.0;
        /// <summary>Azimut West in der Datenbankkonvention [°].</summary>
        internal const double AZIMUT_WEST = 270.0;

        /// <summary>Neigung senkrecht [°].</summary>
        internal const double NEIGUNG_SENKRECHT = 90.0;
        /// <summary>Neigung waagerecht nach oben [°] (Dach).</summary>
        internal const double NEIGUNG_WAAGERECHT_OBEN = 0.0;
        /// <summary>Neigung waagerecht nach unten [°] (Bodenplatte).</summary>
        internal const double NEIGUNG_WAAGERECHT_UNTEN = 180.0;

        private static readonly double[] Azimute = { AZIMUT_NORD, AZIMUT_OST, AZIMUT_SUED, AZIMUT_WEST };
        private static readonly string[] Richtungen = { "Nord", "Ost", "Süd", "West" };

        /// <summary>
        /// Bildet aus der Gebäudezeile <paramref name="g"/> EINE Zone mit Bauteilen ohne
        /// Schichten, ohne Hochrechnung (Faktor 1 — der Grenzfall; Regeln:
        /// <see cref="GebaeudeZonenuebernahme"/>). Liest die Zeile, schreibt sie nicht.
        /// </summary>
        internal static GebaeudeZonensatz AlsEineZone(ProjektGebaeudeModel g) => AlsEineZone(g, 1.0);

        /// <summary>
        /// Bildet aus der Gebäudezeile <paramref name="g"/> EINE Zone mit Bauteilen ohne
        /// Schichten, auf die Projektfläche hochgerechnet (Regeln:
        /// <see cref="GebaeudeZonenuebernahme"/>): jede Bauteilfläche und jedes ψ·L mal
        /// <paramref name="faktor"/>, die Zone mit der Nutzfläche <paramref name="faktor"/> ×
        /// Nutzfläche des Gebäudes. Liest die Zeile, schreibt sie nicht.
        /// </summary>
        /// <param name="g">Die Gebäudezeile (Klassenweg, U-Wert-Gruppen).</param>
        /// <param name="faktor">Der Hochrechnungsfaktor der Fassade (E8) — größer null und endlich.</param>
        internal static GebaeudeZonensatz AlsEineZone(ProjektGebaeudeModel g, double faktor)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (!(faktor > 0.0) || double.IsInfinity(faktor))
                throw new ArgumentOutOfRangeException(nameof(faktor), faktor, "Der Hochrechnungsfaktor ist nicht größer null oder nicht endlich.");

            var bauteile = new List<BauteilEingang>();

            // Σψ·L des Gebäudes — dieselbe Bildung wie im Eingangsbauer, hochgerechnet.
            double psiL = faktor * (g.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand * g.Abmessung_Anschluß_Fenster_Wand
                        + g.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach * g.Abmessung_Anschluß_Wand_Dach
                        + g.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke * g.Abmessung_Anschluß_Außenwand_Kellerdecke);
            bool wand = g.Flaeche_Außenwand > 0.0;
            bool sonstiges = g.Sonstige_Flaechen > 0.0;
            double psiViertel = psiL / 4.0;

            for (int i = 0; i < 4; i++)
                if (wand)
                    bauteile.Add(new BauteilEingang("Außenwand " + Richtungen[i], Bauteilart.Aussenwand,
                        faktor * g.Flaeche_Außenwand / 4.0, Bauteilrand.Aussenluft, g.k_Wert_Außenwand,
                        neigungGrad: NEIGUNG_SENKRECHT, azimutGrad: Azimute[i], psiL_WK: psiViertel));

            for (int i = 0; i < 4; i++)
                if (sonstiges)
                    bauteile.Add(new BauteilEingang("Sonstiges " + Richtungen[i], Bauteilart.Sonstiges,
                        faktor * g.Sonstige_Flaechen / 4.0, Bauteilrand.Aussenluft, g.k_Wert_Sonstiges,
                        neigungGrad: NEIGUNG_SENKRECHT, azimutGrad: Azimute[i],
                        psiL_WK: wand ? 0.0 : psiViertel));

            if (g.Dachflaeche > 0.0)
                bauteile.Add(new BauteilEingang("Dach", Bauteilart.Dach, faktor * g.Dachflaeche, Bauteilrand.Aussenluft,
                    g.k_Wert_Dachflaeche, neigungGrad: NEIGUNG_WAAGERECHT_OBEN,
                    psiL_WK: wand || sonstiges ? 0.0 : psiL));

            if (g.Grundflaeche > 0.0)
                bauteile.Add(new BauteilEingang("Bodenplatte", Bauteilart.Bodenplatte, faktor * g.Grundflaeche,
                    RandAusGrund(g.Grundflaeche_Randbedingung), g.k_Wert_Grundflaeche,
                    neigungGrad: NEIGUNG_WAAGERECHT_UNTEN,
                    psiL_WK: wand || sonstiges || g.Dachflaeche > 0.0 ? 0.0 : psiL));

            // Fenster je Richtung: Ost/West mit der NULL-Vorgabe aus dem Bestandsfeld wie im Klassenweg.
            GebaeudeVorbereitung.FensterflaechenOstWest(g, out double ost, out double west);
            double[] fenster = { g.Fensterflaeche_Nord, ost, g.Fensterflaeche_Sued, west };
            for (int i = 0; i < 4; i++)
                if (fenster[i] > 0.0)
                    bauteile.Add(new BauteilEingang("Fenster " + Richtungen[i], Bauteilart.Fenster, faktor * fenster[i],
                        Bauteilrand.Aussenluft, g.k_Wert_Fenster,
                        neigungGrad: NEIGUNG_SENKRECHT, azimutGrad: Azimute[i]));

            return new GebaeudeZonensatz(0, ZONE_BEZEICHNUNG, bauteile.AsReadOnly(), faktor * g.Nutzflaeche);
        }

        /// <summary>
        /// Die Randbedingung der Bodenplatte aus <c>Grundflaeche_Randbedingung</c>: leer oder
        /// ERDREICH → Erdreich, KELLER → unbeheizter Raum, AUSSENLUFT → Außenluft. Ein
        /// unbekannter Wert bleibt Erdreich — die Gebäudezeile lehnt ihn im Eingangsbauer
        /// ohnehin benannt ab.
        /// </summary>
        internal static Bauteilrand RandAusGrund(string grundRandbedingung)
        {
            if (string.Equals(grundRandbedingung, DbWerte.GRUND_KELLER, StringComparison.Ordinal)) return Bauteilrand.Unbeheizt;
            if (string.Equals(grundRandbedingung, DbWerte.GRUND_AUSSENLUFT, StringComparison.Ordinal)) return Bauteilrand.Aussenluft;
            return Bauteilrand.Erdreich;
        }
    }
}
