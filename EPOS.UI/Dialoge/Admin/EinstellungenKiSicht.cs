using EPOS.UI.Dienste;
using KiKern;
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
/// <para><b>BV-E1:</b> Firma und Vorlagenordner des Abschnitts „Bericht" gehen über
/// <see cref="BerichtFirma"/> und <see cref="BerichtVorlagenordner"/> — an den Arbeitsstand
/// des Dialogs, nicht an den Wertesatz; BV-E2 dazu das Logo über <see cref="BerichtLogo"/>.
/// Die zwei Vorgaben der Installation sind WAHLFELDER (<see cref="BerichtVorgabeWord"/> samt
/// <see cref="BerichtVorgabeWordWahl"/>, <see cref="BerichtVorgabeExcel"/> samt
/// <see cref="BerichtVorgabeExcelWahl"/>).</para>
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
    //  BV-E1: der Abschnitt „Bericht" (Konzept Berichtsvorlagen 10.3)
    //
    //  Beide Werte stehen NICHT im Wertesatz des Kerns, sondern im eigenen Arbeitsstand
    //  des Dialogs; hinaus gehen sie im OK-Weg wie die übrigen. Ein Setzen, das der
    //  Anwender nicht könnte (Abschnitt fehlt, Ordnerwahl gesperrt), lehnt der Dialog
    //  benannt ab. Die Feldkarte des Kerns zieht die zwei Felder nach.
    // =====================================================================

    /// <summary>Liest die Firma aus dem Arbeitsstand.</summary>
    public Func<string>? BerichtFirmaLesen { get; init; }

    /// <summary>Setzt die Firma im Arbeitsstand; wirft mit Grund, wo es nicht geht.</summary>
    public Action<string>? BerichtFirmaSetzen { get; init; }

    /// <summary>Liest den Vorlagenordner aus dem Arbeitsstand.</summary>
    public Func<string>? BerichtVorlagenordnerLesen { get; init; }

    /// <summary>Setzt den Vorlagenordner im Arbeitsstand; wirft mit Grund, wo die Wahl gesperrt ist.</summary>
    public Action<string>? BerichtVorlagenordnerSetzen { get; init; }

    /// <summary>Die Firma für <c>{{ersteller.firma}}</c> im Bericht.</summary>
    public string BerichtFirma
    {
        get => BerichtFirmaLesen?.Invoke() ?? "";
        set
        {
            BerichtFirmaSetzen?.Invoke(value ?? "");
            Gesetzt?.Invoke();
        }
    }

    /// <summary>Der Ordner der eigenen Berichtsvorlagen.</summary>
    public string BerichtVorlagenordner
    {
        get => BerichtVorlagenordnerLesen?.Invoke() ?? "";
        set
        {
            BerichtVorlagenordnerSetzen?.Invoke(value ?? "");
            Gesetzt?.Invoke();
        }
    }

    /// <summary>BV-E2: Liest das Logo (Dateipfad) aus dem Arbeitsstand.</summary>
    public Func<string>? BerichtLogoLesen { get; init; }

    /// <summary>BV-E2: Setzt das Logo im Arbeitsstand; wirft mit Grund, wo es das Feld nicht gibt.</summary>
    public Action<string>? BerichtLogoSetzen { get; init; }

    /// <summary>Das Firmenlogo für die Kopfzeile des Berichts — der Pfad einer PNG- oder JPEG-Datei, leer = ohne Logo.</summary>
    public string BerichtLogo
    {
        get => BerichtLogoLesen?.Invoke() ?? "";
        set
        {
            BerichtLogoSetzen?.Invoke(value ?? "");
            Gesetzt?.Invoke();
        }
    }

    // =====================================================================
    //  Die Vorgaben der Installation (BerichtVorlageWord, BerichtVorlageExcel)
    //
    //  Zwei WAHLFELDER: Die Einträge sind die der zwei Auswahlfelder der Rubrik — dieselben
    //  Listen, die auch die Berichtsseite zeigt, samt dem gesperrten Eintrag einer Vorlage, die es
    //  nicht mehr gibt. Gesetzt wird in den Arbeitsstand des Dialogs; geschrieben wird im OK-Weg.
    //  Ohne Feld und bei einem gesperrten Eintrag lehnt der Dialog benannt ab.
    // =====================================================================

    /// <summary>Liest die vorgegebene Word-Vorlage (<c>Vorlagenzeile.Id</c>); <c>null</c> = keine.</summary>
    public Func<int?>? BerichtVorgabeWordLesen { get; init; }

    /// <summary>Setzt die vorgegebene Word-Vorlage im Arbeitsstand; wirft mit Grund, wo es nicht geht.</summary>
    public Action<int?>? BerichtVorgabeWordSetzen { get; init; }

    /// <summary>Die wählbaren Word-Vorlagen als Wahleinträge.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BerichtVorgabeWordEintraege { get; init; }

    /// <summary>Liest die vorgegebene Excel-Vorlage (<c>Vorlagenzeile.Id</c>); <c>null</c> = keine.</summary>
    public Func<int?>? BerichtVorgabeExcelLesen { get; init; }

    /// <summary>Setzt die vorgegebene Excel-Vorlage im Arbeitsstand; wirft mit Grund, wo es nicht geht.</summary>
    public Action<int?>? BerichtVorgabeExcelSetzen { get; init; }

    /// <summary>Die wählbaren Excel-Vorlagen als Wahleinträge.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BerichtVorgabeExcelEintraege { get; init; }

    /// <summary>Die wählbaren Word-Vorlagen der Vorgabe.</summary>
    public IReadOnlyList<KiWahleintrag> BerichtVorgabeWordWahl
        => BerichtVorgabeWordEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Mit welcher Word-Vorlage ein Bericht ohne eigene Wahl des Stammprojekts entsteht.</summary>
    public int? BerichtVorgabeWord
    {
        get => BerichtVorgabeWordLesen?.Invoke();
        set
        {
            BerichtVorgabeWordSetzen?.Invoke(value);
            Gesetzt?.Invoke();
        }
    }

    /// <summary>Die wählbaren Excel-Vorlagen der Vorgabe.</summary>
    public IReadOnlyList<KiWahleintrag> BerichtVorgabeExcelWahl
        => BerichtVorgabeExcelEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Aus welcher Excel-Vorlage die Mappe ohne eigene Wahl des Stammprojekts entsteht.</summary>
    public int? BerichtVorgabeExcel
    {
        get => BerichtVorgabeExcelLesen?.Invoke();
        set
        {
            BerichtVorgabeExcelSetzen?.Invoke(value);
            Gesetzt?.Invoke();
        }
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
