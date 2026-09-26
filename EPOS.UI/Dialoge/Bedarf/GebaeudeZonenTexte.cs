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

    /// <summary><c>GEBZ_ZEILE_BAUTEILWEG_ZONEN</c> — {0} Zahl der Zonen, {1} Zahl der Bauteile.</summary>
    public string ZeileBauteilwegZonen { get; set; } = Resource.GEBZ_ZEILE_BAUTEILWEG_ZONEN;

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

    /// <summary><c>GEBZ_SPERRE_ZONEN</c> — {0} Zahl der Zonen.</summary>
    public string SperreZonen { get; set; } = Resource.GEBZ_SPERRE_ZONEN;

    /// <summary><c>GEBZ_SPERRE_TAGESBILANZ</c></summary>
    public string SperreTagesbilanz { get; set; } = Resource.GEBZ_SPERRE_TAGESBILANZ;

    /// <summary><c>GEBZ_SPERRE_LAEUFT</c></summary>
    public string SperreLaeuft { get; set; } = Resource.GEBZ_SPERRE_LAEUFT;

    /// <summary><c>GEBZ_FRAGE_UEBERNEHMEN</c> — {0} Faktor, {1} Nutzfläche des Gebäudes, {2} der Zone, {3} Angabe, {4} Einheit.</summary>
    public string FrageUebernehmen { get; set; } = Resource.GEBZ_FRAGE_UEBERNEHMEN;

    /// <summary><c>GEBZ_FRAGE_GRENZEN</c> — {0} die Grenzen des Gebäudes (<see cref="GrenzeHeizung"/>, <see cref="GrenzeKuehlung"/>).</summary>
    public string FrageGrenzen { get; set; } = Resource.GEBZ_FRAGE_GRENZEN;

    /// <summary><c>GEBZ_GRENZE_HEIZUNG</c> — {0} Heizleistungsgrenze in kW.</summary>
    public string GrenzeHeizung { get; set; } = Resource.GEBZ_GRENZE_HEIZUNG;

    /// <summary><c>GEBZ_GRENZE_KUEHLUNG</c> — {0} Kühlleistungsgrenze in kW.</summary>
    public string GrenzeKuehlung { get; set; } = Resource.GEBZ_GRENZE_KUEHLUNG;

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

    /// <summary><c>GEBZ_BTN_NEUE_ZONE</c></summary>
    public string KnopfNeueZone { get; set; } = Resource.GEBZ_BTN_NEUE_ZONE;

    /// <summary><c>GEBZ_BTN_DUPLIZIEREN</c></summary>
    public string KnopfDuplizieren { get; set; } = Resource.GEBZ_BTN_DUPLIZIEREN;

    /// <summary><c>GEBZ_BTN_HOCH</c> — Kurztext des Knopfes ▲.</summary>
    public string KnopfHoch { get; set; } = Resource.GEBZ_BTN_HOCH;

    /// <summary><c>GEBZ_BTN_RUNTER</c> — Kurztext des Knopfes ▼.</summary>
    public string KnopfRunter { get; set; } = Resource.GEBZ_BTN_RUNTER;

    /// <summary><c>GEBZ_NAME_NEU</c> — {0} Nummer der neuen Zone.</summary>
    public string NameNeu { get; set; } = Resource.GEBZ_NAME_NEU;

    /// <summary><c>GEBZ_NAME_KOPIE</c> — {0} Name der Vorlage.</summary>
    public string NameKopie { get; set; } = Resource.GEBZ_NAME_KOPIE;

    /// <summary><c>GEBZ_SUMME</c> — die Summenzeile der Zonenliste.</summary>
    public string Summe { get; set; } = Resource.GEBZ_SUMME;

    /// <summary><c>GEBZ_FLAECHE_FEHLT</c> — die Nutzfläche einer von mehreren Zonen fehlt.</summary>
    public string FlaecheFehlt { get; set; } = Resource.GEBZ_FLAECHE_FEHLT;

    /// <summary><c>GEBZ_FRAGE_ERSTE_ZONE</c> — die Rückfrage vor der ersten Zone.</summary>
    public string FrageErsteZone { get; set; } = Resource.GEBZ_FRAGE_ERSTE_ZONE;

    /// <summary><c>GEBZ_FRAGE_ZWEITE_ZONE</c> — die Rückfrage vor der zweiten Zone.</summary>
    public string FrageZweiteZone { get; set; } = Resource.GEBZ_FRAGE_ZWEITE_ZONE;

    /// <summary><c>GEBZ_SPERRE_HOECHSTZAHL</c> — {0} Höchstzahl der Zonen.</summary>
    public string SperreHoechstzahl { get; set; } = Resource.GEBZ_SPERRE_HOECHSTZAHL;

    /// <summary><c>GEBZ_HINWEIS_SPEICHERN_UNTER_ZONEN</c> — {0} Zahl der Zonen, {1} Zahl der Bauteile.</summary>
    public string HinweisSpeichernUnterZonen { get; set; } = Resource.GEBZ_HINWEIS_SPEICHERN_UNTER_ZONEN;

    /// <summary><c>GEBZ_FRAGE_ENTFERNEN</c> — {0} Zone, {1} Zahl der Bauteile.</summary>
    public string FrageEntfernen { get; set; } = Resource.GEBZ_FRAGE_ENTFERNEN;

    /// <summary><c>GEBZ_FRAGE_ENTFERNEN_ZONE</c> — eine von mehreren Zonen: {0} Zone, {1} Zahl der Bauteile.</summary>
    public string FrageEntfernenZone { get; set; } = Resource.GEBZ_FRAGE_ENTFERNEN_ZONE;

    /// <summary><c>GEBZ_ZEILE_SUMMENREGEL</c> — {0} Zone.</summary>
    public string ZeileSummenregel { get; set; } = Resource.GEBZ_ZEILE_SUMMENREGEL;

    /// <summary><c>GEBZ_ZEILE_SUMMENREGEL_ZONEN</c> — {0} Zahl der Zonen.</summary>
    public string ZeileSummenregelZonen { get; set; } = Resource.GEBZ_ZEILE_SUMMENREGEL_ZONEN;

    /// <summary><c>GEBZ_ZEILE_FENSTER</c> — {0} Fensterfläche der Zone.</summary>
    public string ZeileFenster { get; set; } = Resource.GEBZ_ZEILE_FENSTER;

    /// <summary><c>GEBZ_ZEILE_FENSTER_ZONEN</c> — {0} Fensterfläche aller Zonen, {1} Zahl der Zonen.</summary>
    public string ZeileFensterZonen { get; set; } = Resource.GEBZ_ZEILE_FENSTER_ZONEN;

    /// <summary><c>GEBZ_ZEILE_PSI</c> — die Zeile der Wärmebrücken in der abgeleiteten Hülle.</summary>
    public string ZeilePsi { get; set; } = Resource.GEBZ_ZEILE_PSI;

    /// <summary><c>GEBZ_ZEILE_NUTZFLAECHE</c> — {0} Nutzfläche der Zone.</summary>
    public string ZeileNutzflaeche { get; set; } = Resource.GEBZ_ZEILE_NUTZFLAECHE;

    /// <summary><c>GEBZ_ZEILE_NUTZFLAECHE_ZONEN</c> — {0} Σ Nutzfläche, {1} Zahl der Zonen.</summary>
    public string ZeileNutzflaecheZonen { get; set; } = Resource.GEBZ_ZEILE_NUTZFLAECHE_ZONEN;

    /// <summary><c>GEBZ_ZEILE_NAME_PROJEKT</c> — {0} Name der Projektkopie.</summary>
    public string ZeileNameProjekt { get; set; } = Resource.GEBZ_ZEILE_NAME_PROJEKT;

    /// <summary><c>GEBZ_FRAGE_SPEICHERN_UNTER</c> — {0} Zone, {1} Zahl der Bauteile.</summary>
    public string FrageSpeichernUnter { get; set; } = Resource.GEBZ_FRAGE_SPEICHERN_UNTER;

    /// <summary><c>GEBZ_FRAGE_SPEICHERN_UNTER_ZONEN</c> — {0} Zahl der Zonen, {1} Zahl der Bauteile.</summary>
    public string FrageSpeichernUnterZonen { get; set; } = Resource.GEBZ_FRAGE_SPEICHERN_UNTER_ZONEN;

    /// <summary><c>GEBZ_MSG_ZONEN</c> — {0} Grund.</summary>
    public string MeldungZonen { get; set; } = Resource.GEBZ_MSG_ZONEN;

    /// <summary><c>GEBZ_MSG_AUFBAU</c> — {0} Aufbau, {1} Grund.</summary>
    public string MeldungAufbau { get; set; } = Resource.GEBZ_MSG_AUFBAU;

    /// <summary><c>GEBZ_TITEL_PROJEKT</c> — der Titel des Editors in der Betriebsart Projekt.</summary>
    public string TitelProjekt { get; set; } = Resource.GEBZ_TITEL_PROJEKT;

    // --- Stufe G6b (W2): Zonenliste, Luftaustausch und Rückfragen der Kopplung -----

    /// <summary><c>GEBZ_SP_VOLUMEN</c></summary>
    public string SpalteVolumen { get; set; } = Resource.GEBZ_SP_VOLUMEN;

    /// <summary><c>GEBZ_SP_BEHEIZT</c></summary>
    public string SpalteBeheizt { get; set; } = Resource.GEBZ_SP_BEHEIZT;

    /// <summary><c>GEBZ_BEHEIZT_JA</c></summary>
    public string BeheiztJa { get; set; } = Resource.GEBZ_BEHEIZT_JA;

    /// <summary><c>GEBZ_BEHEIZT_NEIN</c></summary>
    public string BeheiztNein { get; set; } = Resource.GEBZ_BEHEIZT_NEIN;

    /// <summary><c>GEBZ_BTN_LUFTAUSTAUSCH</c></summary>
    public string KnopfLuftaustausch { get; set; } = Resource.GEBZ_BTN_LUFTAUSTAUSCH;

    /// <summary><c>GEBZ_ZEILE_LUFTSTROEME</c> — {0} Zahl der Luftströme.</summary>
    public string ZeileLuftstroeme { get; set; } = Resource.GEBZ_ZEILE_LUFTSTROEME;

    /// <summary><c>GEBZ_FRAGE_ENTFERNEN_TRENNFLAECHEN</c> — {0} Zahl, {1} die Bauteile samt Zone.</summary>
    public string FrageEntfernenTrennflaechen { get; set; } = Resource.GEBZ_FRAGE_ENTFERNEN_TRENNFLAECHEN;

    /// <summary><c>GEBZ_FRAGE_ENTFERNEN_LUFTSTROEME</c> — {0} Zahl der Luftströme.</summary>
    public string FrageEntfernenLuftstroeme { get; set; } = Resource.GEBZ_FRAGE_ENTFERNEN_LUFTSTROEME;

    /// <summary><c>GEBZ_FRAGE_DUPLIZIEREN_TRENNFLAECHEN</c> — {0} Zone, {1} Zahl, {2} die Nachbarn.</summary>
    public string FrageDuplizierenTrennflaechen { get; set; } = Resource.GEBZ_FRAGE_DUPLIZIEREN_TRENNFLAECHEN;

    /// <summary><c>ALLG_BTN_JA</c></summary>
    public string Ja { get; set; } = Resource.ALLG_BTN_JA;

    /// <summary><c>ALLG_BTN_NEIN</c></summary>
    public string Nein { get; set; } = Resource.ALLG_BTN_NEIN;
}

