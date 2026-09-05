namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// Alle Anzeigetexte des Chatfensters — EIN Parameter statt dreissig.
/// </summary>
/// <remarks>
/// <para>
/// <b>Warum gebuendelt.</b> Der Vorlaeufer <c>Form_KiChat</c> setzte seine Texte
/// an zwanzig Stellen im Fensteraufbau; die Razor-Fassung braucht sie als
/// Parameter, weil <c>EPOS.UI</c> keine Huelle kennt. Dreissig einzelne
/// <c>[Parameter] string</c> haetten die Komponente ueber die Hausgrenze von
/// 400 Zeilen getrieben (Risiko R-W15b-1) und die vierzehn FACHparameter des
/// § 15.3 zwischen Beschriftungen versteckt. Hier stehen die Beschriftungen
/// beisammen, und die Huelle baut sie einmal aus <c>MyResource</c>.
/// </para>
/// <para>
/// Jede Eigenschaft traegt ihren Ressourcenschluessel im Kommentar — so bleibt
/// nachvollziehbar, welcher Katalogeintrag wo landet.
/// </para>
/// </remarks>
public sealed class KiChatTexte
{
    /// <summary>Beschriftung des Verlaufs fuer Sprachausgaben.</summary>
    public string Verlauf { get; set; } = "";

    /// <summary>
    /// Beschriftung der aufklappbaren Erlaeuterung im Kopf
    /// (<c>KI_CHAT_ERKLAERUNG_MEHR</c>); leer = keine Klappe.
    /// </summary>
    public string ErklaerungMehr { get; set; } = "";

    /// <summary>
    /// Die Zeile im leeren Verlauf (<c>KI_CHAT_VERLAUF_LEER</c>) - „Noch keine Frage
    /// gestellt." Ohne sie steht dort eine leere Flaeche ohne Rahmen (W15b-E-3).
    /// </summary>
    public string VerlaufLeer { get; set; } = "";

    /// <summary>Titel der Einstellungs-Ueberlagerung (<c>KI_EINST_TITEL</c>).</summary>
    public string EinstellungenTitel { get; set; } = "";

    /// <summary>„Kontext: {0}" (<c>KI_CHAT_KONTEXT</c>).</summary>
    public string KontextFormat { get; set; } = "{0}";

    /// <summary>„Kontext: (nicht erkannt)" (<c>KI_CHAT_KONTEXT_LEER</c>).</summary>
    public string KontextLeer { get; set; } = "";

    /// <summary>„Der Assistent denkt nach…" (<c>KI_CHAT_DENKT</c>).</summary>
    public string Denkt { get; set; } = "";

    /// <summary>„Heute genutzt: {0} von {1}" (<c>KI_CHAT_VERBRAUCH</c>).</summary>
    public string VerbrauchFormat { get; set; } = "{0}/{1}";

    /// <summary>
    /// Der Tooltip der Semantikzeile: Modell, Lizenz und Herkunft, FERTIG
    /// eingesetzt (<c>KI_SEMANTIK_HERKUNFT</c> mit
    /// <c>SemantikModell.NAME</c>/<c>LIZENZ</c>/<c>QUELLE</c>). Leer = kein
    /// Tooltip.
    /// </summary>
    /// <remarks>
    /// Der Vorlaeufer haengte ihn mit einem <c>ToolTip</c> an das Statuslabel
    /// (<c>Form_KiChat:935-938</c>); mit dem Label fiel er weg (Anpassung A‑10,
    /// offener Punkt W15b‑O‑2). Er kommt als <c>title</c> an
    /// <c>.epos-kichat-status</c> zurueck — und zwar GEFUELLT aus der Huelle,
    /// genau wie <see cref="VorschauKopf"/>: Die Komponente kennt weder
    /// <c>SemantikModell</c> noch einen Ressourcenschluessel.
    /// </remarks>
    public string SemantikHerkunft { get; set; } = "";

    /// <summary>Beschriftung des Eingabefeldes.</summary>
    public string Eingabe { get; set; } = "";

    /// <summary>„Fragen" (<c>KI_CHAT_BTN_FRAGEN</c>).</summary>
    public string Fragen { get; set; } = "";

    /// <summary>
    /// „Nur suchen" (<c>KI_CHAT_BTN_SUCHEN</c>) bzw. im Hilfe-Betrieb „Suchen"
    /// (<c>KI_HILFEBETRIEB_SUCHEN_BTN</c>).
    /// </summary>
    public string Suchen { get; set; } = "";

    /// <summary>„Aktionen zulassen" (<c>KI_AKT_SCHALTER</c>).</summary>
    public string Aktionen { get; set; } = "";

