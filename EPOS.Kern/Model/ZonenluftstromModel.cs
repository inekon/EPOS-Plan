namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Luftaustausch zwischen zwei Zonen desselben Gebäudes — eine Zeile aus
    /// <c>Tab_Zonenluftstrom</c> (Schemaschritt S-G, <see cref="ZonenkopplungSchema"/>;
    /// Mehrzonenkonzept 2.7). <b>Ein Paar, kein Einzelstrom:</b> Der Volumenstrom fließt von A nach
    /// B, der Gegenstrom gleicher Größe entsteht beim Rechnen von selbst — so trägt die Eingabe die
    /// Massenbilanz.
    ///
    /// <para><b>Vorläufige Zeilen tragen eine NEGATIVE Id</b> — wie Zone und Bauteil (Muster A6).
    /// Die beiden Zonen dürfen auf vorläufige (negative) Zonen-Ids zeigen; der Schreibweg
    /// (<c>GebaeudeZonenCtrl.SpeichernJeGebaeude</c>) schlüsselt sie nach der Vergabe der
    /// Zonen-Ids um und dreht das Paar auf A &lt; B.</para>
    /// </summary>
    public class ZonenluftstromModel
    {
        /// <summary>Primärschlüssel; ≤ 0 = vorläufig (neu anzulegen).</summary>
        public int ID;

        /// <summary>Die erste Zone des Paares (<c>Tab_Zone.ID</c>, im Arbeitsstand auch eine vorläufige Id).</summary>
        public int ID_ZoneA;

        /// <summary>Die zweite Zone des Paares.</summary>
        public int ID_ZoneB;

        /// <summary>Der Volumenstrom [m³/h], größer null.</summary>
        public double Volumenstrom;

        /// <summary>Eine entkoppelte Kopie.</summary>
        public ZonenluftstromModel Kopie() => (ZonenluftstromModel)MemberwiseClone();
    }
}
