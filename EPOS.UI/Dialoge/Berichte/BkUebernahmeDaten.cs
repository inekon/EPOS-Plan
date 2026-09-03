using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Berichte;

/// <summary>
/// Eine wählbare Quelle der Übernahme — das Stammprojekt oder eine andere
/// Variante derselben Gruppe (iU9-W5.1, Vorbild
/// <c>Form_BkUebernahme.Quelle</c>).
/// </summary>
public sealed class UebernahmeQuelle
{
    /// <summary><c>Tab_Projekt.ID</c> der Quelle.</summary>
    public int Id { get; set; }

    /// <summary>Anzeigetext der Quelle (fertig formatiert von der Hülle).</summary>
    public string Anzeige { get; set; } = "";
}

/// <summary>
/// Was die Hülle zu einer gewählten Quelle liefert (Vorbild
/// <c>Form_BkUebernahme.Vorschau</c>).
///
/// <para><b>Der Dialog rechnet nicht.</b> Bei jedem Wechsel der Quelle ruft er
/// den Lader; ob die Übernahme trägt, entscheiden
/// <c>MerkmalUebernahmeCtrl</c> und <c>KomponentenUebernahmeCtrl</c> im Kern —
/// unverändert zum Vorläufer.</para>
/// </summary>
public sealed class UebernahmeVorschau
{
    /// <summary><c>false</c> = aus dieser Quelle nicht möglich; dann gilt <see cref="Grund"/>.</summary>
    public bool Moeglich { get; set; }

    /// <summary>Warum die Übernahme gesperrt ist (leer = möglich).</summary>
    public string Grund { get; set; } = "";

    /// <summary>Wertgegenüberstellung — der Wert der Quelle (Merkmals-Übernahme).</summary>
    public string WertQuelle { get; set; } = "";

    /// <summary>Wertgegenüberstellung — der Wert des Ziels.</summary>
    public string WertZiel { get; set; } = "";

    /// <summary>Betroffene Zeile(n), z. B. „Quellkomponente → Zielkomponente".</summary>
    public string Komponenten { get; set; } = "";

    /// <summary>Mehrzeilige Zusammenfassung (Komponenten-Übernahme).</summary>
    public string Klartext { get; set; } = "";
}

/// <summary>
/// Das Ergebnis des Dialogs: die gewählte Quelle. <c>null</c> = abgebrochen.
/// </summary>
/// <param name="QuelleId"><c>Tab_Projekt.ID</c> der gewählten Quelle.</param>
public sealed record BkUebernahmeErgebnis(int QuelleId);

/// <summary>Bequemlichkeit: eine leere Quellenliste.</summary>
public static class UebernahmeQuellen
{
    /// <summary>Die leere Liste — ohne Quelle bleibt der Dialog gesperrt.</summary>
    public static readonly IReadOnlyList<UebernahmeQuelle> Keine = new UebernahmeQuelle[0];
}
