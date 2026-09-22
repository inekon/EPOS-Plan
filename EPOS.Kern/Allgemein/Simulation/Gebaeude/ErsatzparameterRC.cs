using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die reduzierten RC-Größen EINES Gebäudes für das 2-K-Modell nach VDI 6007 Blatt 1
    /// (Stufe G0, Umsetzungskonzept 1.3; Rechenschritte Kapitel 2 „Ausgabe von Schritt A").
    ///
    /// <para><b>Was der Satz trägt.</b> Die beiden Kapazitäten der Massenknoten
    /// (Außenbauteile, Innenbauteile) in J/K, die Widerstände des Netzes in K/W, die
    /// Bezugsflächen in m² und die Transmissionsleitwerte in W/K, die Schritt E für die
    /// äquivalente Außentemperatur braucht. Jede Größe trägt ihre Einheit im Namen.</para>
    ///
    /// <para><b>Fenster im Außenwandzweig (E14).</b> Der Fensterzweig wird getrennt
    /// übergeben (<see cref="R_1_AF_KW"/>, <see cref="R_Rest_AF_KW"/>) und im Erbauer
    /// <b>nach</b> den Wänden parallel an den gemeinsamen Oberflächenknoten geschaltet.
    /// Der Löser rechnet mit dem zusammengefassten Paar
    /// <see cref="R_1_AWGruppe_KW"/>/<see cref="R_Rest_AWGruppe_KW"/>; die Kapazität der
    /// Wände bleibt dabei unverändert. Die Zusammenfassung ist die Parallelschaltung der
    /// beiden Zweige: R_1 der Gruppe = R_1,AW ∥ R_1,AF, der Gesamtwiderstand der Gruppe =
    /// (R_1,AW + R_Rest,AW) ∥ (R_1,AF + R_Rest,AF), R_Rest der Gruppe als Differenz.
    /// Ergibt die Differenz keinen positiven Wert, bricht der Erbauer mit benanntem Fehler
    /// ab — ein Klemmwert wird nicht gesetzt (Blatt 1, 6.8; Rechenschritte A4/A7a). Ohne
    /// Fenster sind beide Fensterwiderstände <see cref="double.PositiveInfinity"/>.</para>
    ///
    /// <para><b>Harte Prüfungen im Erbauer</b> (Konzept 4.8): Kapazitäten und Widerstände
    /// endlich und größer null, R_Rest,AW &gt; 0 und R_AF &gt; 0 mit eigenem Grund,
    /// Flächen nicht negativ, Innenfläche größer null. <see cref="R_ext_KW"/> darf
    /// <see cref="double.PositiveInfinity"/> sein (kein masseloser Zweig). Die
    /// Plausibilitätsgrenzen der Eingangsdaten (Bauweise je Nutzfläche, U-Werte, g-Wert)
    /// gehören zum Klassenweg <c>AusKlassenweg</c>, der mit Stufe G1 und seinem
    /// Eingangstyp entsteht.</para>
    ///
    /// <para>Ohne Datenbank, ohne Protokoll, ohne statischen Zustand — Vorbild
    /// <see cref="PvErweitertesModell"/>.</para>
    /// </summary>
    internal sealed record ErsatzparameterRC
    {
        /// <summary>Prüft alle Größen und bildet die zusammengefasste Außenwandgruppe.</summary>
        /// <exception cref="GebaeudeModellException">bei jedem verletzten Grenzwert.</exception>
        internal ErsatzparameterRC(
            double c_AW_Jk,
            double c_IW_Jk,
            double r_1_AW_KW,
            double r_Rest_AW_KW,
            double r_1_IW_KW,
            double r_conv_AW_KW,
            double r_conv_IW_KW,
            double r_rad_KW,
            double r_ext_KW,
            double a_AW_opak_M2,
            double a_IW_M2,
            double summeUA_opak_WK,
            double r_1_AF_KW = double.PositiveInfinity,
            double r_Rest_AF_KW = double.PositiveInfinity,
            double a_Fenster_M2 = 0.0,
            double uA_Fenster_WK = 0.0)
        {
            Kapazitaet(c_AW_Jk, nameof(C_AW_Jk));
            Kapazitaet(c_IW_Jk, nameof(C_IW_Jk));

            Widerstand(r_1_AW_KW, nameof(R_1_AW_KW));
            if (double.IsNaN(r_Rest_AW_KW) || double.IsInfinity(r_Rest_AW_KW) || r_Rest_AW_KW <= 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.RRestAwNichtPositiv,
                    "Der Restwiderstand der Außenwände R_Rest,AW = " + Text(r_Rest_AW_KW) +
                    " K/W ist nicht positiv. Die Außenbauteile leiten mehr, als das Netz " +
                    "abbilden kann; ein Klemmwert wird nicht gesetzt (VDI 6007 Blatt 1, 6.8).");
            Widerstand(r_1_IW_KW, nameof(R_1_IW_KW));
            Widerstand(r_conv_AW_KW, nameof(R_conv_AW_KW));
            Widerstand(r_conv_IW_KW, nameof(R_conv_IW_KW));
            Widerstand(r_rad_KW, nameof(R_rad_KW));
            if (double.IsNaN(r_ext_KW) || r_ext_KW <= 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.WiderstandUngueltig,
                    "R_ext_KW = " + Text(r_ext_KW) + " K/W ist nicht größer null " +
                    "(unendlich heißt: kein Lüftungs- und Wärmebrückenzweig).");

            bool fensterAus = double.IsPositiveInfinity(r_1_AF_KW) && double.IsPositiveInfinity(r_Rest_AF_KW);
            if (!fensterAus)
            {
                if (!IstPositivEndlich(r_1_AF_KW) || !IstPositivEndlich(r_Rest_AF_KW))
                    throw new GebaeudeModellException(GebaeudeModellFehler.FensterzweigUngueltig,
                        "Der Fensterzweig ist ungültig: R_1,AF = " + Text(r_1_AF_KW) +
                        " K/W, R_Rest,AF = " + Text(r_Rest_AF_KW) + " K/W. Beide müssen " +
                        "endlich und größer null sein, oder beide unendlich (keine Fenster).");
            }

            Flaeche(a_AW_opak_M2, nameof(A_AW_opak_M2));
            Flaeche(a_Fenster_M2, nameof(A_Fenster_M2));
            Flaeche(a_IW_M2, nameof(A_IW_M2));
            if (a_IW_M2 <= 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.FlaecheUngueltig,
                    "Die Innenbauteilfläche A_IW = " + Text(a_IW_M2) + " m² muss größer null sein.");
            if (a_AW_opak_M2 + a_Fenster_M2 <= 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.FlaecheUngueltig,
                    "Die Außenbauteilgruppe hat keine Fläche (opak + Fenster = 0 m²).");

            Leitwert(summeUA_opak_WK, nameof(SummeUA_opak_WK));
            Leitwert(uA_Fenster_WK, nameof(UA_Fenster_WK));

            C_AW_Jk = c_AW_Jk;
            C_IW_Jk = c_IW_Jk;
            R_1_AW_KW = r_1_AW_KW;
            R_Rest_AW_KW = r_Rest_AW_KW;
            R_1_IW_KW = r_1_IW_KW;
            R_conv_AW_KW = r_conv_AW_KW;
            R_conv_IW_KW = r_conv_IW_KW;
            R_rad_KW = r_rad_KW;
            R_ext_KW = r_ext_KW;
            R_1_AF_KW = r_1_AF_KW;
            R_Rest_AF_KW = r_Rest_AF_KW;
            A_AW_opak_M2 = a_AW_opak_M2;
            A_Fenster_M2 = a_Fenster_M2;
            A_IW_M2 = a_IW_M2;
            SummeUA_opak_WK = summeUA_opak_WK;
            UA_Fenster_WK = uA_Fenster_WK;

            if (fensterAus)
            {
                R_1_AWGruppe_KW = r_1_AW_KW;
                R_Rest_AWGruppe_KW = r_Rest_AW_KW;
            }
            else
            {
                // Parallelschaltung NACH den Wänden: erst der innere Teil, dann der
                // Gesamtweg; der Rest ist die Differenz. Die Wandkapazität bleibt.
                double r1 = 1.0 / (1.0 / r_1_AW_KW + 1.0 / r_1_AF_KW);
                double rGes = 1.0 / (1.0 / (r_1_AW_KW + r_Rest_AW_KW) + 1.0 / (r_1_AF_KW + r_Rest_AF_KW));
                double rRest = rGes - r1;
                if (!(rRest > 0.0))
                    throw new GebaeudeModellException(GebaeudeModellFehler.RRestAwNichtPositiv,
                        "Nach dem Anschluss des Fensterzweigs bleibt für die Außenbauteilgruppe " +
                        "kein positiver Restwiderstand (R_ges = " + Text(rGes) + " K/W, R_1 = " +
                        Text(r1) + " K/W). Ein Klemmwert wird nicht gesetzt.");
                R_1_AWGruppe_KW = r1;
                R_Rest_AWGruppe_KW = rRest;
            }
        }

        /// <summary>Kapazität des Außenbauteil-Massenknotens [J/K].</summary>
        internal double C_AW_Jk { get; }

        /// <summary>Kapazität des Innenbauteil-Massenknotens [J/K].</summary>
        internal double C_IW_Jk { get; }

        /// <summary>Innerer Widerstand der opaken Außenbauteile, Masse ↔ Oberfläche [K/W].</summary>
        internal double R_1_AW_KW { get; }

        /// <summary>Restwiderstand der opaken Außenbauteile, äquivalente Außentemperatur ↔ Masse, einschließlich äußerem Übergang [K/W].</summary>
        internal double R_Rest_AW_KW { get; }

        /// <summary>Innerer Widerstand des Fensterzweigs [K/W]; unendlich ohne Fenster.</summary>
        internal double R_1_AF_KW { get; }

        /// <summary>Restwiderstand des Fensterzweigs [K/W]; unendlich ohne Fenster.</summary>
        internal double R_Rest_AF_KW { get; }

        /// <summary>R_1 der zusammengefassten Außenbauteilgruppe (Wände und Fenster) [K/W] — damit rechnet der Löser.</summary>
        internal double R_1_AWGruppe_KW { get; }

        /// <summary>R_Rest der zusammengefassten Außenbauteilgruppe (Wände und Fenster) [K/W] — damit rechnet der Löser.</summary>
        internal double R_Rest_AWGruppe_KW { get; }

        /// <summary>Widerstand der Innenbauteile, Masse ↔ Oberfläche [K/W].</summary>
        internal double R_1_IW_KW { get; }

        /// <summary>Konvektiver Übergang Außenbauteiloberfläche ↔ Raumluft [K/W].</summary>
        internal double R_conv_AW_KW { get; }

        /// <summary>Konvektiver Übergang Innenbauteiloberfläche ↔ Raumluft [K/W].</summary>
        internal double R_conv_IW_KW { get; }

        /// <summary>Strahlungsaustausch zwischen den beiden Oberflächenknoten [K/W].</summary>
        internal double R_rad_KW { get; }

        /// <summary>Masseloser Zweig Außenluft ↔ Raumluft: Lüftung und Wärmebrücken [K/W]; unendlich = kein Zweig.</summary>
        internal double R_ext_KW { get; }

        /// <summary>Fläche der opaken Außenbauteile [m²].</summary>
        internal double A_AW_opak_M2 { get; }

        /// <summary>Fensterfläche der Außenbauteilgruppe [m²].</summary>
        internal double A_Fenster_M2 { get; }

        /// <summary>Oberfläche der Außenbauteilgruppe einschließlich Fenster [m²] (E14).</summary>
        internal double A_AW_gesamt_M2 => A_AW_opak_M2 + A_Fenster_M2;

        /// <summary>Fläche der Innenbauteile [m²].</summary>
        internal double A_IW_M2 { get; }

        /// <summary>Σ U·A der opaken Außenbauteile [W/K] — Gewicht der äquivalenten Außentemperatur.</summary>
        internal double SummeUA_opak_WK { get; }

        /// <summary>U·A der Fenster [W/K] — Gewicht der äquivalenten Außentemperatur.</summary>
        internal double UA_Fenster_WK { get; }

        private static bool IstPositivEndlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w) && w > 0.0;

        private static void Kapazitaet(double w, string name)
        {
            if (!IstPositivEndlich(w))
                throw new GebaeudeModellException(GebaeudeModellFehler.KapazitaetUngueltig,
                    name + " = " + Text(w) + " J/K muss endlich und größer null sein.");
        }

        private static void Widerstand(double w, string name)
        {
            if (!IstPositivEndlich(w))
                throw new GebaeudeModellException(GebaeudeModellFehler.WiderstandUngueltig,
                    name + " = " + Text(w) + " K/W muss endlich und größer null sein.");
        }

        private static void Flaeche(double w, string name)
        {
            if (double.IsNaN(w) || double.IsInfinity(w) || w < 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.FlaecheUngueltig,
                    name + " = " + Text(w) + " m² muss endlich und nicht negativ sein.");
        }

        private static void Leitwert(double w, string name)
        {
            if (double.IsNaN(w) || double.IsInfinity(w) || w < 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.LeitwertUngueltig,
                    name + " = " + Text(w) + " W/K muss endlich und nicht negativ sein.");
        }

        private static string Text(double w) => w.ToString("G6", CultureInfo.InvariantCulture);
    }
}
