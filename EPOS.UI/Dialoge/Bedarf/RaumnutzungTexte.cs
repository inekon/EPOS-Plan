namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Das Textbündel des Blatts „Nutzungsprofile"</b> (Konzept Nutzungsprofile 6.1) — EIN Parameter statt
/// vieler; jede Eigenschaft nennt ihren Ressourcenschlüssel, der deutsche Wert ist der Rückfall. Gefüllt
/// wird es von <c>RaumnutzungTexteHuelle</c> in <c>EPOS.UI.Daten</c>.
///
/// <para>Es trägt <b>Beschriftungen</b>, keinen Zustand; der steht im <see cref="RaumnutzungWeg"/>.</para>
/// </summary>
public sealed class RaumnutzungTexte
{
    // ------------------------------------------------------------------ Blatt und Bereiche

    /// <summary>Der Titel des Blatts (<c>RNP_LBL_BLATT</c>).</summary>
    public string Blatt { get; set; } = "Nutzungsprofile";

    /// <summary>Der Knopf des Wirts, der das Blatt öffnet (<c>RNP_BTN_VERWALTEN</c>).</summary>
    public string KnopfVerwalten { get; set; } = "Nutzungsprofile verwalten…";

    /// <summary>Die Überschrift des Katalogbaums (<c>RNP_LBL_KATALOG</c>).</summary>
    public string Katalog { get; set; } = "Katalog";

    /// <summary>Die Überschrift des Profileditors (<c>RNP_LBL_EDITOR</c>).</summary>
    public string Editor { get; set; } = "Profil";

    /// <summary>Die Überschrift der Vorschau (<c>RNP_LBL_VORSCHAU</c>).</summary>
    public string Vorschau { get; set; } = "Vorschau der Kalender";

    /// <summary>Der Reiter des Katalogs (<c>RNP_LBL_REITER_KATALOG</c>).</summary>
    public string ReiterKatalog { get; set; } = "Profile";

    /// <summary>Der Reiter der Zuordnung (<c>RNP_LBL_REITER_ZUORDNUNG</c>).</summary>
    public string ReiterZuordnung { get; set; } = "Zuordnung";

    /// <summary>Die leise Zeile „sofort gespeichert" (<c>RNP_TXT_SOFORT</c>).</summary>
    public string HinweisSofort { get; set; }
        = "Der Katalog gilt für alle Projekte; jede Handlung wird sofort gespeichert.";

    // ------------------------------------------------------------------ Arten

    /// <summary>Die Art „EPOS-Muster" (<c>RNP_LBL_ART_EPOS</c>).</summary>
    public string ArtEposMuster { get; set; } = "EPOS-Muster";

    /// <summary>Die Art „DIN V 18599-10" (<c>RNP_LBL_ART_DIN</c>).</summary>
    public string ArtDin { get; set; } = "DIN/TS 18599-10";

    /// <summary>Die Art „SIA 2024" (<c>RNP_LBL_ART_SIA</c>).</summary>
    public string ArtSia { get; set; } = "SIA 2024";

    /// <summary>Die Art „VDI 2078" (<c>RNP_LBL_ART_VDI</c>).</summary>
    public string ArtVdi { get; set; } = "VDI 2078";

    /// <summary>Die Art „Eigene" (<c>RNP_LBL_ART_EIGEN</c>).</summary>
    public string ArtEigen { get; set; } = "Eigene";

    /// <summary>Die Zuordnungsart „DIN-Nummer" (<c>RNP_LBL_ZUART_DIN</c>).</summary>
    public string ZuordnungsartDin { get; set; } = "DIN-Nummer";

    /// <summary>Die Zuordnungsart „IFC-Klasse" (<c>RNP_LBL_ZUART_IFC</c>).</summary>
    public string ZuordnungsartIfc { get; set; } = "IFC-Klasse";

    /// <summary>Die Zuordnungsart „HottCAD-Raumtyp" (<c>RNP_LBL_ZUART_HOTTCAD</c>).</summary>
    public string ZuordnungsartHottcad { get; set; } = "HottCAD-Raumtyp";

    // ------------------------------------------------------------------ Knöpfe

