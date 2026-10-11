namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Rückfragen des Reiters „Konditionierung“</b> (Stufe KP2, Welle U1; Entwurf KP2
/// Festlegungen 1, 3, 4; E56 F5 (a)) — ein eigenes Bündel neben <see cref="KonditionierungTexte"/>,
/// denn die Rückfragen tragen das eigene Präfix <c>KOND_FRAGE_*</c>.
/// </summary>
/// <remarks>
/// <para><b>Aus dem Befund, vor dem Schreiben.</b> Jede Frage setzt der Reiter aus dem Befund
/// zusammen, den der Weg VOR der Handlung liefert (<see cref="KonditionierungWeg.Rueckfrage"/>): was
/// ersetzt wird, was bleibt, welche Zonen es betrifft — mit Namen. Die Posten nennt
/// <see cref="KonditionierungTexte"/> (<c>KOND_TXT_POSTEN_*</c>).</para>
/// <para>Gefüllt von <c>KonditionierungTexteHuelle.Fragen()</c>; der Vorgabewert ist der deutsche
/// Rückfall.</para>
/// </remarks>
public sealed class KonditionierungFragetexte
{
    /// <summary>
    /// <c>KOND_FRAGE_VERWERFEN</c> — „{0}“ die Größe, „{1}“ was fällt (Posten), „{2}“ was bleibt
    /// </summary>
    public string Verwerfen { get; set; }
        = "Den angelegten Kalender „{0}“ verwerfen? Es fällt: {1}. Es bleibt: {2}. Danach gilt wieder die Matrix.";

    /// <summary>
    /// <c>KOND_FRAGE_MATRIX_ERNEUT</c> — „{0}“ die Größe, „{1}“ was ersetzt wird, „{2}“ was bleibt
    /// </summary>
    public string MatrixErneut { get; set; }
        = "Die Matrix erneut auf den Kalender „{0}“ anwenden? Ersetzt wird: {1}. Es bleibt: {2}.";

    /// <summary>
    /// <c>KOND_FRAGE_KATALOG_ERNEUT</c> — „{0}“ was ersetzt wird, „{1}“ was bleibt (Festlegung 4)
    /// </summary>
    public string KatalogErneut { get; set; }
        = "Die Konditionierung des Gebäudes erneut aus dem Katalogsatz übernehmen? Ersetzt wird die ganze "
        + "Gebäudeebene: {0}. Es bleibt: {1}.";

    /// <summary>
    /// <c>KOND_FRAGE_AUFTEILEN</c> — „{0}“ die Luftwechselrate, „{1}“ die Infiltration, „{2}“ die
    /// Nutzerlüftung nach der Aufteilung [1/h] (E56 F5 (a))
    /// </summary>
    public string Aufteilen { get; set; }
        = "Die Lüftung dieses Gebäudes steht als Gesamtangabe: Luftwechselrate {0} 1/h. Eine Vorgabe der "
        + "Lüftung braucht Infiltration und Nutzerlüftung getrennt. Aufteilen in Infiltration {1} 1/h und "
        + "Nutzerlüftung {2} 1/h? Die Summe bleibt.";

    /// <summary>
    /// <c>KOND_FRAGE_SPEICHERN_UNTER</c> — „{0}“ was im Projekt zurückbleibt (Zonen, Bauteile,
    /// Konditionierung der Zonen; Festlegung 3)
    /// </summary>
    public string SpeichernUnter { get; set; }
        = "„Speichern unter“ legt einen Katalogsatz mit der Gebäudeebene an; die Zonen kommen in ihrem gespeicherten "
        + "Stand mit, darunter: {0}. Anlegen?";

    /// <summary><c>KOND_FRAGE_ZONEN</c> — der Zusatz mit den Namen der betroffenen Zonen; „{0}“ die Namen</summary>
    public string Zonen { get; set; } = "Betroffene Zonen: {0}.";

    /// <summary>
    /// <c>KOND_FRAGE_OHNE_EINZELHEITEN</c> — die Frage, wenn der Weg keinen Befund liefert; „{0}“ die
    /// Handlung
    /// </summary>
    public string OhneEinzelheiten { get; set; } = "„{0}“ ersetzt Angaben dieses Reiters. Fortfahren?";

    /// <summary>
    /// <c>KOND_FRAGE_VORLAGE_UEBERNEHMEN</c> — „{0}“ der Name der Vorlage, „{1}“ die Größe, „{2}“ was
    /// ersetzt wird, „{3}“ was bleibt (P12; Stufe KP2, Welle U2)
    /// </summary>
    public string VorlageUebernehmen { get; set; }
        = "Die Vorlage „{0}“ auf den angelegten Kalender „{1}“ übernehmen? Ersetzt wird: {2}. Es bleibt: {3}.";

    /// <summary>
    /// <c>KOND_FRAGE_VORLAGE_LOESCHEN</c> — „{0}“ der Name der Vorlage; dahinter steht
    /// <c>KOND_TXT_VORLAGE_LOESCHEN</c> (kein Gebäude wird berührt)
    /// </summary>
    public string VorlageLoeschen { get; set; } = "Die Vorlage „{0}“ löschen?";

    /// <summary>
    /// <c>KOND_FRAGE_SOLLWERTPROFIL</c> — „In den Kalender übernehmen“ auf einen angelegten Heizkalender
    /// (Teilkonzept 5.5; Stufe KP2, Welle U3)
    /// </summary>
    public string Sollwertprofil { get; set; }
        = "Das Sollwert-Zeitprogramm ersetzt die Standardwoche des angelegten Heizkalenders; Perioden und Herkunft bleiben. Übernehmen?";

    // ---- Die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…“ (E57; Stufe KP2, Welle U5) ----
    // EINE Rückfrage für alle Größen: der Satz, je Größe eine Zeile („{Größe}: {Teil}“), dazu die Zonen.

    /// <summary><c>KOND_FRAGE_VORLAGE_ALLE_TITEL</c> — der Titel der Rückfrage der Abkürzung</summary>
    public string VorlageAlleTitel { get; set; } = "Vorlage in allen Größen übernehmen";

    /// <summary><c>KOND_FRAGE_VORLAGE_ALLE</c> — der Satz der Rückfrage; „{0}“ der Name der Vorlage (ein Datenwert)</summary>
    public string VorlageAlle { get; set; } = "Die Vorlage „{0}“ in allen Größen übernehmen?";

    /// <summary><c>KOND_FRAGE_VORLAGE_ALLE_ZEILE</c> — die Zeile je Größe; „{0}“ die Größe, „{1}“ was mit ihr geschieht</summary>
    public string VorlageAlleZeile { get; set; } = "{0}: {1}";

    /// <summary><c>KOND_FRAGE_VORLAGE_ALLE_UEBERNEHMEN</c> — eine Größe übernimmt die Vorlage, ohne dass etwas ersetzt wird</summary>
    public string VorlageAlleUebernehmen { get; set; } = "übernehmen";

    /// <summary>
    /// <c>KOND_FRAGE_VORLAGE_ALLE_ERSETZT</c> — am angelegten Kalender (P12): „{0}“ was ersetzt wird, „{1}“ was bleibt
    /// </summary>
    public string VorlageAlleErsetzt { get; set; } = "ersetzt wird: {0}; es bleibt: {1}";

    /// <summary>
    /// <c>KOND_FRAGE_VORLAGE_ALLE_AUFTEILEN</c> — an der Gesamtangabe der Lüftung (E56 F5 (a)): „{0}“ die
    /// Luftwechselrate, „{1}“ die Infiltration, „{2}“ die Nutzerlüftung nach der Aufteilung [1/h]
    /// </summary>
    public string VorlageAlleAufteilen { get; set; } = "die Gesamtangabe {0} 1/h wird aufgeteilt (Infiltration {1}, Nutzerlüftung {2})";

    /// <summary><c>KOND_FRAGE_VORLAGE_ALLE_OHNE</c> — eine Größe ohne Vorlage dieses Namens</summary>
    public string VorlageAlleOhne { get; set; } = "keine Vorlage dieses Namens — bleibt";

    /// <summary><c>KOND_FRAGE_VORLAGE_ALLE_GESPERRT</c> — eine gesperrte Größe; „{0}“ der Grund (die Sperre der Karte)</summary>
    public string VorlageAlleGesperrt { get; set; } = "gesperrt — {0}";
}
