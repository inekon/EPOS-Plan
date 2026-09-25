using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Summen einer Zone</b> für Summenfuß, Zonenliste und die abgeleitete Hülle des
/// Gebäudedialogs (Summenregel, Mehrzonenkonzept 4.3) — eine dünne Hülle um die EINE Formel des
/// Kerns (<see cref="Zonenkennwerte"/>, Stufe G6a; die Gruppierung der Bauteile in
/// <see cref="Gebaeudehuellbilanz.Zonenzeilen"/>): Hier werden nur die Werte des Arbeitsstands
/// hineingereicht.
/// </summary>
public static class Zonensummen
{
    /// <summary>Die Bauteile der Zone, wie der Kern sie liest — U wirksam (eingetragen, sonst aus dem Aufbau).</summary>
    public static IEnumerable<Zonenbauteil> Bauteile(ZoneDaten zone)
        => (zone?.Bauteile ?? new List<BauteilDaten>())
               .Select(b => new Zonenbauteil(b.Bauteilart, b.Randbedingung!, b.Flaeche ?? 0.0, b.UWirksam, b.PsiL));

    /// <summary>
    /// Die Kennwerte der Zone (<see cref="Zonenkennwerte.Bilden(double?, double?, double?, IEnumerable{Zonenbauteil}, double?, double?, double?)"/>)
    /// mit den Werten des Gebäudes: Nutzfläche [m²], Raumhöhe [m] und dem Luftwechsel des Rechenwegs [1/h].
    /// </summary>
    public static Zonenkennwerte Kennwerte(ZoneDaten zone, double? nutzflaecheGebaeude, double? raumhoeheGebaeude, double? luftwechsel)
        => Zonenkennwerte.Bilden(zone?.Nutzflaeche, null, null, Bauteile(zone!), nutzflaecheGebaeude, raumhoeheGebaeude, luftwechsel);

    /// <summary>Die fünf Transmissionsgruppen der Zone (Σ A, U = Σ U·A / Σ A).</summary>
    public static IReadOnlyList<Huellzeile> Zeilen(ZoneDaten zone)
        => Gebaeudehuellbilanz.Zonenzeilen((zone?.Bauteile ?? new List<BauteilDaten>())
               .Select(b => (b.Bauteilart, b.Randbedingung!, b.Flaeche ?? 0.0, b.UWirksam)));

    /// <summary>Σ ψ·L der Bauteile [W/K].</summary>
    public static double PsiL(ZoneDaten zone)
        => (zone?.Bauteile ?? new List<BauteilDaten>()).Sum(b => b.PsiL ?? 0.0);

    /// <summary>H_T = Σ U·A + Σ ψ·L [W/K] — aus <see cref="Zonenkennwerte"/>.</summary>
    public static double HT(ZoneDaten zone) => Kennwerte(zone, null, null, null).HT;

    /// <summary>Die Fensterfläche der Zone [m²] — die Gruppe Fenster (Fenster und Vorhangfassaden).</summary>
    public static double Fensterflaeche(ZoneDaten zone)
        => Zeilen(zone).FirstOrDefault(z => z.Bauteil == Huellbauteil.Fenster)?.Groesse ?? 0.0;

    /// <summary>
    /// Die fünf Transmissionsgruppen ALLER Zonen (Stufe G6a, Summenregel über die Liste): die Bauteile
    /// aller Zonen in eine Gruppierung — Σ A je Gruppe, U = Σ U·A / Σ A. Gilt, solange keine Zone an
    /// eine Nachbarzone grenzt (Trennflächen kommen mit G6b).
    /// </summary>
    public static IReadOnlyList<Huellzeile> Zeilen(IEnumerable<ZoneDaten> zonen)
        => Gebaeudehuellbilanz.Zonenzeilen((zonen ?? Array.Empty<ZoneDaten>()).SelectMany(z => z.Bauteile)
               .Select(b => (b.Bauteilart, b.Randbedingung!, b.Flaeche ?? 0.0, b.UWirksam)));

    /// <summary>Σ ψ·L der Bauteile aller Zonen [W/K].</summary>
    public static double PsiL(IEnumerable<ZoneDaten> zonen)
        => (zonen ?? Array.Empty<ZoneDaten>()).Sum(z => PsiL(z));

    /// <summary>Die Fensterfläche aller Zonen [m²].</summary>
    public static double Fensterflaeche(IEnumerable<ZoneDaten> zonen)
        => Zeilen(zonen).FirstOrDefault(z => z.Bauteil == Huellbauteil.Fenster)?.Groesse ?? 0.0;

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