    /// <summary>„Neue Kategorie…" (<c>RNP_BTN_KATEGORIE_NEU</c>).</summary>
    public string KnopfKategorieNeu { get; set; } = "Neue Kategorie…";

    /// <summary>„Umbenennen" (<c>RNP_BTN_UMBENENNEN</c>).</summary>
    public string KnopfUmbenennen { get; set; } = "Umbenennen";

    /// <summary>„Neues Profil…" (<c>RNP_BTN_PROFIL_NEU</c>).</summary>
    public string KnopfProfilNeu { get; set; } = "Neues Profil…";

    /// <summary>„Duplizieren" (<c>RNP_BTN_DUPLIZIEREN</c>).</summary>
    public string KnopfDuplizieren { get; set; } = "Duplizieren";

    /// <summary>„Löschen" (<c>RNP_BTN_LOESCHEN</c>).</summary>
    public string KnopfLoeschen { get; set; } = "Löschen";

    /// <summary>„Speichern" (<c>RNP_BTN_SPEICHERN</c>).</summary>
    public string KnopfSpeichern { get; set; } = "Speichern";

    /// <summary>„Abbrechen" (<c>RNP_BTN_ABBRECHEN</c>).</summary>
    public string KnopfAbbrechen { get; set; } = "Abbrechen";

    /// <summary>„Vorschau" (<c>RNP_BTN_VORSCHAU</c>).</summary>
    public string KnopfVorschau { get; set; } = "Vorschau";

    /// <summary>„Zeile anlegen" (<c>RNP_BTN_ZEILE_NEU</c>).</summary>
    public string KnopfZeileNeu { get; set; } = "Zeile anlegen";

    // ------------------------------------------------------------------ Spalten und Felder

    /// <summary>Die Spalte „Nummer" (<c>RNP_LBL_NUMMER</c>).</summary>
    public string LabelNummer { get; set; } = "Nummer";

    /// <summary>Die Spalte „Name" (<c>RNP_LBL_NAME</c>).</summary>
    public string LabelName { get; set; } = "Name";

    /// <summary>Die Spalte „Beschreibung" (<c>RNP_LBL_BESCHREIBUNG</c>).</summary>
    public string LabelBeschreibung { get; set; } = "Beschreibung";

    /// <summary>Das Feld „Quellenhinweis" (<c>RNP_LBL_QUELLE</c>).</summary>
    public string LabelQuelle { get; set; } = "Quellenhinweis";

    /// <summary>Die Spalte „Aktionen" (<c>RNP_LBL_AKTIONEN</c>).</summary>
    public string LabelAktionen { get; set; } = "Aktionen";

    /// <summary>Die Spalte „Art" (<c>RNP_LBL_ART</c>).</summary>
    public string LabelArt { get; set; } = "Art";

    /// <summary>Die Spalte „Schlüssel" (<c>RNP_LBL_SCHLUESSEL</c>).</summary>
    public string LabelSchluessel { get; set; } = "Schlüssel";

    /// <summary>Die Spalte „Profil" (<c>RNP_LBL_PROFIL</c>).</summary>
    public string LabelProfil { get; set; } = "Profil";

    /// <summary>Der Eintrag „keine" der Profilwahl (<c>RNP_LBL_KEINE</c>).</summary>
    public string LabelKeine { get; set; } = "keine";

    /// <summary>Die Gruppe „Nutzungszeit und Woche" (<c>RNP_LBL_GRUPPE_ZEIT</c>).</summary>
    public string GruppeZeit { get; set; } = "Nutzungszeit und Woche";

    /// <summary>Die Gruppe „Sollwerte" (<c>RNP_LBL_GRUPPE_SOLL</c>).</summary>
    public string GruppeSollwerte { get; set; } = "Sollwerte Heizen und Kühlen";

    /// <summary>Die Gruppe „Außenluft" (<c>RNP_LBL_GRUPPE_LUFT</c>).</summary>
    public string GruppeLuft { get; set; } = "Außenluft";

    /// <summary>Die Gruppe „Lasten" (<c>RNP_LBL_GRUPPE_LASTEN</c>).</summary>
    public string GruppeLasten { get; set; } = "Personen, Geräte und Beleuchtung";

    /// <summary>„Nutzung von" (<c>RNP_LBL_NUTZUNG_VON</c>).</summary>
    public string LabelNutzungVon { get; set; } = "Nutzung von";