/// <summary>
/// Die Anzeigetexte des <see cref="LuftaustauschDialog"/> (Präfix <c>ZLUFT_</c>, Stufe G6b) — ein Bündel.
/// </summary>
public sealed class LuftaustauschTexte
{
    /// <summary><c>ZLUFT_TITEL</c></summary>
    public string Titel { get; set; } = Resource.ZLUFT_TITEL;

    /// <summary><c>ZLUFT_RASTER</c> — die Beschriftung des Rasters.</summary>
    public string Raster { get; set; } = Resource.ZLUFT_RASTER;

    /// <summary><c>ZLUFT_SP_ZONE_A</c></summary>
    public string SpalteZoneA { get; set; } = Resource.ZLUFT_SP_ZONE_A;

    /// <summary><c>ZLUFT_SP_ZONE_B</c></summary>
    public string SpalteZoneB { get; set; } = Resource.ZLUFT_SP_ZONE_B;

    /// <summary><c>ZLUFT_SP_VOLUMENSTROM</c></summary>
    public string SpalteVolumenstrom { get; set; } = Resource.ZLUFT_SP_VOLUMENSTROM;

    /// <summary><c>GEBZ_SP_AKTIONEN</c></summary>
    public string SpalteAktionen { get; set; } = Resource.GEBZ_SP_AKTIONEN;

