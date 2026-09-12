namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Ein gespeichertes Quellprofil, so weit die Auswahlliste es braucht.
/// </summary>
/// <param name="Id">Primärschlüssel; <c>0</c> ist der Eintrag „&lt;neues Profil&gt;".</param>
/// <param name="Anzeige">Der Listentext (<c>QuellprofilCtrl.Kopf.ToString</c>).</param>
public sealed record QuellprofilZeile(int Id, string Anzeige);

/// <summary>
/// Kopf und Werte EINES Quellprofils — was der Dialog beim Auswählen lädt und beim
/// Speichern hinausgibt.
/// </summary>
/// <param name="Bezeichner">Pflichtangabe; ohne sie meldet der Dialog.</param>
/// <param name="Beschreibung">Freitext.</param>
/// <param name="Betriebsart">Steuerwert: Monat, Tag oder Stunde.</param>
/// <param name="Werte">
/// 12, 365 oder 8 760 Zahlen — je Betriebsart. <c>null</c> heißt „noch keine".
/// </param>
public sealed record QuellprofilInhalt(string Bezeichner, string Beschreibung,
                                       string Betriebsart, double[]? Werte);

/// <summary>
/// Die Übergabefelder des Quellprofil-Dialogs (Vermessung §5).
///
/// <para><b>Der Dialog SPEICHERT selbst</b> (<c>btnOk_Click</c>:1020) — anders als die
/// übrigen Quellendialoge, die nur entscheiden. Herauskommt deshalb nur die
/// <c>ID_Quellprofil</c>, die der Aufrufer an die Anlage schreibt.</para>
/// </summary>
public sealed record QuellprofilDaten
{
    /// <summary>Name der Anlage — er steht im Titel.</summary>
    public string WPName { get; init; } = "";

    /// <summary>Das Projekt, dessen Profile zur Wahl stehen.</summary>
    public int IdProjekt { get; init; }

    /// <summary>Das vorgewählte Profil; <c>0</c> = „neues Profil".</summary>
    public int IdQuellprofil { get; init; }

    /// <summary>
    /// Die zwölf Monatswerte aus dem ALTWEG <c>WQ_Monatswerte</c> — Vorbelegung,
    /// solange die Anlage noch kein Quellprofil führt. <c>null</c> = keine.
    /// </summary>
    public double[]? Monatswerte { get; init; }

    /// <summary>
    /// Die 168 Wochenwerte aus dem ALTWEG <c>WQ_Wochenwerte</c> — NUR ANZEIGE
    /// (Befund W10‑B17). <c>null</c> heißt „kein Wochengang"; dann fehlt der
    /// Altweg-Reiter ganz.
    /// </summary>
    public double[]? Wochenwerte { get; init; }
}