    /// <summary>„Nutzung bis" (<c>RNP_LBL_NUTZUNG_BIS</c>).</summary>
    public string LabelNutzungBis { get; set; } = "Nutzung bis";

    /// <summary>„Betrieb von" (<c>RNP_LBL_BETRIEB_VON</c>).</summary>
    public string LabelBetriebVon { get; set; } = "Betrieb von";

    /// <summary>„Betrieb bis" (<c>RNP_LBL_BETRIEB_BIS</c>).</summary>
    public string LabelBetriebBis { get; set; } = "Betrieb bis";

    /// <summary>„Nutzungstage der Woche" (<c>RNP_LBL_WOCHE</c>).</summary>
    public string LabelWoche { get; set; } = "Nutzungstage der Woche";


    /// <summary>„Feiertage wie Sonntag" (<c>RNP_LBL_FEIERTAGE</c>).</summary>
    public string LabelFeiertage { get; set; } = "Feiertage wie Sonntag";

    /// <summary>„Heizen im Betrieb" (<c>RNP_LBL_HEIZ_SOLL</c>).</summary>
    public string LabelHeizSoll { get; set; } = "Heizen im Betrieb";

    /// <summary>„Heizen außerhalb" (<c>RNP_LBL_HEIZ_AUSSERHALB</c>).</summary>
    public string LabelHeizAusserhalb { get; set; } = "Heizen außerhalb";

    /// <summary>„Heizen außerhalb aus" (<c>RNP_LBL_HEIZ_AUS</c>).</summary>
    public string LabelHeizAus { get; set; } = "Heizen außerhalb aus";

    /// <summary>„Kühlen im Betrieb" (<c>RNP_LBL_KUEHL_SOLL</c>).</summary>
    public string LabelKuehlSoll { get; set; } = "Kühlen im Betrieb";

    /// <summary>„Kühlen außerhalb" (<c>RNP_LBL_KUEHL_AUSSERHALB</c>).</summary>
    public string LabelKuehlAusserhalb { get; set; } = "Kühlen außerhalb";

    /// <summary>„Kühlen außerhalb aus" (<c>RNP_LBL_KUEHL_AUS</c>).</summary>
    public string LabelKuehlAus { get; set; } = "Kühlen außerhalb aus";

    /// <summary>„Außenluft im Betrieb" (<c>RNP_LBL_LUFT</c>).</summary>
    public string LabelLuft { get; set; } = "Außenluft im Betrieb";

    /// <summary>„Außenluft außerhalb" (<c>RNP_LBL_LUFT_AUSSERHALB</c>).</summary>
    public string LabelLuftAusserhalb { get; set; } = "Außenluft außerhalb";

    /// <summary>„Einheit der Außenluft" (<c>RNP_LBL_LUFT_EINHEIT</c>).</summary>
    public string LabelLuftEinheit { get; set; } = "Einheit der Außenluft";

    /// <summary>„Fläche je Person" (<c>RNP_LBL_PERSONEN_FLAECHE</c>).</summary>
    public string LabelPersonenFlaeche { get; set; } = "Fläche je Person";

    /// <summary>„Wärme je Person" (<c>RNP_LBL_PERSONEN_WAERME</c>).</summary>
    public string LabelPersonenWaerme { get; set; } = "Wärme je Person";

    /// <summary>„Personen im Betrieb" (<c>RNP_LBL_PERSONEN_ANTEIL</c>).</summary>
    public string LabelPersonenAnteil { get; set; } = "Personen im Betrieb";

    /// <summary>„Personen außerhalb" (<c>RNP_LBL_PERSONEN_AUSSERHALB</c>).</summary>
    public string LabelPersonenAusserhalb { get; set; } = "Personen außerhalb";

    /// <summary>„Gerätelast" (<c>RNP_LBL_GERAETE</c>).</summary>
    public string LabelGeraete { get; set; } = "Gerätelast";

    /// <summary>„Geräte im Betrieb" (<c>RNP_LBL_GERAETE_ANTEIL</c>).</summary>
    public string LabelGeraeteAnteil { get; set; } = "Geräte im Betrieb";