    /// <summary><c>ZLUFT_PFLICHTSATZ</c> — „Gerechnet wird nur der eingegebene Strom."</summary>
    public string Pflichtsatz { get; set; } = Resource.ZLUFT_PFLICHTSATZ;

    /// <summary><c>ZLUFT_ZEILE</c> — die Erklärzeile zum Paar.</summary>
    public string Zeile { get; set; } = Resource.ZLUFT_ZEILE;

    /// <summary><c>ZLUFT_BTN_NEU</c></summary>
    public string NeuerLuftstrom { get; set; } = Resource.ZLUFT_BTN_NEU;

    /// <summary><c>ZLUFT_LEER</c></summary>
    public string Leer { get; set; } = Resource.ZLUFT_LEER;

    /// <summary><c>ZLUFT_BTN_ENTFERNEN</c></summary>
    public string KnopfEntfernen { get; set; } = Resource.ZLUFT_BTN_ENTFERNEN;

    /// <summary><c>ZLUFT_ZONE_WAHL</c> — der Platzhalter der Zonenwahl.</summary>
    public string ZoneWahl { get; set; } = Resource.ZLUFT_ZONE_WAHL;

    /// <summary><c>ZLUFT_ZEILE_NR</c> — {0} Nummer der Zeile.</summary>
    public string ZeileNr { get; set; } = Resource.ZLUFT_ZEILE_NR;

