namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Grenzen der Zonen eines Gebäudes</b> (Gebäudesimulation Stufe G6a; Mehrzonenkonzept
    /// 5.3, M12) — die EINE Stelle, an der Lauf, Prüfung und Oberfläche lesen, wie viele Zonen ein
    /// Gebäude tragen darf und wie viele der Lauf rechnet. Öffentlich, weil die Oberfläche
    /// (<c>EPOS.UI</c>) sie ohne Freigabe der Interna liest.
    ///
    /// <para><b>Zwei Grenzen.</b> Die <see cref="LAUFGRENZE"/> ist die Zahl der Zonen, die der
    /// Rechenkern je Gebäude rechnet (<see cref="GebaeudeZonensatz.EineZone"/>): mehr lehnt der Lauf
    /// benannt ab. Die <see cref="PFLEGEGRENZE"/> ist die Zahl der Zonen, die ein Gebäude tragen darf
    /// (<see cref="GebaeudeZonenCtrl.Pruefen(System.Collections.Generic.IList{ZoneModel})"/>) —
    /// vorläufig 50 (Anwenderentscheid A3 vom 25.09.2026; Mehrzonenkonzept M12 bleibt bis G6c
    /// offen).</para>
    ///
    /// <para><b>Der Freigabeschalter</b> (<see cref="MehrereZonenFreigegeben"/>, Anwenderentscheid A1
    /// vom 25.09.2026): Steht er auf „an", ist eine zweite Zone speicherbar — mit Rückfrage und
    /// Sperrzeile, weil der Lauf sie erst mit Stufe G6b rechnet. Für eine Auslieferung vor G6b wird
    /// er ausgeschaltet; dann trägt ein Gebäude höchstens eine Zone
    /// (<see cref="Hoechstzahl()"/>), und „+ Neue Zone" nennt die Sperre.</para>
    /// </summary>
    public static class GebaeudeZonenregeln
    {
        /// <summary>Die Zahl der Zonen je Gebäude, die der Lauf rechnet; mehr lehnt er benannt ab.</summary>
        public const int LAUFGRENZE = 1;

        /// <summary>Die Zahl der Zonen, die ein Gebäude höchstens trägt (vorläufig, M12; Entscheid A3).</summary>
        public const int PFLEGEGRENZE = 50;

        /// <summary>Die Stellung des Freigabeschalters in diesem Stand (A1: an).</summary>
        private const bool FREIGABE_MEHRERE_ZONEN = true;

        /// <summary>
        /// Ist mehr als eine Zone je Gebäude speicherbar? Der Freigabeschalter (A1) — aus, trägt ein
        /// Gebäude höchstens eine Zone.
        /// </summary>
        public static bool MehrereZonenFreigegeben => FREIGABE_MEHRERE_ZONEN;

        /// <summary>Die Zahl der Zonen, die ein Gebäude tragen darf, bei dieser Stellung des Schalters.</summary>
        public static int Hoechstzahl(bool mehrereZonenFreigegeben) => mehrereZonenFreigegeben ? PFLEGEGRENZE : LAUFGRENZE;

        /// <summary>Die Zahl der Zonen, die ein Gebäude in diesem Stand tragen darf.</summary>
        public static int Hoechstzahl() => Hoechstzahl(MehrereZonenFreigegeben);

        /// <summary>Rechnet der Lauf ein Gebäude mit <paramref name="zahl"/> Zonen?</summary>
        public static bool Rechenbar(int zahl) => zahl <= LAUFGRENZE;

        /// <summary>
        /// Die Abweichung der Σ Nutzfläche der Zonen von der Nutzfläche des Gebäudes, ab der ein
        /// Hinweis erscheint [–] (Mehrzonenkonzept 5.3, E19): 5 %.
        /// </summary>
        public const double FLAECHENABWEICHUNG_HINWEIS = 0.05;
    }
}