    /// <summary>„Geräte außerhalb" (<c>RNP_LBL_GERAETE_AUSSERHALB</c>).</summary>
    public string LabelGeraeteAusserhalb { get; set; } = "Geräte außerhalb";

    /// <summary>„Beleuchtung" (<c>RNP_LBL_BELEUCHTUNG</c>).</summary>
    public string LabelBeleuchtung { get; set; } = "Beleuchtung";

    /// <summary>„Gleichzeitigkeit der Beleuchtung" (<c>RNP_LBL_BELEUCHTUNG_ANTEIL</c>).</summary>
    public string LabelBeleuchtungAnteil { get; set; } = "Gleichzeitigkeit der Beleuchtung";

    // ------------------------------------------------------------------ Zeilenbild und Stunden

    /// <summary>Die Überschrift des Zeilenbilds (<c>RNP_LBL_ZEILENBILD</c>).</summary>
    public string LabelZeilenbild { get; set; } = "Zeilenbild";

    /// <summary>Die Überschrift der Stundenprofile (<c>RNP_LBL_STUNDEN</c>).</summary>
    public string LabelStunden { get; set; } = "Stundenprofile";

    /// <summary>Die Spalte „Zeile" (<c>RNP_LBL_ZEILE</c>).</summary>
    public string LabelZeile { get; set; } = "Zeile";

    /// <summary>Die Spalte „Wert" (<c>RNP_LBL_WERT</c>).</summary>
    public string LabelWert { get; set; } = "Wert";

    /// <summary>Die Spalte „Zeitfenster" (<c>RNP_LBL_FENSTER</c>).</summary>
    public string LabelFenster { get; set; } = "Zeitfenster";

    /// <summary>Die Spalte „Wochentage" (<c>RNP_LBL_WOCHENTAGE</c>).</summary>
    public string LabelWochentage { get; set; } = "Wochentage";

    /// <summary>Die Tagesart „Werktag" (<c>RNP_LBL_TAGESART_WERKTAG</c>).</summary>
    public string TagesartWerktag { get; set; } = "Werktag";

    /// <summary>Die Tagesart „nutzungsfreier Tag" (<c>RNP_LBL_TAGESART_FREI</c>).</summary>
    public string TagesartFrei { get; set; } = "nutzungsfreier Tag";

    // ------------------------------------------------------------------ Zustände und Meldungen

    /// <summary>Der Kurztext des Schlosses (<c>RNP_TXT_AUSGELIEFERT</c>).</summary>
    public string Ausgeliefert { get; set; } = "Gehört zur Auslieferung — nur duplizieren";

    /// <summary>Der leere Katalog (<c>RNP_TXT_LEER</c>).</summary>
    public string TextLeer { get; set; } = "Noch keine Kategorie im Katalog.";

    /// <summary>Die leere Kategorie (<c>RNP_TXT_KATEGORIE_LEER</c>).</summary>
    public string TextKategorieLeer { get; set; } = "Diese Kategorie führt kein Profil.";

    /// <summary>Kein Profil gewählt (<c>RNP_TXT_OHNE_WAHL</c>).</summary>
    public string TextOhneWahl { get; set; } = "Wählen Sie links ein Profil.";

    /// <summary>Ein Normprofil ohne Werte (<c>RNP_TXT_OHNE_WERTE</c>).</summary>
    public string TextOhneWerte { get; set; } = "ohne Werte — Duplizieren, um Werte einzutragen";

    /// <summary>Eine Größe ist nicht belegt (<c>RNP_TXT_NICHT_BELEGT</c>).</summary>
    public string TextNichtBelegt { get; set; } = "nicht belegt — das Ziel behält seinen Kalender";

    /// <summary>Der Grund ohne Katalogtabellen (<c>RNP_TXT_OHNE_TABELLEN</c>).</summary>
    public string GrundOhneTabellen { get; set; } = "Der Nutzungsprofilkatalog ist in dieser Datenbank nicht angelegt.";

    /// <summary>Die Rückfrage vor dem Löschen eines Profils (<c>RNP_FRAGE_PROFIL_LOESCHEN</c>, {0} = Name).</summary>
    public string FrageProfilLoeschen { get; set; } = "Das Profil „{0}“ löschen?";