    /// <summary>Meldung nach dem Einschalten (<c>KI_AKT_DATENSCHUTZ_EIN</c>).</summary>
    public string AktionenEin { get; set; } = "";

    /// <summary>Meldung nach dem Ausschalten (<c>KI_AKT_DATENSCHUTZ_AUS</c>).</summary>
    public string AktionenAus { get; set; } = "";

    /// <summary>Meldung, wenn die Einwilligung fehlt (<c>KI_HINWEIS_ABGELEHNT</c>).</summary>
    public string EinwilligungFehlt { get; set; } = "";

    /// <summary>„Werkzeuge…" (<c>KI_AKT_WERKZEUGE_BTN</c>).</summary>
    public string Werkzeuge { get; set; } = "";

    /// <summary>Titel der Werkzeugliste (<c>KI_AKT_WERKZEUGE_TITEL</c>).</summary>
    public string WerkzeugeTitel { get; set; } = "";

    /// <summary>
    /// Alle Texte der Werkzeugliste in einem eigenen Buendel — sie ist seit
    /// <b>W15b-E-4</b> eine Maske mit sechzehn Anzeigetexten und traegt sie
    /// deshalb selbst.
    /// </summary>
    public KiWerkzeugTexte Werkzeugliste { get; set; } = new();

    /// <summary>„Ausführen" (<c>KI_AKT_AUSFUEHREN_BTN</c>).</summary>
    public string Ausfuehren { get; set; } = "";

    /// <summary>„Bitte zuerst eine Aktion wählen." (<c>KI_AKT_AKTION_WAEHLEN</c>).</summary>
    public string AktionWaehlen { get; set; } = "";

    /// <summary>Vorderer Teil der Hinweiszeile (<c>KI_HINWEIS_ZEILE</c>).</summary>
    public string HinweisVorn { get; set; } = "";

    /// <summary>
    /// Der Verweis „Rechtshinweis" (<c>KI_HINWEIS_ZEILE_LINK</c>); leer = ohne
    /// Verweis, so wie im Hilfe-Betrieb (<c>KI_WIKI_HINWEIS_ZEILE</c>).
    /// </summary>
    public string HinweisLink { get; set; } = "";

    /// <summary>Titel des Rechtshinweises (<c>KI_HINWEIS_FENSTER</c>).</summary>
    public string RechtshinweisTitel { get; set; } = "";

    /// <summary>„Online-Dokumentation öffnen" (<c>HILFE_POPUP_LINK</c>).</summary>
    public string Doku { get; set; } = "";

    /// <summary>Adresse der Online-Dokumentation.</summary>
    public string DokuAdresse { get; set; } = "";

    /// <summary>„Was wird gesendet?" (<c>KI_VORSCHAU_LINK</c>).</summary>
    public string Vorschau { get; set; } = "";

    /// <summary>Titel der Sendevorschau (<c>KI_VORSCHAU_TITEL</c>).</summary>
    public string VorschauTitel { get; set; } = "";

    /// <summary>Kopfzeile der Sendevorschau (<c>KI_VORSCHAU_HINWEIS</c>).</summary>
    public string VorschauKopf { get; set; } = "";

    /// <summary>„Protokoll anzeigen" (<c>KI_AKT_PROTOKOLL_LINK</c>).</summary>
    public string Protokoll { get; set; } = "";

    /// <summary>Titel des Aktionsprotokolls (<c>KI_AKT_PROTOKOLL_TITEL</c>).</summary>
    public string ProtokollTitel { get; set; } = "";

    /// <summary>„Einstellungen…" (<c>KI_CHAT_BTN_EINSTELLUNGEN</c>).</summary>
    public string Einstellungen { get; set; } = "";

    /// <summary>Meldung nach gespeicherten Einstellungen (<c>KI_EINST_MSG_GESPEICHERT</c>).</summary>
    public string Gespeichert { get; set; } = "";

    /// <summary>„Schließen" (<c>KI_VORSCHAU_SCHLIESSEN</c>).</summary>
    public string Schliessen { get; set; } = "";

    /// <summary>„Verlauf kopieren"; leer = kein Knopf (Entscheid E-11).</summary>
    public string Kopieren { get; set; } = "";

    /// <summary>Titel des Bestaetigungsblocks (<c>KI_AKT_BESTAETIGUNG_TITEL</c>).</summary>
    public string BestaetigungTitel { get; set; } = "";

    /// <summary>„Ausführen" im Block (<c>KI_AKT_BESTAETIGUNG_AUSFUEHREN</c>).</summary>
    public string BestaetigungAusfuehren { get; set; } = "";

    /// <summary>„Abbrechen" im Block (<c>KI_AKT_BESTAETIGUNG_ABBRECHEN</c>).</summary>
    public string BestaetigungAbbrechen { get; set; } = "";
}
