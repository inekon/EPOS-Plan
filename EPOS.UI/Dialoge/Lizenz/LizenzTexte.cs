using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Lizenz;

/// <summary>
/// Alle Anzeigetexte der beiden Lizenzmasken — EIN Parameter statt achtzehn
/// bzw. neunundzwanzig (offener Punkt W15c-O-2, umgesetzt am 04.09.2026).
///
/// <para><b>Warum gebuendelt.</b> <c>LizenzDialog</c> trug seine Beschriftungen
/// als 18 einzelne <c>[Parameter] string</c>, <c>LizenzVerwaltungDialog</c> als
/// 29. Beide Bloecke zusammen waren 148 Zeilen, und sie versteckten die
/// FACHparameter — das Lagebild, die Delegaten, den Zustimmungsmodus —
/// zwischen Knopfbeschriftungen. Dasselbe hatte die Welle W15b beim
/// Chatfenster geloest (<c>KiChatTexte</c>); hier steht die gleiche Bauart.</para>
///
/// <para><b>Ein Buendel, zwei Masken.</b> Die Verwaltung erscheint als
/// UEBERLAGERUNG im Lizenzdialog (Entscheid W15c-E-11) — beide Masken laufen
/// im selben Fenster und ziehen aus demselben Katalogzweig. Ihre Texte stehen
/// deshalb in EINEM Gegenstand: die des Lizenzdialogs (<c>LIZR_*</c>)
/// unmittelbar hier, die der Verwaltung (<c>LIZ_*</c>) unter
/// <see cref="Verwaltung"/>. So gibt es keinen Namensstreit zwischen
/// <c>LIZR_BTN_AKTIVIEREN</c> („Lizenz aktivieren…") und
/// <c>LIZ_BTN_AKTIVIEREN</c> („Jetzt aktivieren"), und die Huelle baut den Satz
/// einmal.</para>
///
/// <para><b>Es fuellt sich selbst.</b> Anders als <c>KiChatTexte</c>, dessen
/// Werte samt Fallunterscheidungen aus der Huelle kommen, sind diese Texte
/// reine Katalogeintraege. Jede Eigenschaft holt darum ihren Wert beim Bauen
/// aus <c>MyResource</c> in der aktuellen Oberflaechensprache — dieselbe Linie
/// wie <c>Menuepunkt.TextFuer</c> (iU9-W16c.1). Wer etwas anderes braucht —
/// die Huelle setzt den Fenstertitel aus <c>LIZ_TITEL</c> PLUS Produktname —
/// ueberschreibt die Eigenschaft; wer nichts angibt, bekommt den Katalogtext.
/// <b>Ohne Katalog</b> bleibt der deutsche Wortlaut stehen, den die Komponenten
/// bis heute als Vorgabewert trugen.</para>
///
/// <para>Jede Eigenschaft traegt ihren Ressourcenschluessel im Kommentar — so
/// bleibt nachvollziehbar, welcher Katalogeintrag wo landet.</para>
/// </summary>
public sealed class LizenzTexte
{
    /// <summary>
    /// Ein Katalogeintrag in der aktuellen Oberflaechensprache.
    ///
    /// <para>Ein FEHLENDER Schluessel liefert <paramref name="ersatz"/> — den
    /// deutschen Wortlaut der Komponente. Ein VORHANDENER, aber leerer Eintrag
    /// bleibt leer: <c>LIZR_HINWEIS_SPRACHE</c> ist auf Deutsch mit Absicht
    /// leer (Entscheid W15c-E-7), und ein Rueckfall auf den Schluesselnamen —
    /// wie ihn <c>Menuepunkt.TextFuer</c> fuer Menuezeilen richtigerweise
    /// macht — schriebe hier „LIZR_HINWEIS_SPRACHE" ueber die Rechtstexte.</para>
    /// </summary>
    internal static string Katalog(string schluessel, string ersatz = "")
    {
        try { return Resource.ResourceManager.GetString(schluessel) ?? ersatz; }
        catch { return ersatz; }
    }

    // ==================================================================
    //  Der Lizenzdialog (LIZR_*)
    // ==================================================================

    /// <summary>Ueberschrift der Kopfzeile (<c>LIZR_KOPF_TITEL</c>).</summary>
    public string KopfTitel { get; set; } = Katalog("LIZR_KOPF_TITEL", "Lizenz und rechtliche Hinweise");

    /// <summary>Zweite Kopfzeile (<c>LIZR_KOPF_UNTERTITEL</c>).</summary>
    public string KopfUntertitel { get; set; } = Katalog("LIZR_KOPF_UNTERTITEL");

    /// <summary>Reiter „Lizenzvereinbarung" (<c>LIZR_REITER_VERTRAG</c>).</summary>
    public string ReiterVertrag { get; set; } = Katalog("LIZR_REITER_VERTRAG", "Lizenzvereinbarung");

