namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Eine Zeile des AUSLIEFERUNGSKATALOGS, so weit der Projektdialog sie braucht:
/// Der Bezeichner steht in der Klappliste, die drei übrigen Felder wandern beim
/// Auswählen in die Eingabefelder (<c>cbKatalog_SelectedIndexChanged</c>:1566-1591).
/// Hersteller, Speichertyp und Investitionskosten bleiben in der Hülle — sie werden
/// gespeichert, aber nie angezeigt.
/// </summary>
public sealed record PspKatalogzeile(int Id, string Bezeichner, int Gesamtvolumen,
                                     double Bereitschaftsverluste);

/// <summary>
/// Eine Zeile der PROJEKTliste. Der Anzeigetext ist bereits fertig
/// (<c>PSP_LISTE_EINTRAG</c> samt „Verwendung fehlt"-Zusatz) — er entsteht aus
/// Daten, die nur die Hülle kennt.
/// </summary>
public sealed record PspProjektzeile(int Id, string Anzeige);

/// <summary>
/// Schichtung und Leistungsgrenzen eines Speichers (Paket P1, Migrationsschritt 53).
///
/// <para><b>Die <c>null</c> ist eine Aussage.</b> Sie bedeutet „nicht gepflegt, es
/// gilt die Vorbelegung" — genau die NULL-Bedeutung der Spalten (Konzept 7.2). Ein
/// leeres Feld ist deshalb überall zulässig.</para>
///
/// <para>Die beiden Leistungsgrenzen führen dagegen <c>0</c> für „unbegrenzt" und
/// werden als LEERES Feld gezeigt: Eine „0" in einem Leistungsfeld liest sich wie
/// „keine Leistung" und meint das Gegenteil.</para>
/// </summary>
public sealed record PspSchichtdaten(
    int Schichten = 1,
    double? Hoehe = null,
    double? LambdaEff = null,
    double? TNutzBW = null,
    double? EntnahmeHeizung = null,
    double? EntnahmeBW = null,
    double? EntnahmeProzess = null,
    double LadeleistungMax = 0,
    double EntladeleistungMax = 0);

/// <summary>
/// Der vollständige Stand EINES Projektpuffers — was der Dialog beim Auswählen in
/// seine Felder lädt (<c>PufferAnzeigen</c>:1242-1300).
/// </summary>
public sealed record PspPufferstand(
    int Id,
    string Bezeichner,
    int Gesamtvolumen,
    double Bereitschaftsverluste,
    int Vorlauf,
    int Ruecklauf,
    double SchwelleEin,
    double SchwelleAus,
    double SchwelleAusNachrang,
    double SchwelleReserve,
    int Entladeprio,
    bool Heizung,
    bool Brauchwasser,
    bool Prozess,
    PspSchichtdaten Schicht);

/// <summary>
/// Was der Dialog beim Übernehmen aus seinen Feldern liest — der Satz, der an
/// <c>Anlegen</c> bzw. <c>Aendern</c> geht.
///
/// <para><b>Die Verwendung steht NICHT darin.</b> Sie wird aus dem Klassen-Set
/// abgeleitet (<c>klassenSet.Verwendung</c>, <c>EingabenLesen</c>:1931) — die
/// Klappliste folgt den Häkchen, nicht umgekehrt (Paket K2). Die Ableitung ist
/// Fachwissen des Kerns und bleibt deshalb in der Hülle.</para>
///
/// <para><b><see cref="Katalogzeile"/> ist der Listenplatz, nicht die Id.</b>
/// <c>-1</c> heißt „freie Eingabe"; die Hülle holt sich Hersteller, Speichertyp und
/// Investitionskosten daraus bzw. aus dem Bestand (<c>KatalogfelderLesen</c>).</para>
/// </summary>
public sealed record PspEingaben(
    string Bezeichner,
    int Volumen,
    double Verluste,
    int? Vorlauf,
    int? Ruecklauf,
    double SchwelleEin,
    double SchwelleAus,
    double SchwelleNachrang,
    double SchwelleReserve,
    int Entladeprio,
    bool Heizung,
    bool Brauchwasser,
    bool Prozess,
    PspSchichtdaten Schicht,
    int Katalogzeile);

