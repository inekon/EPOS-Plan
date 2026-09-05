namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Eine Stützstelle der Wärmepumpen-Kennlinie im EDITOR (iU9-W7.2) — eine Zeile aus
/// <c>Tab_Kenndaten_STAMM</c>.
///
/// <para><b>Warum ein eigener Typ.</b> Im Bestand steht dort eine <c>DataRow</c> eines
/// <c>DataSet</c>, und der Kern führt dafür <c>KenndatenModel</c>. Beide kommen für die
/// Komponente nicht in Frage: Ein <c>DataSet</c> ist Datenbankwerkzeug, und
/// <c>KenndatenModel</c> ist eine Fachklasse des Kerns, die eine Razor-Komponente nicht
/// kennt (<c>EPOS.UI/CLAUDE.md</c>) — sie ist dort obendrein <c>internal</c>. Die Hülle
/// übersetzt zwischen beiden.</para>
///
/// <para><b>Veränderlich, mit Absicht.</b> Der Editor bearbeitet die Zeilen an Ort und
/// Stelle; ein Record mit <c>with</c> müsste die Liste bei jedem Tastendruck neu
/// aufbauen. Die Hülle gibt eine KOPIE herein — abgebrochen wird, indem sie verworfen
/// wird.</para>
/// </summary>
public sealed class KennlinienZeile
{
    /// <summary>
    /// Der Primärschlüssel aus <c>Tab_Kenndaten_STAMM</c>. <b>0 heißt neu</b> — genau
    /// das, was der Vorläufer über <c>DataRowState.Added</c> erkannte.
    /// </summary>
    public int Id { get; set; }

    /// <summary>Vorlauftemperatur [°C] — sie gruppiert die Zeilen.</summary>
    public int Vorlauf { get; set; }

    /// <summary>Außentemperatur [°C]; <c>null</c> = leeres Feld.</summary>
    public int? Temperatur { get; set; }

    /// <summary>Leistungszahl; <c>null</c> = leeres Feld.</summary>
    public double? Cop { get; set; }

    /// <summary>Wärmeleistung [kW]; <c>null</c> = leeres Feld.</summary>
    public double? Ptherm { get; set; }

    /// <summary>Eine wortgleiche Kopie — die Hülle reicht dem Editor nie ihr Original.</summary>
    public KennlinienZeile Kopie() => new()
    {
        Id = Id,
        Vorlauf = Vorlauf,
        Temperatur = Temperatur,
        Cop = Cop,
        Ptherm = Ptherm
    };
}