    /// <summary>Reiter „Rechtliche Hinweise" (<c>LIZR_REITER_HINWEISE</c>).</summary>
    public string ReiterHinweise { get; set; } = Katalog("LIZR_REITER_HINWEISE", "Rechtliche Hinweise");

    /// <summary>Reiter „Komponenten" (<c>LIZR_REITER_KOMPONENTEN</c>).</summary>
    public string ReiterKomponenten { get; set; } = Katalog("LIZR_REITER_KOMPONENTEN", "Komponenten");

    // „Datei wählen…" (LIZR_BTN_DATEI) ist mit der Windows-Abnahme vom
    // 05.09.2026 gefallen (W15c-E-1): Der Knopf ersetzte den lesbaren
    // Vertragstext durch den Zeiger auf eine Datei, die die WebView seit E-1
    // ohnehin nicht anzeigt. Der Katalogeintrag bleibt im Sprachschatz stehen —
    // gelesen wird er von niemandem mehr.
    /// <summary>Knopf „Drucken..." (<c>LIZR_BTN_DRUCKEN</c>).</summary>
    public string KnopfDrucken { get; set; } = Katalog("LIZR_BTN_DRUCKEN", "Drucken...");

    /// <summary>Knopf „Speichern unter..." (<c>LIZR_BTN_SPEICHERN</c>).</summary>
    public string KnopfSpeichern { get; set; } = Katalog("LIZR_BTN_SPEICHERN", "Speichern unter...");

    /// <summary>Knopf „Lizenz aktivieren..." (<c>LIZR_BTN_AKTIVIEREN</c>).</summary>
    public string KnopfAktivieren { get; set; } = Katalog("LIZR_BTN_AKTIVIEREN", "Lizenz aktivieren...");

    /// <summary>Knopf „Schließen" (<c>LIZR_BTN_SCHLIESSEN</c>).</summary>
    public string KnopfSchliessen { get; set; } = Katalog("LIZR_BTN_SCHLIESSEN", "Schließen");

    /// <summary>Knopf „Zustimmen" (<c>LIZR_BTN_ZUSTIMMEN</c>).</summary>
    public string KnopfZustimmen { get; set; } = Katalog("LIZR_BTN_ZUSTIMMEN", "Zustimmen");

    /// <summary>Knopf „Ablehnen" (<c>LIZR_BTN_ABLEHNEN</c>).</summary>
    public string KnopfAblehnen { get; set; } = Katalog("LIZR_BTN_ABLEHNEN", "Ablehnen");

    /// <summary>Der Bestaetigungshinweis im Zustimmungsmodus (<c>LIZR_ZUSTIMMUNG_HINWEIS</c>).</summary>
    public string ZustimmungHinweis { get; set; } = Katalog("LIZR_ZUSTIMMUNG_HINWEIS");

    /// <summary>Formatvorlage „Lizenz: {0}" (<c>LIZR_FUSS_LIZENZ</c>).</summary>
    public string FussLizenz { get; set; } = Katalog("LIZR_FUSS_LIZENZ", "Lizenz: {0}");

    /// <summary>Formatvorlage „Quelle: {0}" (<c>LIZR_FUSS_QUELLE</c>).</summary>
    public string FussQuelle { get; set; } = Katalog("LIZR_FUSS_QUELLE", "Quelle: {0}");

    /// <summary>Formatvorlage „   ·   Stand {0}" (<c>LIZR_FUSS_STAND</c>).</summary>
    public string FussStand { get; set; } = Katalog("LIZR_FUSS_STAND", "   ·   Stand {0}");

    /// <summary>Meldung nach dem Speichern (<c>LIZR_MSG_GESPEICHERT</c>).</summary>
    public string MsgGespeichert { get; set; } = Katalog("LIZR_MSG_GESPEICHERT", "{0}");

    /// <summary>
    /// Der Sprachhinweis ueber den erzeugten Abschnitten
    /// (<c>LIZR_HINWEIS_SPRACHE</c>). Auf Deutsch LEER, auf Englisch „Binding
    /// version in German." — verbindlich ist allein die deutsche Fassung
    /// (Entscheid W15c-E-7).
    /// </summary>
    public string SprachHinweis { get; set; } = Katalog("LIZR_HINWEIS_SPRACHE");

    // ==================================================================
    //  Die Lizenzverwaltung (LIZ_*)
    // ==================================================================

    /// <summary>
    /// Die Texte der Lizenzverwaltung — sie erscheint als Ueberlagerung IM
    /// Lizenzdialog (E-11) und als eigenes Fenster (Menue Administration →
    /// Lizenz…). Beide Male derselbe Satz.
    /// </summary>
    public LizenzVerwaltungTexte Verwaltung { get; set; } = new();
}

