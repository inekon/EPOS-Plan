namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// EINE Zeile der Senkenliste — die Blazor-Fassung von <c>Z_AnlageSenkeModel</c>
/// (Paket S1, Konzept 5.1). Der Rang ist die Listenposition und steht deshalb NICHT
/// darin; er wird beim Speichern festgeschrieben.
///
/// <para><b>Die Ersatzwerte −1/−2/−3 sind weg (Befund W10‑B24).</b> Der Vorläufer
/// kodierte Eingabefehler in DATENfeldern des Modells: <c>Ladegrenze = -1</c> hieß
/// „unlesbar", <c>-2</c> „außerhalb", und bei der Einspeisehöhe begann dieselbe Reihe
/// erst bei <c>-2</c>, weil <c>-1</c> schon „nicht gesetzt" bedeutete. Hier ist
/// „nicht gesetzt" ein <c>null</c>, und ein Eingabefehler bleibt im Formularzustand,
/// wo er hingehört.</para>
/// </summary>
public sealed record SenkenzeileDaten
{
    /// <summary>Das Ziel — einer der sechs <c>WS_ZIEL_*</c>-Werte.</summary>
    public string Ziel { get; init; } = "";

    /// <summary>Der Pufferspeicher; <c>0</c> = keiner (nur bei Ladezielen sinnvoll).</summary>
    public int IdPuffer { get; init; }

    /// <summary>
    /// Der abgedeckte Bedarfsanteil — NUR beim Heizkreis wirksam (Konzept 3.1).
    /// </summary>
    public string Bedarfsart { get; init; } = "";

    /// <summary>Ladepriorität; <c>0</c> = nach Vorgabe.</summary>
    public int Ladeprio { get; init; }

    /// <summary>
    /// Ladeobergrenze in %; <c>null</c> = keine eigene (der Speicher entscheidet).
    /// </summary>
    public double? Ladegrenze { get; init; }

    /// <summary>PV-Sonderpriorität; <c>0</c> = keine. Nur auf Rang 1.</summary>
    public int LadeprioPv { get; init; }

    /// <summary>
    /// Einspeisehöhe 0…1; <c>null</c> = nicht gesetzt und heißt „oben". Das ist der
    /// REGELFALL — dort eine 1 hinzuschreiben behauptete eine Pflege, die es nicht gibt.
    /// </summary>
    public double? Anschlusshoehe { get; init; }
}

/// <summary>
/// Ein Projektpuffer für die Auswahllisten des Senkendialogs.
/// </summary>
/// <param name="Id">Der Fremdschlüssel.</param>
/// <param name="Anzeige">Der Listentext.</param>
/// <param name="Maske">
/// Die Bitmaske des Klassen-Sets (Heizung 1, Brauchwasser 2, Prozess 4). Sie ordnet
/// die Liste und entscheidet, wo ein Gruppenkopf steht.
/// </param>
/// <param name="SetAnzeige">Das Klassen-Set als Text — er steht im Gruppenkopf.</param>
public sealed record SenkenPuffer(int Id, string Anzeige, int Maske, string SetAnzeige);

/// <summary>Das Ergebnis der Bestandsprüfung vor dem Speichern.</summary>
/// <param name="Ok">Darf gespeichert werden?</param>
/// <param name="Fehler">Der Grund, wenn nicht.</param>
/// <param name="AbsprungPufferVerwaltung">
/// Der Fehler lässt sich durch das Anlegen eines Puffers beheben — dann fragt der
/// Dialog, ob er die Verwaltung öffnen soll (Konzept 4.6).
/// </param>
/// <param name="Warnung">Ein Hinweis OHNE Blockerwirkung; er kommt nach dem Speichern.</param>
public sealed record SenkenPruefung(bool Ok, string Fehler, bool AbsprungPufferVerwaltung,
                                    string Warnung);

/// <summary>
/// Die Übergabefelder des Senkendialogs (Vermessung §6 b).
/// </summary>
public sealed record WaermesenkeDaten
{
    /// <summary>Das Projekt.</summary>
    public int IdProjekt { get; init; }

