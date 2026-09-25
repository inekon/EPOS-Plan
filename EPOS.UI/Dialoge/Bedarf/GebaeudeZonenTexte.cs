using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte der Zonen im Gebäudedialog (Gebäudesimulation G3, Welle D2) — Reiter „Zonen",
/// Herleitungszeile des Hüllwegs, Übernahmeknopf samt Sperrgründen und Rückfragen, Summenregel.
/// EIN Parameter statt vieler; jede Eigenschaft füllt sich aus <c>MyResource</c> (Präfix
/// <c>GEBZ_</c>) und nennt ihren Schlüssel.
/// </summary>
public sealed class GebaeudeZonenTexte
{
    /// <summary><c>GEBZ_REITER</c></summary>
    public string Reiter { get; set; } = Resource.GEBZ_REITER;

    /// <summary><c>GEBZ_GRP_ZONEN</c></summary>
    public string Gruppe { get; set; } = Resource.GEBZ_GRP_ZONEN;

    /// <summary><c>GEBZ_ZEILE_KLASSENWEG</c></summary>
    public string ZeileKlassenweg { get; set; } = Resource.GEBZ_ZEILE_KLASSENWEG;

    /// <summary><c>GEBZ_ZEILE_BAUTEILWEG</c> — {0} Zone, {1} Zahl der Bauteile.</summary>
    public string ZeileBauteilweg { get; set; } = Resource.GEBZ_ZEILE_BAUTEILWEG;

    /// <summary><c>GEBZ_ZEILE_KATALOG</c></summary>
    public string ZeileKatalog { get; set; } = Resource.GEBZ_ZEILE_KATALOG;

    /// <summary><c>GEBZ_BTN_UEBERNEHMEN</c></summary>
    public string KnopfUebernehmen { get; set; } = Resource.GEBZ_BTN_UEBERNEHMEN;

    /// <summary><c>GEBZ_HINWEIS_UEBERNEHMEN</c> — die leise Erklärzeile unter dem Knopf.</summary>
    public string HinweisUebernehmen { get; set; } = Resource.GEBZ_HINWEIS_UEBERNEHMEN;

    /// <summary><c>GEBZ_SPERRE_KATALOG</c></summary>
    public string SperreKatalog { get; set; } = Resource.GEBZ_SPERRE_KATALOG;

    /// <summary><c>GEBZ_SPERRE_ZONE</c> — {0} Zone.</summary>
    public string SperreZone { get; set; } = Resource.GEBZ_SPERRE_ZONE;

    /// <summary><c>GEBZ_SPERRE_TAGESBILANZ</c></summary>
    public string SperreTagesbilanz { get; set; } = Resource.GEBZ_SPERRE_TAGESBILANZ;

    /// <summary><c>GEBZ_SPERRE_LAEUFT</c></summary>
    public string SperreLaeuft { get; set; } = Resource.GEBZ_SPERRE_LAEUFT;

    /// <summary><c>GEBZ_FRAGE_UEBERNEHMEN</c> — {0} Faktor, {1} Nutzfläche des Gebäudes, {2} der Zone, {3} Angabe, {4} Einheit.</summary>
    public string FrageUebernehmen { get; set; } = Resource.GEBZ_FRAGE_UEBERNEHMEN;

    /// <summary><c>GEBZ_FRAGE_GRENZEN</c></summary>
    public string FrageGrenzen { get; set; } = Resource.GEBZ_FRAGE_GRENZEN;

    /// <summary><c>GEBZ_LEER</c></summary>
    public string Leer { get; set; } = Resource.GEBZ_LEER;

    /// <summary><c>GEBZ_SP_ZONE</c></summary>
    public string SpalteZone { get; set; } = Resource.GEBZ_SP_ZONE;

    /// <summary><c>GEBZ_SP_NUTZFLAECHE</c></summary>
    public string SpalteNutzflaeche { get; set; } = Resource.GEBZ_SP_NUTZFLAECHE;

    /// <summary><c>GEBZ_SP_HT</c></summary>
    public string SpalteHT { get; set; } = Resource.GEBZ_SP_HT;