/// <summary>
/// Eine Zeile der Kontrollanzeige „Ladereihenfolge dieses Speichers" — alle sechs
/// Spalten bereits als Text (<c>LadereihenfolgeAnzeigen</c>:1384-1429).
/// </summary>
public sealed record PspLadezeile(string Nummer, string Bezeichner, string Erzeuger,
                                  string Rolle, string Ladeprio, string Obergrenze);

/// <summary>
/// Die Datenseite des Pufferspeicher-Projektdialogs — SECHZEHN Delegaten, die die
/// Hülle einmal baut und an alle drei Rollen des Dialogs durchreicht (eigenes
/// Fenster, Überlagerung im Quellendialog, Überlagerung im Senkendialog).
///
/// <para><b>Warum ein Record und nicht sechzehn Parameter.</b> Der Dialog erscheint
/// an drei Stellen (Risiko R‑W10a‑5); ein Satz, den die Hülle einmal baut, hält die
/// drei Aufrufwege gleich — dasselbe Muster wie der Delegatensatz des
/// Wärmepumpendialogs aus Welle 7.</para>
///
/// <para><b>Alles, was Datenbank berührt, steht hier</b> — die Komponente kennt
/// weder <c>PufferSpCtrl</c> noch <c>Ladeordnung</c>. Auch die reine Rechnung
/// <see cref="Kapazitaet"/> kommt als Delegat: Sie liegt seit W10a.0b in
/// <c>ProjektPuffer</c>, und diese Klasse ist im Kern <c>internal</c>.</para>
/// </summary>
/// <param name="Katalogzeilen">Der Auslieferungskatalog, nach Bezeichner sortiert.</param>
/// <param name="Projektliste">Die Puffer DIESES Projekts, fertig beschriftet.</param>
/// <param name="PufferLesen">Der vollständige Stand eines Puffers; <c>null</c> = gibt es nicht.</param>
/// <param name="Systemvorgaben">Kleinster Vorlauf und größter Rücklauf der Erzeuger; je <c>null</c> = nicht gepflegt.</param>
/// <param name="Ladereihenfolge">Wer diesen Speicher lädt, in welcher Reihenfolge.</param>
/// <param name="Automatiktext">Die Zeile „Entladepriorität automatisch: n".</param>
/// <param name="Entladeposition">„Wird als n. von m … entladen" — beim Kombispeicher zwei Zeilen.</param>
/// <param name="KlassenSetAnzeige">Ein Klassen-Set als Text, für die Wechselrückfrage.</param>
/// <param name="IstLeitspeicher">Ist der Speicher Leitspeicher eines Parallelverbunds (Kriterium W6)?</param>
/// <param name="Referenzen">Welche Anlagen den Speicher zugeordnet haben; leer = keine.</param>
/// <param name="TemperaturenPruefen">Prüft das Temperaturpaar; <c>null</c> = in Ordnung, sonst der Fehlertext.</param>
/// <param name="Anlegen">Legt an und liefert die neue Id; <c>&lt;= 0</c> = fehlgeschlagen.</param>
/// <param name="Aendern">Ändert den Speicher; <c>false</c> = fehlgeschlagen.</param>
/// <param name="Entfernen">Entfernt den Speicher; <c>false</c> = fehlgeschlagen.</param>
/// <param name="Klemmhinweis">Kriterium W4 NACH dem Speichern; <c>null</c> = nichts zu sagen.</param>
/// <param name="Kapazitaet">Nutzbare Kapazität [kWh] aus Volumen [l] und Spreizung [K].</param>
public sealed record PufferSpProjektDienste(
    Func<IReadOnlyList<PspKatalogzeile>> Katalogzeilen,
    Func<IReadOnlyList<PspProjektzeile>> Projektliste,
    Func<int, PspPufferstand?> PufferLesen,
    Func<(int? Vorlauf, int? Ruecklauf)> Systemvorgaben,
    Func<int, IReadOnlyList<PspLadezeile>> Ladereihenfolge,
    Func<int, string> Automatiktext,
    Func<int, bool, bool, bool, string> Entladeposition,
    Func<bool, bool, bool, string> KlassenSetAnzeige,
    Func<int, bool> IstLeitspeicher,
    Func<int, IReadOnlyList<string>> Referenzen,
    Func<string, string, string?> TemperaturenPruefen,
    Func<PspEingaben, int> Anlegen,
    Func<int, PspEingaben, bool> Aendern,
    Func<int, bool> Entfernen,
    Func<int, PspEingaben, string?> Klemmhinweis,
    Func<double, double, double> Kapazitaet);
