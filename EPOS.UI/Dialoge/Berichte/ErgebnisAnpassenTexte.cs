namespace EPOS.UI.Dialoge.Berichte;

/// <summary>
/// Die Beschriftungen des Dialogs „Anzeige anpassen…“ im Abschnitt „Ergebnisse — Energie“ der Übersicht; jede
/// Eigenschaft nennt ihren Ressourcenschlüssel, der Rückfall ist der deutsche Text. Kein Zustand.
/// </summary>
public sealed class ErgebnisAnpassenTexte
{
    /// <summary>Der Knopf im Kopf des Abschnitts (<c>BK_ERG_ANP_KNOPF</c>).</summary>
    public string Knopf { get; init; } = "Anzeige anpassen…";

    /// <summary>Der Titel der Überlagerung (<c>BK_ERG_ANP_TITEL</c>).</summary>
    public string Titel { get; init; } = "Anzeige anpassen";

    /// <summary>Der Satz über den Gruppen (<c>BK_ERG_ANP_HINWEIS</c>).</summary>
    public string Hinweis { get; init; } =
        "Legen Sie fest, welche Gruppen offen, zugeklappt oder ausgeblendet erscheinen und welche Kennzahlen die Tabelle zeigt. Die Einstellung gilt für alle Projekte.";

    /// <summary>Der Zustand „offen“ (<c>BK_ERG_ANP_OFFEN</c>).</summary>
    public string Offen { get; init; } = "offen";

    /// <summary>Der Zustand „zugeklappt“ (<c>BK_ERG_ANP_ZUGEKLAPPT</c>).</summary>
    public string Zugeklappt { get; init; } = "zugeklappt";

    /// <summary>Der Zustand „ausgeblendet“ (<c>BK_ERG_ANP_AUSGEBLENDET</c>).</summary>
    public string Ausgeblendet { get; init; } = "ausgeblendet";

    /// <summary>Der Titel der aufklappbaren Kennzahlenliste je Gruppe (<c>BK_ERG_ANP_KENNZAHLEN</c>).</summary>
    public string Kennzahlen { get; init; } = "Kennzahlen";

    /// <summary>Der Zusatz der Kennzahlenliste, {0} gezeigt von {1} (<c>BK_ERG_ANP_KENNZAHLEN_ANZAHL</c>).</summary>
    public string KennzahlenAnzahl { get; init; } = "{0} von {1} gezeigt";

    /// <summary>Der Schalter „ohne Wert“ (<c>BK_ERG_ANP_OHNE_WERT</c>).</summary>
    public string OhneWert { get; init; } = "Zeilen ohne Wert ausblenden";

    /// <summary>Die Erklärung unter dem Schalter (<c>BK_ERG_ANP_OHNE_WERT_ERKL</c>).</summary>
    public string OhneWertErklaerung { get; init; } = "Gemeint sind Zeilen, deren Wert in allen gezeigten Versionen 0 oder leer ist.";

    /// <summary>Der Knopf, der die Vorgabe setzt (<c>BK_ERG_ANP_STANDARD</c>).</summary>
    public string Standard { get; init; } = "Standard wiederherstellen";

    /// <summary>Der Hinweis unter der Tabelle, {0} Kennzahlen (<c>BK_ERG_ANP_OHNE_WERT_HINWEIS</c>).</summary>
    public string OhneWertHinweis { get; init; } = "{0} Kennzahlen ohne Wert ausgeblendet";

    /// <summary>Der Hinweis unter der Tabelle in der Einzahl (<c>BK_ERG_ANP_OHNE_WERT_HINWEIS_EINS</c>).</summary>
    public string OhneWertHinweisEins { get; init; } = "1 Kennzahl ohne Wert ausgeblendet";

    /// <summary>„OK“ (<c>SIM_BTN_OK</c>).</summary>
    public string Ok { get; init; } = "OK";

    /// <summary>„Abbrechen“ (<c>SIM_BTN_ABBRECHEN</c>).</summary>
    public string Abbrechen { get; init; } = "Abbrechen";
}