    /// <summary><c>GEBZ_SP_BAUTEILE</c></summary>
    public string SpalteBauteile { get; set; } = Resource.GEBZ_SP_BAUTEILE;

    /// <summary><c>GEBZ_SP_AKTIONEN</c></summary>
    public string SpalteAktionen { get; set; } = Resource.GEBZ_SP_AKTIONEN;

    /// <summary><c>GEBZ_BTN_OEFFNEN</c></summary>
    public string KnopfOeffnen { get; set; } = Resource.GEBZ_BTN_OEFFNEN;

    /// <summary><c>GEBZ_BTN_ENTFERNEN</c></summary>
    public string KnopfEntfernen { get; set; } = Resource.GEBZ_BTN_ENTFERNEN;

    /// <summary><c>GEBZ_FRAGE_ENTFERNEN</c> — {0} Zone, {1} Zahl der Bauteile.</summary>
    public string FrageEntfernen { get; set; } = Resource.GEBZ_FRAGE_ENTFERNEN;

    /// <summary><c>GEBZ_ZEILE_SUMMENREGEL</c> — {0} Zone.</summary>
    public string ZeileSummenregel { get; set; } = Resource.GEBZ_ZEILE_SUMMENREGEL;

    /// <summary><c>GEBZ_ZEILE_FENSTER</c> — {0} Fensterfläche der Zone.</summary>
    public string ZeileFenster { get; set; } = Resource.GEBZ_ZEILE_FENSTER;

    /// <summary><c>GEBZ_ZEILE_PSI</c> — die Zeile der Wärmebrücken in der abgeleiteten Hülle.</summary>
    public string ZeilePsi { get; set; } = Resource.GEBZ_ZEILE_PSI;

    /// <summary><c>GEBZ_ZEILE_NUTZFLAECHE</c> — {0} Nutzfläche der Zone.</summary>
    public string ZeileNutzflaeche { get; set; } = Resource.GEBZ_ZEILE_NUTZFLAECHE;

    /// <summary><c>GEBZ_ZEILE_NAME_PROJEKT</c> — {0} Name der Projektkopie.</summary>
    public string ZeileNameProjekt { get; set; } = Resource.GEBZ_ZEILE_NAME_PROJEKT;

    /// <summary><c>GEBZ_FRAGE_SPEICHERN_UNTER</c> — {0} Zone, {1} Zahl der Bauteile.</summary>
    public string FrageSpeichernUnter { get; set; } = Resource.GEBZ_FRAGE_SPEICHERN_UNTER;

    /// <summary><c>GEBZ_MSG_ZONEN</c> — {0} Grund.</summary>
    public string MeldungZonen { get; set; } = Resource.GEBZ_MSG_ZONEN;

    /// <summary><c>GEBZ_MSG_AUFBAU</c> — {0} Aufbau, {1} Grund.</summary>
    public string MeldungAufbau { get; set; } = Resource.GEBZ_MSG_AUFBAU;

    /// <summary><c>GEBZ_TITEL_PROJEKT</c> — der Titel des Editors in der Betriebsart Projekt.</summary>
    public string TitelProjekt { get; set; } = Resource.GEBZ_TITEL_PROJEKT;

    /// <summary><c>ALLG_BTN_JA</c></summary>
    public string Ja { get; set; } = Resource.ALLG_BTN_JA;

    /// <summary><c>ALLG_BTN_NEIN</c></summary>
    public string Nein { get; set; } = Resource.ALLG_BTN_NEIN;
}

/// <summary>
/// Die Anzeigetexte des <see cref="ZonenDialog"/> (Präfix <c>ZONDLG_</c>) — ein Bündel.
/// </summary>
public sealed class ZonenDialogTexte
{
    /// <summary><c>ZONDLG_TITEL</c></summary>
    public string Titel { get; set; } = Resource.ZONDLG_TITEL;

    /// <summary><c>ZONDLG_LBL_BEZEICHNER</c></summary>
    public string LabelBezeichner { get; set; } = Resource.ZONDLG_LBL_BEZEICHNER;

