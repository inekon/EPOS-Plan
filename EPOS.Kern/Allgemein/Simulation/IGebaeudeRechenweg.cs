namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Rechenweg eines Gebäudes</b> — die Naht der Weiche in
    /// <see cref="SimulationWaermebedarf.HeizwaermeEinesGebaeudes"/> (Entscheid E20, ADR-006;
    /// Vertrag V16 des Systementwurfs, Softwarearchitektur 1.5).
    ///
    /// <para>Zwei Ausprägungen: der Tagesbilanz-Weg im Modul <c>Altweg/</c> und — ab der
    /// Anbindung in Stufe G1 — der VDI-6007-Weg im Modul <c>Gebaeude/</c>. Die Weiche kennt
    /// allein diese Schnittstelle; keine Ausprägung ruft die andere oder kennt sie
    /// (<c>ModultrennungswacheTests</c>). Schnittstelle und Weiche leben bis zur Stufe GA.</para>
    /// </summary>
    internal interface IGebaeudeRechenweg
    {
        /// <summary>
        /// Rechnet ein Gebäude.
        /// </summary>
        /// <param name="gebaeude">Die Gebäudezeile. Die Fassade hat die Bezugsfläche
        /// (<c>Z_AuswahlWohnflaeche</c>) für diesen Aufruf gesetzt.</param>
        /// <param name="index">Merkplatz des Gebäudes (ab 0), kein Rang.</param>
        /// <param name="ziel">8 760 Stundenwerte in <b>Watt</b>; wird überschrieben.</param>
        /// <param name="gemeinsam">Der gemeinsame Teil des Klimakalenders — mehr bekommt ein
        /// Rechenweg über diese Naht nicht.</param>
        /// <param name="verbrauchAltKwh">Der Jahreswert desselben Aufrufs in kWh, auf der
        /// Fläche, die der Aufruf bekam — Grundlage der Verbrauchs-Rückrechnung (E8).</param>
        /// <returns><c>false</c>, wenn eine Vorbedingung des Rechenwegs fehlt; die Meldung
        /// steht dann im Protokollkanal. Nie eine Ausnahme.</returns>
        bool Rechnen(ProjektGebaeudeModel gebaeude, int index, double[] ziel,
                     KlimakalenderGemeinsam gemeinsam, out double verbrauchAltKwh);
    }
}