    /// <summary>Der Zusatz der Rückfrage mit Zuordnungen (<c>RNP_FRAGE_PROFIL_ZUORDNUNG</c>, {0} = Anzahl).</summary>
    public string FrageProfilZuordnung { get; set; } = "{0} Zuordnungszeile(n) fallen dabei auf „keine“.";

    /// <summary>Die Rückfrage vor dem Löschen einer Kategorie (<c>RNP_FRAGE_KATEGORIE_LOESCHEN</c>, {0} = Name).</summary>
    public string FrageKategorieLoeschen { get; set; } = "Die Kategorie „{0}“ samt ihren Profilen löschen?";

    /// <summary>Die Rückfrage vor dem Löschen einer Zuordnungszeile (<c>RNP_FRAGE_ZUORDNUNG_LOESCHEN</c>, {0} = Schlüssel).</summary>
    public string FrageZuordnungLoeschen { get; set; } = "Die Zuordnung „{0}“ löschen?";

    /// <summary>
    /// Die Lesezeile der abgeleiteten Nutzungstage (<c>RNP_TXT_NUTZUNGSTAGE</c>, {0} = Tage aus Wochenmuster und
    /// Feiertagen; E93).
    /// </summary>
    public string TextNutzungstage { get; set; } = "Nutzungstage im Jahr: {0} (aus Wochenmuster und Feiertagen)";

    /// <summary>
    /// Dieselbe Zeile am Ziel mit seinen Ferien (<c>RNP_TXT_NUTZUNGSTAGE_FERIEN</c>, {0} = aus Wochenmuster und
    /// Feiertagen, {1} = Ferientage, {2} = Nutzungstage am Ziel; E93).
    /// </summary>
    public string TextNutzungstageFerien { get; set; } =
        "Nutzungstage im Jahr: {0} (aus Wochenmuster und Feiertagen), abzüglich {1} Ferientage = {2}";

    // ------------------------------------------------------------------ Nutzungsprofil übernehmen (Stufe NP3b)

    /// <summary>Der Knopf im Reiter Konditionierung und im Zonendialog (<c>RNP_BTN_PROFIL_UEBERNEHMEN</c>).</summary>
    public string KnopfUebernehmenProfil { get; set; } = "Nutzungsprofil übernehmen…";

    /// <summary>Die Überschrift der Auswahl und der Rückfrage (<c>RNP_LBL_UEBERNAHME</c>).</summary>
    public string TitelUebernahme { get; set; } = "Nutzungsprofil übernehmen";

    /// <summary>Der Knopf der Auswahl (<c>RNP_BTN_UEBERNEHMEN</c>).</summary>
    public string KnopfUebernehmen { get; set; } = "Übernehmen";

    /// <summary>Schließt die Auswahl ohne Übernahme (<c>RNP_BTN_SCHLIESSEN</c>).</summary>
    public string KnopfSchliessen { get; set; } = "Schließen";

    /// <summary>Die leise Zeile unter der Auswahl (<c>RNP_TXT_UEBERNAHME_OK</c>).</summary>
    public string HinweisUebernahme { get; set; } = "Das Profil geht in den Arbeitsstand; gespeichert wird mit OK. Eigene Perioden und Ausnahmetage bleiben.";

    /// <summary>Leere Liste (<c>RNP_TXT_KEIN_PROFIL</c>).</summary>
    public string TextKeinProfil { get; set; } = "Der Katalog führt kein Nutzungsprofil.";

    /// <summary>Kurzform eines Profils ohne Kennwerte (NP-F13) (<c>RNP_TXT_OHNE_WERTE_KURZ</c>).</summary>
    public string TextOhneWerteKurz { get; set; } = "ohne Werte";

    /// <summary>Die sieben Wochentage, kurz, durch Komma getrennt (Kurzform der Liste) (<c>RNP_TXT_TAGE_KURZ</c>).</summary>
    public string TageKurz { get; set; } = "Mo,Di,Mi,Do,Fr,Sa,So";

    /// <summary>Die Nennwertzeile ({0} = Größe, {1} = Herleitung) (<c>RNP_TXT_NENNWERT</c>).</summary>
    public string Nennwertzeile { get; set; } = "Nennwert {0}: {1}";

