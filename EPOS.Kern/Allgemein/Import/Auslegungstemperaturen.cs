using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die zwei Auslegungstemperaturen eines Projekts</b> — der kalte und der heiße
    /// Fall der Strangprüfung (<see cref="StrangPlausibilitaet"/>), Anwenderentscheid
    /// <b>W6‑B‑11</b> vom 09.09.2026.
    ///
    /// <para><b><c>null</c> heißt „die Vorgabe"</b> — <see cref="StrangPlausibilitaet.T_KALT"/>
    /// bzw. <see cref="StrangPlausibilitaet.T_HEISS"/>. Der Satz gilt in beiden
    /// Richtungen: Ein Feld, das der Anwender leert, steht wieder auf der Vorgabe, und
    /// eine Datenbank ohne die zwei Spalten verhält sich wie vor Schritt 70.</para>
    /// </summary>
    /// <param name="Kalt">Auslegungstemperatur kalt [°C]; <c>null</c> = Vorgabe.</param>
    /// <param name="Heiss">Auslegungstemperatur heiß, Zelltemperatur [°C]; <c>null</c> = Vorgabe.</param>
    public sealed record Auslegungstemperaturen(double? Kalt, double? Heiss)
    {
        /// <summary>Beide Felder leer — der Stand „nicht gepflegt".</summary>
        public static readonly Auslegungstemperaturen Vorgabe = new(null, null);

        /// <summary>Der kalte Fall, mit dem gerechnet wird.</summary>
        public double KaltOderVorgabe => Kalt ?? StrangPlausibilitaet.T_KALT;

        /// <summary>Der heiße Fall, mit dem gerechnet wird.</summary>
        public double HeissOderVorgabe => Heiss ?? StrangPlausibilitaet.T_HEISS;
    }

    /// <summary>
    /// <b>Der VORSCHLAG für die zwei Auslegungstemperaturen aus den Klimadaten des
    /// Projekts</b> — Anwenderentscheid <b>W6‑B‑11</b> vom 09.09.2026 (Vorschlag V7 des
    /// Prüfberichts vom 08.09.2026, offener Punkt <b>O‑3</b>).
    ///
    /// <para><b>Warum überhaupt ein Vorschlag und keine automatische Kopplung.</b> Die
    /// Auslegungstemperatur ist eine ENTSCHEIDUNG des Planers, keine Ableitung: Das
    /// Jahresminimum eines Testreferenzjahres ist nicht dieselbe Größe wie die
    /// standortbezogen niedrigste zu erwartende Temperatur nach IEC 62548, und eine
    /// unbelüftete Dachfläche wird heißer als jede Formel sagt. Der Knopf rechnet
    /// deshalb eine <b>Herleitung</b> und schreibt nichts von selbst; übernommen wird
    /// per Hand, und die Herleitungszeile sagt, woher jede Zahl kommt.</para>
    ///
    /// <list type="bullet">
    ///   <item><description><b>kalt</b> = Minimum der Außentemperatur der Jahresreihe
    ///     des Klimadatensatzes (<c>Tab_Solar</c>, 8 760 Stundenwerte).</description></item>
    ///   <item><description><b>heiß</b> = Zelltemperatur bei 1 000 W/m²:
    ///     <c>T_amb,max + (T_NOCT − 20) · 1000/800</c>. Das ist Zeichen für Zeichen die
    ///     Zelltemperaturformel, mit der auch <c>SimulationPV</c> rechnet
    ///     (<c>T_Zelle = T_Aussen + G/800 · (T_NOCT − 20)</c>) — nur bei
    ///     Volleinstrahlung und mit dem Jahresmaximum der Außentemperatur.</description></item>
    /// </list>
    ///
    /// <para><b>Der NOCT-Rückfall ist derselbe wie im Rechenweg</b>
    /// (<see cref="NOCT_RUECKFALL"/> = 45 °C, Fenster
    /// <see cref="NOCT_MIN"/>…<see cref="NOCT_MAX"/>): Der Modulbestand führt in
    /// <c>T_NOCT</c> nachweislich den Kurzschlussstrom (Paket‑A‑Befund A1), und ein
    /// Vorschlag, der daraus eine Zelltemperatur von 9 °C errechnet, wäre schlimmer als
    /// keiner.</para>
    ///
    /// <para><b>Warum im Kern und nicht in der Hülle.</b> Die Klimareihe liest
    /// <c>SolardatenCtrl</c>, und der ist im Kern <c>internal</c>; die Hülle käme gar
    /// nicht heran. Der Kern rechnet und formuliert, die Hülle reicht den Vorschlag als
    /// Delegat in die Maske — dieselbe Aufteilung wie bei der Auslegungshilfe
    /// (<c>StrangAuslegung</c>, W6‑B‑8).</para>
    /// </summary>
    public static class AuslegungstemperaturVorschlag
    {
        /// <summary>
        /// Der NOCT-Rückfall [°C] — dieselbe Zahl wie <c>SimulationPV.NOCT_RUECKFALL</c>.
        /// Sie steht hier noch einmal, weil <c>SimulationPV</c> zum Rechenweg gehört und
        /// dieser Vorschlag zur Prüfung; ein Verweis quer durch die Ordner wäre eine
        /// Kante, die keinen Nutzen bringt. Der Nachweis hält beide Zahlen gleich.
        /// </summary>
        public const double NOCT_RUECKFALL = 45.0;

        /// <summary>Untere Grenze eines plausiblen NOCT [°C].</summary>
        public const double NOCT_MIN = 20.0;

        /// <summary>Obere Grenze eines plausiblen NOCT [°C].</summary>
        public const double NOCT_MAX = 60.0;

        /// <summary>Bezugseinstrahlung des NOCT [W/m²].</summary>
        public const double G_NOCT = 800.0;

        /// <summary>Einstrahlung des heißen Falls [W/m²] — Volleinstrahlung, wie STC.</summary>
        public const double G_VOLL = 1000.0;

        /// <summary>Bezugs-Umgebungstemperatur des NOCT [°C].</summary>
        public const double T_NOCT_BEZUG = 20.0;

        /// <summary>
        /// Was der Knopf „Vorschlag aus Klimadaten übernehmen" anbietet.
        /// </summary>
        /// <param name="Moeglich">Die Klimareihe lieferte Werte.</param>
        /// <param name="Kalt">Vorgeschlagene Auslegungstemperatur kalt [°C].</param>
        /// <param name="Heiss">Vorgeschlagene Auslegungstemperatur heiß [°C].</param>
        /// <param name="Satz">
        /// Die HERLEITUNG, fertig formuliert in der Kultur des Anwenders — oder der
        /// Grund, wenn es keinen Vorschlag gibt.
        /// </param>
        public sealed record Vorschlag(bool Moeglich, double Kalt, double Heiss, string Satz)
        {
            /// <summary>Kein Vorschlag und kein Satz.</summary>
            public static readonly Vorschlag Leer = new(false, 0.0, 0.0, "");
        }

        /// <summary>
        /// Rechnet den Vorschlag für ein Projekt.
        /// </summary>
        /// <param name="idProjekt">Das Projekt; ≤ 0 liefert <see cref="Vorschlag.Leer"/>.</param>
        /// <param name="tNoct">
        /// <c>T_NOCT</c> des Anlagenmoduls [°C]; <c>null</c> und jeder Wert außerhalb
        /// von <see cref="NOCT_MIN"/>…<see cref="NOCT_MAX"/> ergeben den Rückfall
        /// <see cref="NOCT_RUECKFALL"/>.
        /// </param>
        public static Vorschlag Fuer(int idProjekt, double? tNoct)
        {
            if (idProjekt <= 0) return Vorschlag.Leer;

            int idKlima = Klimaregion(idProjekt);
            if (idKlima <= 0) return Ohne();

            var ctrl = new SolardatenCtrl();
            ctrl.ReadAll(idKlima);
            if (ctrl.list_Temperatur.Count == 0) return Ohne();

            double min = double.MaxValue, max = double.MinValue;
            foreach (double t in ctrl.list_Temperatur)
            {
                if (t < min) min = t;
                if (t > max) max = t;
            }

            return Aus(min, max, tNoct);
        }

        /// <summary>
        /// Derselbe Vorschlag aus BEREITS gelesenen Extremwerten — der prüfbare Kern
        /// der Rechnung, ohne Datenbank.
        /// </summary>
        /// <param name="tAussenMin">Jahresminimum der Außentemperatur [°C].</param>
        /// <param name="tAussenMax">Jahresmaximum der Außentemperatur [°C].</param>
        /// <param name="tNoct">
        /// <c>T_NOCT</c> des Moduls [°C]; außerhalb des Fensters gilt der Rückfall.
        /// </param>
        public static Vorschlag Aus(double tAussenMin, double tAussenMax, double? tNoct)
        {
            double noct = NoctOderRueckfall(tNoct);
            double heiss = tAussenMax + (noct - T_NOCT_BEZUG) * G_VOLL / G_NOCT;

            string satz = string.Format(CultureInfo.CurrentCulture,
                                        MyResource.Resource.PVS_TEMP_HERLEITUNG,
                                        Z(tAussenMin), Z(tAussenMax), Z(noct), Z(heiss));

            return new Vorschlag(true, tAussenMin, heiss, satz);
        }

        /// <summary>
        /// Der geprüfte NOCT: der Katalogwert, wenn er im Fenster liegt, sonst
        /// <see cref="NOCT_RUECKFALL"/>. Dieselbe Regel wie
        /// <c>SimulationPV.NoctDesModuls</c>.
        /// </summary>
        public static double NoctOderRueckfall(double? tNoct)
        {
            if (!tNoct.HasValue) return NOCT_RUECKFALL;
            double n = tNoct.Value;
            return (n >= NOCT_MIN && n <= NOCT_MAX) ? n : NOCT_RUECKFALL;
        }

        /// <summary>„Die Klimadaten liefern keine Reihe" — ein Grund, kein Vorschlag.</summary>
        private static Vorschlag Ohne()
        {
            return new Vorschlag(false, 0.0, 0.0, MyResource.Resource.PVS_TEMP_OHNE_KLIMA);
        }

        /// <summary>Die Klimaregion des Projekts; 0 = keine.</summary>
        private static int Klimaregion(int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?",
                    new DbParam("?", idProjekt));

                if (dt == null || dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
                return Convert.ToInt32(dt.Rows[0][0]);
            }
            catch { return 0; }
        }

        private static string Z(double wert)
        {
            return wert.ToString("N1", CultureInfo.CurrentCulture);
        }
    }
}