    /// <summary><c>ZLUFT_MSG_ZONE</c> — {0} Nummer der Zeile.</summary>
    public string MeldungZone { get; set; } = Resource.ZLUFT_MSG_ZONE;

    /// <summary><c>ZLUFT_MSG_VOLUMENSTROM</c> — {0} Nummer der Zeile.</summary>
    public string MeldungVolumenstrom { get; set; } = Resource.ZLUFT_MSG_VOLUMENSTROM;

    /// <summary><c>GEBK_MSG_UNGUELTIG</c> — {0} Feld.</summary>
    public string MeldungUngueltig { get; set; } = Resource.GEBK_MSG_UNGUELTIG;

    /// <summary><c>ALLG_BTN_OK</c></summary>
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;

    /// <summary><c>ALLG_BTN_ABBRECHEN</c></summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;
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

    /// <summary><c>ZONE_MSG_NUTZFLAECHE_PFLICHT</c> — {0} Zone; dieselbe Regel wie im Kern (G6a).</summary>
    public string MeldungNutzflaechePflicht { get; set; } = Resource.ZONE_MSG_NUTZFLAECHE_PFLICHT;

    /// <summary><c>GEBZ_ZEILE_NUTZFLAECHE_PFLICHT</c> — {0} Nutzfläche des Gebäudes.</summary>
    public string ZeileNutzflaechePflicht { get; set; } = Resource.GEBZ_ZEILE_NUTZFLAECHE_PFLICHT;

    /// <summary><c>GEBK_MSG_UNGUELTIG</c> — {0} Feld.</summary>
    public string MeldungUngueltig { get; set; } = Resource.GEBK_MSG_UNGUELTIG;

    /// <summary><c>ZONDLG_U_AUS_AUFBAU</c> — der Zusatz einer U-Zelle, die aus dem Aufbau kommt.</summary>
    public string UAusAufbau { get; set; } = Resource.ZONDLG_U_AUS_AUFBAU;

