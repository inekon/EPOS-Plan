namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Angaben EINES Bauteils, wie der Bauteildialog sie prüft</b> (Stufe G3;
    /// Mehrzonenkonzept 5.3) — nur Werte, keine Fachklasse: Die Oberfläche füllt den Satz aus
    /// ihrem Arbeitsstand, und <see cref="GebaeudeZonenCtrl.BauteilPruefen"/> hält die Regeln an
    /// einer Stelle. <c>null</c> heißt „nicht angegeben" (Vorgabe nach Bauteilart bzw. Wert des
    /// Gebäudes), die Persistenzwerte (<c>DbWerte.BAUTEILART_*</c>, <c>DbWerte.RANDBEDINGUNG_*</c>)
    /// kommen als Steuerwerte, nie als Anzeigetext.
    /// </summary>
    /// <param name="Bezeichner">Der Name des Bauteils.</param>
    /// <param name="Bauteilart">Die Bauteilart als Persistenzwert.</param>
    /// <param name="Flaeche">Die Fläche [m²].</param>
    /// <param name="UWert">Der eingetragene U-Wert [W/(m²K)]; <c>null</c> = aus dem Aufbau.</param>
    /// <param name="MitAufbau">Trägt das Bauteil einen Aufbau (aus dem Projekt oder zur Übernahme aus dem Katalog)?</param>
    /// <param name="GWert">g-Wert [–] (Fenster, Vorhangfassade).</param>
    /// <param name="Rahmenanteil">Rahmenanteil [–] (Fenster, Vorhangfassade).</param>
    /// <param name="Verschattung">Verschattungsfaktor [–] (Fenster, Vorhangfassade).</param>
    /// <param name="Azimut">Azimut [°], 0° = Nord.</param>
    /// <param name="Neigung">Neigung [°]; <c>null</c> = nach Bauteilart.</param>
    /// <param name="Randbedingung">Die Randbedingung als Persistenzwert; <c>null</c> = Außenluft, an Innenwand und Decke „innerhalb der Zone".</param>
    /// <param name="PsiL">Wärmebrücke ψ·L [W/K].</param>
    /// <param name="ID_Nachbarzone">Die Nachbarzone einer Trennfläche (Randbedingung <c>ZONE</c>, Stufe G6b);
    /// <c>null</c> = keine.</param>
    /// <param name="TrennflaecheZuordnung">Die Zuordnung einer Trennfläche (<c>DbWerte.TRENNFLAECHE_*</c>);
    /// <c>null</c> = die 4-K-Regel.</param>
    public sealed record Bauteilangabe(string Bezeichner, string Bauteilart, double? Flaeche, double? UWert, bool MitAufbau,
                                       double? GWert, double? Rahmenanteil, double? Verschattung, double? Azimut,
                                       double? Neigung, string Randbedingung, double? PsiL,
                                       int? ID_Nachbarzone = null, string TrennflaecheZuordnung = null);
}