    /// <summary><c>ZONDLG_LBL_NUTZFLAECHE</c></summary>
    public string LabelNutzflaeche { get; set; } = Resource.ZONDLG_LBL_NUTZFLAECHE;

    /// <summary><c>ZONDLG_PLATZHALTER_NUTZFLAECHE</c> — {0} Nutzfläche des Gebäudes.</summary>
    public string PlatzhalterNutzflaeche { get; set; } = Resource.ZONDLG_PLATZHALTER_NUTZFLAECHE;

    /// <summary><c>ZONDLG_ZEILE_NUTZFLAECHE</c> — {0} Nutzfläche des Gebäudes.</summary>
    public string ZeileNutzflaeche { get; set; } = Resource.ZONDLG_ZEILE_NUTZFLAECHE;

    /// <summary><c>ZONDLG_GRP_BAUTEILE</c></summary>
    public string GruppeBauteile { get; set; } = Resource.ZONDLG_GRP_BAUTEILE;

    /// <summary><c>ZONDLG_SP_ART</c></summary>
    public string SpalteArt { get; set; } = Resource.ZONDLG_SP_ART;

    /// <summary><c>ZONDLG_SP_BEZEICHNUNG</c></summary>
    public string SpalteBezeichnung { get; set; } = Resource.ZONDLG_SP_BEZEICHNUNG;

    /// <summary><c>ZONDLG_SP_FLAECHE</c></summary>
    public string SpalteFlaeche { get; set; } = Resource.ZONDLG_SP_FLAECHE;

    /// <summary><c>ZONDLG_SP_U</c></summary>
    public string SpalteU { get; set; } = Resource.ZONDLG_SP_U;

    /// <summary><c>ZONDLG_SP_AZIMUT</c></summary>
    public string SpalteAzimut { get; set; } = Resource.ZONDLG_SP_AZIMUT;

    /// <summary><c>ZONDLG_SP_NEIGUNG</c></summary>
    public string SpalteNeigung { get; set; } = Resource.ZONDLG_SP_NEIGUNG;

    /// <summary><c>ZONDLG_SP_RAND</c></summary>
    public string SpalteRand { get; set; } = Resource.ZONDLG_SP_RAND;

    /// <summary><c>ZONDLG_SP_AUFBAU</c></summary>
    public string SpalteAufbau { get; set; } = Resource.ZONDLG_SP_AUFBAU;

    /// <summary><c>GEBZ_SP_AKTIONEN</c></summary>
    public string SpalteAktionen { get; set; } = Resource.GEBZ_SP_AKTIONEN;

    /// <summary><c>ZONDLG_BTN_NEU</c></summary>
    public string NeuesBauteil { get; set; } = Resource.ZONDLG_BTN_NEU;

    /// <summary><c>ZONDLG_LEER</c></summary>
    public string Leer { get; set; } = Resource.ZONDLG_LEER;

    /// <summary><c>ZONDLG_BTN_OEFFNEN</c></summary>
    public string KnopfOeffnen { get; set; } = Resource.ZONDLG_BTN_OEFFNEN;

    /// <summary><c>ZONDLG_BTN_ENTFERNEN</c></summary>
    public string KnopfEntfernen { get; set; } = Resource.ZONDLG_BTN_ENTFERNEN;

    /// <summary><c>ZONDLG_SUMME_GRUPPE</c> — {0} Gruppe, {1} Fläche.</summary>
    public string SummeGruppe { get; set; } = Resource.ZONDLG_SUMME_GRUPPE;

    /// <summary><c>ZONDLG_SUMME_HT</c> — {0} H_T.</summary>
    public string SummeHT { get; set; } = Resource.ZONDLG_SUMME_HT;

    /// <summary><c>ZONDLG_MSG_NAME</c></summary>
    public string MeldungName { get; set; } = Resource.ZONDLG_MSG_NAME;

    /// <summary><c>ZONE_MSG_NUTZFLAECHE</c> — {0} Zone.</summary>
    public string MeldungNutzflaeche { get; set; } = Resource.ZONE_MSG_NUTZFLAECHE;