    /// <summary>Die Erzeugeranlage, deren Senken gepflegt werden.</summary>
    public int IdAnlage { get; init; }

    /// <summary>Der Anlagentyp.</summary>
    public int IdType { get; init; }

    /// <summary>Der Anlagenname — er steht im Titel.</summary>
    public string AnlagenName { get; init; } = "";

    /// <summary>
    /// Läuft die Anlage PV-optimiert? Nur dann gibt es die PV-Sonderpriorität
    /// (<c>BM_Typ == MODUS_PV</c>).
    /// </summary>
    public bool PvModus { get; init; }

    /// <summary>Die Mitglieder des Parallelverbunds — hinein und hinaus.</summary>
    public IReadOnlyList<int> VerbundMitglieder { get; init; } = Array.Empty<int>();
}

/// <summary>
/// Das Ergebnis des Senkendialogs. Er SPEICHERT SELBST; der Aufrufer erfährt nur, ob
/// es geklappt hat, und bekommt die Liste für seine Statuszeile.
/// </summary>
/// <param name="SpeichernOk">Sind Senkenliste UND Verbund geschrieben worden?</param>
/// <param name="Zeilen">Die gespeicherte Senkenliste in Rangfolge.</param>
/// <param name="Verbund">Die gespeicherten Verbundmitglieder.</param>
public sealed record WaermesenkeErgebnis(bool SpeichernOk,
                                         IReadOnlyList<SenkenzeileDaten> Zeilen,
                                         IReadOnlyList<int> Verbund);

/// <summary>
/// Die Datenseite des Senkendialogs — alles, was Datenbank oder Kernrechnung berührt.
/// </summary>
/// <param name="Zeilen">Die gespeicherte Senkenliste; leer heißt „noch keine".</param>
/// <param name="Puffer">ALLE Projektpuffer — die Liste jedes Ladeziels (Paket S2).</param>
/// <param name="VerbundKandidaten">
/// Die nach <c>Verwendung</c> GEFILTERTE Liste zum Ziel auf Rang 1 (Befund W10‑B26).
/// </param>
/// <param name="VerbundKapazitaet">Q_max des Verbunds aus Leitspeicher und Mitgliedern.</param>
/// <param name="Position">
/// „Lädt als n. von m … bis x %" für EINE Zeile; leer, wenn sie in keiner Ladeordnung
/// steht.
/// </param>
/// <param name="PufferName">Der Bezeichner eines Puffers — für die Doppelbelegungsmeldung.</param>
/// <param name="ZielAnzeige">Der Anzeigename eines Ziels.</param>
/// <param name="HarterBefund">Der erste HARTE Warnbefund über die Liste; <c>null</c> = keiner.</param>
/// <param name="Pruefen">Die Bestandsprüfung vor dem Speichern.</param>
/// <param name="Schreiben">Schreibt Senkenliste UND Verbund; <c>false</c> = nicht vollständig.</param>
/// <param name="WeicheBefunde">Die weichen Befunde NACH dem Speichern.</param>
public sealed record WaermesenkeDienste(
    Func<IReadOnlyList<SenkenzeileDaten>> Zeilen,
    Func<IReadOnlyList<SenkenPuffer>> Puffer,
    Func<string, IReadOnlyList<SenkenPuffer>> VerbundKandidaten,
    Func<int, IReadOnlyList<int>, double> VerbundKapazitaet,
    Func<SenkenzeileDaten, bool, string> Position,
    Func<int, string> PufferName,
    Func<string, string> ZielAnzeige,
    Func<IReadOnlyList<SenkenzeileDaten>, string?> HarterBefund,
    Func<IReadOnlyList<SenkenzeileDaten>, IReadOnlyList<int>, SenkenPruefung> Pruefen,
    Func<IReadOnlyList<SenkenzeileDaten>, IReadOnlyList<int>, bool> Schreiben,
    Func<IReadOnlyList<SenkenzeileDaten>, IReadOnlyList<string>> WeicheBefunde);
