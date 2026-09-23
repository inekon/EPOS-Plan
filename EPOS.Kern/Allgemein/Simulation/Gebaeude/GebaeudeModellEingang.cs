using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Eingang des Gebäudemodells</b> — die aufgelösten Gebäudedaten, der Satz
    /// <see cref="ErsatzparameterRC"/> und die sieben Randreihen zu 8 760 Stunden (Stufe G1;
    /// Umsetzungskonzept 1.4, Rechenschritte Kapitel 1, Schritte A und E).
    ///
    /// <para><b>Der Eingangsbauer ist die statische Fabrik <see cref="Bauen"/></b> und die
    /// eine Stelle, an der <see cref="GebaeudeKlimaweg"/> gerufen wird (A18). Er liest die
    /// Gebäudezeile <b>nach</b> dem Vorbereitungsschritt und die Klimareihen, die dieser
    /// bereitgestellt hat; er liest keine Tabelle selbst.</para>
    ///
    /// <para><b>Vorgaben bei NULL</b> sind die des Eingangsbauers (Rechenschritte 1.1), nicht
    /// DDL-Vorgaben; sie stehen in <see cref="GebaeudeFestwerte"/>. <b>Harte Prüfungen</b>
    /// (Konzept 4.8) brechen mit einem benannten <see cref="GebaeudeModellFehler"/> ab — die
    /// des Klassenwegs in <see cref="ErsatzparameterRC.AusKlassenweg"/>, die des Fahrplans
    /// und der Fensterflächen hier.</para>
    ///
    /// <para><b>Die Lastaufteilung liegt hier, nicht im Löser</b> (F-P2): solare und innere
    /// Lasten werden auf die beiden Oberflächenknoten und die Luft verteilt; der Löser bekommt
    /// genau die drei Lasten seiner Knotenbilanzen. Die Zwischengrößen bleiben hier prüfbar
    /// (<see cref="PhiSolar"/>, <see cref="ThetaGrund"/>).</para>
    ///
    /// <para>Unveränderlich nach <see cref="Bauen"/>; ohne Datenbank, ohne Protokoll.</para>
    /// </summary>
    internal sealed class GebaeudeModellEingang
    {
        private GebaeudeModellEingang() { }

        // =====================================================================
        //  Die aufgelösten Gebäudedaten (Rechenschritte 1.1)
        // =====================================================================

        /// <summary>Bezeichnung des Gebäudes für Meldungen.</summary>
        internal string Bezeichnung { get; private set; }

        /// <summary>Nutzfläche des Katalogbaus A_f [m²] (E13).</summary>
        internal double Nutzflaeche_M2 { get; private set; }
        /// <summary>Raumhöhe H [m].</summary>
        internal double Raumhoehe_M { get; private set; }
        /// <summary>Speichermasse C_ges [Wh/K] (<c>Bauweise</c>).</summary>
        internal double Bauweise_WhK { get; private set; }

        /// <summary>U-Wert Außenwand [W/(m²K)].</summary>
        internal double U_Aussenwand { get; private set; }
        /// <summary>U-Wert Fenster [W/(m²K)].</summary>
        internal double U_Fenster { get; private set; }
        /// <summary>U-Wert Dach [W/(m²K)].</summary>
        internal double U_Dach { get; private set; }
        /// <summary>U-Wert Grundfläche [W/(m²K)].</summary>
        internal double U_Grund { get; private set; }
        /// <summary>U-Wert Sonstiges [W/(m²K)].</summary>
        internal double U_Sonstige { get; private set; }

        /// <summary>Fläche Außenwand [m²].</summary>
        internal double A_Aussenwand_M2 { get; private set; }
        /// <summary>Gesamte Fensterfläche A_w [m²].</summary>
        internal double A_Fenster_M2 { get; private set; }
        /// <summary>Dachfläche [m²].</summary>
        internal double A_Dach_M2 { get; private set; }
        /// <summary>Grundfläche [m²].</summary>
        internal double A_Grund_M2 { get; private set; }
        /// <summary>Sonstige Flächen [m²].</summary>
        internal double A_Sonstige_M2 { get; private set; }
        /// <summary>Fensterfläche Süd [m²].</summary>
        internal double A_FensterSued_M2 { get; private set; }
        /// <summary>Fensterfläche Ost [m²].</summary>
        internal double A_FensterOst_M2 { get; private set; }
        /// <summary>Fensterfläche West [m²].</summary>
        internal double A_FensterWest_M2 { get; private set; }
        /// <summary>Fensterfläche Nord [m²].</summary>
        internal double A_FensterNord_M2 { get; private set; }

        /// <summary>Gesamtenergiedurchlassgrad g [–].</summary>
        internal double GWert { get; private set; }
        /// <summary>Rahmenanteil 1 − F_F [–].</summary>
        internal double Rahmenanteil { get; private set; }
        /// <summary>Verschattungsfaktor F_S [–].</summary>
        internal double Verschattungsfaktor { get; private set; }
        /// <summary>Σψ·L der drei Wärmebrückenpaare [W/K].</summary>
        internal double SummePsiL_WK { get; private set; }
        /// <summary>Luftwechselrate n [1/h].</summary>
        internal double Luftwechselrate_h { get; private set; }
        /// <summary>Innere Wärmegewinne des Katalogbaus [W], zeitlich konstant (G1).</summary>
        internal double InnereGewinne_W { get; private set; }

        /// <summary>Tagsollwert [°C].</summary>
        internal double SollTag { get; private set; }
        /// <summary>Nachtsollwert [°C].</summary>
        internal double SollNacht { get; private set; }
        /// <summary>Wochenendsollwert [°C]; wirksam nur über 5 °C.</summary>
        internal double SollWochenende { get; private set; }
        /// <summary>Feriensollwert [°C].</summary>
        internal double SollFerien { get; private set; }
        /// <summary>Obere Raumtemperatur θ_max [°C] (vor KU1 zugleich der Kühlsollwert).</summary>
        internal double ThetaMaxWert { get; private set; }
        /// <summary>Ferientage (Index 0 … 364), nur bei aktivem Fahrplan belegt.</summary>
        internal bool[] Ferientage { get; private set; }

        /// <summary>Masseanteil der Außenbauteile a_AW [–].</summary>
        internal double MasseanteilAussen { get; private set; }
        /// <summary>Innenflächenfaktor f_IW [–].</summary>
        internal double Innenflaechenfaktor { get; private set; }
        /// <summary>Strahlungsanteil der Heizübergabe [–].</summary>
        internal double HeizungStrahlungsanteil { get; private set; }
        /// <summary>Heizleistungsgrenze [W]; NaN = unbegrenzt.</summary>
        internal double HeizleistungMaxW { get; private set; }
        /// <summary>Randbedingung der Grundfläche (<c>DbWerte.GRUND_*</c>).</summary>
        internal string GrundRandbedingung { get; private set; }
        /// <summary>Kellertemperatur [°C].</summary>
        internal double Kellertemperatur { get; private set; }
        /// <summary>Schalter „Strahlung auf Außenbauteile" (in G1 ohne Wirkung, E5).</summary>
        internal bool AussenbauteileStrahlung { get; private set; }
        /// <summary>Ist eine der G2-Spalten (Infiltration, Nutzerlüftung, Sommerlüftung) gesetzt? In G1 ohne Wirkung.</summary>
        internal bool G2SpaltenGesetzt { get; private set; }

        // =====================================================================
        //  Ersatzparameter und Randreihen (Schritte A und E)
        // =====================================================================

        /// <summary>Die RC-Größen des Klassenwegs (Schritt A).</summary>
        internal ErsatzparameterRC Parameter { get; private set; }

        /// <summary>Der Zeitbezug, mit dem die Fassadenstrahlung gerechnet ist (U6).</summary>
        internal Zeitbezug Zeitbezug { get; private set; }

        /// <summary>Außenlufttemperatur [°C].</summary>
        internal double[] ThetaOut { get; private set; }
        /// <summary>U·A-gewichtete äquivalente Außentemperatur [°C] (E7).</summary>
        internal double[] ThetaEq { get; private set; }
        /// <summary>Strahlungslast Außenbauteiloberfläche [W].</summary>
        internal double[] PhiRadAW { get; private set; }
        /// <summary>Strahlungslast Innenbauteiloberfläche [W].</summary>
        internal double[] PhiRadIW { get; private set; }
        /// <summary>Konvektive Last an der Raumluft [W].</summary>
        internal double[] PhiConv { get; private set; }
        /// <summary>Heizsollwert [°C] (E8).</summary>
        internal double[] ThetaSoll { get; private set; }
        /// <summary>Obere Raumtemperatur [°C].</summary>
        internal double[] ThetaMax { get; private set; }

        /// <summary>Zwischengröße: Fenstersolareintrag gesamt [W] (E3).</summary>
        internal double[] PhiSolar { get; private set; }
        /// <summary>Zwischengröße: Temperatur an der Grundfläche [°C] (E6).</summary>
        internal double[] ThetaGrund { get; private set; }
        /// <summary>Zwischengröße: die Fassadenstrahlung [W/m²] (E2).</summary>
        internal Fassadenstrahlung Strahlung { get; private set; }
        /// <summary>Fiel die Erdreichrechnung auf Ersatzwerte des Jahresgangs zurück?</summary>
        internal bool ErdreichErsatzwerte { get; private set; }
        /// <summary>Zahl der Stunden mit Gegenstrahlung (in G1 nicht rechenwirksam).</summary>
        internal int StundenMitGegenstrahlung { get; private set; }

        /// <summary>Die Randbedingung der Stunde <paramref name="h"/> für den Löser.</summary>
        internal Stundenrand Rand(int h)
        {
            return new Stundenrand(ThetaOut[h], ThetaEq[h], ThetaSoll[h], ThetaMax[h],
                                   PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                   heizleistungMaxW: HeizleistungMaxW,
                                   heizungStrahlungsanteil: HeizungStrahlungsanteil);
        }

        /// <summary>Ist Stunde <paramref name="h"/> (0 … 8759) Nutzungszeit — Stunde des Tages 7 … 22, 1-basiert (Rechenschritte 8.2, E8)?</summary>
        internal static bool Nutzungszeit(int h)
        {
            int stundeDesTages = h % 24 + 1;
            return stundeDesTages >= GebaeudeFestwerte.TAG_ERSTE_STUNDE
                && stundeDesTages <= GebaeudeFestwerte.TAG_LETZTE_STUNDE;
        }

        // =====================================================================
        //  Der Eingangsbauer
        // =====================================================================

        /// <summary>
        /// <b>Der Eingangsbauer</b> (Umsetzungskonzept 1.4): Gebäudedaten auflösen und prüfen,
        /// Ersatzparameter nach dem Klassenweg, Klimaweg, Lastaufteilung und Sollwertfahrplan.
        /// </summary>
        /// <param name="gebaeude">Die Gebäudezeile (nach dem Vorbereitungsschritt).</param>
        /// <param name="solarOrtszeit">Die Klimazeilen in Ortszeit, 8 760, mit UTC-Herkunft.</param>
        /// <param name="wochenende">Die Wochenendmaske des Ortszeit-Kalenders, 365 Tage (U7).</param>
        /// <param name="laengengrad">Längengrad der Klimaregion [°].</param>
        /// <param name="breitengrad">Breitengrad der Klimaregion [°].</param>
        /// <param name="zeitbezug">Zeitbezug der Sonnengeometrie (U6).</param>
        /// <exception cref="GebaeudeModellException">bei jeder verletzten Prüfung.</exception>
        internal static GebaeudeModellEingang Bauen(
            ProjektGebaeudeModel gebaeude,
            IReadOnlyList<SolardatenModel> solarOrtszeit,
            bool[] wochenende,
            double laengengrad, double breitengrad,
            Zeitbezug zeitbezug = GebaeudeKlimaweg.ZEITBEZUG_VORGABE)
        {
            GebaeudeModellEingang e = Daten(gebaeude);
            e.Parameter = ErsatzparameterRC.AusKlassenweg(e);
            e.Zeitbezug = zeitbezug;

            GebaeudeKlimaweg.Pruefen(solarOrtszeit, laengengrad, breitengrad);
            if (wochenende == null || wochenende.Length != 365)
                throw new GebaeudeModellException(GebaeudeModellFehler.KlimadatenUnvollstaendig,
                    "Die Wochenendmaske des Ortszeit-Kalenders fehlt (365 Tage erwartet).");

            // ---- Klimaweg (E1, E2, E6) — die eine Aufrufstelle (A18) ----
            e.ThetaOut = GebaeudeKlimaweg.Aussentemperatur(solarOrtszeit);
            e.Strahlung = GebaeudeKlimaweg.Fassaden(solarOrtszeit, laengengrad, breitengrad, zeitbezug);
            bool erdreichAusKlima;
            e.ThetaGrund = GebaeudeKlimaweg.Grundtemperatur(e.GrundRandbedingung, e.Kellertemperatur,
                                                             e.ThetaOut, out erdreichAusKlima);
            e.ErdreichErsatzwerte = string.Equals(e.GrundRandbedingung, DbWerte.GRUND_ERDREICH, StringComparison.Ordinal)
                                    && !erdreichAusKlima;
            e.StundenMitGegenstrahlung = GebaeudeKlimaweg.StundenMitGegenstrahlung(solarOrtszeit);

            ErsatzparameterRC p = e.Parameter;

            // ---- Flächengewichte der Lastaufteilung (E3, E4) ----
            double aAwGes = p.A_AW_gesamt_M2;
            double aIw = p.A_IW_M2;
            double aRaum = aAwGes + aIw;
            double anteilAW = aAwGes / aRaum;
            double anteilIW = aIw / aRaum;

            double solarFaktor = e.GWert * (1.0 - e.Rahmenanteil) * e.Verschattungsfaktor * GebaeudeFestwerte.F_W;
            double aKon = GebaeudeFestwerte.A_KON_SOLAR;
            double innenKonv = GebaeudeFestwerte.ANTEIL_INNERE_LASTEN_KONVEKTIV * e.InnereGewinne_W;
            double innenRad = e.InnereGewinne_W - innenKonv;

            // ---- Gewichte der äquivalenten Außentemperatur (E7) ----
            double uaLuftseitig = e.U_Aussenwand * e.A_Aussenwand_M2 + e.U_Dach * e.A_Dach_M2
                                  + e.U_Sonstige * e.A_Sonstige_M2;
            double uaGrund = e.U_Grund * e.A_Grund_M2;
            double uaFenster = p.UA_Fenster_WK;
            double uaSumme = p.SummeUA_opak_WK + uaFenster;

            var thetaEq = new double[8760];
            var phiRadAW = new double[8760];
            var phiRadIW = new double[8760];
            var phiConv = new double[8760];
            var phiSolar = new double[8760];
            Fassadenstrahlung s = e.Strahlung;

            for (int h = 0; h < 8760; h++)
            {
                // E3 — Fenstersolareintrag, flächenproportional verteilt (A_v = 0 im Klassenweg).
                double sol = (e.A_FensterSued_M2 * s.Sued[h] + e.A_FensterOst_M2 * s.Ost[h]
                              + e.A_FensterWest_M2 * s.West[h] + e.A_FensterNord_M2 * s.Nord[h]) * solarFaktor;
                phiSolar[h] = sol;
                double solLuft = aKon * sol;
                double solRad = sol - solLuft;

                // E4 — innere Lasten, 0,5/0,5; die drei Summen für den Löser.
                phiRadAW[h] = solRad * anteilAW + innenRad * anteilAW;
                phiRadIW[h] = solRad * anteilIW + innenRad * anteilIW;
                phiConv[h] = solLuft + innenKonv;

                // E5/E7 — in G1: opake Flächen θ_out (Schalter aus), Fenster θ_out + Δθ_lw (NULL-Regel 0).
                double tOut = e.ThetaOut[h];
                double tFenster = tOut + GebaeudeKlimaweg.DeltaThetaLangwellig(
                    GebaeudeKlimaweg.Gegenstrahlung(solarOrtszeit[h]));
                thetaEq[h] = uaSumme > 0.0
                    ? (uaLuftseitig * tOut + uaGrund * e.ThetaGrund[h] + uaFenster * tFenster) / uaSumme
                    : tOut;
            }

            e.ThetaEq = thetaEq;
            e.PhiRadAW = phiRadAW;
            e.PhiRadIW = phiRadIW;
            e.PhiConv = phiConv;
            e.PhiSolar = phiSolar;
            e.ThetaSoll = Sollwertfahrplan(e, wochenende);
            e.ThetaMax = new double[8760];
            for (int h = 0; h < 8760; h++) e.ThetaMax[h] = e.ThetaMaxWert;
            return e;
        }

        /// <summary>
        /// Die Gebäudedaten allein — aufgelöst mit den Vorgaben bei NULL und geprüft, ohne
        /// Klimareihen und ohne Ersatzparameter. Grundlage von <see cref="Bauen"/> und von
        /// <see cref="ErsatzparameterRC.AusKlassenweg"/>.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei einer verletzten Prüfung des Fahrplans,
        /// der Fensterflächen oder eines Modellparameters.</exception>
        internal static GebaeudeModellEingang Daten(ProjektGebaeudeModel g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            var e = new GebaeudeModellEingang
            {
                Bezeichnung = string.IsNullOrEmpty(g.Gebaeudename)
                    ? "Gebäude " + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture)
                    : g.Gebaeudename + " (" + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture) + ")",
                Nutzflaeche_M2 = g.Nutzflaeche,
                Raumhoehe_M = g.Raumhoehe,
                Bauweise_WhK = g.Bauweise,
                U_Aussenwand = g.k_Wert_Außenwand,
                U_Fenster = g.k_Wert_Fenster,
                U_Dach = g.k_Wert_Dachflaeche,
                U_Grund = g.k_Wert_Grundflaeche,
                U_Sonstige = g.k_Wert_Sonstiges,
                A_Aussenwand_M2 = g.Flaeche_Außenwand,
                A_Fenster_M2 = g.gesamte_Fensterflaeche,
                A_Dach_M2 = g.Dachflaeche,
                A_Grund_M2 = g.Grundflaeche,
                A_Sonstige_M2 = g.Sonstige_Flaechen,
                A_FensterSued_M2 = g.Fensterflaeche_Sued,
                A_FensterNord_M2 = g.Fensterflaeche_Nord,
                GWert = g.Fensterdurchlassgrad,
                Rahmenanteil = g.Rahmenanteil ?? GebaeudeFestwerte.VORGABE_RAHMENANTEIL,
                Verschattungsfaktor = g.Verschattungsfaktor ?? GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR,
                Luftwechselrate_h = g.Luftwechselrate,
                InnereGewinne_W = g.Interne_Waermegewinne,
                SollTag = g.Raumsolltemperatur_Tag,
                SollNacht = g.Raumsolltemperatur_Nachtabsenkung,
                SollWochenende = g.Raumsolltemperatur_Wochenende,
                SollFerien = g.Raumsolltemperatur_Ferien,
                ThetaMaxWert = g.Maximaleraumtemperatur,
                MasseanteilAussen = g.Masseanteil_Aussen ?? GebaeudeFestwerte.VORGABE_MASSEANTEIL_AUSSEN,
                Innenflaechenfaktor = g.Innenflaechenfaktor ?? GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR,
                HeizungStrahlungsanteil = g.Heizung_Strahlungsanteil ?? GebaeudeFestwerte.VORGABE_HEIZUNG_STRAHLUNGSANTEIL,
                HeizleistungMaxW = g.Heizleistung_Max.HasValue ? 1000.0 * g.Heizleistung_Max.Value : double.NaN,
                GrundRandbedingung = string.IsNullOrEmpty(g.Grundflaeche_Randbedingung)
                    ? DbWerte.GRUND_ERDREICH : g.Grundflaeche_Randbedingung,
                Kellertemperatur = g.Kellertemperatur ?? GebaeudeFestwerte.VORGABE_KELLERTEMPERATUR,
                AussenbauteileStrahlung = g.Aussenbauteile_Strahlung,
                G2SpaltenGesetzt = g.Luftwechsel_Infiltration.HasValue || g.Luftwechsel_Nutzer.HasValue || g.Sommerlueftung,
            };

            // Ost/West: die NULL-Vorgabe aus dem Bestandsfeld bildet der Vorbereitungsschritt.
            GebaeudeVorbereitung.FensterflaechenOstWest(g, out double ost, out double west);
            e.A_FensterOst_M2 = ost;
            e.A_FensterWest_M2 = west;

            double psiL = g.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand * g.Abmessung_Anschluß_Fenster_Wand
                        + g.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach * g.Abmessung_Anschluß_Wand_Dach
                        + g.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke * g.Abmessung_Anschluß_Außenwand_Kellerdecke;
            e.SummePsiL_WK = psiL;

            e.Pruefen(g);
            e.Ferientage = Ferienfahrplan(g, e.Bezeichnung);
            return e;
        }

        // =====================================================================
        //  Prüfungen außerhalb des Klassenwegs (Konzept 4.8, Rechenschritte 1.1)
        // =====================================================================

        private void Pruefen(ProjektGebaeudeModel g)
        {
            if (!(g.Flaeche_Nutzer > 0.0) || double.IsInfinity(g.Flaeche_Nutzer))
                Fehler(GebaeudeModellFehler.PflichtgroesseFehlt, "Die Fläche je Nutzer ist nicht größer null (" + Text(g.Flaeche_Nutzer) + " m²).");

            // Fensterflächen je Orientierung: nicht negativ, Summe = gesamte Fensterfläche.
            double[] fenster = { A_FensterSued_M2, A_FensterOst_M2, A_FensterWest_M2, A_FensterNord_M2 };
            foreach (double f in fenster)
                if (!Endlich(f) || f < 0.0)
                    Fehler(GebaeudeModellFehler.FensterflaechenWidersprechen, "Eine Fensterfläche je Orientierung ist negativ oder nicht endlich (" + Text(f) + " m²).");
            double summe = A_FensterSued_M2 + A_FensterOst_M2 + A_FensterWest_M2 + A_FensterNord_M2;
            if (!Endlich(A_Fenster_M2) || Math.Abs(summe - A_Fenster_M2) > GebaeudeFestwerte.FENSTERSUMME_TOLERANZ_M2)
                Fehler(GebaeudeModellFehler.FensterflaechenWidersprechen,
                    "Die Fensterflächen je Orientierung ergeben " + Text(summe) + " m², die gesamte Fensterfläche ist " +
                    Text(A_Fenster_M2) + " m².");

            if (!Endlich(Rahmenanteil) || Rahmenanteil < 0.0 || Rahmenanteil >= 1.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Rahmenanteil " + Text(Rahmenanteil) + " liegt nicht in [0, 1).");
            if (!Endlich(Verschattungsfaktor) || Verschattungsfaktor <= 0.0 || Verschattungsfaktor > 1.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Verschattungsfaktor " + Text(Verschattungsfaktor) + " liegt nicht in (0, 1].");
            if (!Endlich(MasseanteilAussen) || MasseanteilAussen <= 0.0 || MasseanteilAussen >= 1.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Masseanteil außen " + Text(MasseanteilAussen) + " liegt nicht in (0, 1).");
            if (!Endlich(Innenflaechenfaktor) || Innenflaechenfaktor <= 0.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Innenflächenfaktor " + Text(Innenflaechenfaktor) + " ist nicht größer null.");
            if (!Endlich(HeizungStrahlungsanteil) || HeizungStrahlungsanteil < 0.0 || HeizungStrahlungsanteil > 1.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Strahlungsanteil der Heizung " + Text(HeizungStrahlungsanteil) + " liegt nicht in [0, 1].");
            if (!double.IsNaN(HeizleistungMaxW) && (!Endlich(HeizleistungMaxW) || HeizleistungMaxW <= 0.0))
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Heizleistungsgrenze " + Text(HeizleistungMaxW) + " W ist gesetzt, aber nicht größer null.");
            if (!Endlich(Kellertemperatur))
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Kellertemperatur ist nicht endlich.");
            if (!Endlich(InnereGewinne_W) || InnereGewinne_W < 0.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die inneren Wärmegewinne " + Text(InnereGewinne_W) + " W sind negativ oder nicht endlich.");
            if (!Endlich(SummePsiL_WK) || SummePsiL_WK < 0.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Wärmebrücken Σψ·L = " + Text(SummePsiL_WK) + " W/K sind negativ oder nicht endlich.");
            if (!string.Equals(GrundRandbedingung, DbWerte.GRUND_ERDREICH, StringComparison.Ordinal)
                && !string.Equals(GrundRandbedingung, DbWerte.GRUND_KELLER, StringComparison.Ordinal)
                && !string.Equals(GrundRandbedingung, DbWerte.GRUND_AUSSENLUFT, StringComparison.Ordinal))
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Randbedingung der Grundfläche '" + GrundRandbedingung + "' ist unbekannt.");

            if (!Endlich(SollTag) || !Endlich(SollNacht) || !Endlich(SollWochenende) || !Endlich(SollFerien) || !Endlich(ThetaMaxWert))
                Fehler(GebaeudeModellFehler.SollwertfahrplanUngueltig, "Ein Sollwert ist nicht endlich.");
            if (!(ThetaMaxWert > SollTag))
                Fehler(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                    "Die obere Raumtemperatur " + Text(ThetaMaxWert) + " °C liegt nicht über dem Tagsollwert " + Text(SollTag) + " °C.");
        }

        /// <summary>
        /// Die Ferientage (E8): Der Fahrplan ist aktiv, wenn <c>Ferien</c> über 0,9 und der
        /// Feriensollwert mindestens 1 °C ist. Ein Zeitraum mit 0 oder 366 an einer Grenze ist
        /// „aus"; ein anderer Tag außerhalb 1 … 365 in einem aktiven Fahrplan wird benannt
        /// abgelehnt. Beginn nach Ende heißt Jahreswechsel.
        /// </summary>
        private static bool[] Ferienfahrplan(ProjektGebaeudeModel g, string bezeichnung)
        {
            var tage = new bool[365];
            bool aktiv = g.Ferien > GebaeudeFestwerte.FERIEN_FLAG_SCHWELLE
                         && g.Raumsolltemperatur_Ferien >= GebaeudeFestwerte.FERIEN_SOLLWERT_MIN;
            if (!aktiv) return tage;

            double[,] zeitraeume =
            {
                { g.Ferienbeginn_1, g.Ferienende_1 }, { g.Ferienbeginn_2, g.Ferienende_2 },
                { g.Ferienbeginn_3, g.Ferienende_3 }, { g.Ferienbeginn_4, g.Ferienende_4 },
            };
            for (int k = 0; k < 4; k++)
            {
                double b = zeitraeume[k, 0], en = zeitraeume[k, 1];
                if (Aus(b) || Aus(en)) continue;
                if (!Tag(b) || !Tag(en))
                    throw new GebaeudeModellException(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                        bezeichnung + ": Der Ferienzeitraum " + (k + 1).ToString(CultureInfo.InvariantCulture) + " (" +
                        Text(b) + " … " + Text(en) + ") hat einen Tag außerhalb 1 … 365.");
                int ib = (int)b - 1, ie = (int)en - 1;
                if (ib <= ie)
                    for (int d = ib; d <= ie; d++) tage[d] = true;
                else
                {
                    for (int d = ib; d < 365; d++) tage[d] = true;
                    for (int d = 0; d <= ie; d++) tage[d] = true;
                }
            }
            return tage;
        }

        private static bool Aus(double tag) => tag == 0.0 || tag == 366.0;

        private static bool Tag(double tag) => Endlich(tag) && tag >= 1.0 && tag <= 365.0 && tag == Math.Floor(tag);

        /// <summary>Der Sollwertfahrplan (E8): Ferien vor Wochenende vor Tag/Nacht.</summary>
        private static double[] Sollwertfahrplan(GebaeudeModellEingang e, bool[] wochenende)
        {
            var soll = new double[8760];
            bool weWirksam = e.SollWochenende > GebaeudeFestwerte.WOCHENENDE_SOLLWERT_SCHWELLE;
            for (int h = 0; h < 8760; h++)
            {
                int tag = h / 24;
                if (e.Ferientage[tag]) soll[h] = e.SollFerien;
                else if (weWirksam && wochenende[tag]) soll[h] = e.SollWochenende;
                else if (Nutzungszeit(h)) soll[h] = e.SollTag;
                else soll[h] = e.SollNacht;
            }
            return soll;
        }

        private void Fehler(GebaeudeModellFehler grund, string text)
        {
            throw new GebaeudeModellException(grund, Bezeichnung + ": " + text);
        }

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);

        private static string Text(double w) => w.ToString("G6", CultureInfo.InvariantCulture);
    }
}
