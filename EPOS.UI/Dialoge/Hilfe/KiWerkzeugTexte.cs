namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// Alle Anzeigetexte der Werkzeugliste — Anwenderbefund <b>W15b‑E‑4</b> der
/// Windows-Abnahme vom 05.09.2026.
/// </summary>
/// <remarks>
/// <para>
/// <b>Der Befund.</b> „Es ist unklar, was ausgeführt werden kann und wie. Es
/// sollte klarer strukturiert sein und mindestens ein Beispiel (direkt und auch
/// in der Hilfe) geben." Die Liste zeigte links rohe Bezeichner
/// (<c>minimale_spitze_ermitteln</c>, <c>speichervariante_aktiv_setzen</c>) und
/// rechts einen technischen Block mit der Zeile
/// „Andockpunkt: VariantenCtrl.AnlegenAusStamm" — beides für das Modell und für
/// das Protokoll geschrieben, nicht für den Anwender.
/// </para>
/// <para>
/// <b>Warum ein eigenes Bündel.</b> Die Liste trägt jetzt sechzehn
/// Anzeigetexte. Sie einzeln als <c>[Parameter] string</c> zu führen hätte die
/// Komponente über die Hausgrenze getrieben und die vier Fachparameter zwischen
/// Beschriftungen versteckt (Hausregel „ab etwa zehn Anzeigetexten ein
/// BÜNDEL"). Es hängt in <see cref="KiChatTexte.Werkzeugliste"/>, damit die
/// Hülle weiterhin EIN Bündel baut.
/// </para>
/// <para>
/// Jede Eigenschaft nennt ihren Ressourcenschlüssel im Kommentar.
/// </para>
/// </remarks>
public sealed class KiWerkzeugTexte
{
    /// <summary>Beschriftung der Aktionsliste für Sprachausgaben (<c>KI_AKT_WERKZEUGE_TITEL</c>).</summary>
    public string Liste { get; set; } = "";

    /// <summary>Der eine Satz über der Liste (<c>KI_AKT_WERKZEUGE_HINWEIS</c>).</summary>
    public string Hinweis { get; set; } = "";

    /// <summary>Beschriftung des Suchfeldes (<c>KI_AKT_WERKZEUGE_SUCHE</c>).</summary>
    public string Suche { get; set; } = "";

    /// <summary>Kein Eintrag passt zum Suchtext (<c>KI_AKT_WERKZEUGE_KEIN_TREFFER</c>).</summary>
    public string KeinTreffer { get; set; } = "";

    /// <summary>Gruppenkopf der lesenden Aktionen (<c>KI_AKT_WERKZEUGE_GRUPPE_LESEND</c>).</summary>
    public string GruppeLesend { get; set; } = "";

    /// <summary>Gruppenkopf der verändernden Aktionen (<c>KI_AKT_WERKZEUGE_GRUPPE_AENDERND</c>).</summary>
    public string GruppeAendernd { get; set; } = "";

    /// <summary>Kennzeichen „Liest nur" (<c>KI_AKT_WERKZEUGE_MERKMAL_LESEND</c>).</summary>
    public string MerkmalLesend { get; set; } = "";

    /// <summary>Kennzeichen „Ändert Daten" (<c>KI_AKT_WERKZEUGE_MERKMAL_AENDERND</c>).</summary>
    public string MerkmalAendernd { get; set; } = "";

    /// <summary>„So fragen Sie:" (<c>KI_AKT_WERKZEUGE_BEISPIEL</c>).</summary>
    public string Beispiel { get; set; } = "";

    /// <summary>Überschrift der Parameterfelder (<c>KI_AKT_WERKZEUGE_ANGABEN</c>).</summary>
    public string Angaben { get; set; } = "";

    /// <summary>Die Aktion braucht keine Angaben (<c>KI_AKT_WERKZEUGE_KEINE_ANGABEN</c>).</summary>
    public string KeineAngaben { get; set; } = "";

    /// <summary>„Danach:" vor der Wirkung (<c>KI_AKT_WERKZEUGE_WIRKUNG</c>).</summary>
    public string Wirkung { get; set; } = "";

    /// <summary>Kurztext am Pflichtzeichen (<c>KI_AKT_WERKZEUGE_PFLICHT</c>).</summary>
    public string Pflicht { get; set; } = "";

    /// <summary>Überschrift des Leerzustands (<c>KI_AKT_WERKZEUGE_LEER_KOPF</c>).</summary>
    public string LeerKopf { get; set; } = "";

    /// <summary>
    /// Das vollständige Beispiel im Leerzustand (<c>KI_AKT_WERKZEUGE_LEER_TEXT</c>) —
    /// derselbe Fall, den das eingebaute Hilfewissen unter „Aktionen des
    /// Assistenten" führt.
    /// </summary>
    public string LeerText { get; set; } = "";

    /// <summary>Der Satz über die Bestätigungspflicht (<c>KI_AKT_WERKZEUGE_BESTAETIGUNG</c>).</summary>
    public string Bestaetigungspflicht { get; set; } = "";

    /// <summary>
    /// Vorlage des Kurztextes mit dem Andockpunkt
    /// (<c>KI_AKT_WERKZEUGE_ANDOCKPUNKT</c>, <c>{0}</c> = Andockpunkt).
    /// </summary>
    /// <remarks>
    /// Der Andockpunkt (<c>VariantenCtrl.AnlegenAusStamm</c>) verschwindet aus der
    /// Anwendersicht und bleibt nur als <c>title</c> stehen — er gehört ins
    /// Protokoll, nicht in die Maske (W15b‑E‑4).
    /// </remarks>
    public string AndockpunktFormat { get; set; } = "{0}";
}
