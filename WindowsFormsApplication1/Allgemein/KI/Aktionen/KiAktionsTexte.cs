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

        // ================================================================ Erlaeuterungen

        internal static string ErlVonProjekt => MyResource.Resource.KI_REG_ERL_VON_PROJEKT;
        internal static string ErlNachProjekt => MyResource.Resource.KI_REG_ERL_NACH_PROJEKT;
        internal static string ErlGewerk => MyResource.Resource.KI_REG_ERL_GEWERK;
        internal static string ErlKomponente => MyResource.Resource.KI_REG_ERL_KOMPONENTE;
        internal static string ErlMerkmal => MyResource.Resource.KI_REG_ERL_MERKMAL;
        internal static string ErlDateipfad => MyResource.Resource.KI_REG_ERL_DATEIPFAD;
        internal static string ErlGanglinieId => MyResource.Resource.KI_REG_ERL_GANGLINIE_ID;
        internal static string ErlProjekteFuerErgebnisse => MyResource.Resource.KI_REG_ERL_PROJEKTE_FUER_ERGEBNISSE;
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
        internal static string ProjekteKeine => MyResource.Resource.KI_REG_PROJEKTE_KEINE;
        internal static string ProjekteGefunden => MyResource.Resource.KI_REG_PROJEKTE_GEFUNDEN;
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
