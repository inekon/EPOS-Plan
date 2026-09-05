namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die sichtbaren Texte des Aktionsregisters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Erledigt mit Paket B5: jeder Text kommt aus <c>MyResource.Resource</c> und liegt
    /// dort in beiden Sprachen (Drei-Schichten-Regel, <c>WindowsFormsApplication1\CLAUDE.md</c>,
    /// Anzeigeschicht). Diese Klasse bleibt als EINE Fundstelle stehen: sie bildet den
    /// Aktionsnamen auf den Ressourcenschluessel ab, sodass die Aktionsdateien selbst
    /// keinen Ressourcennamen kennen muessen.
    /// </para>
    /// <para>
    /// Eigenschaften und keine Konstanten: eine <c>const</c> wuerde beim Uebersetzen in
    /// den Aufrufer kopiert und koennte die zur Laufzeit eingestellte Sprache nicht mehr
    /// sehen (gleiche Begruendung wie in <c>KiKern\KiTexte.cs</c>).
    /// </para>
    /// <para>
    /// NICHT betroffen sind Persistenz- und Schluesselwerte: Gewerknamen,
    /// Kostenkomponenten und Merkmalsschluessel bleiben deutsch und eingefroren und kommen
    /// aus <see cref="DbWerte"/> bzw. aus der Landkarte des jeweiligen Controllers.
    /// </para>
    /// </remarks>
    internal static class KiAktionsTexte
    {
        // ================================================================ Parameter

        internal static string ProjektIdName => MyResource.Resource.KI_REG_PROJEKT_ID_NAME;
        internal static string ProjektIdErlaeuterung => MyResource.Resource.KI_REG_PROJEKT_ID_ERLAEUTERUNG;

        internal static string VonProjektName => MyResource.Resource.KI_REG_VON_PROJEKT_NAME;
        internal static string NachProjektName => MyResource.Resource.KI_REG_NACH_PROJEKT_NAME;
        internal static string GewerkName => MyResource.Resource.KI_REG_GEWERK_NAME;
        internal static string KomponenteName => MyResource.Resource.KI_REG_KOMPONENTE_NAME;
        internal static string MerkmalName => MyResource.Resource.KI_REG_MERKMAL_NAME;
        internal static string DateipfadName => MyResource.Resource.KI_REG_DATEIPFAD_NAME;
        internal static string GanglinieName => MyResource.Resource.KI_REG_GANGLINIE_NAME;
        internal static string AnzahlName => MyResource.Resource.KI_REG_ANZAHL_NAME;
        internal static string ProjekteName => MyResource.Resource.KI_REG_PROJEKTE_NAME;
        internal static string SuchtextName => MyResource.Resource.KI_REG_SUCHTEXT_NAME;
        internal static string KapazitaetName => MyResource.Resource.KI_REG_KAPAZITAET_NAME;
        internal static string LeistungName => MyResource.Resource.KI_REG_LEISTUNG_NAME;
        internal static string WirkungsgradName => MyResource.Resource.KI_REG_WIRKUNGSGRAD_NAME;
        internal static string SocMinName => MyResource.Resource.KI_REG_SOC_MIN_NAME;
        internal static string SocMaxName => MyResource.Resource.KI_REG_SOC_MAX_NAME;

        internal static string StammIdName => MyResource.Resource.KI_REG_STAMM_ID_NAME;
        internal static string BezeichnerName => MyResource.Resource.KI_REG_BEZEICHNER_NAME;
        internal static string VarianteIdName => MyResource.Resource.KI_REG_VARIANTE_ID_NAME;
        internal static string PositionsIdName => MyResource.Resource.KI_REG_POSITIONS_ID_NAME;
        internal static string BetragName => MyResource.Resource.KI_REG_BETRAG_NAME;

        // ================================================================ Namensaufloesung

        internal static string NameFehlt => MyResource.Resource.KI_REG_NAME_FEHLT;
        internal static string NameUnbekannt => MyResource.Resource.KI_REG_NAME_UNBEKANNT;
        internal static string NameMehrdeutig => MyResource.Resource.KI_REG_NAME_MEHRDEUTIG;
        internal static string NameKeine => MyResource.Resource.KI_REG_NAME_KEINE;

        // ================================================================ Zwecke

        internal static string ZweckProjekteAuflisten => MyResource.Resource.KI_REG_ZWECK_PROJEKTE_AUFLISTEN;
        internal static string ZweckProjektAktiv => MyResource.Resource.KI_REG_ZWECK_PROJEKT_AKTIV;
        internal static string ZweckProjektSuchen => MyResource.Resource.KI_REG_ZWECK_PROJEKT_SUCHEN;
        internal static string ZweckProjektLesen => MyResource.Resource.KI_REG_ZWECK_PROJEKT_LESEN;
        internal static string ZweckVariantenAuflisten => MyResource.Resource.KI_REG_ZWECK_VARIANTEN_AUFLISTEN;
        internal static string ZweckSpeichervariantenAuflisten => MyResource.Resource.KI_REG_ZWECK_SPEICHERVARIANTEN_AUFLISTEN;
        internal static string ZweckErgebnisseLesen => MyResource.Resource.KI_REG_ZWECK_ERGEBNISSE_LESEN;
        internal static string ZweckParameterLesen => MyResource.Resource.KI_REG_ZWECK_PARAMETER_LESEN;
        internal static string ZweckKostenlagePruefen => MyResource.Resource.KI_REG_ZWECK_KOSTENLAGE_PRUEFEN;
        internal static string ZweckUebernahmeVorschau => MyResource.Resource.KI_REG_ZWECK_UEBERNAHME_VORSCHAU;
        internal static string ZweckMerkmalVorschau => MyResource.Resource.KI_REG_ZWECK_MERKMAL_VORSCHAU;
        internal static string ZweckLastgangPruefen => MyResource.Resource.KI_REG_ZWECK_LASTGANG_PRUEFEN;
        internal static string ZweckGanglinienAuflisten => MyResource.Resource.KI_REG_ZWECK_GANGLINIEN_AUFLISTEN;
        internal static string ZweckMinimaleSpitze => MyResource.Resource.KI_REG_ZWECK_MINIMALE_SPITZE;
        internal static string ZweckLetzteAktionen => MyResource.Resource.KI_REG_ZWECK_LETZTE_AKTIONEN;

        internal static string ZweckVarianteAnlegen => MyResource.Resource.KI_REG_ZWECK_VARIANTE_ANLEGEN;
        internal static string ZweckSpeichervarianteAktiv => MyResource.Resource.KI_REG_ZWECK_SPEICHERVARIANTE_AKTIV;
        internal static string ZweckKostenpositionSetzen => MyResource.Resource.KI_REG_ZWECK_KOSTENPOSITION_SETZEN;


        // ============================ Titel und Beispiel (Befund W15b-E-4)
        //
        // Die Werkzeugliste zeigte bis zur Windows-Abnahme vom 05.09.2026 den
        // ASCII-Bezeichner der Aktion ("speichervariante_aktiv_setzen"). Der gehoert
        // dem MODELL - der Anwender braucht einen Titel in seiner Sprache und einen
        // Satz, mit dem er dasselbe im Gespraech erreicht. Beides steht wie jeder
        // andere Anzeigetext in MyResource und damit zweisprachig da.

        internal static string TitelProjekteAuflisten => MyResource.Resource.KI_REG_TITEL_PROJEKTE_AUFLISTEN;
        internal static string BeispielProjekteAuflisten => MyResource.Resource.KI_REG_BEISPIEL_PROJEKTE_AUFLISTEN;
        internal static string TitelProjektAktiv => MyResource.Resource.KI_REG_TITEL_PROJEKT_AKTIV;
        internal static string BeispielProjektAktiv => MyResource.Resource.KI_REG_BEISPIEL_PROJEKT_AKTIV;
        internal static string TitelProjektSuchen => MyResource.Resource.KI_REG_TITEL_PROJEKT_SUCHEN;
        internal static string BeispielProjektSuchen => MyResource.Resource.KI_REG_BEISPIEL_PROJEKT_SUCHEN;
        internal static string TitelProjektLesen => MyResource.Resource.KI_REG_TITEL_PROJEKT_LESEN;
        internal static string BeispielProjektLesen => MyResource.Resource.KI_REG_BEISPIEL_PROJEKT_LESEN;
        internal static string TitelVariantenAuflisten => MyResource.Resource.KI_REG_TITEL_VARIANTEN_AUFLISTEN;
        internal static string BeispielVariantenAuflisten => MyResource.Resource.KI_REG_BEISPIEL_VARIANTEN_AUFLISTEN;
        internal static string TitelSpeichervariantenAuflisten => MyResource.Resource.KI_REG_TITEL_SPEICHERVARIANTEN_AUFLISTEN;
        internal static string BeispielSpeichervariantenAuflisten => MyResource.Resource.KI_REG_BEISPIEL_SPEICHERVARIANTEN_AUFLISTEN;
        internal static string TitelErgebnisseLesen => MyResource.Resource.KI_REG_TITEL_ERGEBNISSE_LESEN;
        internal static string BeispielErgebnisseLesen => MyResource.Resource.KI_REG_BEISPIEL_ERGEBNISSE_LESEN;
        internal static string TitelParameterLesen => MyResource.Resource.KI_REG_TITEL_PARAMETER_LESEN;
        internal static string BeispielParameterLesen => MyResource.Resource.KI_REG_BEISPIEL_PARAMETER_LESEN;
        internal static string TitelKostenlagePruefen => MyResource.Resource.KI_REG_TITEL_KOSTENLAGE_PRUEFEN;
        internal static string BeispielKostenlagePruefen => MyResource.Resource.KI_REG_BEISPIEL_KOSTENLAGE_PRUEFEN;
        internal static string TitelUebernahmeVorschau => MyResource.Resource.KI_REG_TITEL_UEBERNAHME_VORSCHAU;
        internal static string BeispielUebernahmeVorschau => MyResource.Resource.KI_REG_BEISPIEL_UEBERNAHME_VORSCHAU;
        internal static string TitelMerkmalVorschau => MyResource.Resource.KI_REG_TITEL_MERKMAL_VORSCHAU;
        internal static string BeispielMerkmalVorschau => MyResource.Resource.KI_REG_BEISPIEL_MERKMAL_VORSCHAU;
        internal static string TitelLastgangPruefen => MyResource.Resource.KI_REG_TITEL_LASTGANG_PRUEFEN;
        internal static string BeispielLastgangPruefen => MyResource.Resource.KI_REG_BEISPIEL_LASTGANG_PRUEFEN;
        internal static string TitelGanglinienAuflisten => MyResource.Resource.KI_REG_TITEL_GANGLINIEN_AUFLISTEN;
        internal static string BeispielGanglinienAuflisten => MyResource.Resource.KI_REG_BEISPIEL_GANGLINIEN_AUFLISTEN;
        internal static string TitelMinimaleSpitze => MyResource.Resource.KI_REG_TITEL_MINIMALE_SPITZE;
        internal static string BeispielMinimaleSpitze => MyResource.Resource.KI_REG_BEISPIEL_MINIMALE_SPITZE;
        internal static string TitelLetzteAktionen => MyResource.Resource.KI_REG_TITEL_LETZTE_AKTIONEN;
        internal static string BeispielLetzteAktionen => MyResource.Resource.KI_REG_BEISPIEL_LETZTE_AKTIONEN;
        internal static string TitelEnergietraegerPruefen => MyResource.Resource.KI_REG_TITEL_ENERGIETRAEGER_PRUEFEN;
        internal static string BeispielEnergietraegerPruefen => MyResource.Resource.KI_REG_BEISPIEL_ENERGIETRAEGER_PRUEFEN;
        internal static string TitelDialogLesen => MyResource.Resource.KI_REG_TITEL_DIALOG_LESEN;
        internal static string BeispielDialogLesen => MyResource.Resource.KI_REG_BEISPIEL_DIALOG_LESEN;
        internal static string TitelDialogErklaeren => MyResource.Resource.KI_REG_TITEL_DIALOG_ERKLAEREN;
        internal static string BeispielDialogErklaeren => MyResource.Resource.KI_REG_BEISPIEL_DIALOG_ERKLAEREN;
        internal static string TitelFeldSetzen => MyResource.Resource.KI_REG_TITEL_FELD_SETZEN;
        internal static string BeispielFeldSetzen => MyResource.Resource.KI_REG_BEISPIEL_FELD_SETZEN;
        internal static string TitelFormularAusfuellen => MyResource.Resource.KI_REG_TITEL_FORMULAR_AUSFUELLEN;
        internal static string BeispielFormularAusfuellen => MyResource.Resource.KI_REG_BEISPIEL_FORMULAR_AUSFUELLEN;
        internal static string TitelDialogAktion => MyResource.Resource.KI_REG_TITEL_DIALOG_AKTION;
        internal static string BeispielDialogAktion => MyResource.Resource.KI_REG_BEISPIEL_DIALOG_AKTION;
        internal static string TitelVarianteAnlegen => MyResource.Resource.KI_REG_TITEL_VARIANTE_ANLEGEN;
        internal static string BeispielVarianteAnlegen => MyResource.Resource.KI_REG_BEISPIEL_VARIANTE_ANLEGEN;
        internal static string TitelSpeichervarianteAktiv => MyResource.Resource.KI_REG_TITEL_SPEICHERVARIANTE_AKTIV;
        internal static string BeispielSpeichervarianteAktiv => MyResource.Resource.KI_REG_BEISPIEL_SPEICHERVARIANTE_AKTIV;
        internal static string TitelKostenpositionSetzen => MyResource.Resource.KI_REG_TITEL_KOSTENPOSITION_SETZEN;
        internal static string BeispielKostenpositionSetzen => MyResource.Resource.KI_REG_BEISPIEL_KOSTENPOSITION_SETZEN;

        // ================================================================ Erlaeuterungen

        internal static string ErlVonProjekt => MyResource.Resource.KI_REG_ERL_VON_PROJEKT;
        internal static string ErlNachProjekt => MyResource.Resource.KI_REG_ERL_NACH_PROJEKT;
        internal static string ErlGewerk => MyResource.Resource.KI_REG_ERL_GEWERK;
        internal static string ErlKomponente => MyResource.Resource.KI_REG_ERL_KOMPONENTE;
        internal static string ErlMerkmal => MyResource.Resource.KI_REG_ERL_MERKMAL;
        internal static string ErlDateipfad => MyResource.Resource.KI_REG_ERL_DATEIPFAD;
        internal static string ErlGanglinieId => MyResource.Resource.KI_REG_ERL_GANGLINIE_ID;
        internal static string ErlProjekteFuerErgebnisse => MyResource.Resource.KI_REG_ERL_PROJEKTE_FUER_ERGEBNISSE;
        internal static string ErlSuchtext => MyResource.Resource.KI_REG_ERL_SUCHTEXT;
        internal static string ErlProjektIdGanglinien => MyResource.Resource.KI_REG_ERL_PROJEKT_ID_GANGLINIEN;
        internal static string ErlProjektIdGanglinieSuche => MyResource.Resource.KI_REG_ERL_PROJEKT_ID_GANGLINIE_SUCHE;
        internal static string ErlAnzahl => MyResource.Resource.KI_REG_ERL_ANZAHL;
        internal static string ErlKapazitaet => MyResource.Resource.KI_REG_ERL_KAPAZITAET;
        internal static string ErlLeistung => MyResource.Resource.KI_REG_ERL_LEISTUNG;
        internal static string ErlWirkungsgrad => MyResource.Resource.KI_REG_ERL_WIRKUNGSGRAD;
        internal static string ErlSocMin => MyResource.Resource.KI_REG_ERL_SOC_MIN;
        internal static string ErlSocMax => MyResource.Resource.KI_REG_ERL_SOC_MAX;

        internal static string ErlStammId => MyResource.Resource.KI_REG_ERL_STAMM_ID;
        internal static string ErlBezeichner => MyResource.Resource.KI_REG_ERL_BEZEICHNER;
        internal static string ErlVarianteId => MyResource.Resource.KI_REG_ERL_VARIANTE_ID;
        internal static string ErlPositionsId => MyResource.Resource.KI_REG_ERL_POSITIONS_ID;
        internal static string ErlBetrag => MyResource.Resource.KI_REG_ERL_BETRAG;

        // ================================================================ Wirkung und Vorschau

        internal static string WirkungVarianteAnlegen => MyResource.Resource.KI_REG_WIRKUNG_VARIANTE_ANLEGEN;
        internal static string WirkungSpeichervarianteAktiv => MyResource.Resource.KI_REG_WIRKUNG_SPEICHERVARIANTE_AKTIV;
        internal static string WirkungKostenpositionSetzen => MyResource.Resource.KI_REG_WIRKUNG_KOSTENPOSITION_SETZEN;

        internal static string VorschauVarianteAnlegen => MyResource.Resource.KI_REG_VORSCHAU_VARIANTE_ANLEGEN;
        internal static string VorschauSpeichervariante => MyResource.Resource.KI_REG_VORSCHAU_SPEICHERVARIANTE;
        internal static string VorschauKostenposition => MyResource.Resource.KI_REG_VORSCHAU_KOSTENPOSITION;

        // ================================================================ Meldungen

        internal static string ProjektUnbekannt => MyResource.Resource.KI_REG_PROJEKT_UNBEKANNT;
        internal static string ProjektAktivGelesen => MyResource.Resource.KI_REG_PROJEKT_AKTIV_GELESEN;
        internal static string ProjektAktivKeines => MyResource.Resource.KI_REG_PROJEKT_AKTIV_KEINES;
        internal static string ProjektAktivNichtGelesen => MyResource.Resource.KI_REG_PROJEKT_AKTIV_NICHT_GELESEN;
        internal static string ProjekteKeine => MyResource.Resource.KI_REG_PROJEKTE_KEINE;
        internal static string ProjekteGefunden => MyResource.Resource.KI_REG_PROJEKTE_GEFUNDEN;
        internal static string ProjektSucheGefunden => MyResource.Resource.KI_REG_PROJEKT_SUCHE_GEFUNDEN;
        internal static string ProjektSucheKeine => MyResource.Resource.KI_REG_PROJEKT_SUCHE_KEINE;
        internal static string ProjektGelesen => MyResource.Resource.KI_REG_PROJEKT_GELESEN;
        internal static string VariantenGruppe => MyResource.Resource.KI_REG_VARIANTEN_GRUPPE;
        internal static string VarianteAufgeloest => MyResource.Resource.KI_REG_VARIANTE_AUFGELOEST;
        internal static string EinzelnesProjekt => MyResource.Resource.KI_REG_EINZELNES_PROJEKT;
        internal static string SpeichervariantenKeine => MyResource.Resource.KI_REG_SPEICHERVARIANTEN_KEINE;
        internal static string SpeichervariantenGefunden => MyResource.Resource.KI_REG_SPEICHERVARIANTEN_GEFUNDEN;
        internal static string SpeichervarianteKeineAktive => MyResource.Resource.KI_REG_SPEICHERVARIANTE_KEINE_AKTIVE;
        internal static string SpeicherTabelleFehlt => MyResource.Resource.KI_REG_SPEICHER_TABELLE_FEHLT;
        internal static string ErgebnisseKeine => MyResource.Resource.KI_REG_ERGEBNISSE_KEINE;
        internal static string ErgebnisseGefunden => MyResource.Resource.KI_REG_ERGEBNISSE_GEFUNDEN;
        internal static string ParameterGelesen => MyResource.Resource.KI_REG_PARAMETER_GELESEN;
        internal static string TarifAktiv => MyResource.Resource.KI_REG_TARIF_AKTIV;
        internal static string TarifAus => MyResource.Resource.KI_REG_TARIF_AUS;
        internal static string KomponenteUnbekannt => MyResource.Resource.KI_REG_KOMPONENTE_UNBEKANNT;
        internal static string KomponenteNichtVerbaut => MyResource.Resource.KI_REG_KOMPONENTE_NICHT_VERBAUT;
        internal static string KostenlageOhneKomponente => MyResource.Resource.KI_REG_KOSTENLAGE_OHNE_KOMPONENTE;
        internal static string KostenlagePasst => MyResource.Resource.KI_REG_KOSTENLAGE_PASST;
        internal static string KostenlageAbweichend => MyResource.Resource.KI_REG_KOSTENLAGE_ABWEICHEND;
        internal static string GleicheProjekte => MyResource.Resource.KI_REG_GLEICHE_PROJEKTE;
        internal static string GewerkNichtUnterstuetzt => MyResource.Resource.KI_REG_GEWERK_NICHT_UNTERSTUETZT;
        internal static string UebernahmeMoeglich => MyResource.Resource.KI_REG_UEBERNAHME_MOEGLICH;
        internal static string UebernahmeNichtMoeglich => MyResource.Resource.KI_REG_UEBERNAHME_NICHT_MOEGLICH;
        internal static string MerkmalUnbekannt => MyResource.Resource.KI_REG_MERKMAL_UNBEKANNT;
        internal static string MerkmalMoeglich => MyResource.Resource.KI_REG_MERKMAL_MOEGLICH;
        internal static string MerkmalGleichstand => MyResource.Resource.KI_REG_MERKMAL_GLEICHSTAND;
        internal static string MerkmalNichtMoeglich => MyResource.Resource.KI_REG_MERKMAL_NICHT_MOEGLICH;
        internal static string DateiFehlt => MyResource.Resource.KI_REG_DATEI_FEHLT;
        internal static string LastgangLesbar => MyResource.Resource.KI_REG_LASTGANG_LESBAR;
        internal static string LastgangNichtLesbar => MyResource.Resource.KI_REG_LASTGANG_NICHT_LESBAR;
        internal static string GanglinienKeine => MyResource.Resource.KI_REG_GANGLINIEN_KEINE;
        internal static string GanglinienGefunden => MyResource.Resource.KI_REG_GANGLINIEN_GEFUNDEN;
        internal static string GanglinieUnbekannt => MyResource.Resource.KI_REG_GANGLINIE_UNBEKANNT;
        internal static string GanglinieLeer => MyResource.Resource.KI_REG_GANGLINIE_LEER;
        internal static string SpitzeErmittelt => MyResource.Resource.KI_REG_SPITZE_ERMITTELT;
        internal static string SocVerdreht => MyResource.Resource.KI_REG_SOC_VERDREHT;
        internal static string LetzteAktionenKeine => MyResource.Resource.KI_REG_LETZTE_AKTIONEN_KEINE;
        internal static string LetzteAktionenGefunden => MyResource.Resource.KI_REG_LETZTE_AKTIONEN_GEFUNDEN;

        // ---------------------------------------------------------- Schreibaktionen

        internal static string KeinStammprojekt => MyResource.Resource.KI_REG_KEIN_STAMMPROJEKT;
        internal static string BezeichnerLeer => MyResource.Resource.KI_REG_BEZEICHNER_LEER;
        internal static string VarianteAngelegt => MyResource.Resource.KI_REG_VARIANTE_ANGELEGT;
        internal static string VarianteFehlgeschlagen => MyResource.Resource.KI_REG_VARIANTE_FEHLGESCHLAGEN;

        internal static string SpeichervarianteUnbekannt => MyResource.Resource.KI_REG_SPEICHERVARIANTE_UNBEKANNT;
        internal static string SpeichervarianteGesetzt => MyResource.Resource.KI_REG_SPEICHERVARIANTE_GESETZT;
        internal static string SpeichervarianteSchonAktiv => MyResource.Resource.KI_REG_SPEICHERVARIANTE_SCHON_AKTIV;
        internal static string SpeichervarianteFehlgeschlagen => MyResource.Resource.KI_REG_SPEICHERVARIANTE_FEHLGESCHLAGEN;

        internal static string PositionUnbekannt => MyResource.Resource.KI_REG_POSITION_UNBEKANNT;
        internal static string PositionFremdesProjekt => MyResource.Resource.KI_REG_POSITION_FREMDES_PROJEKT;
        internal static string PositionGesetzt => MyResource.Resource.KI_REG_POSITION_GESETZT;
        internal static string PositionFehlgeschlagen => MyResource.Resource.KI_REG_POSITION_FEHLGESCHLAGEN;

        internal static string AenderungsdatumFehlt => MyResource.Resource.KI_REG_AENDERUNGSDATUM_FEHLT;

        // ------------------------------------------ Formularsteuerung (Etappe 3b, F3)
        //
        // Die Texte des DIALOGKATALOGS (Maskennamen, Feldnamen, Erlaeuterungen) stehen
        // NICHT hier, sondern in KiDialogTexte: Sie gehoeren den Masken und nicht dem
        // Aktionsregister. Hier steht nur, was die fuenf Aktionen selbst brauchen.

        internal static string ZweckDialogLesen => MyResource.Resource.KI_REG_ZWECK_DIALOG_LESEN;
        internal static string ZweckDialogErklaeren => MyResource.Resource.KI_REG_ZWECK_DIALOG_ERKLAEREN;
        internal static string ZweckFeldSetzen => MyResource.Resource.KI_REG_ZWECK_FELD_SETZEN;
        internal static string ZweckFormularAusfuellen => MyResource.Resource.KI_REG_ZWECK_FORMULAR_AUSFUELLEN;
        internal static string ZweckDialogAktion => MyResource.Resource.KI_REG_ZWECK_DIALOG_AKTION;

        internal static string MaskeName => MyResource.Resource.KI_REG_MASKE_NAME;
        internal static string FeldName => MyResource.Resource.KI_REG_FELD_NAME;
        internal static string WertName => MyResource.Resource.KI_REG_WERT_NAME;
        internal static string WerteName => MyResource.Resource.KI_REG_WERTE_NAME;
        internal static string KnopfName => MyResource.Resource.KI_REG_KNOPF_NAME;

        internal static string ErlMaske => MyResource.Resource.KI_REG_ERL_MASKE;
        internal static string ErlFeld => MyResource.Resource.KI_REG_ERL_FELD;
        internal static string ErlWert => MyResource.Resource.KI_REG_ERL_WERT;
        internal static string ErlWerte => MyResource.Resource.KI_REG_ERL_WERTE;
        internal static string ErlKnopf => MyResource.Resource.KI_REG_ERL_KNOPF;

        internal static string WirkungFeldSetzen => MyResource.Resource.KI_REG_WIRKUNG_FELD_SETZEN;
        internal static string WirkungFormularAusfuellen => MyResource.Resource.KI_REG_WIRKUNG_FORMULAR_AUSFUELLEN;
        internal static string WirkungDialogAktion => MyResource.Resource.KI_REG_WIRKUNG_DIALOG_AKTION;

        // ------------------------------------------- Energietraeger-Einheiten (K3)

        internal static string ZweckEnergietraegerPruefen => MyResource.Resource.KI_REG_ZWECK_ENERGIETRAEGER_PRUEFEN;
        internal static string ErlProjektFuerEinheiten => MyResource.Resource.KI_REG_ERL_PROJEKT_FUER_EINHEITEN;
        internal static string EinheitenKatalog => MyResource.Resource.KI_REG_EINHEITEN_KATALOG;
        internal static string EinheitenOhneBefund => MyResource.Resource.KI_REG_EINHEITEN_OHNE_BEFUND;
        internal static string EinheitenBefunde => MyResource.Resource.KI_REG_EINHEITEN_BEFUNDE;

        // ------------------------------------------------- Sicherung und Schreibschutz

        internal static string SicherungQuelleFehlt => MyResource.Resource.KI_SICH_QUELLE_FEHLT;
        internal static string SicherungGeoeffnet => MyResource.Resource.KI_SICH_GEOEFFNET;
        internal static string SicherungFehlgeschlagen => MyResource.Resource.KI_SICH_FEHLGESCHLAGEN;

        internal static string SchutzSatz => MyResource.Resource.KI_SCHUTZ_SATZ;
        internal static string SchutzKatalog => MyResource.Resource.KI_SCHUTZ_KATALOG;
        internal static string SchutzUnpruefbar => MyResource.Resource.KI_SCHUTZ_UNPRUEFBAR;
    }
}
