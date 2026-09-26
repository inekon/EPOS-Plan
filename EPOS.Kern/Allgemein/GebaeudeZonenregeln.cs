namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Grenze der Zonen eines Gebäudes</b> (Gebäudesimulation Stufen G6a und G6b;
    /// Mehrzonenkonzept 5.3, M12) — die EINE Stelle, an der Lauf, Prüfung und Oberfläche lesen, wie
    /// viele Zonen ein Gebäude tragen darf. Öffentlich, weil die Oberfläche (<c>EPOS.UI</c>) sie ohne
    /// Freigabe der Interna liest.
    ///
    /// <para><b>Eine Grenze für Pflege und Lauf.</b> Die <see cref="PFLEGEGRENZE"/> ist die Zahl der
    /// Zonen, die ein Gebäude tragen darf
    /// (<see cref="GebaeudeZonenCtrl.Pruefen(System.Collections.Generic.IList{ZoneModel})"/>), und
    /// zugleich die Zahl, die der Lauf rechnet (<see cref="Rechenbar"/>): ab zwei Zonen rechnet die
    /// Zonenschleife jede Zone für sich (<see cref="Vdi6007Rechenweg"/>), darüber lehnt der Lauf das
    /// Gebäude benannt ab (<see cref="GebaeudeModellFehler.MehrereZonen"/>). Vorläufig 50
    /// (Anwenderentscheid A3 vom 25.09.2026; Mehrzonenkonzept M12 bleibt bis G6c offen).</para>
    /// </summary>
    public static class GebaeudeZonenregeln
    {
        /// <summary>Die Zahl der Zonen, die ein Gebäude höchstens trägt und der Lauf rechnet (vorläufig, M12; Entscheid A3).</summary>
        public const int PFLEGEGRENZE = 50;

        /// <summary>Die Zahl der Zonen, die ein Gebäude tragen darf.</summary>
        public static int Hoechstzahl() => PFLEGEGRENZE;

        /// <summary>Rechnet der Lauf ein Gebäude mit <paramref name="zahl"/> Zonen? Bis zur <see cref="PFLEGEGRENZE"/>.</summary>
        public static bool Rechenbar(int zahl) => zahl <= PFLEGEGRENZE;

        /// <summary>
        /// Die Abweichung der Σ Nutzfläche der Zonen von der Nutzfläche des Gebäudes, ab der ein
        /// Hinweis erscheint [–] (Mehrzonenkonzept 5.3, E19): 5 %.
        /// </summary>
        public const double FLAECHENABWEICHUNG_HINWEIS = 0.05;
    }
}
