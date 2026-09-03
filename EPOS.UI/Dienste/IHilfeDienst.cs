namespace EPOS.UI.Dienste;

/// <summary>
/// Ein Eintrag des Hilfekatalogs: der kurze Text am Mauszeiger, die
/// ausfuehrliche Beschreibung des Fensters und - wenn vorhanden - die Adresse
/// der Wikiseite.
/// </summary>
/// <param name="Tooltip">Kurztext fuer den Mauszeiger.</param>
/// <param name="Beschreibung">Ausfuehrlicher Text fuer das angeheftete Fenster.</param>
/// <param name="Url">Adresse der Hilfeseite; <c>null</c>, wenn es keine gibt.</param>
public sealed record HilfeEintrag(string Tooltip, string Beschreibung, string? Url);

/// <summary>
/// Die einzige Schnittstelle, ueber die eine Komponente das Hilfesystem
/// erreicht.
///
/// <para>
/// Der Katalog selbst (<c>Allgemein/Hilfe/HelpCatalog.cs</c>, die
/// Zuordnungsdatei <c>help_mapping.txt</c> und das angeheftete Popup) bleibt
/// in der Windows-Huelle. <see cref="EPOS.UI.Bausteine.InfoKnopf"/> kennt nur
/// den Schluessel - dieselbe Zeichenkette, die heute in
/// <c>help_mapping.txt</c> vor dem Gleichheitszeichen steht, etwa
/// <c>Form_Kosten_Auswahl.btn_Help</c>.
/// </para>
/// </summary>
public interface IHilfeDienst
{
    /// <summary>
    /// Sucht den Eintrag zu einem Schluessel, ohne etwas anzuzeigen - fuer
    /// Kurztext und Beschriftung des Knopfes.
    /// </summary>
    /// <returns><c>null</c>, wenn der Katalog den Schluessel nicht kennt.</returns>
    HilfeEintrag? Aufloesen(string schluessel);

    /// <summary>
    /// Zeigt die Hilfe zu einem Schluessel an. Was genau geschieht, entscheidet
    /// die Plattform: unter Windows das angeheftete Hilfefenster, sonst der
    /// Browser. Ein unbekannter Schluessel bleibt folgenlos.
    /// </summary>
    void Oeffnen(string schluessel);
}

/// <summary>
/// Hilfedienst, der nichts kennt und nichts oeffnet.
///
/// <para>
/// Gedacht fuer Tests und fuer Huellen ohne Hilfesystem: Der
/// <see cref="EPOS.UI.Bausteine.InfoKnopf"/> braucht einen registrierten
/// Dienst, soll aber nicht erzwingen, dass jede Huelle einen Katalog
/// mitbringt.
/// </para>
/// </summary>
public sealed class KeineHilfe : IHilfeDienst
{
    /// <summary>Zaehlt die Aufrufe von <see cref="Oeffnen"/> - Pruefhilfe fuer Tests.</summary>
    public int Geoeffnet { get; private set; }

    /// <summary>Der zuletzt an <see cref="Oeffnen"/> uebergebene Schluessel.</summary>
    public string? LetzterSchluessel { get; private set; }

    /// <inheritdoc />
    public HilfeEintrag? Aufloesen(string schluessel) => null;

    /// <inheritdoc />
    public void Oeffnen(string schluessel)
    {
        Geoeffnet++;
        LetzterSchluessel = schluessel;
    }
}