/// <summary>
/// Die Anzeigetexte der Lizenzverwaltung (<c>LIZ_*</c>) — der zweite Teil des
/// Buendels <see cref="LizenzTexte"/> (W15c-O-2).
///
/// <para>Eigener Gegenstand, weil <c>LIZR_BTN_AKTIVIEREN</c> („Lizenz
/// aktivieren…", der Knopf, der die Verwaltung OEFFNET) und
/// <c>LIZ_BTN_AKTIVIEREN</c> („Jetzt aktivieren", der Knopf, der sie
/// AUSFUEHRT) beide „KnopfAktivieren" heissen wollen. Getrennte Saetze halten
/// beide Namen sprechend.</para>
///
/// <para>Gefuellt wird jede Eigenschaft aus <c>MyResource</c>, wie in
/// <see cref="LizenzTexte"/> beschrieben.</para>
/// </summary>
public sealed class LizenzVerwaltungTexte
{
    /// <summary>
    /// Fenstertitel, zugleich Titel der Rueckfrage (<c>LIZ_TITEL</c>). Die
    /// Windows-Huelle setzt ihn auf <c>LIZ_TITEL</c> PLUS Produktname — der
    /// Produktname ist eine Anwendungskonstante und kein Uebersetzungsgut.
    /// </summary>
    public string Titel { get; set; } = LizenzTexte.Katalog("LIZ_TITEL", "Lizenz");

    /// <summary>Gruppentitel „Lizenzstatus auf diesem Arbeitsplatz" (<c>LIZ_GRP_STATUS</c>).</summary>
    public string GruppeStatus { get; set; } = LizenzTexte.Katalog("LIZ_GRP_STATUS", "Lizenzstatus auf diesem Arbeitsplatz");

    /// <summary>Gruppentitel „Aktivieren" (<c>LIZ_GRP_AKTIVIEREN</c>).</summary>
    public string GruppeAktivieren { get; set; } = LizenzTexte.Katalog("LIZ_GRP_AKTIVIEREN", "Aktivieren");

    /// <summary>Gruppentitel „Weitere Aktionen" (<c>LIZ_GRP_AKTIONEN</c>).</summary>
    public string GruppeAktionen { get; set; } = LizenzTexte.Katalog("LIZ_GRP_AKTIONEN", "Weitere Aktionen");

    /// <summary>Beschriftung „Lizenzschlüssel:" (<c>LIZ_LBL_SCHLUESSEL</c>).</summary>
    public string LabelSchluessel { get; set; } = LizenzTexte.Katalog("LIZ_LBL_SCHLUESSEL", "Lizenzschlüssel:");

    /// <summary>Beschriftung „E-Mail (Benutzer):" (<c>LIZ_LBL_EMAIL</c>).</summary>
    public string LabelEmail { get; set; } = LizenzTexte.Katalog("LIZ_LBL_EMAIL", "E-Mail (Benutzer):");

    /// <summary>Knopf „Lizenzdatei (.lic)…" (<c>LIZ_BTN_LIC</c>).</summary>
    public string KnopfLic { get; set; } = LizenzTexte.Katalog("LIZ_BTN_LIC", "Lizenzdatei (.lic)…");

    /// <summary>Knopf „Jetzt aktivieren" (<c>LIZ_BTN_AKTIVIEREN</c>).</summary>
    public string KnopfAktivieren { get; set; } = LizenzTexte.Katalog("LIZ_BTN_AKTIVIEREN", "Jetzt aktivieren");

    /// <summary>Knopf „Testversion anfordern…" (<c>LIZ_BTN_TRIAL</c>).</summary>
    public string KnopfTrial { get; set; } = LizenzTexte.Katalog("LIZ_BTN_TRIAL", "Testversion anfordern…");

    /// <summary>Knopf „Gerät von der Lizenz lösen" (<c>LIZ_BTN_FREIGEBEN</c>).</summary>
    public string KnopfFreigeben { get; set; } = LizenzTexte.Katalog("LIZ_BTN_FREIGEBEN", "Gerät von der Lizenz lösen");

    /// <summary>Knopf „Schließen" (<c>LIZ_BTN_SCHLIESSEN</c>).</summary>
    public string KnopfSchliessen { get; set; } = LizenzTexte.Katalog("LIZ_BTN_SCHLIESSEN", "Schließen");

    /// <summary>Der zweizeilige Datenschutzhinweis (<c>LIZ_HINWEIS_AKTIVIERUNG</c>).</summary>
    public string HinweisAktivierung { get; set; } = LizenzTexte.Katalog("LIZ_HINWEIS_AKTIVIERUNG");

    /// <summary>Beschriftung des Portalverweises (<c>LIZ_LINK_PORTAL</c>).</summary>
    public string LinkPortal { get; set; } = LizenzTexte.Katalog("LIZ_LINK_PORTAL", "Lizenzportal öffnen");