    /// <summary><c>GEBK_MSG_UNGUELTIG</c> — {0} Feld.</summary>
    public string MeldungUngueltig { get; set; } = Resource.GEBK_MSG_UNGUELTIG;

    /// <summary><c>ZONDLG_U_AUS_AUFBAU</c> — der Zusatz einer U-Zelle, die aus dem Aufbau kommt.</summary>
    public string UAusAufbau { get; set; } = Resource.ZONDLG_U_AUS_AUFBAU;

    /// <summary><c>ALLG_BTN_OK</c></summary>
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;

    /// <summary><c>ALLG_BTN_ABBRECHEN</c></summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;
}

/// <summary>
/// Die Anzeigetexte des <see cref="BauteilDialog"/> (Präfix <c>BTDLG_</c>) — ein Bündel.
/// </summary>
public sealed class BauteilDialogTexte
{
    /// <summary><c>BTDLG_TITEL</c></summary>
    public string Titel { get; set; } = Resource.BTDLG_TITEL;

    /// <summary><c>BTDLG_TITEL_NEU</c></summary>
    public string TitelNeu { get; set; } = Resource.BTDLG_TITEL_NEU;

    /// <summary><c>BTDLG_LBL_ART</c></summary>
    public string LabelArt { get; set; } = Resource.BTDLG_LBL_ART;

    /// <summary><c>BTDLG_LBL_BEZEICHNUNG</c></summary>
    public string LabelBezeichnung { get; set; } = Resource.BTDLG_LBL_BEZEICHNUNG;

    /// <summary><c>BTDLG_LBL_FLAECHE</c></summary>
    public string LabelFlaeche { get; set; } = Resource.BTDLG_LBL_FLAECHE;

    /// <summary><c>BTDLG_LBL_AZIMUT</c></summary>
    public string LabelAzimut { get; set; } = Resource.BTDLG_LBL_AZIMUT;

    /// <summary><c>BTDLG_HINWEIS_AZIMUT</c></summary>
    public string HinweisAzimut { get; set; } = Resource.BTDLG_HINWEIS_AZIMUT;

    /// <summary><c>BTDLG_LBL_NEIGUNG</c></summary>
    public string LabelNeigung { get; set; } = Resource.BTDLG_LBL_NEIGUNG;

    /// <summary><c>BTDLG_PLATZHALTER_NEIGUNG</c> — {0} Vorgabe nach Bauteilart.</summary>
    public string PlatzhalterNeigung { get; set; } = Resource.BTDLG_PLATZHALTER_NEIGUNG;

    /// <summary><c>BTDLG_LBL_RAND</c></summary>
    public string LabelRand { get; set; } = Resource.BTDLG_LBL_RAND;

    /// <summary><c>BTDLG_RAND_VORGABE_AUSSEN</c></summary>
    public string RandVorgabeAussen { get; set; } = Resource.BTDLG_RAND_VORGABE_AUSSEN;

    /// <summary><c>BTDLG_RAND_VORGABE_INNEN</c></summary>
    public string RandVorgabeInnen { get; set; } = Resource.BTDLG_RAND_VORGABE_INNEN;

    /// <summary><c>BTDLG_RAND_AUSSENLUFT</c></summary>
    public string RandAussenluft { get; set; } = Resource.BTDLG_RAND_AUSSENLUFT;

    /// <summary><c>BTDLG_RAND_ERDREICH</c></summary>
    public string RandErdreich { get; set; } = Resource.BTDLG_RAND_ERDREICH;

    /// <summary><c>BTDLG_RAND_UNBEHEIZT</c></summary>
    public string RandUnbeheizt { get; set; } = Resource.BTDLG_RAND_UNBEHEIZT;

    /// <summary><c>BTDLG_LBL_GWERT</c></summary>
    public string LabelGWert { get; set; } = Resource.BTDLG_LBL_GWERT;

    /// <summary><c>BTDLG_LBL_RAHMEN</c></summary>
    public string LabelRahmen { get; set; } = Resource.BTDLG_LBL_RAHMEN;