    // --- Stufe G6b (W2): die Werte der Zone und die Trennflächen ------------------

    /// <summary><c>ZONDLG_GRP_WERTE</c></summary>
    public string GruppeWerte { get; set; } = Resource.ZONDLG_GRP_WERTE;

    /// <summary><c>ZONDLG_LBL_RAUMHOEHE</c></summary>
    public string LabelRaumhoehe { get; set; } = Resource.ZONDLG_LBL_RAUMHOEHE;

    /// <summary><c>ZONDLG_LBL_VOLUMEN</c></summary>
    public string LabelVolumen { get; set; } = Resource.ZONDLG_LBL_VOLUMEN;

    /// <summary><c>ZONDLG_LBL_BEHEIZT</c></summary>
    public string LabelBeheizt { get; set; } = Resource.ZONDLG_LBL_BEHEIZT;

    /// <summary><c>ZONDLG_LBL_SOLL_TAG</c></summary>
    public string LabelSollTag { get; set; } = Resource.ZONDLG_LBL_SOLL_TAG;

    /// <summary><c>ZONDLG_LBL_SOLL_NACHT</c></summary>
    public string LabelSollNacht { get; set; } = Resource.ZONDLG_LBL_SOLL_NACHT;

    /// <summary><c>ZONDLG_LBL_SOLL_WOCHENENDE</c></summary>
    public string LabelSollWochenende { get; set; } = Resource.ZONDLG_LBL_SOLL_WOCHENENDE;

    /// <summary><c>ZONDLG_LBL_SOLL_FERIEN</c></summary>
    public string LabelSollFerien { get; set; } = Resource.ZONDLG_LBL_SOLL_FERIEN;

    /// <summary><c>ZONDLG_LBL_MAX_TEMPERATUR</c></summary>
    public string LabelMaxTemperatur { get; set; } = Resource.ZONDLG_LBL_MAX_TEMPERATUR;

    /// <summary><c>ZONDLG_LBL_INFILTRATION</c></summary>
    public string LabelInfiltration { get; set; } = Resource.ZONDLG_LBL_INFILTRATION;

    /// <summary><c>ZONDLG_LBL_NUTZERLUEFTUNG</c></summary>
    public string LabelNutzerlueftung { get; set; } = Resource.ZONDLG_LBL_NUTZERLUEFTUNG;

    /// <summary><c>ZONDLG_LBL_GEWINNE</c></summary>
    public string LabelGewinne { get; set; } = Resource.ZONDLG_LBL_GEWINNE;

    /// <summary><c>ZONDLG_LBL_BEWOHNER</c></summary>
    public string LabelBewohner { get; set; } = Resource.ZONDLG_LBL_BEWOHNER;

    /// <summary><c>ZONDLG_LBL_STRAHLUNG</c></summary>
    public string LabelStrahlungsanteil { get; set; } = Resource.ZONDLG_LBL_STRAHLUNG;

    /// <summary><c>ZONDLG_LBL_HEIZLEISTUNG_MAX</c></summary>
    public string LabelHeizleistungMax { get; set; } = Resource.ZONDLG_LBL_HEIZLEISTUNG_MAX;

    /// <summary><c>ZONDLG_VORGABE</c> — {0} Wert des Gebäudes bzw. Modellvorgabe.</summary>
    public string Vorgabe { get; set; } = Resource.ZONDLG_VORGABE;

    /// <summary><c>ZONDLG_VORGABE_ANTEILIG</c> — {0} anteiliger Wert, {1} Flächenanteil in %.</summary>
    public string VorgabeAnteilig { get; set; } = Resource.ZONDLG_VORGABE_ANTEILIG;

    /// <summary><c>ZONDLG_VORGABE_ABGELEITET</c> — {0} Volumen aus Fläche × Raumhöhe.</summary>
    public string VorgabeAbgeleitet { get; set; } = Resource.ZONDLG_VORGABE_ABGELEITET;

    /// <summary><c>GEBK_VORGABE_UNBEGRENZT</c></summary>
    public string VorgabeUnbegrenzt { get; set; } = Resource.GEBK_VORGABE_UNBEGRENZT;