    /// <summary>„Bitte Lizenzschlüssel und E-Mail-Adresse angeben." (<c>LIZ_MSG_EINGABE_FEHLT</c>).</summary>
    public string MsgEingabeFehlt { get; set; } = LizenzTexte.Katalog("LIZ_MSG_EINGABE_FEHLT");

    /// <summary>Formatvorlage mit der Adresse (<c>LIZ_MSG_EMAIL_UNGUELTIG</c>).</summary>
    public string MsgEmailUngueltig { get; set; } = LizenzTexte.Katalog("LIZ_MSG_EMAIL_UNGUELTIG", "{0}");

    /// <summary>„Die Lizenz wurde erfolgreich aktiviert." (<c>LIZ_MSG_AKTIVIERT</c>).</summary>
    public string MsgAktiviert { get; set; } = LizenzTexte.Katalog("LIZ_MSG_AKTIVIERT");

    /// <summary>„Die Aktivierung ist fehlgeschlagen." (<c>LIZ_MSG_AKTIVIERUNG_FEHLER</c>).</summary>
    public string MsgAktivierungFehler { get; set; } = LizenzTexte.Katalog("LIZ_MSG_AKTIVIERUNG_FEHLER");

    /// <summary>„… kein gültiger Lizenzschlüssel gefunden." (<c>LIZ_MSG_LIC_OHNE_SCHLUESSEL</c>).</summary>
    public string MsgLicOhneSchluessel { get; set; } = LizenzTexte.Katalog("LIZ_MSG_LIC_OHNE_SCHLUESSEL");

    /// <summary>„Bitte oben eine gültige E-Mail-Adresse eintragen …" (<c>LIZ_MSG_TRIAL_EMAIL</c>).</summary>
    public string MsgTrialEmail { get; set; } = LizenzTexte.Katalog("LIZ_MSG_TRIAL_EMAIL");

    /// <summary>„Der Test-Lizenzschlüssel wurde per E-Mail versandt." (<c>LIZ_MSG_TRIAL_OK</c>).</summary>
    public string MsgTrialOk { get; set; } = LizenzTexte.Katalog("LIZ_MSG_TRIAL_OK");

    /// <summary>„Die Anforderung ist fehlgeschlagen." (<c>LIZ_MSG_TRIAL_FEHLER</c>).</summary>
    public string MsgTrialFehler { get; set; } = LizenzTexte.Katalog("LIZ_MSG_TRIAL_FEHLER");

    /// <summary>Die Rueckfrage vor dem Loesen (<c>LIZ_MSG_FREIGEBEN_FRAGE</c>).</summary>
    public string MsgFreigebenFrage { get; set; } = LizenzTexte.Katalog("LIZ_MSG_FREIGEBEN_FRAGE");

    /// <summary>„Der Lizenzserver ist zurzeit nicht erreichbar …" (<c>LIZ_MSG_SERVER_NICHT_ERREICHBAR</c>).</summary>
    public string MsgServerNichtErreichbar { get; set; } = LizenzTexte.Katalog("LIZ_MSG_SERVER_NICHT_ERREICHBAR");

    /// <summary>Statuszeile „Aktivierung läuft…" (<c>LIZ_STATUS_AKTIVIERUNG</c>).</summary>
    public string StatusAktivierung { get; set; } = LizenzTexte.Katalog("LIZ_STATUS_AKTIVIERUNG");

    /// <summary>Statuszeile „Testversion wird angefordert…" (<c>LIZ_STATUS_TRIAL</c>).</summary>
    public string StatusTrial { get; set; } = LizenzTexte.Katalog("LIZ_STATUS_TRIAL");

    /// <summary>Statuszeile „Gerät wird freigegeben…" (<c>LIZ_STATUS_FREIGABE</c>).</summary>
    public string StatusFreigabe { get; set; } = LizenzTexte.Katalog("LIZ_STATUS_FREIGABE");

    /// <summary>„Lizenzdatei geladen — bitte mit ‚Jetzt aktivieren' abschließen." (<c>LIZ_HINWEIS_LIC_GELADEN</c>).</summary>
    public string HinweisLicGeladen { get; set; } = LizenzTexte.Katalog("LIZ_HINWEIS_LIC_GELADEN");

    /// <summary>Beschriftung „Ja" der Rueckfrage (<c>ALLG_BTN_JA</c>).</summary>
    public string Ja { get; set; } = LizenzTexte.Katalog("ALLG_BTN_JA", "Ja");

    /// <summary>Beschriftung „Nein" der Rueckfrage (<c>ALLG_BTN_NEIN</c>).</summary>
    public string Nein { get; set; } = LizenzTexte.Katalog("ALLG_BTN_NEIN", "Nein");
}
