namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild des Dialogs „Messdaten" für den Hilfe-Assistenten (Umsetzungskonzept
/// Zapfprofilgenerator 4.8, Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 3) — Muster
/// <c>TwwTyptagImportKiSicht</c>.
///
/// <para><b>Die Maske führt keinen Einstellwert</b>: Der Dialog zeigt die eingespielten Reihen und
/// den Bericht der Prüfung; geschrieben wird über „Einspielen" und „Löschen" — Handlungen, die
/// Zeilen anlegen oder wegnehmen und Klicks des Anwenders bleiben (KI‑D‑Q11). Auch die
/// <b>Dateiwahl</b> bleibt beim Anwender: Ein Pfad ist keine Eingabe, die ein Assistent setzen
/// darf, und die vier Eingaben über der Dateiwahl (Bezeichnung, Quelle, Größe, Lückenschwelle,
/// Zeitstempel) beschreiben allein die gewählte Datei — ohne sie sind sie ohne Sinn. Freigegeben
/// sind deshalb allein Anzeigen, damit <c>dialog_lesen</c> nennt, was eingespielt ist und was die
/// Prüfung ergeben hat.</para>
///
/// <para><b>Keine Messwerte</b>: Bezeichnung, Größe, Raster, Beginn, Tage, Nullläufe, Quelle und
/// Tag des Einspielens sagen, WAS eingespielt ist — nie, wie groß eine gemessene Menge oder eine
/// gemessene Spitze ist (Konzept Kapitel 9 K5).</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class TwwMessreihenKiSicht
{
    public Func<int>? AnzahlLesen { get; init; }
    public Func<string>? ReihenLesen { get; init; }
    public Func<string>? GewaehltLesen { get; init; }
    public Func<string>? GroesseLesen { get; init; }
    public Func<int>? AufloesungLesen { get; init; }
    public Func<string>? BeginnLesen { get; init; }
    public Func<double>? TageLesen { get; init; }
    public Func<int>? NulllaeufeLesen { get; init; }
    public Func<string>? QuelleLesen { get; init; }
    public Func<string>? ImportdatumLesen { get; init; }
    public Func<string>? GrundLesen { get; init; }
    public Func<string>? BerichtLesen { get; init; }

    /// <summary>Die Zahl der eingespielten Messreihen des Projekts — Anzeige.</summary>
    public int Anzahl => AnzahlLesen?.Invoke() ?? 0;

    /// <summary>Die Bezeichnungen der eingespielten Reihen als Liste — Anzeige.</summary>
    public string Reihen => ReihenLesen?.Invoke() ?? "";

    /// <summary>Die Bezeichnung der gewählten Reihe — Anzeige; leer = keine gewählt.</summary>
    public string Gewaehlt => GewaehltLesen?.Invoke() ?? "";

    /// <summary>Die gemessene Größe der gewählten Reihe — Anzeige.</summary>
    public string Groesse => GroesseLesen?.Invoke() ?? "";

    /// <summary>Das Zeitraster der gewählten Reihe [min] — Anzeige.</summary>
    public int AufloesungMin => AufloesungLesen?.Invoke() ?? 0;

    /// <summary>Der Beginn der gewählten Reihe — Anzeige.</summary>
    public string Beginn => BeginnLesen?.Invoke() ?? "";

    /// <summary>Die Länge der gewählten Reihe in Tagen — Anzeige.</summary>
    public double Tage => TageLesen?.Invoke() ?? 0.0;

    /// <summary>Die Zahl der Nullläufe der gewählten Reihe — Anzeige.</summary>
    public int Nulllaeufe => NulllaeufeLesen?.Invoke() ?? 0;

    /// <summary>Die Quelle der gewählten Reihe — Anzeige.</summary>
    public string Quelle => QuelleLesen?.Invoke() ?? "";

    /// <summary>Der Tag des Einspielens der gewählten Reihe — Anzeige.</summary>
    public string Importdatum => ImportdatumLesen?.Invoke() ?? "";

    /// <summary>Warum es keine Reihe gibt — Anzeige; leer = es stehen Reihen da.</summary>
    public string Grund => GrundLesen?.Invoke() ?? "";

    /// <summary>Was die Prüfung der gewählten Datei ergeben hat — Anzeige; leer = keine Prüfung.</summary>
    public string Pruefbericht => BerichtLesen?.Invoke() ?? "";
}
