using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Summen einer Zone</b> für Summenfuß, Zonenliste und die abgeleitete Hülle des
/// Gebäudedialogs (Summenregel, Mehrzonenkonzept 4.3). Die Gruppierung der Bauteile steht im Kern
/// (<see cref="Gebaeudehuellbilanz.Zonenzeilen"/>); hier werden nur die Werte des Arbeitsstands
/// hineingereicht und Σ ψ·L dazugezählt.
/// </summary>
public static class Zonensummen
{
    /// <summary>Die fünf Transmissionsgruppen der Zone (Σ A, U = Σ U·A / Σ A).</summary>
    public static IReadOnlyList<Huellzeile> Zeilen(ZoneDaten zone)
        => Gebaeudehuellbilanz.Zonenzeilen((zone?.Bauteile ?? new List<BauteilDaten>())
               .Select(b => (b.Bauteilart, b.Randbedingung!, b.Flaeche ?? 0.0, b.UWirksam)));

    /// <summary>Σ ψ·L der Bauteile [W/K].</summary>
    public static double PsiL(ZoneDaten zone)
        => (zone?.Bauteile ?? new List<BauteilDaten>()).Sum(b => b.PsiL ?? 0.0);

    /// <summary>H_T = Σ U·A + Σ ψ·L [W/K].</summary>
    public static double HT(ZoneDaten zone) => Gebaeudehuellbilanz.TransmissionWK(Zeilen(zone)) + PsiL(zone);

    /// <summary>Die Fensterfläche der Zone [m²] — die Gruppe Fenster (Fenster und Vorhangfassaden).</summary>
    public static double Fensterflaeche(ZoneDaten zone)
        => Zeilen(zone).FirstOrDefault(z => z.Bauteil == Huellbauteil.Fenster)?.Groesse ?? 0.0;

    /// <summary>Der Anzeigetext einer Gruppe — die Bauteilart gleichen Namens aus dem Kern.</summary>
    public static string Gruppentext(Huellbauteil gruppe) => gruppe switch
    {
        Huellbauteil.Aussenwand => BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_AUSSENWAND),
        Huellbauteil.Fenster => BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_FENSTER),
        Huellbauteil.Dach => BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_DACH),
        Huellbauteil.Bodenplatte => BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_BODENPLATTE),
        _ => BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_SONSTIGES)
    };
}
