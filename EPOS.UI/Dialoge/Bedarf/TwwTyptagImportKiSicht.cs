namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild des Dialogs „VDI-4655-Typtage" für den Hilfe-Assistenten (Umsetzungskonzept
/// Zapfprofilgenerator 4.2, Kapitel 6; Stufe Z4b, Gruppe 2) — Muster
/// <c>TwwNutzungsartAdminKiSicht</c>.
///
/// <para><b>Die Maske führt keinen Einstellwert</b>: Der Dialog zeigt den eingespielten Stand und
/// den Bericht der Prüfung; geschrieben wird über „Einspielen" und „Löschen" — Handlungen, die
/// Zeilen anlegen oder wegnehmen und Klicks des Anwenders bleiben (KI‑D‑Q11). Auch die Paketwahl
/// bleibt beim Anwender: Ein Pfad ist keine Eingabe, die ein Assistent setzen darf. Freigegeben
/// sind deshalb allein Anzeigen, damit <c>dialog_lesen</c> nennt, was eingespielt ist und was die
/// Prüfung ergeben hat.</para>
///
/// <para><b>Keine Werte der Richtlinie</b>: Quelle, Ausgabe, Tag des Einspielens, Zonen,
/// Gebäudearten und die Zahl der Zeilen sagen, was eingespielt IST — nie, wie groß ein Faktor ist
/// (Konzept Kapitel 6).</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class TwwTyptagImportKiSicht
{
    public Func<string>? QuelleLesen { get; init; }
    public Func<string>? AusgabeLesen { get; init; }
    public Func<string>? DatumLesen { get; init; }
    public Func<string>? ZonenLesen { get; init; }
    public Func<string>? GebaeudeartenLesen { get; init; }
    public Func<int>? ZeilenLesen { get; init; }
    public Func<string>? GrundLesen { get; init; }
    public Func<string>? BerichtLesen { get; init; }

    /// <summary>Die Quelle des eingespielten Pakets — Anzeige; leer = nichts eingespielt.</summary>
    public string Quelle => QuelleLesen?.Invoke() ?? "";

    /// <summary>Die Ausgabe des eingespielten Pakets — Anzeige; leer = ohne Angabe.</summary>
    public string Ausgabe => AusgabeLesen?.Invoke() ?? "";

    /// <summary>Der Tag des Einspielens — Anzeige.</summary>
    public string Importdatum => DatumLesen?.Invoke() ?? "";

    /// <summary>Die Klimazonen des Pakets als Liste — Anzeige.</summary>
    public string Klimazonen => ZonenLesen?.Invoke() ?? "";

    /// <summary>Die Gebäudearten des Pakets als Liste — Anzeige.</summary>
    public string Gebaeudearten => GebaeudeartenLesen?.Invoke() ?? "";

    /// <summary>Die Zahl der eingespielten Zeilen — Anzeige.</summary>
    public int Zeilen => ZeilenLesen?.Invoke() ?? 0;

    /// <summary>Warum der Typtagweg nicht verfügbar ist — Anzeige; leer = verfügbar.</summary>
    public string Grund => GrundLesen?.Invoke() ?? "";

    /// <summary>Was die Prüfung des gewählten Pakets ergeben hat — Anzeige; leer = keine Prüfung.</summary>
    public string Pruefbericht => BerichtLesen?.Invoke() ?? "";
}