    /// <summary>Erster Satz der Rückfrage ({0} = Profilname) (<c>RNP_FRAGE_UEBERNEHMEN</c>).</summary>
    public string FrageUebernehmen { get; set; } = "Das Nutzungsprofil „{0}“ übernehmen?";

    /// <summary>Eine Zeile der Rückfrage ({0} = Größe, {1} = was geschieht) (<c>RNP_FRAGE_ZEILE</c>).</summary>
    public string FrageZeile { get; set; } = "{0}: {1}";

    /// <summary>Größe ohne Kalender am Ziel (<c>RNP_FRAGE_UEBERNIMMT</c>).</summary>
    public string FrageUebernimmt { get; set; } = "wird übernommen";

    /// <summary>Größe mit Kalender am Ziel (P12) (<c>RNP_FRAGE_ERSETZT</c>).</summary>
    public string FrageErsetzt { get; set; } = "ersetzt den Matrixbereich des Kalenders; eigene Perioden und Ausnahmetage bleiben";

    /// <summary>Größe, die das Profil nicht trägt (NP-F6) (<c>RNP_FRAGE_BLEIBT</c>).</summary>
    public string FrageBleibt { get; set; } = "nicht belegt — bleibt, wie es ist";

    /// <summary>Heizen und Kühlen an einer unbeheizten Zone (<c>RNP_FRAGE_UNBEHEIZT</c>).</summary>
    public string FrageUnbeheizt { get; set; } = "unbeheizte Zone — übersprungen";

    /// <summary>Zusatz, wenn die Lüftung die Aufteilung verlangt (E56 F5 (a)) (<c>RNP_FRAGE_AUFTEILEN</c>).</summary>
    public string FrageAufteilen { get; set; } = "Die Gesamtangabe des Luftwechsels wird in Infiltration und Nutzerlüftung aufgeteilt; der wirksame Luftwechsel bleibt.";

    /// <summary>Leeres Profil an einer Zone (NP-F13) (<c>RNP_FRAGE_OHNE_WERTE</c>).</summary>
    public string FrageOhneWerte { get; set; } = "Profil ohne Werte: kein Kalender, die Zone trägt nur den Namen.";

    /// <summary>Zusatz an einer Zone ({0} = Profilname) (<c>RNP_FRAGE_NAME</c>).</summary>
    public string FrageName { get; set; } = "Die Zone trägt danach das Nutzungsprofil „{0}“.";

    /// <summary>Weiche Sperre von „Übernehmen“ am Gebäude (<c>RNP_GRUND_OHNE_WERTE</c>).</summary>
    public string GrundOhneWerte { get; set; } = "Profil ohne Werte — hier gibt es nichts zu übernehmen; Duplizieren, um Werte einzutragen.";

    /// <summary>Weiche Sperre von „Übernehmen“ ohne Wahl (<c>RNP_GRUND_OHNE_WAHL</c>).</summary>
    public string GrundOhneWahl { get; set; } = "Zuerst ein Profil wählen.";

    /// <summary>Meldung nach „Ja“ ({0} = Profilname) (<c>RNP_TXT_UEBERNOMMEN</c>).</summary>
    public string TextUebernommen { get; set; } = "Nutzungsprofil „{0}“ übernommen — gespeichert wird mit OK.";

    /// <summary>Die Kopfzeile der Zone ({0} = zuletzt übernommenes Profil) (<c>RNP_TXT_ZONENKOPF</c>).</summary>
    public string Zonenkopf { get; set; } = "Nutzungsprofil: {0}";

    /// <summary>Die Vorschläge der Nutzung in „Als Vorlage speichern…" (<c>RNP_LBL_NUTZUNG_VORSCHLAG</c>, NP-F15).</summary>
    public string LabelNutzungVorschlag { get; set; } = "Vorschlag aus dem Katalog";

    /// <summary>Der Knopf am Zonenbaum des Imports (<c>RNP_BTN_PROFILE_IMPORT</c>).</summary>
    public string KnopfProfileImport { get; set; } = "Nutzungsprofile…";

    /// <summary>Die Reiterleiste der Wochenvorschau (aria-label) (<c>RNP_LBL_VORSCHAU_GROESSE</c>).</summary>
    public string LabelVorschauGroesse { get; set; } = "Größe der Vorschau";
}
