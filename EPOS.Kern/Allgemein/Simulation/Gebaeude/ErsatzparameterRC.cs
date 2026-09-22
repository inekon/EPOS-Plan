using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welcher Fall der Zusammenfassung von Wänden und Fenstern zur Außenbauteilgruppe
    /// gegriffen hat (VDI 6007 Blatt 1, Gl. (27)–(28c); Rechenschritte A7a, B6).
    /// </summary>
    internal enum AussenbauteilgruppeFall
    {
        /// <summary>
        /// Der Restwiderstand ist die Differenz aus Gesamtwiderstand, innerem Widerstand
        /// und innerem Übergang, Gl. (28); er ist positiv.
        /// </summary>
        Regelfall,

        /// <summary>
        /// Der Gesamtwiderstand der Gruppe liegt unter dem äußeren Übergangswiderstand:
        /// Der Restwiderstand wird auf den äußeren Übergang gesetzt, Gl. (28a), und der innere
        /// Widerstand aus der Bilanz neu bestimmt, Gl. (28b).
        /// </summary>
        Grenzfall28a,
    }

    /// <summary>
    /// Die reduzierten RC-Größen EINES Gebäudes für das 2-K-Modell nach VDI 6007 Blatt 1
    /// (Stufe G0, Umsetzungskonzept 1.3; Rechenschritte Kapitel 2 „Ausgabe von Schritt A").
    ///
    /// <para><b>Was der Satz trägt.</b> Die beiden Kapazitäten der Massenknoten
    /// (Außenbauteile, Innenbauteile) in J/K, die Widerstände des Netzes in K/W, die
    /// Bezugsflächen in m² und die Transmissionsleitwerte in W/K, die Schritt E für die
    /// äquivalente Außentemperatur braucht. Jede Größe trägt ihre Einheit im Namen.</para>
    ///
    /// <para><b>Die Außenbauteilgruppe (E14, Gl. (27)–(28c)).</b> Wände und Fenster werden
    /// getrennt übergeben — die Wände als R_1,AW/R_Rest,AW, der Fensterzweig als
    /// <see cref="R_1_AF_KW"/>/<see cref="R_Rest_AF_KW"/> — und im Erbauer zu EINEM Paar
    /// <see cref="R_1_AWGruppe_KW"/>/<see cref="R_Rest_AWGruppe_KW"/> zusammengefasst; damit
    /// rechnet der Löser. Die Kapazität der Wände bleibt dabei unverändert. Der Weg:</para>
    /// <list type="number">
    /// <item>R_1 der Gruppe: der innere Widerstand der Fenster wird <b>nach</b> den Wänden
    /// parallel geschaltet.</item>
    /// <item>Innerer Übergang der Gruppe R_α,i: konvektiver Übergang der Außenbauteile
    /// parallel zum Strahlungsaustausch, wie ihn Gl. (28) abzieht.</item>
    /// <item>Gesamtwiderstand der Gruppe nach Gl. (27) als Summe der Leitwerte der Zweige.
    /// Die Zweige liegen hier in Netzform vor (innerer Widerstand plus Rest, ohne inneren
    /// Übergang); jeder Zweig bekommt deshalb seinen Flächenanteil am inneren Übergang
    /// zurück, bevor die Leitwerte addiert werden. Ohne Fenster ist das genau
    /// R_1,AW + R_Rest,AW + R_α,i.</item>
    /// <item>Regelfall, Gl. (28): Rest = Gesamtwiderstand − R_1 − R_α,i. Ohne Fenster ist
    /// das der übergebene R_Rest,AW selbst.</item>
    /// <item>Grenzfall (28a)/(28b): liegt der Gesamtwiderstand unter dem äußeren
    /// Übergangswiderstand <see cref="R_alphaAussen_KW"/>, wird der Rest gleich diesem
    /// gesetzt und R_1 aus der Bilanz neu bestimmt.</item>
    /// <item>Untergrenze (28c): fällt R_1 der Gruppe unter
    /// <see cref="R_1_NUMERISCH_NULL_KW"/>, gilt dieser Setzwert.</item>
    /// </list>
    /// <para><b>Die Bedingung von (28a) gilt wörtlich:</b> verglichen wird der
    /// Gesamtwiderstand, nicht der Rest, mit dem äußeren Übergang. Weil das U·A jedes
    /// Bauteils seinen äußeren Übergang enthält, ist bei physikalisch stimmigen Eingaben
    /// stets U·A &lt; α_A·A, also R_ges &gt; R_α,A — (28a)–(28c) sind eine Schutzregel für
    /// widersprüchliche Eingaben, und (28c) greift dann bestimmungsgemäß mit, weil (28b) in
    /// diesem Fall stets negativ ist. Ein Regelfall mit nicht positivem Rest, für den (28a)
    /// nicht greift, ist ebenso eine widersprüchliche Eingabe und bleibt benannter Fehler.</para>
    /// <para><b>Der Fensterzweig trägt seinen äußeren Übergang im Rest:</b> R_Rest,AF ist so
    /// zu bilden, dass R_1,AF + R_Rest,AF + Flächenanteil an R_α,i = 1/(U·A) des Fensters
    /// ist; so geht das Fenster mit seinem vollen U·A in Gl. (27) ein (Rechenschritte A7a).</para>
    /// <para>Welcher Fall gegriffen hat, steht in <see cref="Gruppenfall"/> und
    /// <see cref="R_1_Untergrenze28c"/> — der Setzwert der Richtlinie ist kein stiller
    /// Rückfall, der Aufrufer kann ihn ausweisen. Ein benannter Fehler bleibt, wo auch die
    /// Richtlinie keinen gültigen Fall hat: Regelfall mit nicht positivem Rest, ein nicht
    /// positiver Gesamtwiderstand eines Zweigs oder der Gruppe, und ein nicht positiver Rest
    /// ohne angegebenen äußeren Übergang (dann ist (28a) nicht prüfbar). Ohne Fenster sind
    /// beide Fensterwiderstände <see cref="double.PositiveInfinity"/>.</para>
    ///
    /// <para><b>Harte Prüfungen im Erbauer</b> (Konzept 4.8): Kapazitäten und Widerstände
    /// endlich und größer null, R_Rest,AW endlich, Fensterzweig vollständig und positiv mit
    /// eigener Fläche, Flächen nicht negativ, opake Außen- und Innenfläche größer null.
    /// <see cref="R_ext_KW"/> darf <see cref="double.PositiveInfinity"/> sein (kein
    /// masseloser Zweig). Die Plausibilitätsgrenzen der Eingangsdaten (Bauweise je
    /// Nutzfläche, U-Werte, g-Wert) gehören zum Klassenweg <c>AusKlassenweg</c>, der mit
    /// Stufe G1 und seinem Eingangstyp entsteht.</para>
    ///
    /// <para>Ohne Datenbank, ohne Protokoll, ohne statischen Zustand — Vorbild
    /// <see cref="PvErweitertesModell"/>.</para>
    /// </summary>
    internal sealed record ErsatzparameterRC
    {
        /// <summary>
        /// Setzwert für den inneren Widerstand der Außenbauteilgruppe, wenn er rechnerisch
        /// verschwindet oder negativ wird (VDI 6007-1 Gl. (28c)) [K/W]. Er steht für
        /// „numerisch null": Der Massenknoten der Außenbauteile liegt dann praktisch am
        /// Oberflächenknoten, ohne dass der Leitwert 1/R_1 unendlich wird.
        /// </summary>
        internal const double R_1_NUMERISCH_NULL_KW = 1e-10;

        /// <summary>Prüft alle Größen und bildet die zusammengefasste Außenwandgruppe.</summary>
        /// <param name="r_alphaAussen_KW">Gesamtwärmeübergangswiderstand an den Außenseiten der
        /// Gruppe R_α,ges,AW,A [K/W], Bedingung und Setzwert von Gl. (28a).
        /// <see cref="double.NaN"/> = nicht angegeben: (28a) wird nicht geprüft.</param>
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
            double uA_Fenster_WK = 0.0,
            double r_alphaAussen_KW = double.NaN)
        {
            Kapazitaet(c_AW_Jk, nameof(C_AW_Jk));
            Kapazitaet(c_IW_Jk, nameof(C_IW_Jk));

            Widerstand(r_1_AW_KW, nameof(R_1_AW_KW));
            if (double.IsNaN(r_Rest_AW_KW) || double.IsInfinity(r_Rest_AW_KW))
                throw new GebaeudeModellException(GebaeudeModellFehler.RRestAwNichtPositiv,
                    "Der Restwiderstand der Außenwände R_Rest,AW = " + Text(r_Rest_AW_KW) +
                    " K/W ist nicht endlich.");
            Widerstand(r_1_IW_KW, nameof(R_1_IW_KW));
            Widerstand(r_conv_AW_KW, nameof(R_conv_AW_KW));
            Widerstand(r_conv_IW_KW, nameof(R_conv_IW_KW));
            Widerstand(r_rad_KW, nameof(R_rad_KW));
            if (double.IsNaN(r_ext_KW) || r_ext_KW <= 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.WiderstandUngueltig,
                    "R_ext_KW = " + Text(r_ext_KW) + " K/W ist nicht größer null " +
                    "(unendlich heißt: kein Lüftungs- und Wärmebrückenzweig).");
            if (!double.IsNaN(r_alphaAussen_KW) && !IstPositivEndlich(r_alphaAussen_KW))
                throw new GebaeudeModellException(GebaeudeModellFehler.WiderstandUngueltig,
                    nameof(R_alphaAussen_KW) + " = " + Text(r_alphaAussen_KW) + " K/W muss endlich " +
                    "und größer null sein (oder nicht angegeben).");

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
            if (a_AW_opak_M2 <= 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.FlaecheUngueltig,
                    "Die opake Außenbauteilfläche A_AW,opak = " + Text(a_AW_opak_M2) + " m² muss " +
                    "größer null sein; der Wandzweig trägt die Kapazität der Gruppe.");
            if (!fensterAus && a_Fenster_M2 <= 0.0)
                throw new GebaeudeModellException(GebaeudeModellFehler.FensterzweigUngueltig,
                    "Der Fensterzweig ist gesetzt, die Fensterfläche aber null; ohne Fläche hat er " +
                    "keinen Anteil am inneren Übergang (Gl. (27)).");

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
            R_alphaAussen_KW = r_alphaAussen_KW;
            A_AW_opak_M2 = a_AW_opak_M2;
            A_Fenster_M2 = a_Fenster_M2;
            A_IW_M2 = a_IW_M2;
            SummeUA_opak_WK = summeUA_opak_WK;
            UA_Fenster_WK = uA_Fenster_WK;

            // Innerer Übergang der Gruppe, wie ihn Gl. (28) abzieht: Konvektion ∥ Strahlung.
            double rAlphaI = 1.0 / (1.0 / r_conv_AW_KW + 1.0 / r_rad_KW);
            R_alphaInnen_KW = rAlphaI;

            // Gl. (27) in Netzform: je Zweig innerer Widerstand + Rest + Flächenanteil am
            // inneren Übergang, die Leitwerte der Zweige addiert. Ohne Fenster bleibt der
            // Wandzweig allein und bekommt den ganzen inneren Übergang.
            double r1, rGes;
            if (fensterAus)
            {
                r1 = r_1_AW_KW;
                rGes = r_1_AW_KW + r_Rest_AW_KW + rAlphaI;
            }
            else
            {
                double aGruppe = a_AW_opak_M2 + a_Fenster_M2;
                double rGesWand = r_1_AW_KW + r_Rest_AW_KW + rAlphaI * aGruppe / a_AW_opak_M2;
                double rGesFenster = r_1_AF_KW + r_Rest_AF_KW + rAlphaI * aGruppe / a_Fenster_M2;
                if (!(rGesWand > 0.0))
                    throw new GebaeudeModellException(GebaeudeModellFehler.RRestAwNichtPositiv,
                        "Der Gesamtwiderstand des Wandzweigs ist nicht positiv (" + Text(rGesWand) +
                        " K/W); Gl. (27) ist so nicht auswertbar.");
                // Fenster nach den Wänden parallel; die Wandkapazität bleibt.
                r1 = 1.0 / (1.0 / r_1_AW_KW + 1.0 / r_1_AF_KW);
                rGes = 1.0 / (1.0 / rGesWand + 1.0 / rGesFenster);
            }
            if (!(rGes > 0.0))
                throw new GebaeudeModellException(GebaeudeModellFehler.RRestAwNichtPositiv,
                    "Der Gesamtwiderstand der Außenbauteilgruppe R_ges = " + Text(rGes) +
                    " K/W ist nicht positiv; dafür kennt die Richtlinie keinen Fall.");
            R_ges_AWGruppe_KW = rGes;

            double rRest;
            if (rGes < r_alphaAussen_KW)
            {
                // (28a): der Rest ist der äußere Übergang; (28b): R_1 aus der Bilanz.
                Gruppenfall = AussenbauteilgruppeFall.Grenzfall28a;
                rRest = r_alphaAussen_KW;
                r1 = rGes - rRest - rAlphaI;
            }
            else
            {
                // (28): ohne Fenster ist das der übergebene Rest selbst (bitgleich).
                Gruppenfall = AussenbauteilgruppeFall.Regelfall;
                rRest = fensterAus ? r_Rest_AW_KW : rGes - r1 - rAlphaI;
                if (!(rRest > 0.0))
                    throw new GebaeudeModellException(GebaeudeModellFehler.RRestAwNichtPositiv,
                        "Der Restwiderstand der Außenbauteilgruppe R_Rest = " + Text(rRest) +
                        " K/W ist nicht positiv (R_ges = " + Text(rGes) + " K/W, R_1 = " + Text(r1) +
                        " K/W, R_α,i = " + Text(rAlphaI) + " K/W). " +
                        (double.IsNaN(r_alphaAussen_KW)
                            ? "Ohne äußeren Übergangswiderstand ist Gl. (28a) nicht prüfbar."
                            : "Gl. (28a) greift nicht (R_ges ≥ R_α,A = " + Text(r_alphaAussen_KW) +
                              " K/W); dafür kennt die Richtlinie keinen Setzwert."));
            }

            // (28c): Untergrenze des inneren Widerstands.
            if (r1 < R_1_NUMERISCH_NULL_KW)
            {
                r1 = R_1_NUMERISCH_NULL_KW;
                R_1_Untergrenze28c = true;
            }

            R_1_AWGruppe_KW = r1;
            R_Rest_AWGruppe_KW = rRest;
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

        /// <summary>Restwiderstand des Fensterzweigs, äquivalente Außentemperatur ↔ innerer Teil, einschließlich äußerem Übergang [K/W]: 1/(U·A)_w − R_1,AF − Flächenanteil an R_α,i; unendlich ohne Fenster.</summary>
        internal double R_Rest_AF_KW { get; }

        /// <summary>Gesamtwärmeübergangswiderstand an den Außenseiten der Gruppe R_α,ges,AW,A [K/W]; <see cref="double.NaN"/> = nicht angegeben.</summary>
        internal double R_alphaAussen_KW { get; }

        /// <summary>Innerer Übergang der Gruppe R_α,i = R_conv,AW ∥ R_rad [K/W], wie ihn Gl. (28) abzieht.</summary>
        internal double R_alphaInnen_KW { get; }

        /// <summary>Gesamtwiderstand der Außenbauteilgruppe nach Gl. (27) in Netzform [K/W].</summary>
        internal double R_ges_AWGruppe_KW { get; }

        /// <summary>Welcher Fall der Zusammenfassung gegriffen hat (Gl. (28) oder (28a)/(28b)).</summary>
        internal AussenbauteilgruppeFall Gruppenfall { get; }

        /// <summary>Wahr, wenn R_1 der Gruppe auf den Setzwert <see cref="R_1_NUMERISCH_NULL_KW"/> gehoben wurde (Gl. (28c)).</summary>
        internal bool R_1_Untergrenze28c { get; }

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