    /// <summary><c>ZONDLG_ZEILE_VORGABEN</c></summary>
    public string ZeileVorgaben { get; set; } = Resource.ZONDLG_ZEILE_VORGABEN;

    /// <summary><c>ZONDLG_ZEILE_UNBEHEIZT</c></summary>
    public string ZeileUnbeheizt { get; set; } = Resource.ZONDLG_ZEILE_UNBEHEIZT;

    /// <summary><c>ZONDLG_ZEILE_LUFTWECHSEL</c> — {0} Luftwechsel, {1} Herkunft.</summary>
    public string ZeileLuftwechsel { get; set; } = Resource.ZONDLG_ZEILE_LUFTWECHSEL;

    /// <summary><c>GEBK_HERKUNFT_INFILTRATION_NUTZER</c></summary>
    public string HerkunftInfiltrationNutzer { get; set; } = Resource.GEBK_HERKUNFT_INFILTRATION_NUTZER;

    /// <summary><c>GEBK_HERKUNFT_LUFTWECHSELRATE</c></summary>
    public string HerkunftLuftwechselrate { get; set; } = Resource.GEBK_HERKUNFT_LUFTWECHSELRATE;

    /// <summary><c>GEBK_HERKUNFT_VORGABE</c></summary>
    public string HerkunftVorgabe { get; set; } = Resource.GEBK_HERKUNFT_VORGABE;

    /// <summary><c>ZONDLG_SP_NACHBAR</c></summary>
    public string SpalteNachbar { get; set; } = Resource.ZONDLG_SP_NACHBAR;

    /// <summary><c>ZONDLG_GEFUEHRT_VON</c> — {0} die führende Zone.</summary>
    public string GefuehrtVon { get; set; } = Resource.ZONDLG_GEFUEHRT_VON;

    /// <summary><c>ZONDLG_ZEILE_GEGENSEITE</c></summary>
    public string ZeileGegenseite { get; set; } = Resource.ZONDLG_ZEILE_GEGENSEITE;

    /// <summary><c>ZONDLG_MSG_RAUMHOEHE</c> — {0} Zone.</summary>
    public string MeldungRaumhoehe { get; set; } = Resource.ZONDLG_MSG_RAUMHOEHE;

    /// <summary><c>ZONDLG_MSG_VOLUMEN</c> — {0} Zone.</summary>
    public string MeldungVolumen { get; set; } = Resource.ZONDLG_MSG_VOLUMEN;

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

    /// <summary><c>BTDLG_RAND_ZONE</c> — die Randbedingung „Nachbarzone" (Stufe G6b).</summary>
    public string RandZone { get; set; } = Resource.BTDLG_RAND_ZONE;

    /// <summary><c>BTDLG_LBL_NACHBARZONE</c></summary>
    public string LabelNachbarzone { get; set; } = Resource.BTDLG_LBL_NACHBARZONE;

    /// <summary><c>BTDLG_NACHBAR_WAHL</c> — der Platzhalter der Wahl.</summary>
    public string NachbarWahl { get; set; } = Resource.BTDLG_NACHBAR_WAHL;

    /// <summary><c>BTDLG_LBL_ZUORDNUNG</c></summary>
    public string LabelZuordnung { get; set; } = Resource.BTDLG_LBL_ZUORDNUNG;

    /// <summary><c>BTDLG_ZUORDNUNG_AUTO</c></summary>
    public string ZuordnungAutomatisch { get; set; } = Resource.BTDLG_ZUORDNUNG_AUTO;

    /// <summary><c>BTDLG_ZUORDNUNG_IW</c></summary>
    public string ZuordnungInnen { get; set; } = Resource.BTDLG_ZUORDNUNG_IW;

    /// <summary><c>BTDLG_ZUORDNUNG_AW</c></summary>
    public string ZuordnungAussen { get; set; } = Resource.BTDLG_ZUORDNUNG_AW;

    /// <summary><c>BTDLG_ZEILE_ZONE</c> — die Herleitungszeile einer Trennfläche.</summary>
    public string ZeileZone { get; set; } = Resource.BTDLG_ZEILE_ZONE;

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
