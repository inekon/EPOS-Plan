using EPOS.UI.Dienste;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Admin;

/// <summary>
/// Das Abbild der Programmeinstellungen für den Hilfe-Assistenten (Welle #458, Stufe 2)
/// — <see cref="EinstellungenDialog"/>.
///
/// <para><b>Eine Sichtklasse ist Pflicht:</b> Der Wertesatz <see cref="Einstellungensatz"/>
/// führt öffentliche FELDER und keine Eigenschaften, und die Reflection der Anmeldung
/// liest nur Eigenschaften. Diese Klasse reicht die sechs setzbaren Werte als
/// Eigenschaften durch — immer an den LEBENDEN Arbeitsstand des Dialogs, den
/// „Standardwerte" austauscht.</para>
///
/// <para><b>Die Diagrammfarben sind eine FELDTAFEL</b> (<see cref="IKiFeldtafel"/>): Je
/// Farbrolle ein Feld, der Schlüssel ist der sprachneutrale Rollenname
/// (<c>WAERME_WP</c>). Die Feldkarte erzeugt der Kern aus derselben Rollenliste, aus der
/// die Hülle die Rubrik „Diagramme" füllt (<c>Diagrammfarben.Gruppen</c>) — keine zweite
/// Liste von Hand. Gesetzt wird ein Farbton als <c>#RRGGBB</c>; alles andere lehnt die
/// Sicht benannt ab, wie das Farbfeld eine Fehleingabe nicht übernimmt.</para>
///
/// <para><b>Was draußen bleibt:</b> die fünf Ordner (Dateiwahlen), der Name der
/// Datenbank (ein Datenbankwechsel beim nächsten Start) und der Abschalter des
/// Assistenten selbst.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jeder Zugriff ruft die Delegaten des Dialogs.</para>
/// </summary>
public sealed class EinstellungenKiSicht : IKiFeldtafel
{
    /// <summary>Der Arbeitsstand des Dialogs — er wechselt mit „Standardwerte".</summary>
    public Func<Einstellungensatz?>? Satz { get; init; }

    /// <summary>Die eingestellte Farbe einer Rolle (<c>#RRGGBB</c>); <c>null</c> = keine solche Rolle.</summary>
    public Func<string, string?>? FarbeLesen { get; init; }

    /// <summary>Setzt die Farbe einer Rolle — derselbe Weg wie das Farbfeld.</summary>
    public Action<string, string>? FarbeSetzen { get; init; }

    /// <summary>Wird nach jedem Setzen gerufen — der Dialog frischt seine Meldung auf.</summary>
    public Action? Gesetzt { get; init; }

    // =====================================================================
    //  Die sechs benannten Werte
    // =====================================================================

    public string WikiUrl
    {
        get => Satz?.Invoke()?.WikiUrl ?? "";
        set => Schreiben(s => s.WikiUrl = value ?? "");
    }

    public string GeokodierungUrl
    {
        get => Satz?.Invoke()?.GeokodierungUrl ?? "";
        set => Schreiben(s => s.GeokodierungUrl = value ?? "");
    }

    public string PvgisUrl
    {
        get => Satz?.Invoke()?.PvgisUrl ?? "";
        set => Schreiben(s => s.PvgisUrl = value ?? "");
    }

    public string TryPortalUrl
    {
        get => Satz?.Invoke()?.TryPortalUrl ?? "";
        set => Schreiben(s => s.TryPortalUrl = value ?? "");
    }

    public string TryRegionalUrl
    {
        get => Satz?.Invoke()?.TryRegionalUrl ?? "";
        set => Schreiben(s => s.TryRegionalUrl = value ?? "");
    }

    /// <summary>„Neue Projekte mit Kühlung anlegen" — der Anfangswert neuer Projekte.</summary>
    public bool NeueProjekteMitKuehlung
    {
        get => Satz?.Invoke()?.NeueProjekteMitKuehlung ?? false;
        set => Schreiben(s => s.NeueProjekteMitKuehlung = value);
    }

    private void Schreiben(Action<Einstellungensatz> weg)
    {
        Einstellungensatz? satz = Satz?.Invoke();
        if (satz is null) return;
        weg(satz);
        Gesetzt?.Invoke();
    }

    // =====================================================================
    //  Die Feldtafel — eine Farbe je Rolle
    // =====================================================================

    /// <inheritdoc/>
    public object? Lesen(string schluessel) => FarbeLesen?.Invoke(schluessel);

    /// <inheritdoc/>
    /// <remarks>
    /// Ein Farbton ist <c>#RRGGBB</c> — dieselbe Regel wie im Farbfeld und in der
    /// Ablage (<c>Diagrammfarben.IstHex</c>); die Deckung gehört zum Bildaufbau und wird
    /// nicht eingestellt. Eine unbekannte Rolle nimmt nichts an.
    /// </remarks>
    public void Setzen(string schluessel, object? wert)
    {
        if (FarbeLesen?.Invoke(schluessel) is null) return;

        string text = (Convert.ToString(wert, System.Globalization.CultureInfo.InvariantCulture) ?? "").Trim();
        if (!WindowsFormsApplication1.Zeichnung.Diagrammfarben.IstHex(text))
            throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.KI_DLG_ADMSET_FARBE_UNGUELTIG, text));

        FarbeSetzen?.Invoke(schluessel, text.ToUpperInvariant());
        Gesetzt?.Invoke();
    }
}