    /// <summary><c>BTDLG_LBL_VERSCHATTUNG</c></summary>
    public string LabelVerschattung { get; set; } = Resource.BTDLG_LBL_VERSCHATTUNG;

    /// <summary><c>BTDLG_PLATZHALTER_GEBAEUDE</c></summary>
    public string PlatzhalterGebaeude { get; set; } = Resource.BTDLG_PLATZHALTER_GEBAEUDE;

    /// <summary><c>BTDLG_LBL_PSIL</c></summary>
    public string LabelPsiL { get; set; } = Resource.BTDLG_LBL_PSIL;

    /// <summary><c>BTDLG_LBL_UWERT</c></summary>
    public string LabelUWert { get; set; } = Resource.BTDLG_LBL_UWERT;

    /// <summary><c>BTDLG_PLATZHALTER_U</c></summary>
    public string PlatzhalterU { get; set; } = Resource.BTDLG_PLATZHALTER_U;

    /// <summary><c>BTDLG_GRP_AUFBAU</c></summary>
    public string GruppeAufbau { get; set; } = Resource.BTDLG_GRP_AUFBAU;

    /// <summary><c>BTDLG_LBL_AUFBAU_PROJEKT</c></summary>
    public string LabelAufbauProjekt { get; set; } = Resource.BTDLG_LBL_AUFBAU_PROJEKT;

    /// <summary><c>BTDLG_OHNE_AUFBAU</c></summary>
    public string OhneAufbau { get; set; } = Resource.BTDLG_OHNE_AUFBAU;

    /// <summary><c>BTDLG_LBL_AUFBAU_KATALOG</c></summary>
    public string LabelAufbauKatalog { get; set; } = Resource.BTDLG_LBL_AUFBAU_KATALOG;

    /// <summary><c>BTDLG_KATALOG_WAHL</c></summary>
    public string KatalogWahl { get; set; } = Resource.BTDLG_KATALOG_WAHL;

    /// <summary><c>BTDLG_BTN_AUFBAU_UEBERNEHMEN</c></summary>
    public string KnopfAufbauUebernehmen { get; set; } = Resource.BTDLG_BTN_AUFBAU_UEBERNEHMEN;

    /// <summary><c>BTDLG_HINWEIS_KATALOG</c></summary>
    public string HinweisKatalog { get; set; } = Resource.BTDLG_HINWEIS_KATALOG;

    /// <summary><c>BTDLG_AUFBAU_KATALOG</c> — {0} Aufbau.</summary>
    public string AufbauAusKatalog { get; set; } = Resource.BTDLG_AUFBAU_KATALOG;

    /// <summary><c>BTDLG_ZEILE_U_AUFBAU</c> — {0} U des Aufbaus.</summary>
    public string ZeileUAufbau { get; set; } = Resource.BTDLG_ZEILE_U_AUFBAU;

    /// <summary><c>BTDLG_ZEILE_U_ABWEICHUNG</c> — {0} eingetragen, {1} Prozent, {2} Aufbau.</summary>
    public string ZeileUAbweichung { get; set; } = Resource.BTDLG_ZEILE_U_ABWEICHUNG;

    /// <summary><c>BTDLG_ZEILE_FENSTER</c></summary>
    public string ZeileFenster { get; set; } = Resource.BTDLG_ZEILE_FENSTER;

    /// <summary><c>BTDLG_ZEILE_KEINE_SCHICHTEN</c></summary>
    public string ZeileKeineSchichten { get; set; } = Resource.BTDLG_ZEILE_KEINE_SCHICHTEN;

    /// <summary><c>GEBK_MSG_UNGUELTIG</c> — {0} Feld.</summary>
    public string MeldungUngueltig { get; set; } = Resource.GEBK_MSG_UNGUELTIG;

    /// <summary>Die Texte des Schichtenrasters (Ansicht des Aufbaus).</summary>
    public BauteilschichtenTexte Schichten { get; set; } = new();

    /// <summary><c>ALLG_BTN_OK</c></summary>
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;

    /// <summary><c>ALLG_BTN_ABBRECHEN</c></summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;
}
