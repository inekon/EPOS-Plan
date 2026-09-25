using System;
using System.Collections.Generic;
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

        /// <summary>Die Herleitung je Bauteil — nur im Bauteilweg; im Klassenweg leer.</summary>
        internal IReadOnlyList<BauteilHerleitung> Bauteilherleitung { get; private init; } = Array.Empty<BauteilHerleitung>();

        /// <summary>
        /// Der in Gl. (27) wirksame U-Wert je Bauteil [W/(m²K)], in der Reihenfolge des
        /// übergebenen Bauteilsatzes (Bauteilweg; leer im Klassenweg): eingetragen, sonst aus den
        /// Schichten; NaN für ein Innenbauteil ohne Angabe. Aus derselben Rechnung wie
        /// <see cref="SummeUA_opak_WK"/> und <see cref="UA_Fenster_WK"/> — der Eingangsbauer
        /// gewichtet damit die äquivalente Außentemperatur je Bauteil (Gl. (41)), ohne den
        /// U-Wert ein zweites Mal zu rechnen.
        /// </summary>
        internal IReadOnlyList<double> UWirksamJeBauteil_WM2K { get; private init; } = Array.Empty<double>();

        /// <summary>Der Weg der Außenbauteilgruppe (Klassenweg oder Bauteilweg, Mehrzonenkonzept 3.6).</summary>
        internal Gruppenweg WegAussen { get; private init; }

        /// <summary>Der Weg der Innenbauteilgruppe (Klassenweg oder Bauteilweg, Mehrzonenkonzept 3.6).</summary>
        internal Gruppenweg WegInnen { get; private init; }

        // =====================================================================
        //  Fabrikweg 1: der Klassenweg (Stufe G1, Rechenschritte Schritt A)
        // =====================================================================

        /// <summary>
        /// <b>Der Klassenweg</b> (Rechenschritte A1–A7a, EPOS-Klassenweg nach Konzept 4.3):
        /// die RC-Größen aus dem, was die Gebäudezeile führt — U-Wert-/Flächenpaare, Bauweise,
        /// Nutzung. Er läuft einmal je Gebäude und Lauf.
        ///
        /// <list type="bullet">
        /// <item>A1: C_ges = Bauweise · 3 600, aufgeteilt nach a_AW.</item>
        /// <item>A2: A_AW,opak = Wand + Dach + Grund + Sonstiges; A_AW,ges mit Fenstern;
        /// A_IW = f_IW · A_f; A_rad = min(A_AW,ges, A_IW).</item>
        /// <item>A3: U·A je Gruppe ungewichtet (E2); Σψ·L.</item>
        /// <item>A4: R_1,AW = 1/(h_ms·A_AW,opak), R_Rest,AW = 1/Σ(U·A)_opak − R_1,AW − R_si/A_AW,opak.</item>
        /// <item>A5, A6: R_1,IW, R_conv,AW (mit Fenstern), R_conv,IW, R_rad.</item>
        /// <item>A7: R_ext = 1/(n·A_f·H·c·ρ + Σψ·L); unendlich ohne Zweig.</item>
        /// <item>A7a: Fensterzweig R_AF = (1/U_w − R_si − 1/α_A)/A_w, R_1,AF = R_AF/6,
        /// R_Rest,AF = 1/(U_w·A_w) − R_1,AF − R_α,i·A_AW,ges/A_w; die Zusammenfassung nach
        /// (27)–(28c) leistet der Erbauer.</item>
        /// </list>
        ///
        /// <para><b>Plausibilitätsgrenzen</b> (Konzept 4.8) mit benanntem Fehler: A_f, H und n
        /// größer null; 5 ≤ Bauweise/A_f ≤ 200 Wh/(m²K); U-Werte 0,1 … 6 W/(m²K) für jede
        /// Gruppe mit Fläche; 0 &lt; g ≤ 1, wenn Fenster da sind; Flächen nicht negativ.
        /// R_AF ≤ 0 bricht ab — (26) hat dafür keinen Wert.</para>
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jeder verletzten Grenze.</exception>
        internal static ErsatzparameterRC AusKlassenweg(GebaeudeModellEingang e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            string wer = e.Bezeichnung + ": ";

            double af = e.Nutzflaeche_M2;
            if (!IstPositivEndlich(af))
                throw new GebaeudeModellException(GebaeudeModellFehler.PflichtgroesseFehlt, wer + "Die Nutzfläche A_f = " + Text(af) + " m² ist nicht größer null.");
            if (!IstPositivEndlich(e.Raumhoehe_M))
                throw new GebaeudeModellException(GebaeudeModellFehler.PflichtgroesseFehlt, wer + "Die Raumhöhe H = " + Text(e.Raumhoehe_M) + " m ist nicht größer null.");
            if (!IstPositivEndlich(e.Luftwechselrate_h))
                throw new GebaeudeModellException(GebaeudeModellFehler.PflichtgroesseFehlt, wer + "Die Luftwechselrate n = " + Text(e.Luftwechselrate_h) + " 1/h ist nicht größer null.");

            double bauweiseJeM2 = e.Bauweise_WhK / af;
            if (!(bauweiseJeM2 >= GebaeudeFestwerte.BAUWEISE_JE_M2_MIN && bauweiseJeM2 <= GebaeudeFestwerte.BAUWEISE_JE_M2_MAX))
                throw new GebaeudeModellException(GebaeudeModellFehler.BauweiseUnplausibel,
                    wer + "Die Speichermasse je Nutzfläche " + Text(bauweiseJeM2) + " Wh/(m²K) (Bauweise " + Text(e.Bauweise_WhK) +
                    " Wh/K, Nutzfläche " + Text(af) + " m²) liegt nicht in " + Text(GebaeudeFestwerte.BAUWEISE_JE_M2_MIN) +
                    " … " + Text(GebaeudeFestwerte.BAUWEISE_JE_M2_MAX) + " Wh/(m²K).");

            double aWand = e.A_Aussenwand_M2, aDach = e.A_Dach_M2, aGrund = e.A_Grund_M2, aSonst = e.A_Sonstige_M2, aFenster = e.A_Fenster_M2;
            foreach (double a in new[] { aWand, aDach, aGrund, aSonst, aFenster })
                if (double.IsNaN(a) || double.IsInfinity(a) || a < 0.0)
                    throw new GebaeudeModellException(GebaeudeModellFehler.FlaecheUngueltig, wer + "Eine Bauteilfläche ist negativ oder nicht endlich (" + Text(a) + " m²).");

            UWert(wer, "Außenwand", e.U_Aussenwand, aWand);
            UWert(wer, "Fenster", e.U_Fenster, aFenster);
            UWert(wer, "Dach", e.U_Dach, aDach);
            UWert(wer, "Grundfläche", e.U_Grund, aGrund);
            UWert(wer, "Sonstiges", e.U_Sonstige, aSonst);
            if (aFenster > 0.0 && !(e.GWert > 0.0 && e.GWert <= 1.0))
                throw new GebaeudeModellException(GebaeudeModellFehler.GWertUnplausibel, wer + "Der Gesamtenergiedurchlassgrad g = " + Text(e.GWert) + " liegt nicht in (0, 1].");

            // A1 — Speichermasse
            double cGes = e.Bauweise_WhK * GebaeudeFestwerte.SEKUNDEN_JE_STUNDE;
            double cAw = e.MasseanteilAussen * cGes;
            double cIw = (1.0 - e.MasseanteilAussen) * cGes;

            // A2 — Bezugsflächen
            double aOpak = aWand + aDach + aGrund + aSonst;
            if (!(aOpak > 0.0))
                throw new GebaeudeModellException(GebaeudeModellFehler.FlaecheUngueltig, wer + "Die opake Außenbauteilfläche ist null; der Klassenweg braucht Wand, Dach, Grund oder Sonstiges.");
            double aGes = aOpak + aFenster;
            double aIw = e.Innenflaechenfaktor * af;
            double aRad = Math.Min(aGes, aIw);

            // A3 — Transmissionsleitwerte (ungewichtet, E2)
            double uaOpak = e.U_Aussenwand * aWand + e.U_Dach * aDach + e.U_Grund * aGrund + e.U_Sonstige * aSonst;
            double uaFenster = aFenster > 0.0 ? e.U_Fenster * aFenster : 0.0;

            // A4 — Außenwandpfad (mit R_si-Abzug)
            double r1Aw = 1.0 / (GebaeudeFestwerte.H_MS * aOpak);
            double rRestAw = 1.0 / uaOpak - r1Aw - GebaeudeFestwerte.R_SI / aOpak;

            // A5, A6
            double r1Iw = 1.0 / (GebaeudeFestwerte.H_MS * aIw);
            double rConvAw = 1.0 / (GebaeudeFestwerte.ALPHA_KON_INNEN * aGes);
            double rConvIw = 1.0 / (GebaeudeFestwerte.ALPHA_KON_INNEN * aIw);
            double rRad = 1.0 / (GebaeudeFestwerte.ALPHA_STR_INNEN * aRad);

            // A7 — Lüftung und Wärmebrücken
            double hVe = e.Luftwechselrate_h * af * e.Raumhoehe_M * GebaeudeFestwerte.C_RHO_LUFT;
            double hExt = hVe + e.SummePsiL_WK;
            double rExt = hExt > 0.0 ? 1.0 / hExt : double.PositiveInfinity;

            // A7a — Fensterzweig
            double r1Af = double.PositiveInfinity, rRestAf = double.PositiveInfinity;
            double alphaA = GebaeudeFestwerte.ALPHA_AUSSEN;
            if (aFenster > 0.0)
            {
                double rAf = (1.0 / e.U_Fenster - GebaeudeFestwerte.R_SI - 1.0 / alphaA) / aFenster;
                if (!(rAf > 0.0))
                    throw new GebaeudeModellException(GebaeudeModellFehler.FensterzweigUngueltig,
                        wer + "Der Fensterwiderstand R_AF = " + Text(rAf) + " K/W nach Gl. (26) ist nicht positiv (U_w = " +
                        Text(e.U_Fenster) + " W/(m²K)); dafür setzt EPOS keinen Wert.");
                r1Af = rAf / 6.0;
                double rAlphaI = 1.0 / (1.0 / rConvAw + 1.0 / rRad);
                rRestAf = 1.0 / uaFenster - r1Af - rAlphaI * aGes / aFenster;
            }
            double rAlphaAussen = 1.0 / (alphaA * aGes);

            try
            {
                return new ErsatzparameterRC(cAw, cIw, r1Aw, rRestAw, r1Iw, rConvAw, rConvIw, rRad, rExt,
                                             aOpak, aIw, uaOpak, r1Af, rRestAf, aFenster, uaFenster, rAlphaAussen);
            }
            catch (GebaeudeModellException ex)
            {
                throw new GebaeudeModellException(ex.Grund, wer + ex.Message);
            }
        }

        // =====================================================================
        //  Fabrikweg 2: der Bauteilweg (Stufe G3, Rechenschritte Schritt B)
        // =====================================================================

        /// <summary>
        /// <b>Der Bauteilweg</b> für ein Gebäude (Stufe G3; Rechenschritte Kapitel 3,
        /// Mehrzonenkonzept 2.2, 3.1–3.6): die RC-Größen aus einem Bauteilsatz statt aus den
        /// U-Wert-/Flächenpaaren der Gebäudezeile. Die Lüftung kommt aus dem Eingang
        /// (H_ve = n·A_f·H·c·ρ, Rechenschritte A7), Bauweise, Masseanteil und Innenflächenfaktor
        /// für eine Gruppe, die mangels Schichten den Klassenweg rechnet. Der Gebäudewert Σψ·L gilt
        /// im Bauteilweg <b>nicht</b> — die Wärmebrücken tragen die Bauteile.
        /// Regeln: <see cref="AusBauteilweg(BauteilwegGebaeude, IReadOnlyList{BauteilEingang})"/>.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jeder verletzten Prüfung.</exception>
        internal static ErsatzparameterRC AusBauteilweg(GebaeudeModellEingang e, IReadOnlyList<BauteilEingang> bauteile)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            string wer = e.Bezeichnung;
            Pflicht(wer, "A_f", e.Nutzflaeche_M2);
            Pflicht(wer, "H", e.Raumhoehe_M);
            Pflicht(wer, "n", e.Luftwechselrate_h);
            double hVe = e.Luftwechselrate_h * e.Nutzflaeche_M2 * e.Raumhoehe_M * GebaeudeFestwerte.C_RHO_LUFT;
            return AusBauteilweg(new BauteilwegGebaeude(wer, e.Nutzflaeche_M2, e.Bauweise_WhK, e.MasseanteilAussen,
                                                        e.Innenflaechenfaktor, hVe), bauteile);
        }

        /// <summary>
        /// <b>Der Bauteilweg</b> mit ausdrücklichen Gebäudegrößen — die Rechnung selbst.
        ///
        /// <list type="bullet">
        /// <item><b>Gruppen</b> (VDI 6007-1, 6.4; Mehrzonenkonzept 2.2): Außenbauteilgruppe = alle
        /// opaken Bauteile an Außenluft, Erdreich und unbeheiztem Raum, einseitig reduziert
        /// (R₁, C₁,korr nach Gl. (17)); Innenbauteilgruppe = Bauteile innerhalb der Zone,
        /// symmetrisch (R₁, C₁; der volle Schichtaufbau geht in die Kettenmatrix, keine zweite
        /// Halbierung an der Mittelebene); Fenster und Vorhangfassaden = Fensterzweig nach
        /// Gl. (25)/(26), nach den Wänden parallel, die Kapazität der Wände bleibt.</item>
        /// <item><b>Mit Schichten:</b> Bezugsperiode je Bauteil (10a)–(10d), Reduktion (12)–(17),
        /// komplexe Parallelschaltung je Gruppe mit T_RA = 5 d (19)–(22). Der U-Wert eines
        /// Bauteils folgt aus den Schichten; ein eingetragener U-Wert gilt vorrangig in Gl. (27),
        /// der gerechnete steht in der Herleitung daneben (Mehrzonenkonzept 3.4). Der Rest der
        /// Wände ist so gebildet, dass der Wandzweig mit seinem vollen U·A in Gl. (27) eingeht:
        /// R_Rest,AW = 1/Σ(U·A)_opak − R₁,AW − R_α,i·A_AW,ges/A_AW,opak (Normweg; der R_si-Abzug
        /// des Klassenwegs ist dessen benannte Abweichung, Rechenschritte A4).</item>
        /// <item><b>Ohne Schichten — Grenzfall Klassenweg</b> (Mehrzonenkonzept 3.6): Trägt eine
        /// Gruppe kein Bauteil mit Speichermasse, rechnet sie den Klassenweg aus den
        /// Bauteilsummen — R₁ = 1/(h_ms·A), Kapazität aus der Bauweise nach a_AW bzw. 1 − a_AW,
        /// R_si-Konvention wie A4. Fehlen Innenbauteile ganz, gilt A_IW = f_IW·A_f. Ein
        /// Bauteilsatz ohne Schichten, der die U/A-Gruppen eines Gebäudes wiedergibt, gleicht so
        /// dem Klassenweg.</item>
        /// <item><b>Gemischte Außengruppe</b> (benannte EPOS-Regel; VDI 6007-1, 6.3 führt das
        /// Außenfenster als Sonderfall mit C ≈ 0): ein masseloses opakes Bauteil geht wie ein
        /// Fenster ein — R₁ = R/6 nach Gl. (25) sinngemäß, R aus Gl. (26) (mit Schichten aus
        /// Σ d/λ), parallel nach den Wänden, ohne Kapazität, mit vollem U·A in Gl. (27). Es wird
        /// dem Wandzweig zugeschlagen; weil die reelle Parallelschaltung nach der komplexen
        /// assoziativ ist, ist das Ergebnis der Gruppe dasselbe wie im Fensterzweig, und
        /// Σ(U·A)_opak behält sein opakes Gewicht für die äquivalente Außentemperatur.</item>
        /// <item><b>Gemischte Innengruppe</b> (benannte EPOS-Regel, Grenzwert von Gl. (19) für
        /// C → 0: Z → ∞): ein masseloses Innenbauteil trägt Fläche (Übergänge,
        /// Strahlungsaustausch), aber weder R₁ noch C.</item>
        /// <item><b>Übergänge:</b> R_conv,AW = 1/Σ(α_kon,i·A) über Außenbauteile und Fenster,
        /// R_conv,IW = 1/Σ(α_kon,i·A) über die Innenbauteile (ohne sie α_kon,i·A_IW), R_rad wie im
        /// Klassenweg; α_kon,i fehlt ⇒ <see cref="GebaeudeFestwerte.ALPHA_KON_INNEN"/>. Gl. (26)
        /// nimmt 1/α_I = 1/(α_kon,i + α_str,i), ohne Angabe <see cref="GebaeudeFestwerte.R_SI"/>,
        /// und 1/α_A = 1/(α_kon,a + α_str), ohne Angabe 1/<see cref="GebaeudeFestwerte.ALPHA_AUSSEN"/>
        /// an Außenluft, 0 an Erdreich, R_SI an einem unbeheizten Raum. Der U-Wert aus Schichten
        /// nimmt dieselben α-Werte, ohne Angabe die Bemessungswerte der DIN EN ISO 6946
        /// (<see cref="Bauteilreduktion.Uebergangswiderstaende"/>). R_α,A der Gruppe für (28a) ist
        /// 1/Σ(α_A·A) mit α_A = α_kon,a + α_str, ohne Angabe <see cref="GebaeudeFestwerte.ALPHA_AUSSEN"/>.</item>
        /// <item><b>R_ext</b> = 1/(H_ve + Σψ·L der Bauteile); unendlich ohne Zweig.</item>
        /// </list>
        /// <para>Die Zusammenfassung zur Außenbauteilgruppe nach Gl. (27)–(28c) leistet der
        /// Erbauer des Records. Welche Gruppe welchen Weg nahm, steht in <see cref="WegAussen"/>
        /// und <see cref="WegInnen"/>, die Herleitung je Bauteil — gewählte Bezugsperiode,
        /// gerechneter und wirksamer U-Wert, Hinweis bei mehr als 10 % Abweichung — in
        /// <see cref="Bauteilherleitung"/>.</para>
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jeder verletzten Prüfung, benannt.</exception>
        internal static ErsatzparameterRC AusBauteilweg(BauteilwegGebaeude g, IReadOnlyList<BauteilEingang> bauteile)
        {
            string wer = string.IsNullOrEmpty(g.Bezeichnung) ? "—" : g.Bezeichnung;
            if (bauteile == null || bauteile.Count == 0)
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_KEINE_AUSSENBAUTEILE, wer));
            if (!(g.Lueftungsleitwert_WK >= 0.0) || double.IsInfinity(g.Lueftungsleitwert_WK))
                throw BauteilEingang.Bereich(GebaeudeModellFehler.ParameterUngueltig, wer, "H_ve", g.Lueftungsleitwert_WK, "[0; ∞) W/K");

            // ---- Prüfen und Gruppen bilden ----
            var aussen = new List<(BauteilEingang B, string Wer, int I)>();
            var fenster = new List<(BauteilEingang B, string Wer, int I)>();
            var innen = new List<(BauteilEingang B, string Wer, int I)>();
            double psiL = 0.0;
            for (int i = 0; i < bauteile.Count; i++)
            {
                BauteilEingang b = bauteile[i] ?? throw new ArgumentException("Der Bauteilsatz enthält einen leeren Eintrag.", nameof(bauteile));
                string werB = wer + ", " + (string.IsNullOrEmpty(b.Bezeichnung) ? "#" + (i + 1).ToString(CultureInfo.InvariantCulture) : b.Bezeichnung);
                b.Pruefen(werB);
                psiL += b.PsiL_WK;
                switch (b.Gruppe)
                {
                    case Bauteilgruppe.Fenster: fenster.Add((b, werB, i)); break;
                    case Bauteilgruppe.Innen: innen.Add((b, werB, i)); break;
                    default: aussen.Add((b, werB, i)); break;
                }
            }
            if (aussen.Count == 0)
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_KEINE_AUSSENBAUTEILE, wer));

            var herleitung = new List<BauteilHerleitung>();
            var uJeBauteil = new double[bauteile.Count];

            // ---- Flächen und Übergänge im Raum (A2, A6) ----
            double aOpak = 0.0, aFenster = 0.0, alphaAw = 0.0, alphaAussen = 0.0;
            foreach ((BauteilEingang b, _, _) in aussen)
            {
                aOpak += b.Flaeche_M2;
                alphaAw += AlphaKonInnen(b) * b.Flaeche_M2;
                alphaAussen += AlphaAussenGesamt(b) * b.Flaeche_M2;
            }
            foreach ((BauteilEingang b, _, _) in fenster)
            {
                aFenster += b.Flaeche_M2;
                alphaAw += AlphaKonInnen(b) * b.Flaeche_M2;
                alphaAussen += AlphaAussenGesamt(b) * b.Flaeche_M2;
            }
            double aGes = aOpak + aFenster;

            double aIw, rConvIw;
            if (innen.Count > 0)
            {
                aIw = 0.0;
                double alphaIw = 0.0;
                foreach ((BauteilEingang b, _, _) in innen)
                {
                    aIw += b.Flaeche_M2;
                    alphaIw += AlphaKonInnen(b) * b.Flaeche_M2;
                }
                rConvIw = 1.0 / alphaIw;
            }
            else
            {
                Pflicht(wer, "A_f", g.Nutzflaeche_M2);
                Pflicht(wer, "f_IW", g.Innenflaechenfaktor);
                aIw = g.Innenflaechenfaktor * g.Nutzflaeche_M2;
                rConvIw = 1.0 / (GebaeudeFestwerte.ALPHA_KON_INNEN * aIw);
            }
            double rConvAw = 1.0 / alphaAw;
            double rRad = 1.0 / (GebaeudeFestwerte.ALPHA_STR_INNEN * Math.Min(aGes, aIw));
            double rAlphaI = 1.0 / (1.0 / rConvAw + 1.0 / rRad);

            // ---- Außenbauteilgruppe ----
            double uaOpak = 0.0;
            var zweigeAw = new List<(double R1_KW, double C1_Jk)>();
            var masseloseAw = new List<(BauteilEingang B, string Wer, double R_KW, double UGerechnet)>();
            foreach ((BauteilEingang b, string werB, int ib) in aussen)
            {
                double uGerechnet = double.NaN;
                Schichtkennwerte kennwerte = default;
                if (b.HatSchichten)
                {
                    (double rSi, double rSe) = Uebergaenge(b, gleichung26: false, werB);
                    kennwerte = Bauteilreduktion.Kennwerte(b.Schichten, Bauteilreduktion.RichtungAusNeigung(b.NeigungWirksamGrad, werB), rSi, rSe, werB);
                    uGerechnet = kennwerte.U_WM2K;
                }
                double uWirksam = double.IsNaN(b.UWert_WM2K) ? uGerechnet : b.UWert_WM2K;
                uaOpak += uWirksam * b.Flaeche_M2;
                uJeBauteil[ib] = uWirksam;

                if (b.HatSchichten && kennwerte.Kapazitaet_JM2K > 0.0)
                {
                    Bezugsperiodenwahl wahl = Bauteilreduktion.BezugsperiodeWaehlen(b.Schichten, b.Flaeche_M2,
                        Bauteilreduktion.RichtungAusNeigung(b.NeigungWirksamGrad, werB), werB);
                    Bauteilkennwerte k = wahl.Kennwerte;
                    if (double.IsNaN(k.C1korr_Jk) || double.IsInfinity(k.C1korr_Jk) || k.C1korr_Jk <= 0.0)
                        throw new GebaeudeModellException(GebaeudeModellFehler.BauteilreduktionUngueltig,
                            Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_REDUKTION_UNGUELTIG, werB,
                                                    Bauteilreduktion.Text(k.R1_KW), Bauteilreduktion.Text(k.C1korr_Jk)));
                    zweigeAw.Add((k.R1_KW, k.C1korr_Jk));
                    herleitung.Add(new BauteilHerleitung(b.Bezeichnung, Bauteilgruppe.Aussen, false, wahl.Periode_d, wahl.R1rel, wahl.C1rel,
                                                         k.R1_KW, k.C1korr_Jk, uGerechnet, uWirksam));
                }
                else
                {
                    double r = b.HatSchichten ? kennwerte.R_M2KW / b.Flaeche_M2 : WiderstandGl26(b, werB, GebaeudeModellFehler.BauteilUngueltig);
                    masseloseAw.Add((b, werB, r, uGerechnet));
                    herleitung.Add(new BauteilHerleitung(b.Bezeichnung, Bauteilgruppe.Aussen, true, double.NaN, double.NaN, double.NaN,
                                                         r / 6.0, double.NaN, uGerechnet, uWirksam));
                }
            }

            double cAw, r1Aw, rRestAw;
            Gruppenweg wegAussen;
            if (zweigeAw.Count == 0)
            {
                // Grenzfall Klassenweg (A1, A4): kein Außenbauteil mit Speichermasse.
                wegAussen = Gruppenweg.Klassenweg;
                cAw = g.MasseanteilAussen * KapazitaetAusBauweise(g, wer);
                r1Aw = 1.0 / (GebaeudeFestwerte.H_MS * aOpak);
                rRestAw = 1.0 / uaOpak - r1Aw - GebaeudeFestwerte.R_SI / aOpak;
            }
            else
            {
                wegAussen = Gruppenweg.Bauteilweg;
                (double r1Masse, double c) = Bauteilreduktion.Parallel(zweigeAw);
                cAw = c;
                // Masselose opake Bauteile: R₁ = R/6, reell parallel nach den Wänden (EPOS-Regel).
                if (masseloseAw.Count == 0) r1Aw = r1Masse;
                else
                {
                    double leitwert = 1.0 / r1Masse;
                    foreach ((_, _, double r, _) in masseloseAw) leitwert += 1.0 / (r / 6.0);
                    r1Aw = 1.0 / leitwert;
                }
                // Normweg: der Wandzweig geht mit seinem vollen U·A in Gl. (27) ein.
                rRestAw = 1.0 / uaOpak - r1Aw - rAlphaI * aGes / aOpak;
            }

            // ---- Fensterzweig (Gl. (25), (26)) ----
            double r1Af = double.PositiveInfinity, rRestAf = double.PositiveInfinity, uaFenster = 0.0;
            if (fenster.Count > 0)
            {
                double leitwertAf = 0.0;
                foreach ((BauteilEingang b, string werB, int ib) in fenster)
                {
                    double rAf = WiderstandGl26(b, werB, GebaeudeModellFehler.FensterzweigUngueltig);
                    double r1 = rAf / 6.0;
                    leitwertAf += 1.0 / r1;
                    uaFenster += b.UWert_WM2K * b.Flaeche_M2;
                    uJeBauteil[ib] = b.UWert_WM2K;
                    herleitung.Add(new BauteilHerleitung(b.Bezeichnung, Bauteilgruppe.Fenster, true, double.NaN, double.NaN, double.NaN,
                                                         r1, double.NaN, double.NaN, b.UWert_WM2K));
                }
                r1Af = 1.0 / leitwertAf;
                rRestAf = 1.0 / uaFenster - r1Af - rAlphaI * aGes / aFenster;
            }

            // ---- Innenbauteilgruppe ----
            var zweigeIw = new List<(double R1_KW, double C1_Jk)>();
            foreach ((BauteilEingang b, string werB, int ib) in innen)
            {
                double uGerechnet = double.NaN;
                bool mitMasse = false;
                if (b.HatSchichten)
                {
                    (double rSi, double rSe) = Uebergaenge(b, gleichung26: false, werB);
                    Schichtkennwerte kennwerte = Bauteilreduktion.Kennwerte(b.Schichten,
                        Bauteilreduktion.RichtungAusNeigung(b.NeigungWirksamGrad, werB), rSi, rSe, werB);
                    uGerechnet = kennwerte.U_WM2K;
                    mitMasse = kennwerte.Kapazitaet_JM2K > 0.0;
                }
                double uWirksam = double.IsNaN(b.UWert_WM2K) ? uGerechnet : b.UWert_WM2K;
                uJeBauteil[ib] = uWirksam;
                if (mitMasse)
                {
                    Bezugsperiodenwahl wahl = Bauteilreduktion.BezugsperiodeWaehlen(b.Schichten, b.Flaeche_M2,
                        Bauteilreduktion.RichtungAusNeigung(b.NeigungWirksamGrad, werB), werB);
                    Bauteilkennwerte k = wahl.Kennwerte;
                    zweigeIw.Add((k.R1_KW, k.C1_Jk));
                    herleitung.Add(new BauteilHerleitung(b.Bezeichnung, Bauteilgruppe.Innen, false, wahl.Periode_d, wahl.R1rel, wahl.C1rel,
                                                         k.R1_KW, k.C1_Jk, uGerechnet, uWirksam));
                }
                else
                {
                    // Masseloses Innenbauteil: nur Fläche (EPOS-Regel, Gl. (19) mit C → 0).
                    herleitung.Add(new BauteilHerleitung(b.Bezeichnung, Bauteilgruppe.Innen, true, double.NaN, double.NaN, double.NaN,
                                                         double.NaN, double.NaN, uGerechnet, uWirksam));
                }
            }

            double cIw, r1Iw;
            Gruppenweg wegInnen;
            if (zweigeIw.Count == 0)
            {
                // Grenzfall Klassenweg (A1, A5): kein Innenbauteil mit Speichermasse.
                wegInnen = Gruppenweg.Klassenweg;
                cIw = (1.0 - g.MasseanteilAussen) * KapazitaetAusBauweise(g, wer);
                r1Iw = 1.0 / (GebaeudeFestwerte.H_MS * aIw);
            }
            else
            {
                wegInnen = Gruppenweg.Bauteilweg;
                (r1Iw, cIw) = Bauteilreduktion.Parallel(zweigeIw);
            }

            // ---- Lüftung und Wärmebrücken (A7) ----
            double hExt = g.Lueftungsleitwert_WK + psiL;
            double rExt = hExt > 0.0 ? 1.0 / hExt : double.PositiveInfinity;
            double rAlphaAussen = 1.0 / alphaAussen;

            ErsatzparameterRC p;
            try
            {
                p = new ErsatzparameterRC(cAw, cIw, r1Aw, rRestAw, r1Iw, rConvAw, rConvIw, rRad, rExt,
                                          aOpak, aIw, uaOpak, r1Af, rRestAf, aFenster, uaFenster, rAlphaAussen);
            }
            catch (GebaeudeModellException ex)
            {
                throw new GebaeudeModellException(ex.Grund, wer + ": " + ex.Message);
            }
            return p with { Bauteilherleitung = herleitung.AsReadOnly(), WegAussen = wegAussen, WegInnen = wegInnen,
                            UWirksamJeBauteil_WM2K = Array.AsReadOnly(uJeBauteil) };
        }

        /// <summary>Der konvektive Übergang raumseitig α_kon,i [W/(m²K)]: eingetragen, sonst <see cref="GebaeudeFestwerte.ALPHA_KON_INNEN"/>.</summary>
        private static double AlphaKonInnen(BauteilEingang b)
            => double.IsNaN(b.AlphaKonInnen_WM2K) ? GebaeudeFestwerte.ALPHA_KON_INNEN : b.AlphaKonInnen_WM2K;

        /// <summary>
        /// Der Gesamtübergang außen α_A [W/(m²K)] für R_α,A der Gruppe (Gl. (28a)): α_kon,a plus
        /// Strahlungsanteil, ohne Angabe <see cref="GebaeudeFestwerte.ALPHA_AUSSEN"/> (wie im Klassenweg).
        /// </summary>
        private static double AlphaAussenGesamt(BauteilEingang b)
        {
            if (double.IsNaN(b.AlphaKonAussen_WM2K)) return GebaeudeFestwerte.ALPHA_AUSSEN;
            return b.AlphaKonAussen_WM2K + (b.Rand == Bauteilrand.Aussenluft ? GebaeudeFestwerte.ALPHA_STR_AUSSEN_RUECKFALL
                                                                             : GebaeudeFestwerte.ALPHA_STR_INNEN);
        }

        /// <summary>
        /// Die Übergangswiderstände eines Bauteils [m²K/W]. Trägt es α_kon, gilt 1/(α_kon + α_str)
        /// (α_str = <see cref="GebaeudeFestwerte.ALPHA_STR_INNEN"/> raumseitig und zu einem
        /// Nachbarraum, <see cref="GebaeudeFestwerte.ALPHA_STR_AUSSEN_RUECKFALL"/> an Außenluft);
        /// sonst für Gl. (26) die Vorgabe des Klassenwegs (1/α_I = R_SI, 1/α_A = 1/ALPHA_AUSSEN,
        /// am unbeheizten Raum R_SI), für den U-Wert aus Schichten die Bemessungswerte der
        /// DIN EN ISO 6946. An Erdreich gibt es keinen äußeren Übergang.
        /// </summary>
        private static (double R_si_M2KW, double R_se_M2KW) Uebergaenge(BauteilEingang b, bool gleichung26, string wer)
        {
            (double dinSi, double dinSe) = Bauteilreduktion.Uebergangswiderstaende(b.NeigungWirksamGrad, b.Rand, wer);
            double rSi = !double.IsNaN(b.AlphaKonInnen_WM2K)
                ? 1.0 / (b.AlphaKonInnen_WM2K + GebaeudeFestwerte.ALPHA_STR_INNEN)
                : gleichung26 ? GebaeudeFestwerte.R_SI : dinSi;
            double rSe;
            switch (b.Rand)
            {
                case Bauteilrand.Erdreich:
                    rSe = GebaeudeFestwerte.R_SE_ERDREICH;
                    break;
                case Bauteilrand.Aussenluft:
                    rSe = !double.IsNaN(b.AlphaKonAussen_WM2K)
                        ? 1.0 / (b.AlphaKonAussen_WM2K + GebaeudeFestwerte.ALPHA_STR_AUSSEN_RUECKFALL)
                        : gleichung26 ? 1.0 / GebaeudeFestwerte.ALPHA_AUSSEN : dinSe;
                    break;
                default:
                    rSe = !double.IsNaN(b.AlphaKonAussen_WM2K)
                        ? 1.0 / (b.AlphaKonAussen_WM2K + GebaeudeFestwerte.ALPHA_STR_INNEN)
                        : gleichung26 ? GebaeudeFestwerte.R_SI : dinSe;
                    break;
            }
            return (rSi, rSe);
        }

        /// <summary>
        /// Der Widerstand nach VDI 6007-1 Gl. (26): R = (1/U − 1/α_I − 1/α_A)/A [K/W] — der U-Wert
        /// um beide Übergänge bereinigt, weil das Netz sie selbst führt. R ≤ 0 bricht benannt ab;
        /// einen selbst gewählten Wert setzt EPOS nicht.
        /// </summary>
        private static double WiderstandGl26(BauteilEingang b, string wer, GebaeudeModellFehler grund)
        {
            (double rSi, double rSe) = Uebergaenge(b, gleichung26: true, wer);
            double r = (1.0 / b.UWert_WM2K - rSi - rSe) / b.Flaeche_M2;
            if (!(r > 0.0) || double.IsInfinity(r))
                throw new GebaeudeModellException(grund,
                    Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_WIDERSTAND_GL26, wer, Bauteilreduktion.Text(r),
                                            Bauteilreduktion.Text(b.UWert_WM2K)));
            return r;
        }

        /// <summary>
        /// Die Speichermasse aus der Bauweise C_ges = Bauweise · 3 600 [J/K] (A1) für eine Gruppe
        /// im Klassenweg, mit den Prüfungen des Klassenwegs: A_f größer null, Bauweise je
        /// Nutzfläche im Plausibilitätsband, Masseanteil außen in (0, 1).
        /// </summary>
        private static double KapazitaetAusBauweise(BauteilwegGebaeude g, string wer)
        {
            Pflicht(wer, "A_f", g.Nutzflaeche_M2);
            double jeM2 = g.Bauweise_WhK / g.Nutzflaeche_M2;
            if (!(jeM2 >= GebaeudeFestwerte.BAUWEISE_JE_M2_MIN && jeM2 <= GebaeudeFestwerte.BAUWEISE_JE_M2_MAX))
                throw new GebaeudeModellException(GebaeudeModellFehler.BauweiseUnplausibel,
                    Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_BAUWEISE, wer, Bauteilreduktion.Text(jeM2),
                                            Bauteilreduktion.Text(GebaeudeFestwerte.BAUWEISE_JE_M2_MIN),
                                            Bauteilreduktion.Text(GebaeudeFestwerte.BAUWEISE_JE_M2_MAX)));
            if (!(g.MasseanteilAussen > 0.0 && g.MasseanteilAussen < 1.0))
                throw BauteilEingang.Bereich(GebaeudeModellFehler.ParameterUngueltig, wer, "a_AW", g.MasseanteilAussen, "(0; 1)");
            return g.Bauweise_WhK * GebaeudeFestwerte.SEKUNDEN_JE_STUNDE;
        }

        private static void Pflicht(string wer, string groesse, double wert)
        {
            if (!IstPositivEndlich(wert))
                throw new GebaeudeModellException(GebaeudeModellFehler.PflichtgroesseFehlt,
                    Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_PFLICHT, wer, groesse, Bauteilreduktion.Text(wert)));
        }

        private static void UWert(string wer, string gruppe, double u, double flaeche)
        {
            if (!(flaeche > 0.0)) return;
            if (!(u >= GebaeudeFestwerte.U_MIN && u <= GebaeudeFestwerte.U_MAX))
                throw new GebaeudeModellException(GebaeudeModellFehler.UWertUnplausibel,
                    wer + "Der U-Wert " + gruppe + " " + Text(u) + " W/(m²K) liegt nicht in " +
                    Text(GebaeudeFestwerte.U_MIN) + " … " + Text(GebaeudeFestwerte.U_MAX) + " W/(m²K).");
        }

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
