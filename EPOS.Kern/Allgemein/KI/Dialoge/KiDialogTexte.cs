namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die sichtbaren Texte des Dialogkatalogs (Umsetzungskonzept Etappe 3b, Paket F3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Dasselbe Muster wie <see cref="KiAktionsTexte"/>.</b> Jeder Text kommt aus
    /// <c>MyResource.Resource</c> und liegt dort in beiden Sprachen
    /// (Drei-Schichten-Regel, <c>WindowsFormsApplication1\CLAUDE.md</c>, Anzeigeschicht).
    /// Diese Klasse bleibt die EINE Fundstelle, die Maskenfeld auf Ressourcenschluessel
    /// abbildet - die Katalogdatei selbst kennt damit keinen Ressourcennamen.
    /// </para>
    /// <para>
    /// <b>Eigenschaften und keine Konstanten:</b> eine <c>const</c> wuerde beim Uebersetzen
    /// in den Aufrufer kopiert und koennte die zur Laufzeit eingestellte Sprache nicht mehr
    /// sehen (gleiche Begruendung wie in <c>KiKern\KiTexte.cs</c>).
    /// </para>
    /// <para>
    /// <b>Warum die Einheiten hier als Konstanten stehen und nicht in der Ressource.</b>
    /// „kW", „%", „€", „°C", „l" und „°" sind Einheitenzeichen und keine
    /// Uebersetzung; sie stehen auf der deutschen wie auf der englischen Oberflaeche gleich
    /// - genau wie die eingefrorenen Persistenzwerte in <see cref="DbWerte"/>. Nur „Jahre"
    /// ist ein Wort und kommt deshalb aus der Ressource.
    /// </para>
    /// </remarks>
    internal static class KiDialogTexte
    {
        // ==================================================================== Einheiten

        /// <summary>Einheit der thermischen Leistung.</summary>
        internal const string EINHEIT_KW = "kW";

        /// <summary>Einheit eines Prozentanteils.</summary>
        internal const string EINHEIT_PROZENT = "%";

        /// <summary>Einheit eines Geldbetrags.</summary>
        internal const string EINHEIT_EURO = "€";

        /// <summary>Einheit eines Rauminhalts in Litern.</summary>
        internal const string EINHEIT_LITER = "l";

        /// <summary>Einheit einer Temperatur.</summary>
        internal const string EINHEIT_GRAD_C = "°C";

        /// <summary>Einheit eines Winkels.</summary>
        internal const string EINHEIT_GRAD = "°";

        /// <summary>Einheit einer Energiemenge in Kilowattstunden (Auftrag #200).</summary>
        internal const string EINHEIT_KWH = "kWh";

        /// <summary>Einheit einer Jahresenergiemenge in Megawattstunden (Auftrag #221).</summary>
        internal const string EINHEIT_MWH_A = "MWh/a";

        /// <summary>Einheit einer Jahresbetriebsdauer (Auftrag #221).</summary>
        internal const string EINHEIT_H_A = "h/a";

        /// <summary>
        /// Einheit einer Nutzungsdauer in Jahren, als Zeichen (Welle KI-F1) — so steht
        /// sie an den Feldern der Waermepumpen-Anlage.
        /// </summary>
        internal const string EINHEIT_JAHR = "a";

        /// <summary>
        /// Einheit einer taeglichen Stundenzahl (Welle KI-F1) — so steht sie an der
        /// Sperrzeit der Waermepumpe, auf der deutschen wie auf der englischen
        /// Oberflaeche.
        /// </summary>
        internal const string EINHEIT_H_TAG = "h/Tag";

        /// <summary>
        /// Einheit eines Arbeitspreises in Cent je Kilowattstunde (Welle KI-F2) — so
        /// steht sie an der Ladeschwelle und am Netzladeaufschlag des Stromspeichers.
        /// </summary>
        internal const string EINHEIT_CT_KWH = "ct/kWh";

        /// <summary>
        /// Einheit eines Leistungspreises (Welle KI-F2) — Euro je Kilowatt und Jahr.
        /// </summary>
        internal const string EINHEIT_EURO_KW_A = "€/(kW·a)";

        /// <summary>Einheit eines kapazitaetsbezogenen Jahresbetrags (Welle KI-F6).</summary>
        internal const string EINHEIT_EURO_KWH_A = "€/(kWh·a)";


        /// <summary>Einheit einer Laenge in Metern (Welle KI-F2).</summary>
        internal const string EINHEIT_METER = "m";

        /// <summary>Einheit einer Flaeche in Quadratmetern (Welle KI-F2).</summary>
        internal const string EINHEIT_M2 = "m²";

        /// <summary>Einheit einer Temperaturspreizung in Kelvin (Welle KI-F2).</summary>
        internal const string EINHEIT_KELVIN = "K";

        /// <summary>
        /// Einheit der Bereitschaftsverluste eines Speichers (Welle KI-F2) —
        /// Kilowattstunden je Tag, so steht sie am Feld.
        /// </summary>
        internal const string EINHEIT_KWH_24H = "kWh/24h";

        /// <summary>
        /// Einheit der effektiven Waermeleitfaehigkeit der Schichtung (Welle KI-F2).
        /// </summary>
        internal const string EINHEIT_W_MK = "W/(m·K)";

        /// <summary>Einheit einer Waermeleistung in Watt (Welle KI-F3).</summary>
        internal const string EINHEIT_W = "W";

        /// <summary>Einheit eines Waermedurchgangskoeffizienten (Welle KI-F3).</summary>
        internal const string EINHEIT_W_M2K = "W/(m²·K)";

        /// <summary>Einheit einer Luftwechselrate (Welle KI-F3).</summary>
        internal const string EINHEIT_1_H = "1/h";

        /// <summary>Einheit eines Jahresgrundpreises (Welle KI-F4).</summary>
        internal const string EINHEIT_EURO_A = "€/a";

        /// <summary>Einheit eines Emissionsfaktors je Kilowattstunde (Welle KI-F4).</summary>
        internal const string EINHEIT_G_KWH = "g/kWh";

        /// <summary>Einheit eines Arbeitspreises in Euro je Kilowattstunde (Welle KI-F4).</summary>
        internal const string EINHEIT_EURO_KWH = "€/kWh";

        /// <summary>Einheit eines Preises je Tonne CO₂ (Welle KI-F4).</summary>
        internal const string EINHEIT_EURO_T = "€/t";

        /// <summary>Einheit einer jaehrlichen Preissteigerung (Welle KI-F4).</summary>
        internal const string EINHEIT_PROZENT_A = "%/a";

        /// <summary>Einheit einer Betriebsstundenzahl (Welle KI-F4).</summary>
        internal const string EINHEIT_STUNDE = "h";

        /// <summary>Einheit einer elektrischen Leistung (Welle KI-F4).</summary>
        internal const string EINHEIT_KWP = "kWp";

        /// <summary>Einheit einer Zeitspanne in Jahren.</summary>
        internal static string EinheitJahre => MyResource.Resource.KI_DLG_EINHEIT_JAHRE;

        // ====================================================================== Masken

        internal static string MaskeHeizkessel => MyResource.Resource.KI_DLG_MASKE_HEIZKESSEL;
        internal static string MaskePv => MyResource.Resource.KI_DLG_MASKE_PV;

        /// <summary>
        /// Die Ueberlagerung „Anlagenwerte" der Photovoltaik (Welle KI-F7,
        /// Anwenderentscheid 21.09.2026).
        /// </summary>
        internal static string MaskePvAnlagenwerte
            => MyResource.Resource.KI_DLG_MASKE_PV_ANLAGENWERTE;

        internal static string MaskePufferSp => MyResource.Resource.KI_DLG_MASKE_PUFFERSP;
        internal static string MaskeWp => MyResource.Resource.KI_DLG_MASKE_WP;

        /// <summary>Die fuenfte Maske (Auftrag #200): die Stromspeicher-Ansicht.</summary>
        internal static string MaskeSpeicherauslegung => MyResource.Resource.KI_DLG_MASKE_SPA;

        /// <summary>Die siebte Maske (14.09.2026): die Kostenverwaltung mit ihrem Raster.</summary>
        internal static string MaskeKostenverwaltung => MyResource.Resource.KI_DLG_MASKE_KV;

        // ============================================================= Kostenverwaltung

        internal static string KvTitelName => MyResource.Resource.KI_DLG_KV_TITEL_NAME;
        internal static string KvTitelErl => MyResource.Resource.KI_DLG_KV_TITEL_ERL;
        internal static string KvUntertitelName => MyResource.Resource.KI_DLG_KV_UNTERTITEL_NAME;
        internal static string KvUntertitelErl => MyResource.Resource.KI_DLG_KV_UNTERTITEL_ERL;
        internal static string KvNurLesenName => MyResource.Resource.KI_DLG_KV_NURLESEN_NAME;
        internal static string KvNurLesenErl => MyResource.Resource.KI_DLG_KV_NURLESEN_ERL;
        internal static string KvPositionName => MyResource.Resource.KI_DLG_KV_POSITION_NAME;
        internal static string KvPositionErl => MyResource.Resource.KI_DLG_KV_POSITION_ERL;
        internal static string KvBemessungName => MyResource.Resource.KDLG_SP_BEMESSUNG;
        internal static string KvBemessungErl => MyResource.Resource.KI_DLG_KV_BEMESSUNG_ERL;
        internal static string KvSatzName => MyResource.Resource.KI_DLG_KV_SATZ_NAME;
        internal static string KvSatzErl => MyResource.Resource.KI_DLG_KV_SATZ_ERL;
        internal static string KvNutzungsdauerName => MyResource.Resource.KI_DLG_KV_NUTZUNGSDAUER_NAME;
        internal static string KvNutzungsdauerErl => MyResource.Resource.KI_DLG_KV_NUTZUNGSDAUER_ERL;
        internal static string KvBetragName => MyResource.Resource.KI_DLG_KV_BETRAG_NAME;
        internal static string KvBetragErl => MyResource.Resource.KI_DLG_KV_BETRAG_ERL;

        // Die drei Luecken der Welle KI-F4, geschlossen mit der Welle KI-F7: die
        // Komponentenwahl der Kontextleiste und die zwei Wahlen des Reiters „Ertrag".
        // Ihre Anzeigenamen sind die Beschriftungen der Maske.
        internal static string KvKomponenteName
            => MyResource.Resource.KI_DLG_KV_KOMPONENTENWAHL_NAME;
        internal static string KvKomponenteErl
            => MyResource.Resource.KI_DLG_KV_KOMPONENTENWAHL_ERL;
        internal static string KvPvWahlName => MyResource.Resource.KI_DLG_KV_PV_WAHL_NAME;
        internal static string KvPvWahlErl => MyResource.Resource.KI_DLG_KV_PV_WAHL_ERL;
        internal static string KvPvProjektName => MyResource.Resource.KI_DLG_KV_PV_PROJEKT_NAME;
        internal static string KvPvProjektErl => MyResource.Resource.KI_DLG_KV_PV_PROJEKT_ERL;

        /// <summary>Die sechste Maske (Auftrag #221): die Ansicht „Simulation".</summary>
        internal static string MaskeSimulation => MyResource.Resource.KI_DLG_MASKE_SIM;

        // ====================================================================== Knoepfe

        internal static string KnopfSpeichern => MyResource.Resource.KI_DLG_KNOPF_SPEICHERN;
        internal static string KnopfSpeichernUnter => MyResource.Resource.KI_DLG_KNOPF_SPEICHERN_UNTER;
        internal static string KnopfUeberschreiben => MyResource.Resource.KI_DLG_KNOPF_UEBERSCHREIBEN;
        internal static string KnopfAbbrechen => MyResource.Resource.KI_DLG_KNOPF_ABBRECHEN;
        internal static string KnopfOk => MyResource.Resource.KI_DLG_KNOPF_OK;

        /// <summary>Der nicht schliessende „Uebernehmen"-Knopf (Welle KI-F1).</summary>
        internal static string KnopfUebernehmen => MyResource.Resource.SKV_BTN_UEBERNEHMEN;

        // ========================================================== Heizkessel: Felder

        internal static string HkLeistungName => MyResource.Resource.KI_DLG_HK_LEISTUNG_NAME;
        internal static string HkLeistungErl => MyResource.Resource.KI_DLG_HK_LEISTUNG_ERL;
        internal static string HkWgGasName => MyResource.Resource.KI_DLG_HK_WG_GAS_NAME;
        internal static string HkWgGasErl => MyResource.Resource.KI_DLG_HK_WG_GAS_ERL;
        internal static string HkWgOelName => MyResource.Resource.KI_DLG_HK_WG_OEL_NAME;
        internal static string HkWgOelErl => MyResource.Resource.KI_DLG_HK_WG_OEL_ERL;
        internal static string HkBbVerlustName => MyResource.Resource.KI_DLG_HK_BB_VERLUST_NAME;
        internal static string HkBbVerlustErl => MyResource.Resource.KI_DLG_HK_BB_VERLUST_ERL;

        // HIER STANDEN DIE TEXTE der Felder Investition, Wartung, Raumbedarf,
        // Nutzungsdauer und der fuenf Emissionsfaktoren. Der Heizkessel-Katalogeditor
        // fuehrt sie seit dem Anwenderentscheid vom 15.09.2026 nicht mehr, also kennt
        // KiDialoge.Heizkessel() sie auch nicht mehr - ein Text ohne Feld ist eine
        // Pflegestelle ohne Leser. Mit ihnen sind die Schluessel KI_DLG_HK_INVEST_*,
        // KI_DLG_HK_WARTUNG_ERL, KI_DLG_HK_RAUMBEDARF_*, KI_DLG_HK_NUTZUNGSDAUER_*,
        // KI_DLG_HK_CO2_*, KI_DLG_HK_SO2_*, KI_DLG_HK_NOX_*, KI_DLG_HK_CO_* und
        // KI_DLG_HK_STAUB_* aus beiden .resx gefallen. KESSEL_WARTUNG_LBL bleibt:
        // Diesen Text setzt WartungsfeldAufbauen zur Laufzeit als Beschriftung.

        internal static string HkVorlaufName => MyResource.Resource.KI_DLG_HK_VORLAUF_NAME;
        internal static string HkVorlaufErl => MyResource.Resource.KI_DLG_HK_VORLAUF_ERL;
        internal static string HkRuecklaufName => MyResource.Resource.KI_DLG_HK_RUECKLAUF_NAME;
        internal static string HkRuecklaufErl => MyResource.Resource.KI_DLG_HK_RUECKLAUF_ERL;

        // Die uebrigen Eingabefelder des Katalogeditors (KI-F1b, KI-D-Q6).
        internal static string HkNameName => MyResource.Resource.HZKK_LBL_NAME;
        internal static string HkNameErl => MyResource.Resource.KI_DLG_HK_NAME_ERL;
        internal static string HkFirmaName => MyResource.Resource.HZKK_LBL_HERSTELLER;
        internal static string HkFirmaErl => MyResource.Resource.KI_DLG_HK_FIRMA_ERL;
        internal static string HkBeschreibungName => MyResource.Resource.HZKK_LBL_BESCHREIBUNG;
        internal static string HkBeschreibungErl => MyResource.Resource.KI_DLG_HK_BESCHREIBUNG_ERL;
        internal static string HkTraegerName => MyResource.Resource.HZKK_LBL_ENERGIETRAEGER;
        internal static string HkTraegerErl => MyResource.Resource.KI_DLG_HK_TRAEGER_ERL;
        internal static string HkBrennwertName => MyResource.Resource.HZKK_LBL_BRENNWERT;
        internal static string HkBrennwertErl => MyResource.Resource.KI_DLG_HK_BRENNWERT_ERL;

        // ======================================================== Photovoltaik: Felder

        internal static string PvNeigungName => MyResource.Resource.KI_DLG_PV_NEIGUNG_NAME;
        internal static string PvNeigungErl => MyResource.Resource.KI_DLG_PV_NEIGUNG_ERL;
        internal static string PvAzimutName => MyResource.Resource.KI_DLG_PV_AZIMUT_NAME;
        internal static string PvAzimutErl => MyResource.Resource.KI_DLG_PV_AZIMUT_ERL;
        internal static string PvAnzahlName => MyResource.Resource.KI_DLG_PV_ANZAHL_NAME;
        internal static string PvAnzahlErl => MyResource.Resource.KI_DLG_PV_ANZAHL_ERL;

        // Die Modellfelder und die Straenge (Welle KI-F1). Ihre Anzeigenamen sind die
        // Beschriftungen der beiden Bausteine - dieselben Schluessel, die PvModellTexte
        // und PvStrangTexte fuehren.
        internal static string PvModellName => MyResource.Resource.PVM_ANLAGE_LABEL_MODELL;
        internal static string PvModellErl => MyResource.Resource.KI_DLG_PV_MODELL_ERL;
        internal static string PvWrWirkungsgradName => MyResource.Resource.PV_ANLAGE_LABEL_WRWIRKUNGSGRAD;
        internal static string PvWrWirkungsgradErl => MyResource.Resource.KI_DLG_PV_WR_WIRKUNGSGRAD_ERL;
        internal static string PvSystemverlusteName => MyResource.Resource.PV_ANLAGE_LABEL_SYSTEMVERLUSTE;
        internal static string PvSystemverlusteErl => MyResource.Resource.KI_DLG_PV_SYSTEMVERLUSTE_ERL;
        internal static string PvMitWrName => MyResource.Resource.PVS_WAHL;
        internal static string PvMitWrErl => MyResource.Resource.KI_DLG_PV_MIT_WR_ERL;

        internal static string PvStrangName => MyResource.Resource.PVS_SP_BEZEICHNER;
        internal static string PvStrangErl => MyResource.Resource.KI_DLG_PV_STRANG_ERL;
        internal static string PvStrangGeraetName => MyResource.Resource.PVS_SP_GERAET;
        internal static string PvStrangGeraetErl => MyResource.Resource.KI_DLG_PV_STRANG_GERAET_ERL;
        internal static string PvStrangMpptName => MyResource.Resource.PVS_SP_MPPT;
        internal static string PvStrangMpptErl => MyResource.Resource.KI_DLG_PV_STRANG_MPPT_ERL;
        internal static string PvStrangReiheName => MyResource.Resource.PVS_SP_REIHE;
        internal static string PvStrangReiheErl => MyResource.Resource.KI_DLG_PV_STRANG_REIHE_ERL;
        internal static string PvStrangParallelName => MyResource.Resource.PVS_SP_PARALLEL;
        internal static string PvStrangParallelErl => MyResource.Resource.KI_DLG_PV_STRANG_PARALLEL_ERL;
        internal static string PvStrangNeigungName => MyResource.Resource.PVS_SP_NEIGUNG;
        internal static string PvStrangNeigungErl => MyResource.Resource.KI_DLG_PV_STRANG_NEIGUNG_ERL;
        internal static string PvStrangAzimutName => MyResource.Resource.PVS_SP_AZIMUT;
        internal static string PvStrangAzimutErl => MyResource.Resource.KI_DLG_PV_STRANG_AZIMUT_ERL;
        internal static string PvTraegerName => MyResource.Resource.ETW_LBL_ART;
        internal static string PvTraegerErl => MyResource.Resource.KI_DLG_PV_TRAEGER_ERL;

        // Die zwei AUSLEGUNGSTEMPERATUREN des Projekts (Welle KI-F7). Ihre
        // Maskenbeschriftungen sind „kalt [°C]" und „heiss [°C]" unter der Zeile
        // „Auslegungstemperaturen:" - als Feldname allein zu wenig, deshalb ein
        // eigener, ausgeschriebener Schluessel je Fall.
        internal static string PvAuslegKaltName => MyResource.Resource.KI_DLG_PV_AUSLEG_KALT_NAME;
        internal static string PvAuslegKaltErl => MyResource.Resource.KI_DLG_PV_AUSLEG_KALT_ERL;
        internal static string PvAuslegHeissName => MyResource.Resource.KI_DLG_PV_AUSLEG_HEISS_NAME;
        internal static string PvAuslegHeissErl => MyResource.Resource.KI_DLG_PV_AUSLEG_HEISS_ERL;

        // ============================================== Photovoltaik: Anlagenwerte

        // Die Ueberlagerung „Anlagenwerte" (Welle KI-F7). Ihre Feldnamen sind die
        // Beschriftungen des Fensters - dieselben Schluessel, die PvModellTexte fuehrt.
        internal static string PvaNennleistungName => MyResource.Resource.PVM_DLG_NENNLEISTUNG;
        internal static string PvaNennleistungErl => MyResource.Resource.KI_DLG_PVA_NENNLEISTUNG_ERL;
        internal static string PvaEta10Name => MyResource.Resource.PVM_DLG_ETA10;
        internal static string PvaEta50Name => MyResource.Resource.PVM_DLG_ETA50;
        internal static string PvaEta100Name => MyResource.Resource.PVM_DLG_ETA100;

        /// <summary>
        /// EINE Erlaeuterung fuer alle drei Wirkungsgrade: Sie sagen dasselbe ueber
        /// drei Lastpunkte, und drei fast gleiche Saetze waeren drei Stellen zum
        /// Auseinanderlaufen.
        /// </summary>
        internal static string PvaEtaErl => MyResource.Resource.KI_DLG_PVA_ETA_ERL;

        // ====================================================== Pufferspeicher: Felder

        internal static string PspVolumenName => MyResource.Resource.KI_DLG_PSP_VOLUMEN_NAME;
        internal static string PspVolumenErl => MyResource.Resource.KI_DLG_PSP_VOLUMEN_ERL;

        // Die uebrigen Eingabefelder des Katalogeditors (KI-F1b, KI-D-Q6).
        internal static string PspNameName => MyResource.Resource.PSPK_LBL_NAME;
        internal static string PspNameErl => MyResource.Resource.KI_DLG_PSP_NAME_ERL;
        internal static string PspFirmaName => MyResource.Resource.PSPK_LBL_HERSTELLER;
        internal static string PspFirmaErl => MyResource.Resource.KI_DLG_PSP_FIRMA_ERL;
        internal static string PspSpeichertypName => MyResource.Resource.PSPK_LBL_SPEICHERTYP;
        internal static string PspSpeichertypErl => MyResource.Resource.KI_DLG_PSP_SPEICHERTYP_ERL;
        internal static string PspVerlusteName => MyResource.Resource.PSPK_LBL_VERLUSTE;
        internal static string PspVerlusteErl => MyResource.Resource.KI_DLG_PSP_VERLUSTE_ERL;

        // ========================================================= Waermepumpe: Felder

        internal static string WpModulkostenName => MyResource.Resource.KI_DLG_WP_MODULKOSTEN_NAME;
        internal static string WpModulkostenErl => MyResource.Resource.KI_DLG_WP_MODULKOSTEN_ERL;

        // Die Stammfelder des Katalogeditors (KI-F1b, KI-D-Q6). Ihre Anzeigenamen sind
        // die Beschriftungen des Bausteins WaermepumpeStammFelder - dieselben, die auch
        // der Anlagendialog zeigt.
        internal static string WpNameName => MyResource.Resource.WPS_LBL_NAME;
        internal static string WpNameErl => MyResource.Resource.KI_DLG_WP_NAME_ERL;
        internal static string WpKuehlleistungName => MyResource.Resource.WPS_LBL_KUEHLLEISTUNG;
        internal static string WpKuehlleistungErl => MyResource.Resource.KI_DLG_WP_KUEHLLEISTUNG_ERL;

        // ========================================= Welle KI-F1: Erzeuger im Projekt
        //
        // DIE ANZEIGENAMEN SIND DIE BESCHRIFTUNGEN DER MASKE, nicht eigene KI-Texte:
        // Es sind dieselben Ressourcenschluessel, die die Huelle in den Dialog reicht
        // (HZKK_LBL_VORLAUF, BHKWV_LBL_GRENZLEISTUNG …). Ein zweiter Name fuer dasselbe
        // Feld waere eine zweite Wahrheit - der Anwender liest in der Bestaetigung
        // woertlich das, was ueber dem Eingabefeld steht. Neu ist je Feld allein die
        // ERLAEUTERUNG (KI_DLG_<MASKE>_<FELD>_ERL).

        internal static string MaskeHeizkesselProjekt => MyResource.Resource.KI_DLG_MASKE_HEIZKESSEL_PROJEKT;
        internal static string MaskeBhkwProjekt => MyResource.Resource.KI_DLG_MASKE_BHKW;

        // ------------------------------------------- Form_Heizkessel (Projektmaske)

        internal static string HkpAnlageName => MyResource.Resource.HZK_LBL_NAME;
        internal static string HkpAnlageErl => MyResource.Resource.KI_DLG_HKP_ANLAGE_ERL;
        internal static string HkpVorlaufName => MyResource.Resource.HZKK_LBL_VORLAUF;
        internal static string HkpVorlaufErl => MyResource.Resource.KI_DLG_HKP_VORLAUF_ERL;
        internal static string HkpRuecklaufName => MyResource.Resource.HZKK_LBL_RUECKLAUF;
        internal static string HkpRuecklaufErl => MyResource.Resource.KI_DLG_HKP_RUECKLAUF_ERL;
        internal static string HkpTraegerName => MyResource.Resource.HZK_LBL_TRAEGER;
        internal static string HkpTraegerErl => MyResource.Resource.KI_DLG_HKP_TRAEGER_ERL;

        // -------------------------------------------- Form_BHKWEing (Projektmaske)

        internal static string BhkwAnlageName => MyResource.Resource.BHKWV_LBL_NAME;
        internal static string BhkwAnlageErl => MyResource.Resource.KI_DLG_BHKW_ANLAGE_ERL;
        internal static string BhkwGrenzleistungName => MyResource.Resource.BHKWV_LBL_GRENZLEISTUNG;
        internal static string BhkwGrenzleistungErl => MyResource.Resource.KI_DLG_BHKW_GRENZLEISTUNG_ERL;
        internal static string BhkwVorlaufName => MyResource.Resource.BHKWV_LBL_VORLAUF;
        internal static string BhkwVorlaufErl => MyResource.Resource.KI_DLG_BHKW_VORLAUF_ERL;
        internal static string BhkwRuecklaufName => MyResource.Resource.BHKWV_LBL_RUECKLAUF;
        internal static string BhkwRuecklaufErl => MyResource.Resource.KI_DLG_BHKW_RUECKLAUF_ERL;
        internal static string BhkwTraegerName => MyResource.Resource.HZK_LBL_TRAEGER;
        internal static string BhkwTraegerErl => MyResource.Resource.KI_DLG_BHKW_TRAEGER_ERL;

        // -------------------------------------------- Form_PufferSp (Projektmaske)

        internal static string MaskePufferSpProjekt => MyResource.Resource.KI_DLG_MASKE_PUFFERSP_PROJEKT;
        internal static string PspAnlageName => MyResource.Resource.HZK_LBL_NAME;
        internal static string PspAnlageErl => MyResource.Resource.KI_DLG_PSPP_ANLAGE_ERL;

        // ---------------------------------------- Form_Stromspeicher (Projektmaske)

        internal static string MaskeStromspeicherProjekt => MyResource.Resource.KI_DLG_MASKE_STROMSPEICHER;
        internal static string StspAnlageName => MyResource.Resource.HZK_LBL_NAME;
        internal static string StspAnlageErl => MyResource.Resource.KI_DLG_STSP_ANLAGE_ERL;
        internal static string StspTraegerName => MyResource.Resource.ETW_LBL_ART;
        internal static string StspTraegerErl => MyResource.Resource.KI_DLG_STSP_TRAEGER_ERL;

        // ------------------------------------- Form_SolarKollektoren (Projektmaske)

        internal static string MaskeSolarkollektoren => MyResource.Resource.KI_DLG_MASKE_SOLARKOLLEKTOREN;
        internal static string SkAnzahlName => MyResource.Resource.SKV_LBL_ANZAHL;
        internal static string SkAnzahlErl => MyResource.Resource.KI_DLG_SK_ANZAHL_ERL;
        internal static string SkNeigungName => MyResource.Resource.SKV_LBL_NEIGUNG;
        internal static string SkNeigungErl => MyResource.Resource.KI_DLG_SK_NEIGUNG_ERL;
        internal static string SkAzimutName => MyResource.Resource.SKV_LBL_AZIMUT;
        internal static string SkAzimutErl => MyResource.Resource.KI_DLG_SK_AZIMUT_ERL;
        internal static string SkVorlaufName => MyResource.Resource.SKK_LBL_VORLAUF;
        internal static string SkVorlaufErl => MyResource.Resource.KI_DLG_SK_VORLAUF_ERL;
        internal static string SkRuecklaufName => MyResource.Resource.SKK_LBL_RUECKLAUF;
        internal static string SkRuecklaufErl => MyResource.Resource.KI_DLG_SK_RUECKLAUF_ERL;

        // ------------------------------------------ Form_WP_Anlage (Waermepumpe)

        internal static string MaskeWpAnlage => MyResource.Resource.KI_DLG_MASKE_WP_ANLAGE;

        internal static string WpaAnlageName => MyResource.Resource.WPS_LBL_NAME;
        internal static string WpaAnlageErl => MyResource.Resource.KI_DLG_WPA_ANLAGE_ERL;
        internal static string WpaVorlaufName => MyResource.Resource.WPA_LBL_VORLAUF;
        internal static string WpaVorlaufErl => MyResource.Resource.KI_DLG_WPA_VORLAUF_ERL;
        internal static string WpaRuecklaufName => MyResource.Resource.WPA_LBL_RUECKLAUF;
        internal static string WpaRuecklaufErl => MyResource.Resource.KI_DLG_WPA_RUECKLAUF_ERL;
        internal static string WpaNutzungsdauerName => MyResource.Resource.WPA_LBL_NUTZUNGSZEIT;
        internal static string WpaNutzungsdauerErl => MyResource.Resource.KI_DLG_WPA_NUTZUNGSDAUER_ERL;

        internal static string WpaHeizstabName => MyResource.Resource.WPA_CHK_HEIZSTAB;
        internal static string WpaHeizstabErl => MyResource.Resource.KI_DLG_WPA_HEIZSTAB_ERL;
        internal static string WpaSperrungName => MyResource.Resource.WPA_CHK_SPERRZEIT;
        internal static string WpaSperrungErl => MyResource.Resource.KI_DLG_WPA_SPERRUNG_ERL;
        internal static string WpaSperrzeitVonName => MyResource.Resource.WPA_LBL_VON;
        internal static string WpaSperrzeitVonErl => MyResource.Resource.KI_DLG_WPA_SPERRZEIT_VON_ERL;
        internal static string WpaSperrzeitBisName => MyResource.Resource.WPA_LBL_BIS;
        internal static string WpaSperrzeitBisErl => MyResource.Resource.KI_DLG_WPA_SPERRZEIT_BIS_ERL;
        internal static string WpaBivalentName => MyResource.Resource.WPA_LBL_BIVALENT;
        internal static string WpaBivalentErl => MyResource.Resource.KI_DLG_WPA_BIVALENT_ERL;
        internal static string WpaBetriebsartName => MyResource.Resource.WPA_LBL_BETRIEBSART;
        internal static string WpaBetriebsartErl => MyResource.Resource.KI_DLG_WPA_BETRIEBSART_ERL;
        internal static string WpaTraegerName => MyResource.Resource.ETW_LBL_ART;
        internal static string WpaTraegerErl => MyResource.Resource.KI_DLG_WPA_TRAEGER_ERL;
        internal static string WpaAbschaltpunktName => MyResource.Resource.WPA_LBL_ABSCHALTTEMP;
        internal static string WpaAbschaltpunktErl => MyResource.Resource.KI_DLG_WPA_ABSCHALTPUNKT_ERL;

        internal static string WpaFirmaName => MyResource.Resource.WPS_LBL_HERSTELLER;
        internal static string WpaFirmaErl => MyResource.Resource.KI_DLG_WPA_FIRMA_ERL;
        internal static string WpaBeschreibungName => MyResource.Resource.WPS_LBL_BESCHREIBUNG;
        internal static string WpaBeschreibungErl => MyResource.Resource.KI_DLG_WPA_BESCHREIBUNG_ERL;
        internal static string WpaTypName => MyResource.Resource.WPS_LBL_TYP;
        internal static string WpaTypErl => MyResource.Resource.KI_DLG_WPA_TYP_ERL;
        internal static string WpaRegelungName => MyResource.Resource.WPS_LBL_REGELUNG;
        internal static string WpaRegelungErl => MyResource.Resource.KI_DLG_WPA_REGELUNG_ERL;
        internal static string WpaAufstellungName => MyResource.Resource.WPS_LBL_AUFSTELLUNG;
        internal static string WpaAufstellungErl => MyResource.Resource.KI_DLG_WPA_AUFSTELLUNG_ERL;
        internal static string WpaBaujahrName => MyResource.Resource.WPS_LBL_BAUJAHR;
        internal static string WpaBaujahrErl => MyResource.Resource.KI_DLG_WPA_BAUJAHR_ERL;
        internal static string WpaNennleistungName => MyResource.Resource.WPS_LBL_NENNLEISTUNG;
        internal static string WpaNennleistungErl => MyResource.Resource.KI_DLG_WPA_NENNLEISTUNG_ERL;
        internal static string WpaHeizstabLeistungName => MyResource.Resource.WPS_LBL_HEIZSTAB;
        internal static string WpaHeizstabLeistungErl => MyResource.Resource.KI_DLG_WPA_HEIZSTAB_KW_ERL;
        internal static string WpaKuehlleistungName => MyResource.Resource.WPS_LBL_KUEHLLEISTUNG;
        internal static string WpaKuehlleistungErl => MyResource.Resource.KI_DLG_WPA_KUEHLLEISTUNG_ERL;
        internal static string WpaModulkostenName => MyResource.Resource.MODK_LBL_MODULKOSTEN;
        internal static string WpaModulkostenErl => MyResource.Resource.KI_DLG_WPA_MODULKOSTEN_ERL;

        // Welle #458: die Projekteinstellung neben der Auslegung.
        internal static string WpaExtrapolationName => MyResource.Resource.SIM_EXTRAPOLATION_SCHALTER;
        internal static string WpaExtrapolationErl => MyResource.Resource.KI_DLG_WPA_EXTRAPOLATION_ERL;

        // =================================================================== Feldarten

        internal static string TypGanzzahl => MyResource.Resource.KI_DLG_TYP_GANZZAHL;
        internal static string TypZahl => MyResource.Resource.KI_DLG_TYP_ZAHL;
        internal static string TypText => MyResource.Resource.KI_DLG_TYP_TEXT;
        internal static string TypWahrheit => MyResource.Resource.KI_DLG_TYP_WAHRHEIT;
        internal static string TypAuswahl => MyResource.Resource.KI_DLG_TYP_AUSWAHL;
        internal static string LeerErlaubt => MyResource.Resource.KI_DLG_LEER_ERLAUBT;
        internal static string LeerPflicht => MyResource.Resource.KI_DLG_LEER_PFLICHT;

        // ================================================================ Ablehnungen

        /// <summary>{0} = genannte Maske, {1} = freigegebene Masken.</summary>
        internal static string MaskeUnbekannt => MyResource.Resource.KI_DLG_MASKE_UNBEKANNT;

        /// <summary>{0} = freigegebene Masken.</summary>
        internal static string KeineOffen => MyResource.Resource.KI_DLG_KEINE_OFFEN;

        /// <summary>
        /// Die gemeinte Maske ist bekannt, aber nicht offen - mit dem Weg dorthin
        /// (Anwenderbefund 15.09.2026). {0} = Anzeigename, {1} = Maskenschluessel.
        /// </summary>
        internal static string MaskeNichtOffen => MyResource.Resource.KI_DLG_MASKE_NICHT_OFFEN;

        /// <summary>{0} = die offenen Masken.</summary>
        internal static string MehrereOffen => MyResource.Resource.KI_DLG_MEHRERE_OFFEN;

        /// <summary>{0} = Anzeigename der Maske.</summary>
        internal static string NichtOffen => MyResource.Resource.KI_DLG_NICHT_OFFEN;

        /// <summary>{0} = Anzeigename der Maske.</summary>
        internal static string MehrfachOffen => MyResource.Resource.KI_DLG_MEHRFACH_OFFEN;

        /// <summary>{0} = Anzeigename der Maske.</summary>
        internal static string NichtAktiv => MyResource.Resource.KI_DLG_NICHT_AKTIV;

        /// <summary>{0} = genanntes Feld, {1} = Maske, {2} = bekannte Felder.</summary>
        internal static string FeldUnbekannt => MyResource.Resource.KI_DLG_FELD_UNBEKANNT;

        /// <summary>{0} = genannter Knopf, {1} = Maske, {2} = freigegebene Knoepfe.</summary>
        internal static string KnopfUnbekannt => MyResource.Resource.KI_DLG_KNOPF_UNBEKANNT;

        /// <summary>{0} = Anzeigename, {1} = Controlpfad.</summary>
        internal static string ControlFehlt => MyResource.Resource.KI_DLG_CONTROL_FEHLT;

        /// <summary>{0} = Anzeigename, {1} = Typname des Controls.</summary>
        internal static string ControlArt => MyResource.Resource.KI_DLG_CONTROL_ART;

        /// <summary>{0} = Anzeigename.</summary>
        internal static string ControlReadOnly => MyResource.Resource.KI_DLG_CONTROL_READONLY;

        /// <summary>{0} = Anzeigename.</summary>
        internal static string ControlGesperrt => MyResource.Resource.KI_DLG_CONTROL_GESPERRT;

        /// <summary>{0} = Anzeigename des Knopfes.</summary>
        internal static string KnopfGesperrt => MyResource.Resource.KI_DLG_KNOPF_GESPERRT;

        /// <summary>
        /// Hinweis an einem Feld, das <c>dialog_lesen</c> zwar liefert, aber nicht setzen
        /// kann — eine abgeleitete Groesse (Auftrag #200).
        /// </summary>
        internal static string NichtSetzbar => MyResource.Resource.KI_DLG_NICHT_SETZBAR;

        /// <summary>
        /// Hinweis an einem Knopf: Ausloesen kommt mit Stufe S3 (Auftrag #201).
        /// </summary>
        internal static string KnopfSpaeter => MyResource.Resource.KI_DLG_KNOPF_SPAETER;

        /// <summary>{0} = Anzeigename des Feldes, {1} = gewuenschter Wert, {2} = Eintraege.</summary>
        internal static string AuswahlUnbekannt => MyResource.Resource.KI_DLG_AUSWAHL_UNBEKANNT;

        /// <summary>{0} = Anzeigename des Feldes, {1} = gewuenschter Wert.</summary>
        internal static string AuswahlMehrdeutig => MyResource.Resource.KI_DLG_AUSWAHL_MEHRDEUTIG;

        /// <summary>{0} = Anzeigename des Feldes.</summary>
        internal static string FalscherThread => MyResource.Resource.KI_DLG_FALSCHER_THREAD;

        internal static string WerteLeer => MyResource.Resource.KI_DLG_WERTE_LEER;

        /// <summary>{0} = der nicht lesbare Abschnitt.</summary>
        internal static string WerteFormat => MyResource.Resource.KI_DLG_WERTE_FORMAT;

        /// <summary>{0} = Feldname.</summary>
        internal static string WerteDoppelt => MyResource.Resource.KI_DLG_WERTE_DOPPELT;

        /// <summary>{0} = Anzeigename der Maske.</summary>
        internal static string OhneAenderung => MyResource.Resource.KI_DLG_OHNE_AENDERUNG;

        // ================================================================= Ergebnisse

        /// <summary>{0} = Maske, {1} = Zahl der Felder, {2} = Zahl der Knoepfe.</summary>
        internal static string Gelesen => MyResource.Resource.KI_DLG_GELESEN;

        /// <summary>{0} = Feld, {1} = Maske.</summary>
        internal static string Erklaert => MyResource.Resource.KI_DLG_ERKLAERT;

        /// <summary>{0} = Feld, {1} = neuer Wert, {2} = alter Wert.</summary>
        internal static string FeldGesetzt => MyResource.Resource.KI_DLG_FELD_GESETZT;

        /// <summary>{0} = Zahl der Felder, {1} = Maske.</summary>
        internal static string FelderGesetzt => MyResource.Resource.KI_DLG_FELDER_GESETZT;

        /// <summary>{0} = Knopf, {1} = Maske.</summary>
        internal static string KnopfAusgeloest => MyResource.Resource.KI_DLG_KNOPF_AUSGELOEST;

        internal static string KnopfHinweis => MyResource.Resource.KI_DLG_KNOPF_HINWEIS;

        // ========================================== Stromspeicher-Auslegung: Felder
        //
        // Die FUENFTE Deklaration (Auftrag #200, Konzept 3.3). Sie ist die einzige, die
        // auch ABGELEITETE Groessen fuehrt - Diagnose und Ergebnis der letzten Bewertung.
        // Genau die machen die Erklaerung stark: "Warum ist die Flotte arbeitslos?" wird
        // damit mit den echten Zaehlern beantwortet und nicht mit einer Vermutung.

        internal static string SpaEinheitenName => MyResource.Resource.KI_DLG_SPA_EINHEITEN_NAME;
        internal static string SpaEinheitenErl => MyResource.Resource.KI_DLG_SPA_EINHEITEN_ERL;
        internal static string SpaListeName => MyResource.Resource.KI_DLG_SPA_LISTE_NAME;
        internal static string SpaListeErl => MyResource.Resource.KI_DLG_SPA_LISTE_ERL;
        internal static string SpaKapazitaetName => MyResource.Resource.KI_DLG_SPA_KAPAZITAET_NAME;
        internal static string SpaKapazitaetErl => MyResource.Resource.KI_DLG_SPA_KAPAZITAET_ERL;
        internal static string SpaLadeleistungName => MyResource.Resource.KI_DLG_SPA_LADEN_NAME;
        internal static string SpaLadeleistungErl => MyResource.Resource.KI_DLG_SPA_LADEN_ERL;
        internal static string SpaEntladeleistungName => MyResource.Resource.KI_DLG_SPA_ENTLADEN_NAME;
        internal static string SpaEntladeleistungErl => MyResource.Resource.KI_DLG_SPA_ENTLADEN_ERL;
        internal static string SpaBetriebszielName => MyResource.Resource.KI_DLG_SPA_ZIEL_NAME;
        internal static string SpaBetriebszielErl => MyResource.Resource.KI_DLG_SPA_ZIEL_ERL;
        internal static string SpaPeakZielName => MyResource.Resource.KI_DLG_SPA_PEAK_NAME;
        internal static string SpaPeakZielErl => MyResource.Resource.KI_DLG_SPA_PEAK_ERL;
        internal static string SpaAdaptivName => MyResource.Resource.KI_DLG_SPA_ADAPTIV_NAME;
        internal static string SpaAdaptivErl => MyResource.Resource.KI_DLG_SPA_ADAPTIV_ERL;
        internal static string SpaNetzladungName => MyResource.Resource.KI_DLG_SPA_NETZLADUNG_NAME;
        internal static string SpaNetzladungErl => MyResource.Resource.KI_DLG_SPA_NETZLADUNG_ERL;
        internal static string SpaStartSocName => MyResource.Resource.KI_DLG_SPA_STARTSOC_NAME;
        internal static string SpaStartSocErl => MyResource.Resource.KI_DLG_SPA_STARTSOC_ERL;
        internal static string SpaReserveName => MyResource.Resource.KI_DLG_SPA_RESERVE_NAME;
        internal static string SpaReserveErl => MyResource.Resource.KI_DLG_SPA_RESERVE_ERL;
        internal static string SpaArbeitslosName => MyResource.Resource.KI_DLG_SPA_ARBEITSLOS_NAME;
        internal static string SpaArbeitslosErl => MyResource.Resource.KI_DLG_SPA_ARBEITSLOS_ERL;
        internal static string SpaGruendeName => MyResource.Resource.KI_DLG_SPA_GRUENDE_NAME;
        internal static string SpaGruendeErl => MyResource.Resource.KI_DLG_SPA_GRUENDE_ERL;
        internal static string SpaHinweiseName => MyResource.Resource.KI_DLG_SPA_HINWEISE_NAME;
        internal static string SpaHinweiseErl => MyResource.Resource.KI_DLG_SPA_HINWEISE_ERL;
        internal static string SpaSpitzeName => MyResource.Resource.KI_DLG_SPA_SPITZE_NAME;
        internal static string SpaSpitzeErl => MyResource.Resource.KI_DLG_SPA_SPITZE_ERL;
        internal static string SpaNetzbezugName => MyResource.Resource.KI_DLG_SPA_NETZBEZUG_NAME;
        internal static string SpaNetzbezugErl => MyResource.Resource.KI_DLG_SPA_NETZBEZUG_ERL;
        internal static string SpaKapitalwertName => MyResource.Resource.KI_DLG_SPA_KAPITALWERT_NAME;
        internal static string SpaKapitalwertErl => MyResource.Resource.KI_DLG_SPA_KAPITALWERT_ERL;

        // --- Station 4 „Optimierung" und der Schritt der Ansicht (Auftrag #224) ---

        internal static string SpaSchrittName => MyResource.Resource.KI_DLG_SPA_SCHRITT_NAME;
        internal static string SpaSchrittErl => MyResource.Resource.KI_DLG_SPA_SCHRITT_ERL;
        internal static string SpaSucheName => MyResource.Resource.KI_DLG_SPA_SUCHE_NAME;
        internal static string SpaSucheErl => MyResource.Resource.KI_DLG_SPA_SUCHE_ERL;
        internal static string SpaMethodeName => MyResource.Resource.KI_DLG_SPA_METHODE_NAME;
        internal static string SpaMethodeErl => MyResource.Resource.KI_DLG_SPA_METHODE_ERL;
        internal static string SpaFeinrasterName => MyResource.Resource.KI_DLG_SPA_FEINRASTER_NAME;
        internal static string SpaFeinrasterErl => MyResource.Resource.KI_DLG_SPA_FEINRASTER_ERL;
        internal static string SpaMaxKandidatenName => MyResource.Resource.KI_DLG_SPA_MAXKAND_NAME;
        internal static string SpaMaxKandidatenErl => MyResource.Resource.KI_DLG_SPA_MAXKAND_ERL;
        internal static string SpaKandidatenzahlName => MyResource.Resource.KI_DLG_SPA_KANDZAHL_NAME;
        internal static string SpaKandidatenzahlErl => MyResource.Resource.KI_DLG_SPA_KANDZAHL_ERL;
        internal static string SpaBestwertName => MyResource.Resource.KI_DLG_SPA_BESTWERT_NAME;
        internal static string SpaBestwertErl => MyResource.Resource.KI_DLG_SPA_BESTWERT_ERL;
        internal static string SpaBestkapazitaetName => MyResource.Resource.KI_DLG_SPA_BESTKAP_NAME;
        internal static string SpaBestkapazitaetErl => MyResource.Resource.KI_DLG_SPA_BESTKAP_ERL;
        internal static string SpaBestersparnisName => MyResource.Resource.KI_DLG_SPA_BESTERSPARNIS_NAME;
        internal static string SpaBestersparnisErl => MyResource.Resource.KI_DLG_SPA_BESTERSPARNIS_ERL;
        internal static string SpaBestphaseName => MyResource.Resource.KI_DLG_SPA_BESTPHASE_NAME;
        internal static string SpaBestphaseErl => MyResource.Resource.KI_DLG_SPA_BESTPHASE_ERL;

        // ========================================= Simulation: Felder (Auftrag #221)

        internal static string SimSchrittName => MyResource.Resource.KI_DLG_SIM_SCHRITT_NAME;
        internal static string SimSchrittErl => MyResource.Resource.KI_DLG_SIM_SCHRITT_ERL;
        internal static string SimReiterName => MyResource.Resource.KI_DLG_SIM_REITER_NAME;
        internal static string SimReiterErl => MyResource.Resource.KI_DLG_SIM_REITER_ERL;
        internal static string SimKaskadeName => MyResource.Resource.KI_DLG_SIM_KASKADE_NAME;
        internal static string SimKaskadeErl => MyResource.Resource.KI_DLG_SIM_KASKADE_ERL;
        internal static string SimOhnePlatzName => MyResource.Resource.KI_DLG_SIM_OHNE_PLATZ_NAME;
        internal static string SimOhnePlatzErl => MyResource.Resource.KI_DLG_SIM_OHNE_PLATZ_ERL;
        internal static string SimNetzverlusteName => MyResource.Resource.KI_DLG_SIM_NETZVERLUSTE_NAME;
        internal static string SimNetzverlusteErl => MyResource.Resource.KI_DLG_SIM_NETZVERLUSTE_ERL;
        internal static string SimBetriebsartName => MyResource.Resource.KI_DLG_SIM_BETRIEBSART_NAME;
        internal static string SimBetriebsartErl => MyResource.Resource.KI_DLG_SIM_BETRIEBSART_ERL;
        internal static string SimLeistungsgrenzeName => MyResource.Resource.KI_DLG_SIM_GRENZE_NAME;
        internal static string SimLeistungsgrenzeErl => MyResource.Resource.KI_DLG_SIM_GRENZE_ERL;
        internal static string SimHeizstabName => MyResource.Resource.KI_DLG_SIM_HEIZSTAB_NAME;
        internal static string SimHeizstabErl => MyResource.Resource.KI_DLG_SIM_HEIZSTAB_ERL;
        internal static string SimBereitschaftName => MyResource.Resource.KI_DLG_SIM_BEREITSCHAFT_NAME;
        internal static string SimBereitschaftErl => MyResource.Resource.KI_DLG_SIM_BEREITSCHAFT_ERL;
        internal static string SimWaermebedarfName => MyResource.Resource.KI_DLG_SIM_WBEDARF_NAME;
        internal static string SimWaermebedarfErl => MyResource.Resource.KI_DLG_SIM_WBEDARF_ERL;
        internal static string SimWaermedeckungName => MyResource.Resource.KI_DLG_SIM_WDECKUNG_NAME;
        internal static string SimWaermedeckungErl => MyResource.Resource.KI_DLG_SIM_WDECKUNG_ERL;
        internal static string SimRestwaermeName => MyResource.Resource.KI_DLG_SIM_WREST_NAME;
        internal static string SimRestwaermeErl => MyResource.Resource.KI_DLG_SIM_WREST_ERL;
        internal static string SimStrombedarfName => MyResource.Resource.KI_DLG_SIM_SBEDARF_NAME;
        internal static string SimStrombedarfErl => MyResource.Resource.KI_DLG_SIM_SBEDARF_ERL;
        internal static string SimStromdeckungName => MyResource.Resource.KI_DLG_SIM_SDECKUNG_NAME;
        internal static string SimStromdeckungErl => MyResource.Resource.KI_DLG_SIM_SDECKUNG_ERL;
        internal static string SimReststromName => MyResource.Resource.KI_DLG_SIM_SREST_NAME;
        internal static string SimReststromErl => MyResource.Resource.KI_DLG_SIM_SREST_ERL;
        internal static string SimSpeicherEntladungName => MyResource.Resource.KI_DLG_SIM_SP_ENTLADUNG_NAME;
        internal static string SimSpeicherEntladungErl => MyResource.Resource.KI_DLG_SIM_SP_ENTLADUNG_ERL;
        internal static string SimSpeicherSocName => MyResource.Resource.KI_DLG_SIM_SP_SOC_NAME;
        internal static string SimSpeicherSocErl => MyResource.Resource.KI_DLG_SIM_SP_SOC_ERL;
        internal static string SimHinweiseName => MyResource.Resource.KI_DLG_SIM_HINWEISE_NAME;
        internal static string SimHinweiseErl => MyResource.Resource.KI_DLG_SIM_HINWEISE_ERL;

        // ---- Der Lesepunkt und der Reiter „Stromspeicher" (Welle KI-F2) ----------
        //
        // Die ANZEIGENAMEN sind die Beschriftungen, die auf der Ansicht stehen
        // (SP_PARAM_*, PREIS_PARAM_*, SIM_BOOSTER_*); neu ist je Feld allein die
        // Erlaeuterung.

        internal static string SimLesepunktName => MyResource.Resource.SIM_BOOSTER_LESEPUNKT_SCHALTER;
        internal static string SimLesepunktErl => MyResource.Resource.KI_DLG_SIM_LESEPUNKT_ERL;

        // ---- Kuehlschalter und Werte je Anlage von Schritt ① (Welle #458) --------

        internal static string SimKuehlbetriebName => MyResource.Resource.SIMKONF_LBL_KUEHLBETRIEB;
        internal static string SimKuehlbetriebErl => MyResource.Resource.KI_DLG_SIM_KUEHLBETRIEB_ERL;
        internal static string SimAnlageName => MyResource.Resource.KI_DLG_SIM_ANLAGE_NAME;
        internal static string SimAnlageErl => MyResource.Resource.KI_DLG_SIM_ANLAGE_ERL;
        internal static string SimQuelleName => MyResource.Resource.KI_DLG_SIM_QUELLE_NAME;
        internal static string SimQuelleErl => MyResource.Resource.KI_DLG_SIM_QUELLE_ERL;
        internal static string SimQuelltempName => MyResource.Resource.KI_DLG_SIM_QUELLTEMP_NAME;
        internal static string SimQuelltempErl => MyResource.Resource.KI_DLG_SIM_QUELLTEMP_ERL;
        internal static string SimPrioritaetName => MyResource.Resource.KI_DLG_SIM_PRIORITAET_NAME;
        internal static string SimPrioritaetErl => MyResource.Resource.KI_DLG_SIM_PRIORITAET_ERL;
        internal static string SimBetriebsmodusName => MyResource.Resource.KI_DLG_SIM_BETRIEBSMODUS_NAME;
        internal static string SimBetriebsmodusErl => MyResource.Resource.KI_DLG_SIM_BETRIEBSMODUS_ERL;
        internal static string SimSpSocMinName => MyResource.Resource.SP_PARAM_LABEL_SOC_MIN;
        internal static string SimSpSocMinErl => MyResource.Resource.KI_DLG_SIM_SP_SOCMIN_ERL;
        internal static string SimSpSocMaxName => MyResource.Resource.SP_PARAM_LABEL_SOC_MAX;
        internal static string SimSpSocMaxErl => MyResource.Resource.KI_DLG_SIM_SP_SOCMAX_ERL;
        internal static string SimSpLadeleistungName => MyResource.Resource.SP_PARAM_LABEL_LADELEISTUNG;
        internal static string SimSpLadeleistungErl => MyResource.Resource.KI_DLG_SIM_SP_LADELEISTUNG_ERL;
        internal static string SimSpKapazitaetName => MyResource.Resource.SP_PARAM_LABEL_KAPAZITAET;
        internal static string SimSpKapazitaetErl => MyResource.Resource.KI_DLG_SIM_SP_KAPAZITAET_ERL;
        internal static string SimSpLadeschwelleName => MyResource.Resource.SP_PARAM_LABEL_LADESCHWELLE;
        internal static string SimSpLadeschwelleErl => MyResource.Resource.KI_DLG_SIM_SP_LADESCHWELLE_ERL;
        internal static string SimSpBetriebsartName => MyResource.Resource.SP_PARAM_LABEL_BETRIEBSART;
        internal static string SimSpBetriebsartErl => MyResource.Resource.KI_DLG_SIM_SP_BETRIEBSART_ERL;
        internal static string SimSpBerechnungsartName => MyResource.Resource.SP_PARAM_LABEL_BERECHNUNGSART;
        internal static string SimSpBerechnungsartErl => MyResource.Resource.KI_DLG_SIM_SP_BERECHNUNGSART_ERL;
        internal static string SimSpPeakZielName => MyResource.Resource.SP_PARAM_LABEL_PEAKZIEL;
        internal static string SimSpPeakZielErl => MyResource.Resource.KI_DLG_SIM_SP_PEAKZIEL_ERL;
        internal static string SimSpPeakAdaptivName => MyResource.Resource.SP_PARAM_LABEL_PEAKZIEL_ADAPTIV;
        internal static string SimSpPeakAdaptivErl => MyResource.Resource.KI_DLG_SIM_SP_PEAKADAPTIV_ERL;
        internal static string SimSpKompatibilitaetName => MyResource.Resource.SP_PARAM_LABEL_KOMPATIBILITAET;
        internal static string SimSpKompatibilitaetErl => MyResource.Resource.KI_DLG_SIM_SP_KOMPAT_ERL;
        internal static string SimSpLadenPvName => MyResource.Resource.SP_PARAM_CHK_PV;
        internal static string SimSpLadenPvErl => MyResource.Resource.KI_DLG_SIM_SP_LADEN_PV_ERL;
        internal static string SimSpLadenBhkwName => MyResource.Resource.SP_PARAM_CHK_BHKW;
        internal static string SimSpLadenBhkwErl => MyResource.Resource.KI_DLG_SIM_SP_LADEN_BHKW_ERL;
        internal static string SimSpNetzentladungName => MyResource.Resource.SP_PARAM_CHK_NETZENTLADUNG;
        internal static string SimSpNetzentladungErl => MyResource.Resource.KI_DLG_SIM_SP_NETZENTLADUNG_ERL;
        internal static string SimSpStromgefuehrtName => MyResource.Resource.SP_PARAM_CHK_BHKW_STROMGEFUEHRT;
        internal static string SimSpStromgefuehrtErl => MyResource.Resource.KI_DLG_SIM_SP_STROMGEFUEHRT_ERL;
        internal static string SimSpKapitalzinsName => MyResource.Resource.SP_PARAM_LABEL_KAPITALZINS;
        internal static string SimSpKapitalzinsErl => MyResource.Resource.KI_DLG_SIM_SP_KAPITALZINS_ERL;
        internal static string SimSpNutzungsdauerName => MyResource.Resource.SP_PARAM_LABEL_NUTZUNGSDAUER;
        internal static string SimSpNutzungsdauerErl => MyResource.Resource.KI_DLG_SIM_SP_NUTZUNGSDAUER_ERL;
        internal static string SimSpLeistungspreisName => MyResource.Resource.SP_PARAM_LABEL_LEISTUNGSPREIS;
        internal static string SimSpLeistungspreisErl => MyResource.Resource.KI_DLG_SIM_SP_LEISTUNGSPREIS_ERL;
        internal static string SimSpNetzaufschlagName => MyResource.Resource.SP_PARAM_LABEL_NETZLADEAUFSCHLAG;
        internal static string SimSpNetzaufschlagErl => MyResource.Resource.KI_DLG_SIM_SP_NETZAUFSCHLAG_ERL;
        internal static string SimSpPreisquelleName => MyResource.Resource.PREIS_PARAM_LABEL_PREISQUELLE;
        internal static string SimSpPreisreiheName => MyResource.Resource.PREIS_PARAM_LABEL_REIHE;
        internal static string SimSpPreisreiheErl => MyResource.Resource.KI_DLG_SIM_SP_PREISREIHE_ERL;
        internal static string SimSpPreisquelleErl => MyResource.Resource.KI_DLG_SIM_SP_PREISQUELLE_ERL;
        internal static string SimSpAufschlagName => MyResource.Resource.PREIS_PARAM_CHK_AUFSCHLAG;
        internal static string SimSpAufschlagErl => MyResource.Resource.KI_DLG_SIM_SP_AUFSCHLAG_ERL;

        // ============ Pufferspeicher-Verwaltung des Projekts (Welle KI-F2)
        //
        // Die ANZEIGENAMEN sind die Beschriftungen, die auf der Maske stehen
        // (PSP_LABEL_*); neu ist je Feld allein die Erlaeuterung.

        /// <summary>Die achte Maske (Welle KI-F2): die Pufferverwaltung des Projekts.</summary>
        internal static string MaskePufferSpVerwaltung => MyResource.Resource.KI_DLG_MASKE_PSPV;

        internal static string PspvBezeichnerName => MyResource.Resource.PSP_LABEL_BEZEICHNER;
        internal static string PspvBezeichnerErl => MyResource.Resource.KI_DLG_PSPV_BEZEICHNER_ERL;
        internal static string PspvVolumenName => MyResource.Resource.PSP_LABEL_GESAMTVOLUMEN;
        internal static string PspvVolumenErl => MyResource.Resource.KI_DLG_PSPV_VOLUMEN_ERL;
        internal static string PspvVerlusteName => MyResource.Resource.PSP_LABEL_BEREITSCHAFTSVERLUSTE;
        internal static string PspvVerlusteErl => MyResource.Resource.KI_DLG_PSPV_VERLUSTE_ERL;
        internal static string PspvVorlaufName => MyResource.Resource.PSP_LABEL_VORLAUF;
        internal static string PspvVorlaufErl => MyResource.Resource.KI_DLG_PSPV_VORLAUF_ERL;
        internal static string PspvRuecklaufName => MyResource.Resource.PSP_LABEL_RUECKLAUF;
        internal static string PspvRuecklaufErl => MyResource.Resource.KI_DLG_PSPV_RUECKLAUF_ERL;
        internal static string PspvSchwelleEinName => MyResource.Resource.PSP_LABEL_EINSCHALTSCHWELLE;
        internal static string PspvSchwelleEinErl => MyResource.Resource.KI_DLG_PSPV_SCHWELLE_EIN_ERL;
        internal static string PspvSchwelleAusName => MyResource.Resource.PSP_LABEL_ABSCHALTSCHWELLE;
        internal static string PspvSchwelleAusErl => MyResource.Resource.KI_DLG_PSPV_SCHWELLE_AUS_ERL;
        internal static string PspvSchwelleNachrangName => MyResource.Resource.PSP_LABEL_SCHWELLE_NACHRANGIG;
        internal static string PspvSchwelleNachrangErl => MyResource.Resource.KI_DLG_PSPV_SCHWELLE_NACHRANG_ERL;
        internal static string PspvMindestfuellstandName => MyResource.Resource.PSP_LABEL_MINDESTFUELLSTAND;
        internal static string PspvMindestfuellstandErl => MyResource.Resource.KI_DLG_PSPV_MINDESTFUELLSTAND_ERL;
        internal static string PspvSchichtenName => MyResource.Resource.PSP_LABEL_SCHICHTEN;
        internal static string PspvSchichtenErl => MyResource.Resource.KI_DLG_PSPV_SCHICHTEN_ERL;
        internal static string PspvHoeheName => MyResource.Resource.PSP_LABEL_HOEHE;
        internal static string PspvHoeheErl => MyResource.Resource.KI_DLG_PSPV_HOEHE_ERL;
        internal static string PspvLambdaName => MyResource.Resource.PSP_LABEL_LAMBDA_EFF;
        internal static string PspvLambdaErl => MyResource.Resource.KI_DLG_PSPV_LAMBDA_ERL;
        internal static string PspvNutztemperaturName => MyResource.Resource.PSP_LABEL_T_NUTZ_BW;
        internal static string PspvNutztemperaturErl => MyResource.Resource.KI_DLG_PSPV_T_NUTZ_BW_ERL;
        internal static string PspvLadeleistungName => MyResource.Resource.PSP_LABEL_LADELEISTUNG_MAX;
        internal static string PspvLadeleistungErl => MyResource.Resource.KI_DLG_PSPV_LADELEISTUNG_ERL;
        internal static string PspvEntladeleistungName => MyResource.Resource.PSP_LABEL_ENTLADELEISTUNG_MAX;
        internal static string PspvEntladeleistungErl => MyResource.Resource.KI_DLG_PSPV_ENTLADELEISTUNG_ERL;
        internal static string PspvEntladeprioName => MyResource.Resource.PSP_LABEL_ENTLADEPRIORITAET;
        internal static string PspvEntladeprioErl => MyResource.Resource.KI_DLG_PSPV_ENTLADEPRIO_ERL;

        // Die NUTZUNG ist auf der Maske EINE Mehrfachwahl; im Katalog sind es drei
        // Wahrheitswerte, denn ein Katalogfeld traegt EINEN Wert. Ihre Anzeigenamen
        // sind die Kanalnamen des Hauses - dieselben, die die Mehrfachauswahl zeigt.
        internal static string PspvNutzungHeizungName => MyResource.Resource.KANAL_HEIZUNG_ANZEIGE;
        internal static string PspvNutzungHeizungErl => MyResource.Resource.KI_DLG_PSPV_NUTZUNG_HEIZUNG_ERL;
        internal static string PspvNutzungBwName => MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE;
        internal static string PspvNutzungBwErl => MyResource.Resource.KI_DLG_PSPV_NUTZUNG_BW_ERL;
        internal static string PspvNutzungProzessName => MyResource.Resource.KANAL_PROZESS_ANZEIGE;
        internal static string PspvNutzungProzessErl => MyResource.Resource.KI_DLG_PSPV_NUTZUNG_PROZESS_ERL;

        internal static string PspvEntnahmeHeizungName => MyResource.Resource.PSP_LABEL_ENTNAHME_HEIZUNG;
        internal static string PspvEntnahmeHeizungErl => MyResource.Resource.KI_DLG_PSPV_ENTNAHME_HEIZUNG_ERL;
        internal static string PspvEntnahmeBwName => MyResource.Resource.PSP_LABEL_ENTNAHME_BW;
        internal static string PspvEntnahmeBwErl => MyResource.Resource.KI_DLG_PSPV_ENTNAHME_BW_ERL;
        internal static string PspvEntnahmeProzessName => MyResource.Resource.PSP_LABEL_ENTNAHME_PROZESS;
        internal static string PspvEntnahmeProzessErl => MyResource.Resource.KI_DLG_PSPV_ENTNAHME_PROZESS_ERL;

        // ================= Waermequelle Erdreich und Pufferspeicher (Welle KI-F2)

        /// <summary>Die neunte Maske (Welle KI-F2): die Waermequelle Erdreich.</summary>
        internal static string MaskeQuelleErdreich => MyResource.Resource.KI_DLG_MASKE_QERD;

        /// <summary>Die zehnte Maske (Welle KI-F2): die Waermequelle Pufferspeicher.</summary>
        internal static string MaskeQuellePuffer => MyResource.Resource.KI_DLG_MASKE_QPUF;

        internal static string QerdSondeName => MyResource.Resource.SIMQ_ERDREICH_RB_SONDE_WAHL;
        internal static string QerdSondeErl => MyResource.Resource.KI_DLG_QERD_SONDE_ERL;
        internal static string QerdTiefeName => MyResource.Resource.SIMQ_ERDREICH_VERLEGETIEFE;
        internal static string QerdTiefeErl => MyResource.Resource.KI_DLG_QERD_TIEFE_ERL;
        internal static string QerdFlaecheName => MyResource.Resource.SIMQ_ERDREICH_FLAECHE;
        internal static string QerdFlaecheErl => MyResource.Resource.KI_DLG_QERD_FLAECHE_ERL;
        internal static string QerdLaengeName => MyResource.Resource.SIMQ_ERDREICH_LAENGE_SONDE;
        internal static string QerdLaengeErl => MyResource.Resource.KI_DLG_QERD_LAENGE_ERL;
        internal static string QerdAnzahlName => MyResource.Resource.SIMQ_ERDREICH_ANZAHL_SONDEN;
        internal static string QerdAnzahlErl => MyResource.Resource.KI_DLG_QERD_ANZAHL_ERL;
        internal static string QerdSpreizungName => MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG;
        internal static string QerdSpreizungErl => MyResource.Resource.KI_DLG_QERD_SPREIZUNG_ERL;
        internal static string QerdKlimazoneName => MyResource.Resource.SIMQ_ERDREICH_KLIMAZONE;
        internal static string QerdKlimazoneErl => MyResource.Resource.KI_DLG_QERD_KLIMAZONE_ERL;
        internal static string QerdBodentypName => MyResource.Resource.SIMQ_ERDREICH_BODENTYP;
        internal static string QerdBodentypErl => MyResource.Resource.KI_DLG_QERD_BODENTYP_ERL;

        internal static string QpufTemperaturName => MyResource.Resource.SIMQ_PUFFER_QUELLTEMPERATUR;
        internal static string QpufTemperaturErl => MyResource.Resource.KI_DLG_QPUF_TEMPERATUR_ERL;
        internal static string QpufSpreizungName => MyResource.Resource.SIMQ_PUFFER_SPREIZUNG;
        internal static string QpufSpreizungErl => MyResource.Resource.KI_DLG_QPUF_SPREIZUNG_ERL;
        internal static string QpufRegenerationName => MyResource.Resource.SIMQ_PUFFER_REGENERATION;
        internal static string QpufRegenerationErl => MyResource.Resource.KI_DLG_QPUF_REGENERATION_ERL;
        internal static string QpufUnbegrenztName => MyResource.Resource.SIMQ_PUFFER_CB_UNBEGRENZT;
        internal static string QpufUnbegrenztErl => MyResource.Resource.KI_DLG_QPUF_UNBEGRENZT_ERL;
        internal static string QpufFestName => MyResource.Resource.SIMQ_PUFFER_TB_FEST;
        internal static string QpufFestErl => MyResource.Resource.KI_DLG_QPUF_FEST_ERL;
        internal static string QpufVorlaufName => MyResource.Resource.SIMQ_PUFFER_TB_VORLAUF;
        internal static string QpufVorlaufErl => MyResource.Resource.KI_DLG_QPUF_VORLAUF_ERL;
        internal static string QpufRuecklaufName => MyResource.Resource.SIMQ_PUFFER_TB_RUECKLAUF;
        internal static string QpufRuecklaufErl => MyResource.Resource.KI_DLG_QPUF_RUECKLAUF_ERL;
        internal static string QpufAnschlusshoeheName => MyResource.Resource.SIMQ_PUFFER_ANSCHLUSSHOEHE;
        internal static string QpufAnschlusshoeheErl => MyResource.Resource.KI_DLG_QPUF_ANSCHLUSSHOEHE_ERL;
        internal static string QpufPufferName => MyResource.Resource.SIMQ_PUFFER_KOPF;
        internal static string QpufPufferErl => MyResource.Resource.KI_DLG_QPUF_PUFFER_ERL;

        // ======================= Quellprofil und Waermesenken (Welle KI-F2)

        /// <summary>Die elfte Maske (Welle KI-F2): der Kopf eines Quellprofils.</summary>
        internal static string MaskeQuellprofil => MyResource.Resource.KI_DLG_MASKE_QPROF;

        /// <summary>Die zwoelfte Maske (Welle KI-F2): die Waermesenken einer Anlage.</summary>
        internal static string MaskeWaermesenke => MyResource.Resource.KI_DLG_MASKE_WSEN;

        internal static string QprofBezeichnungName => MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BEZEICHNER;
        internal static string QprofBezeichnungErl => MyResource.Resource.KI_DLG_QPROF_BEZEICHNUNG_ERL;
        internal static string QprofBeschreibungName => MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BESCHREIBUNG;
        internal static string QprofBeschreibungErl => MyResource.Resource.KI_DLG_QPROF_BESCHREIBUNG_ERL;
        internal static string QprofBetriebsartName => MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BETRIEBSART;
        internal static string QprofBetriebsartErl => MyResource.Resource.KI_DLG_QPROF_BETRIEBSART_ERL;
        internal static string QprofProfilName => MyResource.Resource.SIMQ_QUELLPROFIL_LBL_PROFIL;
        internal static string QprofProfilErl => MyResource.Resource.KI_DLG_QPROF_PROFIL_ERL;

        internal static string WsenZielName => MyResource.Resource.SIM_SPALTE_ZIEL;
        internal static string WsenZielErl => MyResource.Resource.KI_DLG_WSEN_ZIEL_ERL;
        internal static string WsenBedarfsartName => MyResource.Resource.SIM_SPALTE_BEDARFSART;
        internal static string WsenBedarfsartErl => MyResource.Resource.KI_DLG_WSEN_BEDARFSART_ERL;
        internal static string WsenLadeprioName => MyResource.Resource.PSP_SPALTE_LADEPRIO;
        internal static string WsenLadeprioErl => MyResource.Resource.KI_DLG_WSEN_LADEPRIO_ERL;
        internal static string WsenLadeprioPvName => MyResource.Resource.SIM_LBL_PV_UEBERSCHUSS;
        internal static string WsenLadeprioPvErl => MyResource.Resource.KI_DLG_WSEN_LADEPRIO_PV_ERL;
        internal static string WsenLadegrenzeAktivName => MyResource.Resource.SIM_CHK_LADEGRENZE;
        internal static string WsenLadegrenzeAktivErl => MyResource.Resource.KI_DLG_WSEN_LADEGRENZE_AKTIV_ERL;
        internal static string WsenLadegrenzeName => MyResource.Resource.SIM_CHK_LADEGRENZE;
        internal static string WsenLadegrenzeErl => MyResource.Resource.KI_DLG_WSEN_LADEGRENZE_ERL;
        internal static string WsenHoeheAktivName => MyResource.Resource.SIM_CHK_EINSPEISEHOEHE;
        internal static string WsenHoeheAktivErl => MyResource.Resource.KI_DLG_WSEN_HOEHE_AKTIV_ERL;
        internal static string WsenHoeheName => MyResource.Resource.SIM_CHK_EINSPEISEHOEHE;
        internal static string WsenHoeheErl => MyResource.Resource.KI_DLG_WSEN_HOEHE_ERL;
        internal static string WsenSpeicherName => MyResource.Resource.SIM_SPALTE_SPEICHER;
        internal static string WsenSpeicherErl => MyResource.Resource.KI_DLG_WSEN_SPEICHER_ERL;

        // ================= Konfiguration einer Komponente (Welle KI-F2)
        //
        // Die sieben Felder der Waermepumpen-Konfiguration stehen schon unter
        // Form_WP_Anlage und nehmen deren Texte (Wpa*) - es sind dieselben Felder,
        // nur unter einer anderen offenen Maske.

        /// <summary>Die dreizehnte Maske (Welle KI-F2): die Komponentenkonfiguration.</summary>
        internal static string MaskeKomponentenkonfiguration => MyResource.Resource.KI_DLG_MASKE_KKONF;

        internal static string KkonfBereitschaftName => MyResource.Resource.SIMERG_LBL_BEREITSCHAFT;
        internal static string KkonfBereitschaftErl => MyResource.Resource.KI_DLG_KKONF_BEREITSCHAFT_ERL;
        internal static string KkonfBetriebsartName => MyResource.Resource.SIMERG_GRP_BETRIEBSART;
        internal static string KkonfBetriebsartErl => MyResource.Resource.KI_DLG_KKONF_BETRIEBSART_ERL;
        internal static string KkonfGrenzeName => MyResource.Resource.SIMERG_LBL_UNTERE_LEISTUNGSGRENZE;
        internal static string KkonfGrenzeErl => MyResource.Resource.KI_DLG_KKONF_GRENZE_ERL;
        internal static string KkonfTraegerName => MyResource.Resource.ETW_LBL_ART;
        internal static string KkonfTraegerErl => MyResource.Resource.KI_DLG_KKONF_TRAEGER_ERL;

        // ======================= Gebaeude im Projekt und im Katalog (Welle KI-F3)

        /// <summary>Die vierzehnte Maske (Welle KI-F3): die Gebaeudemaske.</summary>
        internal static string MaskeGebaeude => MyResource.Resource.KI_DLG_MASKE_GEB;

        /// <summary>Die fuenfzehnte Maske (Welle KI-F3): die Wohn-/Nutzflaechenangabe.</summary>
        internal static string MaskeGebaeudeWohnflaeche => MyResource.Resource.KI_DLG_MASKE_GEBW;

        /// <summary>Die sechzehnte Maske (Welle KI-F3): der Gebaeude-Katalogeditor.</summary>
        internal static string MaskeGebaeudeKatalog => MyResource.Resource.KI_DLG_MASKE_GEBK;

        internal static string GebVerwendungName => MyResource.Resource.GEBK_LBL_VERWENDUNG;
        internal static string GebVerwendungErl => MyResource.Resource.KI_DLG_GEB_VERWENDUNG_ERL;
        internal static string GebFilterArtName => MyResource.Resource.GEB_LBL_GEBAEUDEART;
        internal static string GebFilterArtErl => MyResource.Resource.KI_DLG_GEB_FILTER_ART_ERL;
        internal static string GebFilterBaujahrName => MyResource.Resource.GEB_LBL_BAUJAHR;
        internal static string GebFilterBaujahrErl => MyResource.Resource.KI_DLG_GEB_FILTER_BAUJAHR_ERL;
        internal static string GebSucheName => MyResource.Resource.GEB_LBL_SUCHE;
        internal static string GebSucheErl => MyResource.Resource.KI_DLG_GEB_SUCHE_ERL;
        internal static string GebNameName => MyResource.Resource.GEB_LBL_GEBAEUDENAME;
        internal static string GebNameErl => MyResource.Resource.KI_DLG_GEB_NAME_ERL;
        internal static string GebArtName => MyResource.Resource.GEB_LBL_GEBAEUDEART;
        internal static string GebArtErl => MyResource.Resource.KI_DLG_GEB_ART_ERL;
        internal static string GebBeschreibungName => MyResource.Resource.GEB_LBL_BESCHREIBUNG;
        internal static string GebBeschreibungErl => MyResource.Resource.KI_DLG_GEB_BESCHREIBUNG_ERL;
        internal static string GebWohnflaecheName => MyResource.Resource.GEB_LBL_WOHNFLAECHE;
        internal static string GebWohnflaecheErl => MyResource.Resource.KI_DLG_GEB_WOHNFLAECHE_ERL;
        internal static string GebAngabeartName => MyResource.Resource.GEBW_LBL_ART_ANGABE;
        internal static string GebAngabeartErl => MyResource.Resource.KI_DLG_GEB_ANGABEART_ERL;
        internal static string GebVerwaltungName => MyResource.Resource.KI_DLG_GEB_VERWALTUNG_NAME;
        internal static string GebVerwaltungErl => MyResource.Resource.KI_DLG_GEB_VERWALTUNG_ERL;

        internal static string GebwBedarfsartName => MyResource.Resource.GEBW_LBL_BEDARFSART;
        internal static string GebwBedarfsartErl => MyResource.Resource.KI_DLG_GEBW_BEDARFSART_ERL;
        internal static string GebwWertName => MyResource.Resource.GEBW_LBL_VERBRAUCH;
        internal static string GebwWertErl => MyResource.Resource.KI_DLG_GEBW_WERT_ERL;
        internal static string GebwNutzungsgradName => MyResource.Resource.GEBW_LBL_NUTZUNGSGRAD;
        internal static string GebwNutzungsgradErl => MyResource.Resource.KI_DLG_GEBW_NUTZUNGSGRAD_ERL;
        internal static string GebwDezentralName => MyResource.Resource.GEBW_LBL_DEZ_WARMWASSER;
        internal static string GebwDezentralErl => MyResource.Resource.KI_DLG_GEBW_DEZENTRAL_ERL;
        internal static string GebwNameName => MyResource.Resource.GEBW_LBL_GEBAEUDENAME;
        internal static string GebwNameErl => MyResource.Resource.KI_DLG_GEBW_NAME_ERL;
        internal static string GebwArtName => MyResource.Resource.GEBW_LBL_GEBAEUDEART;
        internal static string GebwArtErl => MyResource.Resource.KI_DLG_GEBW_ART_ERL;
        internal static string GebwBeschreibungName => MyResource.Resource.GEBW_LBL_BESCHREIBUNG;
        internal static string GebwBeschreibungErl => MyResource.Resource.KI_DLG_GEBW_BESCHREIBUNG_ERL;
        internal static string GebwBaujahrName => MyResource.Resource.GEBW_LBL_BAUJAHR;
        internal static string GebwBaujahrErl => MyResource.Resource.KI_DLG_GEBW_BAUJAHR_ERL;
        internal static string GebwAngabeartName => MyResource.Resource.GEBW_LBL_ART_ANGABE;
        internal static string GebwAngabeartErl => MyResource.Resource.KI_DLG_GEBW_ANGABEART_ERL;

        internal static string GebkNameName => MyResource.Resource.GEBK_LBL_NAME;
        internal static string GebkNameErl => MyResource.Resource.KI_DLG_GEBK_NAME_ERL;
        internal static string GebkTypName => MyResource.Resource.GEBK_LBL_GEBAEUDETYP;
        internal static string GebkTypErl => MyResource.Resource.KI_DLG_GEBK_TYP_ERL;
        internal static string GebkBeschreibungName => MyResource.Resource.GEBK_LBL_BESCHREIBUNG;
        internal static string GebkBeschreibungErl => MyResource.Resource.KI_DLG_GEBK_BESCHREIBUNG_ERL;
        internal static string GebkArtName => MyResource.Resource.GEBK_LBL_GEBAEUDEART;
        internal static string GebkArtErl => MyResource.Resource.KI_DLG_GEBK_ART_ERL;
        internal static string GebkBaujahrName => MyResource.Resource.GEBK_LBL_BAUJAHR;
        internal static string GebkBaujahrErl => MyResource.Resource.KI_DLG_GEBK_BAUJAHR_ERL;
        internal static string GebkVerwendungName => MyResource.Resource.GEBK_LBL_VERWENDUNG;
        internal static string GebkVerwendungErl => MyResource.Resource.KI_DLG_GEBK_VERWENDUNG_ERL;
        internal static string GebkBauartName => MyResource.Resource.GEBK_LBL_BAUART;
        internal static string GebkBauartErl => MyResource.Resource.KI_DLG_GEBK_BAUART_ERL;
        internal static string GebkWohnflaecheName => MyResource.Resource.GEBK_LBL_WOHNFLAECHE;
        internal static string GebkWohnflaecheErl => MyResource.Resource.KI_DLG_GEBK_WOHNFLAECHE_ERL;
        internal static string GebkFlaecheNutzerName => MyResource.Resource.GEBK_LBL_FLAECHE_NUTZER;
        internal static string GebkFlaecheNutzerErl => MyResource.Resource.KI_DLG_GEBK_FLAECHE_NUTZER_ERL;
        internal static string GebkWaermegewinneName => MyResource.Resource.GEBK_LBL_WAERMEGEWINNE;
        internal static string GebkWaermegewinneErl => MyResource.Resource.KI_DLG_GEBK_WAERMEGEWINNE_ERL;
        internal static string GebkDurchlassgradName => MyResource.Resource.GEBK_LBL_FENSTERDURCHLASS;
        internal static string GebkDurchlassgradErl => MyResource.Resource.KI_DLG_GEBK_DURCHLASSGRAD_ERL;
        internal static string GebkRaumhoeheName => MyResource.Resource.GEBK_LBL_RAUMHOEHE;
        internal static string GebkRaumhoeheErl => MyResource.Resource.KI_DLG_GEBK_RAUMHOEHE_ERL;
        internal static string GebkFfNordName => MyResource.Resource.GEBK_LBL_FF_NORD;
        internal static string GebkFfNordErl => MyResource.Resource.KI_DLG_GEBK_FF_NORD_ERL;
        internal static string GebkFfSuedName => MyResource.Resource.GEBK_LBL_FF_SUED;
        internal static string GebkFfSuedErl => MyResource.Resource.KI_DLG_GEBK_FF_SUED_ERL;
        internal static string GebkFfOstWestName => MyResource.Resource.GEBK_LBL_FF_OSTWEST;
        internal static string GebkFfOstWestErl => MyResource.Resource.KI_DLG_GEBK_FF_OSTWEST_ERL;
        internal static string GebkAussenwandName => MyResource.Resource.GEBK_LBL_FL_AUSSENWAND;
        internal static string GebkAussenwandErl => MyResource.Resource.KI_DLG_GEBK_AUSSENWAND_ERL;
        internal static string GebkDachflaecheName => MyResource.Resource.GEBK_LBL_DACHFLAECHE;
        internal static string GebkDachflaecheErl => MyResource.Resource.KI_DLG_GEBK_DACHFLAECHE_ERL;
        internal static string GebkGrundflaecheName => MyResource.Resource.GEBK_LBL_GRUNDFLAECHE;
        internal static string GebkGrundflaecheErl => MyResource.Resource.KI_DLG_GEBK_GRUNDFLAECHE_ERL;
        internal static string GebkSonstFlaechenName => MyResource.Resource.GEBK_LBL_SONST_FLAECHEN;
        internal static string GebkSonstFlaechenErl => MyResource.Resource.KI_DLG_GEBK_SONST_FLAECHEN_ERL;
        internal static string GebkUAussenwandName => MyResource.Resource.GEBK_LBL_U_AUSSENWAND;
        internal static string GebkUAussenwandErl => MyResource.Resource.KI_DLG_GEBK_U_AUSSENWAND_ERL;
        internal static string GebkUFensterName => MyResource.Resource.GEBK_LBL_U_FENSTER;
        internal static string GebkUFensterErl => MyResource.Resource.KI_DLG_GEBK_U_FENSTER_ERL;
        internal static string GebkUDachName => MyResource.Resource.GEBK_LBL_U_DACHFLAECHE;
        internal static string GebkUDachErl => MyResource.Resource.KI_DLG_GEBK_U_DACH_ERL;
        internal static string GebkUGrundName => MyResource.Resource.GEBK_LBL_U_GRUNDFLAECHE;
        internal static string GebkUGrundErl => MyResource.Resource.KI_DLG_GEBK_U_GRUND_ERL;
        internal static string GebkUSonstigesName => MyResource.Resource.GEBK_LBL_U_SONSTIGES;
        internal static string GebkUSonstigesErl => MyResource.Resource.KI_DLG_GEBK_U_SONSTIGES_ERL;
        internal static string GebkSollTagName => MyResource.Resource.GEBK_LBL_SOLL_TAG;
        internal static string GebkSollTagErl => MyResource.Resource.KI_DLG_GEBK_SOLL_TAG_ERL;
        internal static string GebkNachtName => MyResource.Resource.GEBK_LBL_NACHTABSENKUNG;
        internal static string GebkNachtErl => MyResource.Resource.KI_DLG_GEBK_NACHT_ERL;
        internal static string GebkMaxTemperaturName => MyResource.Resource.GEBK_LBL_MAXTEMPERATUR;
        internal static string GebkMaxTemperaturErl => MyResource.Resource.KI_DLG_GEBK_MAX_TEMPERATUR_ERL;
        internal static string GebkWochenendeName => MyResource.Resource.GEBK_LBL_WE_ABSENKUNG;
        internal static string GebkWochenendeErl => MyResource.Resource.KI_DLG_GEBK_WOCHENENDE_ERL;
        internal static string GebkSollFerienName => MyResource.Resource.GEBK_LBL_SOLL_FERIEN;
        internal static string GebkSollFerienErl => MyResource.Resource.KI_DLG_GEBK_SOLL_FERIEN_ERL;
        internal static string GebkFensterWandName => MyResource.Resource.GEBK_LBL_FENSTER_WAND;
        internal static string GebkFensterWandErl => MyResource.Resource.KI_DLG_GEBK_FENSTER_WAND_ERL;
        internal static string GebkWandDachName => MyResource.Resource.GEBK_LBL_WAND_DACH;
        internal static string GebkWandDachErl => MyResource.Resource.KI_DLG_GEBK_WAND_DACH_ERL;
        internal static string GebkWandKellerName => MyResource.Resource.GEBK_LBL_AUSSENWAND_KELLER;
        internal static string GebkWandKellerErl => MyResource.Resource.KI_DLG_GEBK_WAND_KELLER_ERL;
        internal static string GebkAnschlussFensterName => MyResource.Resource.GEBK_FELD_ANSCHLUSS_FENSTER;
        internal static string GebkAnschlussFensterErl => MyResource.Resource.KI_DLG_GEBK_ANS_FENSTER_ERL;
        internal static string GebkAnschlussDachName => MyResource.Resource.GEBK_FELD_ANSCHLUSS_DACH;
        internal static string GebkAnschlussDachErl => MyResource.Resource.KI_DLG_GEBK_ANS_DACH_ERL;
        internal static string GebkAnschlussKellerName => MyResource.Resource.GEBK_FELD_ANSCHLUSS_KELLER;
        internal static string GebkAnschlussKellerErl => MyResource.Resource.KI_DLG_GEBK_ANS_KELLER_ERL;
        internal static string GebkLuftwechselName => MyResource.Resource.GEBK_LBL_LUFTWECHSEL;
        internal static string GebkLuftwechselErl => MyResource.Resource.KI_DLG_GEBK_LUFTWECHSEL_ERL;
        internal static string GebkRahmenanteilName => MyResource.Resource.GEBK_LBL_RAHMENANTEIL;
        internal static string GebkRahmenanteilErl => MyResource.Resource.KI_DLG_GEBK_RAHMENANTEIL_ERL;
        internal static string GebkVerschattungsfaktorName => MyResource.Resource.GEBK_LBL_VERSCHATTUNG;
        internal static string GebkVerschattungsfaktorErl => MyResource.Resource.KI_DLG_GEBK_VERSCHATTUNGSFAKTOR_ERL;
        internal static string GebkMasseanteilAussenName => MyResource.Resource.GEBK_LBL_MASSEANTEIL;
        internal static string GebkMasseanteilAussenErl => MyResource.Resource.KI_DLG_GEBK_MASSEANTEIL_AUSSEN_ERL;
        internal static string GebkInnenflaechenfaktorName => MyResource.Resource.GEBK_LBL_INNENFLAECHENFAKTOR;
        internal static string GebkInnenflaechenfaktorErl => MyResource.Resource.KI_DLG_GEBK_INNENFLAECHENFAKTOR_ERL;
        internal static string GebkHeizungStrahlungsanteilName => MyResource.Resource.GEBK_LBL_HEIZUNG_STRAHLUNG;
        internal static string GebkHeizungStrahlungsanteilErl => MyResource.Resource.KI_DLG_GEBK_HEIZUNG_STRAHLUNGSANTEIL_ERL;
        internal static string GebkHeizleistungMaxName => MyResource.Resource.GEBK_LBL_HEIZLEISTUNG_MAX;
        internal static string GebkHeizleistungMaxErl => MyResource.Resource.KI_DLG_GEBK_HEIZLEISTUNG_MAX_ERL;
        internal static string GebkAussenbauteileStrahlungName => MyResource.Resource.GEBK_LBL_AUSSEN_STRAHLUNG;
        internal static string GebkAussenbauteileStrahlungErl => MyResource.Resource.KI_DLG_GEBK_AUSSENBAUTEILE_STRAHLUNG_ERL;
        internal static string GebkLuftwechselInfiltrationName => MyResource.Resource.GEBK_LBL_INFILTRATION;
        internal static string GebkLuftwechselInfiltrationErl => MyResource.Resource.KI_DLG_GEBK_LUFTWECHSEL_INFILTRATION_ERL;
        internal static string GebkLuftwechselNutzerName => MyResource.Resource.GEBK_LBL_NUTZERLUEFTUNG;
        internal static string GebkLuftwechselNutzerErl => MyResource.Resource.KI_DLG_GEBK_LUFTWECHSEL_NUTZER_ERL;
        internal static string GebkSommerlueftungName => MyResource.Resource.GEBK_LBL_SOMMERLUEFTUNG;
        internal static string GebkSommerlueftungErl => MyResource.Resource.KI_DLG_GEBK_SOMMERLUEFTUNG_ERL;
        internal static string GebkKuehlungAktivName => MyResource.Resource.GEBK_LBL_KUEHLUNG_AKTIV;
        internal static string GebkKuehlungAktivErl => MyResource.Resource.KI_DLG_GEBK_KUEHLUNG_AKTIV_ERL;
        internal static string GebkKuehlSollwertName => MyResource.Resource.GEBK_LBL_KUEHL_SOLLWERT;
        internal static string GebkKuehlSollwertErl => MyResource.Resource.KI_DLG_GEBK_KUEHL_SOLLWERT_ERL;
        internal static string GebkKuehlleistungMaxName => MyResource.Resource.GEBK_LBL_KUEHLLEISTUNG_MAX;
        internal static string GebkKuehlleistungMaxErl => MyResource.Resource.KI_DLG_GEBK_KUEHLLEISTUNG_MAX_ERL;
        internal static string GebkFensterflaecheOstName => MyResource.Resource.GEBK_LBL_FF_OST;
        internal static string GebkFensterflaecheOstErl => MyResource.Resource.KI_DLG_GEBK_FENSTERFLAECHE_OST_ERL;
        internal static string GebkFensterflaecheWestName => MyResource.Resource.GEBK_LBL_FF_WEST;
        internal static string GebkFensterflaecheWestErl => MyResource.Resource.KI_DLG_GEBK_FENSTERFLAECHE_WEST_ERL;
        internal static string GebkKellertemperaturName => MyResource.Resource.GEBK_LBL_KELLERTEMPERATUR;
        internal static string GebkKellertemperaturErl => MyResource.Resource.KI_DLG_GEBK_KELLERTEMPERATUR_ERL;
        internal static string GebkRechenwegName => MyResource.Resource.GEBK_LBL_RECHENWEG;
        internal static string GebkRechenwegErl => MyResource.Resource.KI_DLG_GEBK_RECHENWEG_ERL;
        internal static string GebkBetriebsartName => MyResource.Resource.KI_DLG_GEBK_BETRIEBSART_NAME;
        internal static string GebkBetriebsartErl => MyResource.Resource.KI_DLG_GEBK_BETRIEBSART_ERL;

        // ================= Bedarf: Gebaeudebedarf, Typen und Profile (Welle KI-F3)

        /// <summary>Die siebzehnte Maske (Welle KI-F3): der Waermebedarf eines Gebaeudes.</summary>
        internal static string MaskeGebaeudeBedarf => MyResource.Resource.KI_DLG_MASKE_GEBB;

        /// <summary>Die achtzehnte Maske (Welle KI-F3): die Gebaeudetypen-Verwaltung.</summary>
        internal static string MaskeGebaeudetyp => MyResource.Resource.KI_DLG_MASKE_GTYP;

        /// <summary>Die neunzehnte Maske (Welle KI-F3): das Wochen-Stundenprofil eines Typs.</summary>
        internal static string MaskeTypprofil => MyResource.Resource.KI_DLG_MASKE_TPROF;

        /// <summary>Die zwanzigste Maske (Welle KI-F3): der Kopfsatz eines Bedarfskatalogs.</summary>
        internal static string MaskeTypstamm => MyResource.Resource.KI_DLG_MASKE_TSTAMM;

        internal static string GebbEinheitName => MyResource.Resource.ALLG_LBL_EINHEIT;
        internal static string GebbEinheitErl => MyResource.Resource.KI_DLG_GEBB_EINHEIT_ERL;
        internal static string GebbSortiertName => MyResource.Resource.SIM_CHK_SORTIERT;
        internal static string GebbSortiertErl => MyResource.Resource.KI_DLG_GEBB_SORTIERT_ERL;
        internal static string GebbGebaeudeName => MyResource.Resource.GEB_LBL_GEBAEUDENAME;
        internal static string GebbGebaeudeErl => MyResource.Resource.KI_DLG_GEBB_GEBAEUDE_ERL;
        internal static string GebbHeizwaermeName => MyResource.Resource.GEBB_LBL_HEIZWAERME;
        internal static string GebbHeizwaermeErl => MyResource.Resource.KI_DLG_GEBB_HEIZWAERME_ERL;
        internal static string GebbMaxLastName => MyResource.Resource.SIMERG_LBL_MAX_WAERMELAST;
        internal static string GebbMaxLastErl => MyResource.Resource.KI_DLG_GEBB_MAX_LAST_ERL;
        internal static string GebbVollbenutzungName => MyResource.Resource.GEBB_LBL_VOLLBENUTZUNG;
        internal static string GebbVollbenutzungErl => MyResource.Resource.KI_DLG_GEBB_VOLLBENUTZUNG_ERL;

        internal static string GtypTypName => MyResource.Resource.GTYP_LBL_NAME;
        internal static string GtypTypErl => MyResource.Resource.KI_DLG_GTYP_TYP_ERL;
        internal static string GtypKurveName => MyResource.Resource.GTYP_LBL_KURVE;
        internal static string GtypKurveErl => MyResource.Resource.KI_DLG_GTYP_KURVE_ERL;
        internal static string GtypBeschreibungName => MyResource.Resource.GTYP_LBL_BESCHREIBUNG;
        internal static string GtypBeschreibungErl => MyResource.Resource.KI_DLG_GTYP_BESCHREIBUNG_ERL;

        internal static string TprofTypName => MyResource.Resource.BPRO_LBL_LISTE_STROM;
        internal static string TprofTypErl => MyResource.Resource.KI_DLG_TPROF_TYP_ERL;
        internal static string TprofWochentagName => MyResource.Resource.BPRO_LBL_WOCHENTAG;
        internal static string TprofWochentagErl => MyResource.Resource.KI_DLG_TPROF_WOCHENTAG_ERL;
        internal static string TprofBeschreibungName => MyResource.Resource.BPRO_LBL_BESCHR_STROM;
        internal static string TprofBeschreibungErl => MyResource.Resource.KI_DLG_TPROF_BESCHREIBUNG_ERL;

        internal static string TstammNameName => MyResource.Resource.BTYP_LBL_NAME;
        internal static string TstammNameErl => MyResource.Resource.KI_DLG_TSTAMM_NAME_ERL;
        internal static string TstammTypName => MyResource.Resource.BTYP_LBL_TYP_STROM;
        internal static string TstammTypErl => MyResource.Resource.KI_DLG_TSTAMM_TYP_ERL;
        internal static string TstammBeschreibungName => MyResource.Resource.BTYP_LBL_BESCHREIBUNG;
        internal static string TstammBeschreibungErl => MyResource.Resource.KI_DLG_TSTAMM_BESCHREIBUNG_ERL;

        // =========== Bedarfsprofile, Katalogverwaltungen, Ergebnis (Welle KI-F3)

        /// <summary>Die einundzwanzigste Maske (Welle KI-F3): die Bedarfsprofile eines Projekts.</summary>
        internal static string MaskeBedarfsprofile => MyResource.Resource.KI_DLG_MASKE_BPF;

        /// <summary>Die Prozesswaerme-Katalogverwaltung (Welle KI-F3).</summary>
        internal static string MaskeProzesswaermeAdmin => MyResource.Resource.KI_DLG_MASKE_BADM_PROZ;

        /// <summary>Die Stromverbraucher-Katalogverwaltung (Welle KI-F3).</summary>
        internal static string MaskeStromverbraucherAdmin => MyResource.Resource.KI_DLG_MASKE_BADM_STROM;

        /// <summary>Die Brauchwasser-Katalogverwaltung (Welle KI-F3).</summary>
        internal static string MaskeBrauchwasserAdmin => MyResource.Resource.KI_DLG_MASKE_BADM_BW;

        /// <summary>Die Ergebnisanzeige eines Bedarfs (Welle KI-F3).</summary>
        internal static string MaskeBedarfErgebnis => MyResource.Resource.KI_DLG_MASKE_BERG;

        /// <summary>Welche der drei Auspraegungen offen ist (Welle KI-F3).</summary>
        internal static string BedarfsartName => MyResource.Resource.KI_DLG_BEDARFSART_NAME;
        internal static string BedarfsartErl => MyResource.Resource.KI_DLG_BEDARFSART_ERL;

        internal static string BpfEinheitName => MyResource.Resource.ALLG_LBL_EINHEIT;
        internal static string BpfEinheitErl => MyResource.Resource.KI_DLG_BPF_EINHEIT_ERL;
        internal static string BpfNeuerWertName => MyResource.Resource.BPF_LBL_NEUER_WERT;
        internal static string BpfNeuerWertErl => MyResource.Resource.KI_DLG_BPF_NEUER_WERT_ERL;
        internal static string BpfProfilName => MyResource.Resource.BTYP_LBL_NAME;
        internal static string BpfProfilErl => MyResource.Resource.KI_DLG_BPF_PROFIL_ERL;
        internal static string BpfTypName => MyResource.Resource.BPF_LBL_TYP;
        internal static string BpfTypErl => MyResource.Resource.KI_DLG_BPF_TYP_ERL;
        internal static string BpfBeschreibungName => MyResource.Resource.BTYP_LBL_BESCHREIBUNG;
        internal static string BpfBeschreibungErl => MyResource.Resource.KI_DLG_BPF_BESCHREIBUNG_ERL;
        internal static string BpfJahresverbrauchName => MyResource.Resource.BPF_LBL_JAHRESVERBRAUCH_PROZ;
        internal static string BpfJahresverbrauchErl => MyResource.Resource.KI_DLG_BPF_JAHRESVERBRAUCH_ERL;
        internal static string BpfSummeName => MyResource.Resource.BPF_LBL_SUMME_PROZ;
        internal static string BpfSummeErl => MyResource.Resource.KI_DLG_BPF_SUMME_ERL;

        internal static string BadmSatzName => MyResource.Resource.BADM_LBL_NAME;
        internal static string BadmSatzErl => MyResource.Resource.KI_DLG_BADM_SATZ_ERL;
        internal static string BadmTypName => MyResource.Resource.BADM_LBL_TYP;
        internal static string BadmTypErl => MyResource.Resource.KI_DLG_BADM_TYP_ERL;
        internal static string BadmBeschreibungName => MyResource.Resource.BADM_LBL_BESCHREIBUNG;
        internal static string BadmBeschreibungErl => MyResource.Resource.KI_DLG_BADM_BESCHREIBUNG_ERL;
        internal static string BadmJahressummeName => MyResource.Resource.KI_DLG_BADM_JAHRESSUMME_NAME;
        internal static string BadmJahressummeErl => MyResource.Resource.KI_DLG_BADM_JAHRESSUMME_ERL;

        internal static string BergEinheitName => MyResource.Resource.ALLG_LBL_EINHEIT;
        internal static string BergEinheitErl => MyResource.Resource.KI_DLG_BERG_EINHEIT_ERL;
        internal static string BergTabellensichtName => MyResource.Resource.KI_DLG_BERG_TABELLE_NAME;
        internal static string BergTabellensichtErl => MyResource.Resource.KI_DLG_BERG_TABELLE_ERL;
        internal static string BergGrafiksichtName => MyResource.Resource.KI_DLG_BERG_GRAFIK_NAME;
        internal static string BergGrafiksichtErl => MyResource.Resource.KI_DLG_BERG_GRAFIK_ERL;
        internal static string BergJahresverlaufName => MyResource.Resource.BERG_SCH_JAHRESVERLAUF;
        internal static string BergJahresverlaufErl => MyResource.Resource.KI_DLG_BERG_JAHRESVERLAUF_ERL;

        // ============ Waermebedarf, Solarganglinie und Klimadaten (Welle KI-F3)

        /// <summary>Die externen Waermebedarfsganglinien eines Projekts (Welle KI-F3).</summary>
        internal static string MaskeWaermebedarfExtern => MyResource.Resource.KI_DLG_MASKE_WBX;

        /// <summary>Die Solarganglinien eines Projekts (Welle KI-F3).</summary>
        internal static string MaskeSolarganglinie => MyResource.Resource.KI_DLG_MASKE_SGL;

        /// <summary>Die Klimadatenverwaltung (Welle KI-F3).</summary>
        internal static string MaskeKlimadaten => MyResource.Resource.KI_DLG_MASKE_KLIMA;

        internal static string WbxKanalName => MyResource.Resource.KANAL_LABEL;
        internal static string WbxKanalErl => MyResource.Resource.KI_DLG_WBX_KANAL_ERL;
        internal static string WbxGanglinieName => MyResource.Resource.BHKWV_SP_NAME;
        internal static string WbxGanglinieErl => MyResource.Resource.KI_DLG_WBX_GANGLINIE_ERL;

        internal static string SglKatalogName => MyResource.Resource.SGL_LBL_KATALOGLISTE;
        internal static string SglKatalogErl => MyResource.Resource.KI_DLG_SGL_KATALOG_ERL;
        internal static string SglProjektName => MyResource.Resource.SGL_LBL_PROJEKTLISTE;
        internal static string SglProjektErl => MyResource.Resource.KI_DLG_SGL_PROJEKT_ERL;
        internal static string SglBeschreibungName => MyResource.Resource.SGL_LBL_BESCHREIBUNG;
        internal static string SglBeschreibungErl => MyResource.Resource.KI_DLG_SGL_BESCHREIBUNG_ERL;

        internal static string KlimaQuelleName => MyResource.Resource.KLIMA_QUELLE;
        internal static string KlimaQuelleErl => MyResource.Resource.KI_DLG_KLIMA_QUELLE_ERL;
        internal static string KlimaOrtName => MyResource.Resource.KLIMA_LBL_ORT;
        internal static string KlimaOrtErl => MyResource.Resource.KI_DLG_KLIMA_ORT_ERL;
        internal static string KlimaLaengeName => MyResource.Resource.KLIMA_LBL_LONGITUDE;
        internal static string KlimaLaengeErl => MyResource.Resource.KI_DLG_KLIMA_LAENGE_ERL;
        internal static string KlimaBreiteName => MyResource.Resource.KLIMA_LBL_LATITUDE;
        internal static string KlimaBreiteErl => MyResource.Resource.KI_DLG_KLIMA_BREITE_ERL;
        internal static string KlimaBezeichnungName => MyResource.Resource.KLIMA_LBL_BEZEICHNUNG;
        internal static string KlimaBezeichnungErl => MyResource.Resource.KI_DLG_KLIMA_BEZEICHNUNG_ERL;
        internal static string KlimaJahrName => MyResource.Resource.KLIMA_TRY_JAHR;
        internal static string KlimaJahrErl => MyResource.Resource.KI_DLG_KLIMA_JAHR_ERL;
        internal static string KlimaSzenarioName => MyResource.Resource.KLIMA_TRY_SZENARIO;
        internal static string KlimaSzenarioErl => MyResource.Resource.KI_DLG_KLIMA_SZENARIO_ERL;
        internal static string KlimaTryDateiName => MyResource.Resource.KLIMA_TRY_DATEI;
        internal static string KlimaTryDateiErl => MyResource.Resource.KI_DLG_KLIMA_TRY_DATEI_ERL;
        internal static string KlimaTryPaketName => MyResource.Resource.KLIMA_TRY_PAKET;
        internal static string KlimaTryPaketErl => MyResource.Resource.KI_DLG_KLIMA_TRY_PAKET_ERL;

        /// <summary>Der Knopf „Werte uebernehmen" des zweiten Reiterblatts (Welle KI-F3).</summary>
        internal static string KnopfWerteUebernehmen => MyResource.Resource.GEBK_BTN_UEBERNEHMEN;

        /// <summary>Der Knopf „Beenden" (Welle KI-F3).</summary>
        internal static string KnopfBeenden => MyResource.Resource.GEBK_BTN_BEENDEN;

        /// <summary>Ersatztext fuer einen leeren Feldinhalt in der Ergebnisliste.</summary>
        internal static string KeinWert => MyResource.Resource.KI_DLG_KEIN_WERT;

        // ================================ Kosten und Wirtschaftlichkeit (Welle KI-F4)

        /// <summary>Die Energietraegerverwaltung (Welle KI-F4).</summary>
        internal static string MaskeEnergietraeger => MyResource.Resource.KI_DLG_MASKE_ET;

        /// <summary>„Energietraeger-Variante anlegen" (Welle KI-F4).</summary>
        internal static string MaskeEnergietraegerVariante => MyResource.Resource.KI_DLG_MASKE_ETV;

        /// <summary>Die saisonalen Leistungspreis-Saetze (Welle KI-F4).</summary>
        internal static string MaskeLeistungspreisreihe => MyResource.Resource.KI_DLG_MASKE_LPR;

        /// <summary>Das Kostenprofil eines Stromtraegers (Welle KI-F4).</summary>
        internal static string MaskeKostenprofil => MyResource.Resource.KI_DLG_MASKE_KPR;

        /// <summary>
        /// Der Anzeigename eines Bestandteil-SCHALTERS: „&lt;Bestandteil&gt; — Anteil
        /// gepflegt" (Welle KI-F4).
        /// </summary>
        /// <remarks>
        /// <b>Eine Vorlage statt dreizehn Schluessel.</b> Jeder Preisbestandteil traegt
        /// auf der Maske seinen Wert UND seinen Schalter; der Schalter heisst ueberall
        /// dasselbe, nur der Bestandteil wechselt. Dreizehn gleichlautende
        /// Ressourceneintraege waeren dreizehn Pflegestellen fuer einen Satz.
        /// </remarks>
        internal static string Aktiv(string bestandteil)
            => string.Format(System.Globalization.CultureInfo.CurrentCulture,
                             MyResource.Resource.KI_DLG_ET_AKTIV_VORLAGE, bestandteil);

        // ---- Form_Energietraeger: Listenkopf und Stammfelder
        internal static string EtSucheName => MyResource.Resource.IMP_KAT_FILTER_SUCHE;
        internal static string EtSucheErl => MyResource.Resource.KI_DLG_ET_SUCHE_ERL;
        internal static string EtTraegerName => MyResource.Resource.KI_DLG_ET_TRAEGER_NAME;
        internal static string EtTraegerErl => MyResource.Resource.KI_DLG_ET_TRAEGER_ERL;
        internal static string EtStammnameName => MyResource.Resource.KDLG_ET_STAMM_NAME;
        internal static string EtStammnameErl => MyResource.Resource.KI_DLG_ET_STAMMNAME_ERL;
        internal static string EtStammgruppeName => MyResource.Resource.KDLG_ET_STAMM_GRUPPE;
        internal static string EtStammgruppeErl => MyResource.Resource.KI_DLG_ET_STAMMGRUPPE_ERL;

        // ---- Form_Energietraeger: die Traegerkarte
        internal static string EtArbeitspreisName => MyResource.Resource.ETV_LBL_ARBEITSPREIS;
        internal static string EtArbeitspreisErl => MyResource.Resource.KI_DLG_ET_ARBEITSPREIS_ERL;
        internal static string EtGrundpreisName => MyResource.Resource.ETV_LBL_GRUNDPREIS;
        internal static string EtGrundpreisErl => MyResource.Resource.KI_DLG_ET_GRUNDPREIS_ERL;
        internal static string EtLeistungspreisName => MyResource.Resource.ETV_LBL_LEISTUNGSPREIS;
        internal static string EtLeistungspreisErl => MyResource.Resource.KI_DLG_ET_LEISTUNGSPREIS_ERL;
        internal static string EtLpModusName => MyResource.Resource.KI_DLG_ET_LPMODUS_NAME;
        internal static string EtLpModusErl => MyResource.Resource.KI_DLG_ET_LPMODUS_ERL;
        // Q11: die zweistufige Leistungspreis-Staffel des Stromträgers
        internal static string EtStaffelGrenzeName => MyResource.Resource.ETV_STAFFEL_GRENZE;
        internal static string EtStaffelGrenzeErl => MyResource.Resource.KI_DLG_ET_STAFFEL_GRENZE_ERL;
        internal static string EtStaffelPreis1Name => MyResource.Resource.ETV_STAFFEL_PREIS1;
        internal static string EtStaffelPreis1Erl => MyResource.Resource.KI_DLG_ET_STAFFEL_PREIS1_ERL;
        internal static string EtStaffelPreis2Name => MyResource.Resource.ETV_STAFFEL_PREIS2;
        internal static string EtStaffelPreis2Erl => MyResource.Resource.KI_DLG_ET_STAFFEL_PREIS2_ERL;
        internal static string EtHeizwertName => MyResource.Resource.ETV_LBL_HEIZWERT;
        internal static string EtHeizwertErl => MyResource.Resource.KI_DLG_ET_HEIZWERT_ERL;
        internal static string EtBrennwertName => MyResource.Resource.ETV_LBL_BRENNWERT;
        internal static string EtBrennwertErl => MyResource.Resource.KI_DLG_ET_BRENNWERT_ERL;
        internal static string EtPreisbasisName => MyResource.Resource.ETV_LBL_PREISBASIS;
        internal static string EtPreisbasisErl => MyResource.Resource.KI_DLG_ET_PREISBASIS_ERL;
        internal static string EtBasiseinheitName => MyResource.Resource.ETV_LBL_BASISEINHEIT;
        internal static string EtBasiseinheitErl => MyResource.Resource.KI_DLG_ET_BASISEINHEIT_ERL;
        internal static string EtEffektivName => MyResource.Resource.KI_DLG_ET_EFFEKTIV_NAME;
        internal static string EtEffektivErl => MyResource.Resource.KI_DLG_ET_EFFEKTIV_ERL;
        internal static string EtGueltigAbName => MyResource.Resource.ETV_LBL_GUELTIG_AB;
        internal static string EtGueltigAbErl => MyResource.Resource.KI_DLG_ET_GUELTIGAB_ERL;

        // ---- Form_Energietraeger: Emissionen
        internal static string EtModusName => MyResource.Resource.KI_DLG_ET_CO2E_NAME;
        internal static string EtModusErl => MyResource.Resource.KI_DLG_ET_CO2E_ERL;
        internal static string EtCo2Name => MyResource.Resource.ETV_LBL_CO2;
        internal static string EtCo2Erl => MyResource.Resource.KI_DLG_ET_CO2_ERL;
        internal static string EtSo2Name => MyResource.Resource.ETV_LBL_SO2;
        internal static string EtSo2Erl => MyResource.Resource.KI_DLG_ET_SO2_ERL;
        internal static string EtNoxName => MyResource.Resource.ETV_LBL_NOX;
        internal static string EtNoxErl => MyResource.Resource.KI_DLG_ET_NOX_ERL;

        /// <summary>Die EINE Erlaeuterung aller dreizehn Bestandteil-Schalter.</summary>
        internal static string EtAnteilAktivErl => MyResource.Resource.KI_DLG_ET_ANTEIL_AKTIV_ERL;

        // ---- Form_Energietraeger: Baustein „Strompreis Details"
        internal static string EtStromBeschaffungName => MyResource.Resource.PREIS_KOMP_BESCHAFFUNG;
        internal static string EtStromBeschaffungAktivName => Aktiv(EtStromBeschaffungName);
        internal static string EtStromBeschaffungErl => MyResource.Resource.KI_DLG_ET_STROM_BESCHAFFUNG_ERL;
        internal static string EtStromVertriebName => MyResource.Resource.PREIS_KOMP_VERTRIEB;
        internal static string EtStromVertriebAktivName => Aktiv(EtStromVertriebName);
        internal static string EtStromVertriebErl => MyResource.Resource.KI_DLG_ET_STROM_VERTRIEB_ERL;
        internal static string EtStromNetzName => MyResource.Resource.PREIS_KOMP_NETZENTGELT;
        internal static string EtStromNetzAktivName => Aktiv(EtStromNetzName);
        internal static string EtStromNetzErl => MyResource.Resource.KI_DLG_ET_STROM_NETZ_ERL;
        internal static string EtStromsteuerName => MyResource.Resource.PREIS_KOMP_STROMSTEUER;
        internal static string EtStromsteuerAktivName => Aktiv(EtStromsteuerName);
        internal static string EtStromsteuerErl => MyResource.Resource.KI_DLG_ET_STROM_STEUER_ERL;
        internal static string EtStromKonzessionName => MyResource.Resource.PREIS_KOMP_KONZESSION;
        internal static string EtStromKonzessionAktivName => Aktiv(EtStromKonzessionName);
        internal static string EtStromKonzessionErl => MyResource.Resource.KI_DLG_ET_STROM_KONZESSION_ERL;
        internal static string EtStromUmlagenName => MyResource.Resource.PREIS_KOMP_UMLAGEN;
        internal static string EtStromUmlagenAktivName => Aktiv(EtStromUmlagenName);
        internal static string EtStromUmlagenErl => MyResource.Resource.KI_DLG_ET_STROM_UMLAGEN_ERL;
        internal static string EtStromEinzelnName => MyResource.Resource.PREIS_UMLAGEN_EINZELN;
        internal static string EtStromEinzelnErl => MyResource.Resource.KI_DLG_ET_STROM_EINZELN_ERL;
        internal static string EtStromKwkgName => MyResource.Resource.PREIS_KOMP_KWKG;
        internal static string EtStromKwkgAktivName => Aktiv(EtStromKwkgName);
        internal static string EtStromKwkgErl => MyResource.Resource.KI_DLG_ET_STROM_KWKG_ERL;
        internal static string EtStromOffshoreName => MyResource.Resource.PREIS_KOMP_OFFSHORE;
        internal static string EtStromOffshoreAktivName => Aktiv(EtStromOffshoreName);
        internal static string EtStromOffshoreErl => MyResource.Resource.KI_DLG_ET_STROM_OFFSHORE_ERL;
        internal static string EtStromNevName => MyResource.Resource.PREIS_KOMP_STROMNEV19;
        internal static string EtStromNevAktivName => Aktiv(EtStromNevName);
        internal static string EtStromNevErl => MyResource.Resource.KI_DLG_ET_STROM_NEV_ERL;

        // ---- Form_Energietraeger: Baustein „Preisbestandteile"
        internal static string EtBsEnergiesteuerName => MyResource.Resource.BB_KOMP_ENERGIESTEUER;
        internal static string EtBsEnergiesteuerAktivName => Aktiv(EtBsEnergiesteuerName);
        internal static string EtBsEnergiesteuerErl => MyResource.Resource.KI_DLG_ET_BS_ENERGIESTEUER_ERL;
        internal static string EtBsCo2Name => MyResource.Resource.BB_KOMP_CO2;
        internal static string EtBsCo2AktivName => Aktiv(EtBsCo2Name);
        internal static string EtBsCo2Erl => MyResource.Resource.KI_DLG_ET_BS_CO2_ERL;
        internal static string EtBsNetzName => MyResource.Resource.BB_KOMP_NETZENTGELT;
        internal static string EtBsNetzAktivName => Aktiv(EtBsNetzName);
        internal static string EtBsNetzErl => MyResource.Resource.KI_DLG_ET_BS_NETZ_ERL;
        internal static string EtBsVertriebName => MyResource.Resource.BB_KOMP_VERTRIEB;
        internal static string EtBsVertriebAktivName => Aktiv(EtBsVertriebName);
        internal static string EtBsVertriebErl => MyResource.Resource.KI_DLG_ET_BS_VERTRIEB_ERL;

        // ---- Form_Kosten_Auswahl
        internal static string EtvTraegerName => MyResource.Resource.KI_DLG_ET_TRAEGER_NAME;
        internal static string EtvTraegerErl => MyResource.Resource.KI_DLG_ETV_TRAEGER_ERL;
        internal static string EtvNameName => MyResource.Resource.KAUSW_LBL_VARIANTE;
        internal static string EtvNameErl => MyResource.Resource.KI_DLG_ETV_NAME_ERL;

        // ---- Form_LeistungspreisReihe
        internal static string LprJahrName => MyResource.Resource.KDLG_LPR_JAHR;
        internal static string LprJahrErl => MyResource.Resource.KI_DLG_LPR_JAHR_ERL;
        internal static string LprEinheitName => MyResource.Resource.KI_DLG_EINHEIT_NAME;
        internal static string LprEinheitErl => MyResource.Resource.KI_DLG_LPR_EINHEIT_ERL;
        internal static string LprKontextName => MyResource.Resource.KI_DLG_KONTEXT_NAME;
        internal static string LprKontextErl => MyResource.Resource.KI_DLG_LPR_KONTEXT_ERL;

        // ---- Form_Kostenprofil
        internal static string KprBezeichnerName => MyResource.Resource.PREIS_PROFIL_LABEL_BEZEICHNER;
        internal static string KprBezeichnerErl => MyResource.Resource.KI_DLG_KPR_BEZEICHNER_ERL;
        internal static string KprWochentagName => MyResource.Resource.PREIS_PROFIL_LBL_WOCHENTAG;
        internal static string KprWochentagErl => MyResource.Resource.KI_DLG_KPR_WOCHENTAG_ERL;
        internal static string KprEinheitName => MyResource.Resource.KI_DLG_EINHEIT_NAME;
        internal static string KprEinheitErl => MyResource.Resource.KI_DLG_KPR_EINHEIT_ERL;

        // ---- Die VARIANTE der Kostenverwaltung (Welle KI-F4)
        internal static string KvVarianteName => MyResource.Resource.KDLG_LBL_VARIANTE;
        internal static string KvVarianteErl => MyResource.Resource.KI_DLG_KV_VARIANTE_ERL;

        // ---- Form_KostenAdmin
        internal static string MaskeKostenfaktorkatalog => MyResource.Resource.KI_DLG_MASKE_KFK;
        internal static string KfkFaktorName => MyResource.Resource.KI_DLG_KFK_FAKTOR_NAME;
        internal static string KfkFaktorErl => MyResource.Resource.KI_DLG_KFK_FAKTOR_ERL;
        internal static string KfkNeuName => MyResource.Resource.KI_DLG_KFK_NEU_NAME;
        internal static string KfkNeuErl => MyResource.Resource.KI_DLG_KFK_NEU_ERL;

        // ---- Form_Emissionskatalog
        internal static string MaskeEmissionskatalog => MyResource.Resource.KI_DLG_MASKE_EMK;
        internal static string EmkModusName => MyResource.Resource.KI_DLG_ET_CO2E_NAME;
        internal static string EmkModusErl => MyResource.Resource.KI_DLG_EMK_MODUS_ERL;
        internal static string EmkArtName => MyResource.Resource.KI_DLG_EMK_ART_NAME;
        internal static string EmkArtErl => MyResource.Resource.KI_DLG_EMK_ART_ERL;
        internal static string EmkWertName => MyResource.Resource.KI_DLG_EMK_WERT_NAME;
        internal static string EmkWertErl => MyResource.Resource.KI_DLG_EMK_WERT_ERL;
        internal static string EmkKuerzelName => MyResource.Resource.KI_DLG_EMK_KUERZEL_NAME;
        internal static string EmkKuerzelErl => MyResource.Resource.KI_DLG_EMK_KUERZEL_ERL;
        internal static string EmkNameName => MyResource.Resource.KI_DLG_EMK_NAME_NAME;
        internal static string EmkNameErl => MyResource.Resource.KI_DLG_EMK_NAME_ERL;
        internal static string EmkEinheitName => MyResource.Resource.KI_DLG_EINHEIT_NAME;
        internal static string EmkEinheitErl => MyResource.Resource.KI_DLG_EMK_EINHEIT_ERL;
        internal static string EmkGwpName => MyResource.Resource.KI_DLG_EMK_GWP_NAME;
        internal static string EmkGwpErl => MyResource.Resource.KI_DLG_EMK_GWP_ERL;
        internal static string EmkArtQuelleName => MyResource.Resource.KI_DLG_EMK_ARTQUELLE_NAME;
        internal static string EmkArtQuelleErl => MyResource.Resource.KI_DLG_EMK_ARTQUELLE_ERL;
        internal static string EmkWertQuelleName => MyResource.Resource.KI_DLG_EMK_WERTQUELLE_NAME;
        internal static string EmkWertQuelleErl => MyResource.Resource.KI_DLG_EMK_WERTQUELLE_ERL;
        internal static string EmkWertZahlName => MyResource.Resource.KI_DLG_EMK_WERTZAHL_NAME;
        internal static string EmkWertZahlErl => MyResource.Resource.KI_DLG_EMK_WERTZAHL_ERL;
        internal static string EmkWertCo2eName => MyResource.Resource.KI_DLG_EMK_WERTCO2E_NAME;
        internal static string EmkWertCo2eErl => MyResource.Resource.KI_DLG_EMK_WERTCO2E_ERL;
        internal static string EmkWertVorlageName => MyResource.Resource.KI_DLG_EMK_VORLAGE_NAME;
        internal static string EmkWertVorlageErl => MyResource.Resource.KI_DLG_EMK_VORLAGE_ERL;

        // ---- Form_Nutzungsdauer
        internal static string MaskeNutzungsdauer => MyResource.Resource.KI_DLG_MASKE_NUD;
        internal static string NudSucheName => MyResource.Resource.IMP_KAT_FILTER_SUCHE;
        internal static string NudSucheErl => MyResource.Resource.KI_DLG_NUD_SUCHE_ERL;
        internal static string NudTechnikName => MyResource.Resource.KI_DLG_NUD_TECHNIK_NAME;
        internal static string NudTechnikErl => MyResource.Resource.KI_DLG_NUD_TECHNIK_ERL;
        internal static string NudArtName => MyResource.Resource.KI_DLG_NUD_ART_NAME;
        internal static string NudArtErl => MyResource.Resource.KI_DLG_NUD_ART_ERL;
        internal static string NudNeuWertName => MyResource.Resource.KI_DLG_NUD_NEUWERT_NAME;
        internal static string NudNeuWertErl => MyResource.Resource.KI_DLG_NUD_NEUWERT_ERL;
        internal static string NudNeuAfaName => MyResource.Resource.KI_DLG_NUD_NEUAFA_NAME;
        internal static string NudNeuAfaErl => MyResource.Resource.KI_DLG_NUD_NEUAFA_ERL;
        internal static string NudWertName => MyResource.Resource.KI_DLG_NUD_WERT_NAME;
        internal static string NudWertErl => MyResource.Resource.KI_DLG_NUD_WERT_ERL;
        internal static string NudAfaName => MyResource.Resource.KI_DLG_NUD_AFA_NAME;
        internal static string NudAfaErl => MyResource.Resource.KI_DLG_NUD_AFA_ERL;
        internal static string NudQuelleName => MyResource.Resource.KI_DLG_NUD_QUELLE_NAME;
        internal static string NudQuelleErl => MyResource.Resource.KI_DLG_NUD_QUELLE_ERL;

        // ---- Form_VorlagenPosition
        internal static string MaskeVorlagenposition => MyResource.Resource.KI_DLG_MASKE_VOP;
        internal static string VopBezeichnungName => MyResource.Resource.KI_DLG_VOP_BEZ_NAME;
        internal static string VopBezeichnungErl => MyResource.Resource.KI_DLG_VOP_BEZ_ERL;
        internal static string VopKostenartName => MyResource.Resource.KI_DLG_VOP_KOSTENART_NAME;
        internal static string VopKostenartErl => MyResource.Resource.KI_DLG_VOP_KOSTENART_ERL;
        internal static string VopErloesName => MyResource.Resource.KI_DLG_VOP_ERLOES_NAME;
        internal static string VopErloesErl => MyResource.Resource.KI_DLG_VOP_ERLOES_ERL;
        internal static string VopPositionsartName => MyResource.Resource.KI_DLG_VOP_POSART_NAME;
        internal static string VopPositionsartErl => MyResource.Resource.KI_DLG_VOP_POSART_ERL;
        internal static string VopVonName => MyResource.Resource.KI_DLG_VOP_VON_NAME;
        internal static string VopVonErl => MyResource.Resource.KI_DLG_VOP_VON_ERL;
        internal static string VopBisName => MyResource.Resource.KI_DLG_VOP_BIS_NAME;
        internal static string VopBisErl => MyResource.Resource.KI_DLG_VOP_BIS_ERL;
        // ETAPPE E7c (Schritt E): die zwei Kennzeichen der Position.
        internal static string VopErsatzName => MyResource.Resource.KI_DLG_VOP_ERSATZ_NAME;
        internal static string VopErsatzErl => MyResource.Resource.KI_DLG_VOP_ERSATZ_ERL;
        internal static string VopRestwertName => MyResource.Resource.KI_DLG_VOP_RESTWERT_NAME;
        internal static string VopRestwertErl => MyResource.Resource.KI_DLG_VOP_RESTWERT_ERL;

        // ---- Form_CaseEingabe
        internal static string MaskeCaseEingabe => MyResource.Resource.KI_DLG_MASKE_CSE;
        internal static string CseModusName => MyResource.Resource.KI_DLG_CSE_MODUS_NAME;
        internal static string CseModusErl => MyResource.Resource.KI_DLG_CSE_MODUS_ERL;
        internal static string CseBestName => MyResource.Resource.KI_DLG_CSE_BEST_NAME;
        internal static string CseBestErl => MyResource.Resource.KI_DLG_CSE_BEST_ERL;
        internal static string CseWorstName => MyResource.Resource.KI_DLG_CSE_WORST_NAME;
        internal static string CseWorstErl => MyResource.Resource.KI_DLG_CSE_WORST_ERL;
        internal static string CseBestDauerName => MyResource.Resource.KI_DLG_CSE_BESTDAUER_NAME;
        internal static string CseBestDauerErl => MyResource.Resource.KI_DLG_CSE_BESTDAUER_ERL;
        internal static string CseWorstDauerName => MyResource.Resource.KI_DLG_CSE_WORSTDAUER_NAME;
        internal static string CseWorstDauerErl => MyResource.Resource.KI_DLG_CSE_WORSTDAUER_ERL;
        internal static string CseJahrName => MyResource.Resource.KI_DLG_CSE_JAHR_NAME;
        internal static string CseJahrErl => MyResource.Resource.KI_DLG_CSE_JAHR_ERL;
        internal static string CseZuschussName => MyResource.Resource.KI_DLG_CSE_ZUSCHUSS_NAME;
        internal static string CseZuschussErl => MyResource.Resource.KI_DLG_CSE_ZUSCHUSS_ERL;

        // ============================================ Wirtschaftlichkeit (Welle KI-F4)

        /// <summary>
        /// Der Anzeigename eines Feldes, das es je BLOCK einmal gibt: „&lt;Block&gt;:
        /// &lt;Feld&gt;" (Welle KI-F4).
        /// </summary>
        /// <remarks>
        /// <b>Eine Vorlage statt doppelter Schluessel.</b> Die Tarifstruktur fuehrt
        /// „Arbeitspreis" und „Grundpreis" fuer Bezug, Reststrom UND Einspeisung;
        /// die Szenariotabelle fuehrt jede Groesse als Best- und als
        /// Worst-Fall. Der Block wechselt, das Feld bleibt - zwei Eintraege je Paar
        /// waeren zwei Pflegestellen fuer denselben Begriff.
        /// </remarks>
        internal static string Block(string block, string feld)
            => string.Format(System.Globalization.CultureInfo.CurrentCulture,
                             MyResource.Resource.KI_DLG_BLOCK_VORLAGE, block, feld);

        // ---- Form_WirtschaftlichkeitParameter
        internal static string MaskeWirtParameter => MyResource.Resource.KI_DLG_MASKE_WPA;
        internal static string WpaZinsName => MyResource.Resource.WPAR_ZINS;
        internal static string WpaZinsErl => MyResource.Resource.KI_DLG_WPA_ZINS_ERL;
        internal static string WpaJahreName => MyResource.Resource.WPAR_JAHRE;
        internal static string WpaJahreErl => MyResource.Resource.KI_DLG_WPA_JAHRE_ERL;
        internal static string WpaPreisEName => MyResource.Resource.WPAR_PREIS_E;
        internal static string WpaPreisEErl => MyResource.Resource.KI_DLG_WPA_PREIS_E_ERL;
        internal static string WpaPreisBName => MyResource.Resource.WPAR_PREIS_B;
        internal static string WpaPreisBErl => MyResource.Resource.KI_DLG_WPA_PREIS_B_ERL;
        internal static string WpaPreisIName => MyResource.Resource.WPAR_PREIS_I;
        internal static string WpaPreisIErl => MyResource.Resource.KI_DLG_WPA_PREIS_I_ERL;
        internal static string WpaEinspName => MyResource.Resource.WPAR_EINSP_PV;
        internal static string WpaEinspErl => MyResource.Resource.KI_DLG_WPA_EINSP_ERL;
        internal static string WpaCo2Name => MyResource.Resource.WIRT_DLG_CO2;
        internal static string WpaCo2Erl => MyResource.Resource.KI_DLG_WPA_CO2_ERL;
        internal static string WpaParkName => MyResource.Resource.WPAR_PARK;
        internal static string WpaParkErl => MyResource.Resource.KI_DLG_WPA_PARK_ERL;
        internal static string WpaBilanzjahrName => MyResource.Resource.BILANZ_DLG_JAHR;
        internal static string WpaBilanzjahrErl => MyResource.Resource.KI_DLG_WPA_BILANZJAHR_ERL;
        internal static string WpaMethodeName => MyResource.Resource.BILANZ_DLG_METHODE;
        internal static string WpaMethodeErl => MyResource.Resource.KI_DLG_WPA_METHODE_ERL;
        internal static string WpaBiomasseName => MyResource.Resource.BILANZ_DLG_BIOMASSE;
        internal static string WpaBiomasseErl => MyResource.Resource.KI_DLG_WPA_BIOMASSE_ERL;
        internal static string WpaNachweisName => MyResource.Resource.BILANZ_DLG_NACHWEIS;
        internal static string WpaNachweisErl => MyResource.Resource.KI_DLG_WPA_NACHWEIS_ERL;

        private static string SzBest => MyResource.Resource.WPAR_SZ_SPALTE_BEST;
        private static string SzWorst => MyResource.Resource.WPAR_SZ_SPALTE_WORST;

        internal static string WpaSzBestZins => Block(SzBest, MyResource.Resource.WPAR_SZ_ZINS);
        internal static string WpaSzWorstZins => Block(SzWorst, MyResource.Resource.WPAR_SZ_ZINS);
        internal static string WpaSzZinsErl => MyResource.Resource.KI_DLG_WPA_SZ_ZINS_ERL;
        internal static string WpaSzBestPreisE => Block(SzBest, MyResource.Resource.WPAR_SZ_PREIS_E);
        internal static string WpaSzWorstPreisE => Block(SzWorst, MyResource.Resource.WPAR_SZ_PREIS_E);
        internal static string WpaSzPreisEErl => MyResource.Resource.KI_DLG_WPA_SZ_PREIS_E_ERL;
        internal static string WpaSzBestPreisB => Block(SzBest, MyResource.Resource.WPAR_SZ_PREIS_B);
        internal static string WpaSzWorstPreisB => Block(SzWorst, MyResource.Resource.WPAR_SZ_PREIS_B);
        internal static string WpaSzPreisBErl => MyResource.Resource.KI_DLG_WPA_SZ_PREIS_B_ERL;
        internal static string WpaSzBestPreisI => Block(SzBest, MyResource.Resource.WPAR_SZ_PREIS_I);
        internal static string WpaSzWorstPreisI => Block(SzWorst, MyResource.Resource.WPAR_SZ_PREIS_I);
        internal static string WpaSzPreisIErl => MyResource.Resource.KI_DLG_WPA_SZ_PREIS_I_ERL;
        internal static string WpaSzBestInvest => Block(SzBest, MyResource.Resource.WPAR_SZ_INVEST);
        internal static string WpaSzWorstInvest => Block(SzWorst, MyResource.Resource.WPAR_SZ_INVEST);
        internal static string WpaSzInvestErl => MyResource.Resource.KI_DLG_WPA_SZ_INVEST_ERL;
        internal static string WpaSzBestErtrag => Block(SzBest, MyResource.Resource.WPAR_SZ_ERTRAG);
        internal static string WpaSzWorstErtrag => Block(SzWorst, MyResource.Resource.WPAR_SZ_ERTRAG);
        internal static string WpaSzErtragErl => MyResource.Resource.KI_DLG_WPA_SZ_ERTRAG_ERL;
        internal static string WpaSzBestDauer => Block(SzBest, MyResource.Resource.WPAR_SZ_DAUER);
        internal static string WpaSzWorstDauer => Block(SzWorst, MyResource.Resource.WPAR_SZ_DAUER);
        internal static string WpaSzDauerErl => MyResource.Resource.KI_DLG_WPA_SZ_DAUER_ERL;

        // ---- Form_BhkwWirtschaftlichkeit
        internal static string MaskeBhkwWirtschaft => MyResource.Resource.KI_DLG_MASKE_BHW;
        internal static string BhwModulName => MyResource.Resource.BHW_SP_ANLAGE;
        internal static string BhwModulErl => MyResource.Resource.KI_DLG_BHW_MODUL_ERL;
        internal static string BhwStichtagAName => MyResource.Resource.BHW_A_STICHTAG;
        internal static string BhwStichtagAErl => MyResource.Resource.KI_DLG_BHW_STICHTAG_A_ERL;
        internal static string BhwIbnAName => MyResource.Resource.BHW_A_IBN;
        internal static string BhwIbnAErl => MyResource.Resource.KI_DLG_BHW_IBN_A_ERL;
        internal static string BhwArtName => MyResource.Resource.BHW_A_ANLAGENART;
        internal static string BhwArtErl => MyResource.Resource.KI_DLG_BHW_ART_ERL;
        internal static string BhwFallName => MyResource.Resource.BHW_A_EIGENFALL;
        internal static string BhwFallErl => MyResource.Resource.KI_DLG_BHW_FALL_ERL;
        internal static string BhwSatzEinspName => MyResource.Resource.BHW_A_SATZ_EINSP;
        internal static string BhwSatzEinspErl => MyResource.Resource.KI_DLG_BHW_SATZ_EINSP_ERL;
        internal static string BhwSatzEigenName => MyResource.Resource.BHW_A_SATZ_EIGEN;
        internal static string BhwSatzEigenErl => MyResource.Resource.KI_DLG_BHW_SATZ_EIGEN_ERL;
        internal static string BhwKontingentName => MyResource.Resource.BHW_A_KONTINGENT;
        internal static string BhwKontingentErl => MyResource.Resource.KI_DLG_BHW_KONTINGENT_ERL;
        internal static string BhwDeckelName => MyResource.Resource.BHW_A_DECKEL;
        internal static string BhwDeckelErl => MyResource.Resource.KI_DLG_BHW_DECKEL_ERL;
        internal static string BhwKostenanteilName => MyResource.Resource.BHW_A_KOSTENANTEIL;
        internal static string BhwKostenanteilErl => MyResource.Resource.KI_DLG_BHW_KOSTENANTEIL_ERL;
        internal static string BhwEsAName => MyResource.Resource.BHW_A_ENERGIESTEUER;
        internal static string BhwEsAErl => MyResource.Resource.KI_DLG_BHW_ES_A_ERL;
        internal static string BhwAufAName => MyResource.Resource.BHW_A_AUFTEILUNG;
        internal static string BhwAufAErl => MyResource.Resource.KI_DLG_BHW_AUF_A_ERL;
        internal static string BhwHilfsName => MyResource.Resource.BHW_A_HILFSANTEIL;
        internal static string BhwHilfsErl => MyResource.Resource.KI_DLG_BHW_HILFS_ERL;
        // ETAPPE E7c (E7c1-Q7): Kennzeichen und Stromkennzahl des zweiten Falls.
        internal static string BhwAbwaermeName => MyResource.Resource.BHW_FLD_ABWAERMEABFUHR;
        internal static string BhwAbwaermeErl => MyResource.Resource.KI_DLG_BHW_ABWAERME_ERL;
        internal static string BhwSigmaName => MyResource.Resource.BHW_FLD_STROMKENNZAHL;
        internal static string BhwSigmaErl => MyResource.Resource.KI_DLG_BHW_SIGMA_ERL;
        internal static string BhwEinspKwkName => MyResource.Resource.WPAR_EINSP_KWK;
        internal static string BhwEinspKwkErl => MyResource.Resource.KI_DLG_BHW_EINSP_KWK_ERL;
        internal static string BhwAbschlagName => MyResource.Resource.BHW_P_ABSCHLAG;
        internal static string BhwAbschlagErl => MyResource.Resource.KI_DLG_BHW_ABSCHLAG_ERL;
        internal static string BhwPauschalName => MyResource.Resource.BHW_P_PAUSCHAL;
        internal static string BhwPauschalErl => MyResource.Resource.KI_DLG_BHW_PAUSCHAL_ERL;
        internal static string BhwStichtagPName => MyResource.Resource.BHW_P_STICHTAG;
        internal static string BhwStichtagPErl => MyResource.Resource.KI_DLG_BHW_STICHTAG_P_ERL;
        internal static string BhwIbnPName => MyResource.Resource.BHW_P_IBN;
        internal static string BhwIbnPErl => MyResource.Resource.KI_DLG_BHW_IBN_P_ERL;
        internal static string BhwEsPName => MyResource.Resource.BHW_E_WAHL;
        internal static string BhwEsPErl => MyResource.Resource.KI_DLG_BHW_ES_P_ERL;
        internal static string BhwAufPName => MyResource.Resource.BHW_E_AUFTEILUNG;
        internal static string BhwAufPErl => MyResource.Resource.KI_DLG_BHW_AUF_P_ERL;
        internal static string BhwNutzungsgradName => MyResource.Resource.BHW_E_NUTZUNGSGRAD;
        internal static string BhwNutzungsgradErl => MyResource.Resource.KI_DLG_BHW_NUTZUNGSGRAD_ERL;
        internal static string BhwUaName => MyResource.Resource.BHW_S_UNTERNEHMENSART;
        internal static string BhwUaErl => MyResource.Resource.KI_DLG_BHW_UA_ERL;
        internal static string BhwRaeumlichName => MyResource.Resource.BHW_S_RAEUMLICH;
        internal static string BhwRaeumlichErl => MyResource.Resource.KI_DLG_BHW_RAEUMLICH_ERL;
        internal static string BhwHocheffizienzName => MyResource.Resource.BHW_S_HOCHEFFIZIENZ;
        internal static string BhwHocheffizienzErl => MyResource.Resource.KI_DLG_BHW_HOCHEFFIZIENZ_ERL;
        internal static string BhwModusName => MyResource.Resource.BHW_S_MODUS;
        internal static string BhwModusErl => MyResource.Resource.KI_DLG_BHW_MODUS_ERL;

        // ---- Form_Tarifstruktur
        internal static string MaskeTarifstruktur => MyResource.Resource.KI_DLG_MASKE_TAR;

        private static string TarBezug => MyResource.Resource.KI_DLG_TAR_BEZUG;
        private static string TarEinsp => MyResource.Resource.KI_DLG_TAR_EINSPEISUNG;
        private static string TarRest => MyResource.Resource.KI_DLG_TAR_REST;

        internal static string TarAktivName => MyResource.Resource.TARIF_AKTIV;
        internal static string TarAktivErl => MyResource.Resource.KI_DLG_TAR_AKTIV_ERL;
        internal static string TarGueltigAbName => MyResource.Resource.TARIF_GUELTIG_AB;
        internal static string TarGueltigAbErl => MyResource.Resource.KI_DLG_TAR_GUELTIGAB_ERL;
        internal static string TarWinterVonName => MyResource.Resource.TARIF_WINTER_VON;
        internal static string TarWinterVonErl => MyResource.Resource.KI_DLG_TAR_WINTERVON_ERL;
        internal static string TarWinterBisName => MyResource.Resource.TARIF_WINTER_BIS;
        internal static string TarWinterBisErl => MyResource.Resource.KI_DLG_TAR_WINTERBIS_ERL;

        internal static string TarBezugArbeitName => Block(TarBezug, MyResource.Resource.TARIF_ARBEITSPREIS);
        internal static string TarBezugArbeitErl => MyResource.Resource.KI_DLG_TAR_BEZUG_ARBEIT_ERL;
        internal static string TarBezugGrundName => Block(TarBezug, MyResource.Resource.TARIF_GRUNDPREIS);
        internal static string TarBezugGrundErl => MyResource.Resource.KI_DLG_TAR_BEZUG_GRUND_ERL;
        internal static string TarBezugModellName => Block(TarBezug, MyResource.Resource.TARIF_LEISTUNGSMODELL);
        internal static string TarBezugMonatName => Block(TarBezug, MyResource.Resource.TARIF_MONATSPREIS);
        internal static string TarRestArbeitName => Block(TarRest, MyResource.Resource.TARIF_ARBEITSPREIS);
        internal static string TarRestArbeitErl => MyResource.Resource.KI_DLG_TAR_REST_ARBEIT_ERL;
        internal static string TarRestGrundName => Block(TarRest, MyResource.Resource.TARIF_GRUNDPREIS);
        internal static string TarRestGrundErl => MyResource.Resource.KI_DLG_TAR_REST_GRUND_ERL;
        internal static string TarRestModellName => Block(TarRest, MyResource.Resource.TARIF_LEISTUNGSMODELL);
        internal static string TarRestMonatName => Block(TarRest, MyResource.Resource.TARIF_MONATSPREIS);
        internal static string TarLeistungsmodellErl => MyResource.Resource.KI_DLG_TAR_LM_ERL;
        internal static string TarMonatspreisErl => MyResource.Resource.KI_DLG_TAR_MONAT_ERL;
        internal static string TarEinspArbeitName => Block(TarEinsp, MyResource.Resource.TARIF_EINSPEISEPREIS);
        internal static string TarEinspArbeitErl => MyResource.Resource.KI_DLG_TAR_EINSP_ARBEIT_ERL;
        internal static string TarEinspGrundName => Block(TarEinsp, MyResource.Resource.TARIF_GRUNDPREIS);
        internal static string TarEinspGrundErl => MyResource.Resource.KI_DLG_TAR_EINSP_GRUND_ERL;

        // ---- Form_PhotovoltaikVerguetung
        internal static string MaskePvVerguetung => MyResource.Resource.KI_DLG_MASKE_PVV;
        internal static string PvvAktivName => MyResource.Resource.PVW_AKTIV;
        internal static string PvvAktivErl => MyResource.Resource.KI_DLG_PVV_AKTIV_ERL;
        internal static string PvvLeistungName => MyResource.Resource.PVW_KWP_OVR;
        internal static string PvvLeistungErl => MyResource.Resource.KI_DLG_PVV_LEISTUNG_ERL;
        internal static string PvvIbnName => MyResource.Resource.PVW_IBN;
        internal static string PvvIbnErl => MyResource.Resource.KI_DLG_PVV_IBN_ERL;
        internal static string PvvDegradationName => MyResource.Resource.PVM_DEGRADATION;
        internal static string PvvDegradationErl => MyResource.Resource.KI_DLG_PVV_DEGRADATION_ERL;
        internal static string PvvEinspeiseartName => MyResource.Resource.PVV_G_EINSPEISEART;
        internal static string PvvEinspeiseartErl => MyResource.Resource.KI_DLG_PVV_EINSPEISEART_ERL;
        internal static string PvvAwName => MyResource.Resource.PVW_AW_OVR;
        internal static string PvvAwErl => MyResource.Resource.KI_DLG_PVV_AW_ERL;
        internal static string PvvVermarktungName => MyResource.Resource.PVW_G_VERMARKTUNG;
        internal static string PvvVermarktungErl => MyResource.Resource.KI_DLG_PVV_VERMARKTUNG_ERL;
        internal static string PvvDvName => MyResource.Resource.PVW_DV;
        internal static string PvvDvErl => MyResource.Resource.KI_DLG_PVV_DV_ERL;
        internal static string PvvPpaName => MyResource.Resource.PVW_PPA_PREIS;
        internal static string PvvPpaErl => MyResource.Resource.KI_DLG_PVV_PPA_ERL;
        internal static string PvvPpaAufschlagName => MyResource.Resource.PVW_PPA_AUFSCHLAG;
        internal static string PvvPpaAufschlagErl => MyResource.Resource.KI_DLG_PVV_PPA_AUFSCHLAG_ERL;
        internal static string PvvPar51Name => MyResource.Resource.KI_DLG_PVV_PAR51_NAME;
        internal static string PvvPar51Erl => MyResource.Resource.KI_DLG_PVV_PAR51_ERL;
        internal static string PvvImsysName => MyResource.Resource.PVW_IMSYS;
        internal static string PvvImsysErl => MyResource.Resource.KI_DLG_PVV_IMSYS_ERL;
        internal static string PvvAusfallName => MyResource.Resource.PVW_AUSFALL;
        internal static string PvvAusfallErl => MyResource.Resource.KI_DLG_PVV_AUSFALL_ERL;
        internal static string PvvPar51aName => MyResource.Resource.PVW_51A;
        internal static string PvvPar51aErl => MyResource.Resource.KI_DLG_PVV_PAR51A_ERL;
        internal static string PvvBezugName => MyResource.Resource.PVW_BEZUG_REIHE;
        internal static string PvvBezugErl => MyResource.Resource.KI_DLG_PVV_BEZUG_ERL;
        internal static string PvvKappungName => MyResource.Resource.PVW_G_KAPPUNG;
        internal static string PvvKappungErl => MyResource.Resource.KI_DLG_PVV_KAPPUNG_ERL;

        // ---- Form_Gesetzesparameter und Form_GesetzparameterZeile
        internal static string MaskeGesetzeskatalog => MyResource.Resource.KI_DLG_MASKE_GSK;
        internal static string MaskeGesetzeszeile => MyResource.Resource.KI_DLG_MASKE_GSZ;
        internal static string GskKlasseName => MyResource.Resource.GESETZ_LBL_KLASSE;
        internal static string GskKlasseErl => MyResource.Resource.KI_DLG_GSK_KLASSE_ERL;
        internal static string GskZeileName => MyResource.Resource.KI_DLG_GSK_ZEILE_NAME;
        internal static string GskZeileErl => MyResource.Resource.KI_DLG_GSK_ZEILE_ERL;
        internal static string GszSchluesselName => MyResource.Resource.GESETZ_SP_SCHLUESSEL;
        internal static string GszSchluesselErl => MyResource.Resource.KI_DLG_GSZ_SCHLUESSEL_ERL;
        internal static string GszKlasseErl => MyResource.Resource.KI_DLG_GSZ_KLASSE_ERL;
        internal static string GszJahrName => MyResource.Resource.GESETZ_SP_JAHRVON;
        internal static string GszJahrErl => MyResource.Resource.KI_DLG_GSZ_JAHR_ERL;
        internal static string GszWertName => MyResource.Resource.GESETZ_SP_WERT;
        internal static string GszWertErl => MyResource.Resource.KI_DLG_GSZ_WERT_ERL;
        internal static string GszEinheitErl => MyResource.Resource.KI_DLG_GSZ_EINHEIT_ERL;
        internal static string GszStatusName => MyResource.Resource.GESETZ_SP_STATUS;
        internal static string GszStatusErl => MyResource.Resource.KI_DLG_GSZ_STATUS_ERL;
        internal static string GszQuelleErl => MyResource.Resource.KI_DLG_GSZ_QUELLE_ERL;

        // ---- Die zwei Reiterblaetter der Ansicht „Berichte und Kosten"
        internal static string MaskeKostenseite => MyResource.Resource.KI_DLG_MASKE_KSE;
        internal static string KseAnlageName => MyResource.Resource.KI_DLG_KSE_ANLAGE_NAME;
        internal static string KseAnlageErl => MyResource.Resource.KI_DLG_KSE_ANLAGE_ERL;
        internal static string KseProjektName => MyResource.Resource.KI_DLG_KSE_PROJEKT_NAME;
        internal static string KseProjektErl => MyResource.Resource.KI_DLG_KSE_PROJEKT_ERL;
        internal static string KseStatusName => MyResource.Resource.KI_DLG_KSE_STATUS_NAME;
        internal static string KseStatusErl => MyResource.Resource.KI_DLG_KSE_STATUS_ERL;

        internal static string MaskeWirtschaftsseite => MyResource.Resource.KI_DLG_MASKE_WSE;
        internal static string WseSzenarioName => MyResource.Resource.WIRT_LBL_SZENARIO;
        internal static string WseSzenarioErl => MyResource.Resource.KI_DLG_WSE_SZENARIO_ERL;
        internal static string WseSichtName => MyResource.Resource.WIRT_SICHT_ALLE;
        internal static string WseSichtErl => MyResource.Resource.KI_DLG_WSE_SICHT_ERL;
        internal static string WseReferenzName => MyResource.Resource.WIRT_SICHT_REF_SPALTE;
        internal static string WseReferenzErl => MyResource.Resource.KI_DLG_WSE_REFERENZ_ERL;
        internal static string WseAName => MyResource.Resource.WIRT_SICHT_A;
        internal static string WseAErl => MyResource.Resource.KI_DLG_WSE_A_ERL;
        internal static string WseBName => MyResource.Resource.WIRT_SICHT_B;
        internal static string WseBErl => MyResource.Resource.KI_DLG_WSE_B_ERL;
        internal static string WseWirkungName => MyResource.Resource.WPAR_NICHT_MONETAER;
        internal static string WseWirkungErl => MyResource.Resource.KI_DLG_WSE_WIRKUNG_ERL;
        internal static string WseZrStandName => MyResource.Resource.KI_DLG_WSE_ZR_STAND_NAME;
        internal static string WseZrStandErl => MyResource.Resource.KI_DLG_WSE_ZR_STAND_ERL;
        internal static string WseZrSzenarioName => MyResource.Resource.KI_DLG_WSE_ZR_SZENARIO_NAME;
        internal static string WseZrSzenarioErl => MyResource.Resource.KI_DLG_WSE_ZR_SZENARIO_ERL;

        // ============================================== Einheiten der Welle KI-F5

        /// <summary>Ein dimensionsloser Faktor - so steht das Zeichen auf der Maske.</summary>
        internal const string EINHEIT_FAKTOR = "-";

        /// <summary>Einheit der leistungsbezogenen Kosten.</summary>
        internal const string EINHEIT_EURO_KW = "€/kW";

        /// <summary>Einheit einer elektrischen Spannung.</summary>
        internal const string EINHEIT_VOLT = "V";

        /// <summary>Einheit einer elektrischen Stromstaerke.</summary>
        internal const string EINHEIT_AMPERE = "A";

        /// <summary>Einheit eines Temperaturkoeffizienten der Leistung.</summary>
        internal const string EINHEIT_PROZENT_K = "%/K";

        /// <summary>Einheit einer elektrischen Scheinleistung.</summary>
        internal const string EINHEIT_KVA = "kVA";

        /// <summary>Einheit des quadratischen Waermeverlustbeiwerts k2.</summary>
        internal const string EINHEIT_W_M2K2 = "W/(m²·K²)";

        /// <summary>
        /// Einheit der Verschleisskosten eines Stromspeichers - sie traegt ein WORT
        /// und kommt deshalb aus der Ressource.
        /// </summary>
        internal static string EinheitZykluskosten => MyResource.Resource.SP_EINHEIT_ZYKLUSKOSTEN;

        // ============================================== Masken der Welle KI-F5

        internal static string MaskeBhkwKatalog => MyResource.Resource.BHKWK_TITEL;
        internal static string MaskeSolarkollektorKatalog => MyResource.Resource.SKK_TITEL;
        internal static string MaskePvModulkatalog => MyResource.Resource.MODK_TITEL_PV;
        internal static string MaskeStromspeicherkatalog => MyResource.Resource.MODK_TITEL_STROMSPEICHER;
        internal static string MaskeWechselrichterkatalog => MyResource.Resource.WRK_TITEL_VERWALTUNG;

        // ============================================== Knoepfe des Modulkatalogs

        internal static string KnopfNeu => MyResource.Resource.KBROW_BTN_NEU;

        // ============================================== Welle KI-F5: ERZEUGERKATALOGE
        //
        // Die Anzeigenamen kommen aus den BESCHRIFTUNGEN der Masken (BHKWK_LBL_*,
        // SKK_LBL_*, MODK_LBL_*, SP_LABEL_*, WRK_LBL_*) - genau der Text, den der
        // Anwender dort liest. Eine zweite Schreibweise daneben koennte auseinander
        // laufen; nur die fuenf FORMELZEICHEN des Kollektors (h0, k1, k2, Kdir,
        // Kdiff) tragen einen eigenen Schluessel, weil die Maske sie als Vorgabewert
        // ihrer Parameter fuehrt und nicht aus der Ressource holt.

        // ---- Der BHKW-Katalogeditor (Form_DBBHKW)
        internal static string BhkkNameName => MyResource.Resource.BHKWK_LBL_NAME;
        internal static string BhkkNameErl => MyResource.Resource.KI_DLG_BHKK_NAME_ERL;
        internal static string BhkkFirmaName => MyResource.Resource.BHKWK_LBL_HERSTELLER;
        internal static string BhkkFirmaErl => MyResource.Resource.KI_DLG_BHKK_FIRMA_ERL;
        internal static string BhkkMotortypName => MyResource.Resource.BHKWK_LBL_MOTORTYP;
        internal static string BhkkMotortypErl => MyResource.Resource.KI_DLG_BHKK_MOTORTYP_ERL;
        internal static string BhkkBeschreibungName => MyResource.Resource.BHKWK_LBL_BESCHREIBUNG;
        internal static string BhkkBeschreibungErl => MyResource.Resource.KI_DLG_BHKK_BESCHREIBUNG_ERL;
        internal static string BhkkPthermName => MyResource.Resource.BHKWK_LBL_PTHERM;
        internal static string BhkkPthermErl => MyResource.Resource.KI_DLG_BHKK_PTHERM_ERL;
        internal static string BhkkPelName => MyResource.Resource.BHKWK_LBL_PEL;
        internal static string BhkkPelErl => MyResource.Resource.KI_DLG_BHKK_PEL_ERL;
        internal static string BhkkWgElName => MyResource.Resource.BHKWK_LBL_WIRKUNGSGRAD_EL;
        internal static string BhkkWgElErl => MyResource.Resource.KI_DLG_BHKK_WG_EL_ERL;
        internal static string BhkkWgThName => MyResource.Resource.BHKWK_LBL_WIRKUNGSGRAD_TH;
        internal static string BhkkWgThErl => MyResource.Resource.KI_DLG_BHKK_WG_TH_ERL;
        internal static string BhkkWgGesamtName => MyResource.Resource.BHKWK_LBL_WIRKUNGSGRAD;
        internal static string BhkkWgGesamtErl => MyResource.Resource.KI_DLG_BHKK_WG_GESAMT_ERL;
        internal static string BhkkGrenzleistungName => MyResource.Resource.BHKWK_LBL_GRENZLEISTUNG;
        internal static string BhkkGrenzleistungErl => MyResource.Resource.KI_DLG_BHKK_GRENZLEISTUNG_ERL;
        internal static string BhkkTraegerName => MyResource.Resource.BHKWK_LBL_ENERGIETRAEGER;
        internal static string BhkkTraegerErl => MyResource.Resource.KI_DLG_BHKK_TRAEGER_ERL;
        internal static string BhkkVorlaufName => MyResource.Resource.BHKWK_LBL_VORLAUF;
        internal static string BhkkVorlaufErl => MyResource.Resource.KI_DLG_BHKK_VORLAUF_ERL;
        internal static string BhkkRuecklaufName => MyResource.Resource.BHKWK_LBL_RUECKLAUF;
        internal static string BhkkRuecklaufErl => MyResource.Resource.KI_DLG_BHKK_RUECKLAUF_ERL;

        // ---- Der Solarkollektor-Katalogeditor (Form_SolarDB)
        internal static string SkkNameName => MyResource.Resource.SKK_LBL_NAME;
        internal static string SkkNameErl => MyResource.Resource.KI_DLG_SKK_NAME_ERL;
        internal static string SkkFirmaName => MyResource.Resource.SKK_LBL_HERSTELLER;
        internal static string SkkFirmaErl => MyResource.Resource.KI_DLG_SKK_FIRMA_ERL;
        internal static string SkkBeschreibungName => MyResource.Resource.SKK_LBL_BESCHREIBUNG;
        internal static string SkkBeschreibungErl => MyResource.Resource.KI_DLG_SKK_BESCHREIBUNG_ERL;
        internal static string SkkTypName => MyResource.Resource.SKK_LBL_TYP;
        internal static string SkkTypErl => MyResource.Resource.KI_DLG_SKK_TYP_ERL;
        internal static string SkkModulflaecheName => MyResource.Resource.SKK_LBL_MODULFLAECHE;
        internal static string SkkModulflaecheErl => MyResource.Resource.KI_DLG_SKK_MODULFLAECHE_ERL;
        internal static string SkkAperturflaecheName => MyResource.Resource.SKK_LBL_APERTURFLAECHE;
        internal static string SkkAperturflaecheErl => MyResource.Resource.KI_DLG_SKK_APERTURFLAECHE_ERL;
        internal static string SkkH0Name => MyResource.Resource.KI_DLG_SKK_H0_NAME;
        internal static string SkkH0Erl => MyResource.Resource.KI_DLG_SKK_H0_ERL;
        internal static string SkkK1Name => MyResource.Resource.KI_DLG_SKK_K1_NAME;
        internal static string SkkK1Erl => MyResource.Resource.KI_DLG_SKK_K1_ERL;
        internal static string SkkK2Name => MyResource.Resource.KI_DLG_SKK_K2_NAME;
        internal static string SkkK2Erl => MyResource.Resource.KI_DLG_SKK_K2_ERL;
        internal static string SkkKdirName => MyResource.Resource.KI_DLG_SKK_KDIR_NAME;
        internal static string SkkKdirErl => MyResource.Resource.KI_DLG_SKK_KDIR_ERL;
        internal static string SkkKdiffName => MyResource.Resource.KI_DLG_SKK_KDIFF_NAME;
        internal static string SkkKdiffErl => MyResource.Resource.KI_DLG_SKK_KDIFF_ERL;
        internal static string SkkVorlaufName => MyResource.Resource.SKK_LBL_VORLAUF;
        internal static string SkkVorlaufErl => MyResource.Resource.KI_DLG_SKK_VORLAUF_ERL;
        internal static string SkkRuecklaufName => MyResource.Resource.SKK_LBL_RUECKLAUF;
        internal static string SkkRuecklaufErl => MyResource.Resource.KI_DLG_SKK_RUECKLAUF_ERL;

        // ---- Der Modulkatalog: Stromspeicher und PV-Modul
        internal static string ModkBezeichnerName => MyResource.Resource.MODK_LBL_BEZEICHNER;
        internal static string ModkBezeichnerErl => MyResource.Resource.KI_DLG_MODK_BEZEICHNER_ERL;
        internal static string ModkPvBezeichnerName => MyResource.Resource.MODK_LBL_BEZEICHNER_PV;
        internal static string ModkPvBezeichnerErl => MyResource.Resource.KI_DLG_MODK_PV_BEZEICHNER_ERL;
        internal static string ModkFirmaName => MyResource.Resource.MODK_LBL_FIRMA;
        internal static string ModkFirmaErl => MyResource.Resource.KI_DLG_MODK_FIRMA_ERL;
        internal static string ModkBeschreibungName => MyResource.Resource.MODK_LBL_BESCHREIBUNG;
        internal static string ModkBeschreibungErl => MyResource.Resource.KI_DLG_MODK_BESCHREIBUNG_ERL;
        internal static string ModkTypName => MyResource.Resource.MODK_LBL_TYP;
        internal static string ModkTypErl => MyResource.Resource.KI_DLG_MODK_TYP_ERL;
        internal static string ModkEnergieName => MyResource.Resource.SP_LABEL_ENERGIE_KURZ;
        internal static string ModkEnergieErl => MyResource.Resource.KI_DLG_MODK_ENERGIE_ERL;
        internal static string ModkLeistungName => MyResource.Resource.MODK_LBL_LEISTUNG;
        internal static string ModkLeistungErl => MyResource.Resource.KI_DLG_MODK_LEISTUNG_ERL;
        internal static string ModkLadezustandName => MyResource.Resource.MODK_LBL_LADEZUSTAND;
        internal static string ModkLadezustandErl => MyResource.Resource.KI_DLG_MODK_LADEZUSTAND_ERL;
        internal static string ModkDegradationName => MyResource.Resource.MODK_LBL_DEGRADATION;
        internal static string ModkDegradationErl => MyResource.Resource.KI_DLG_MODK_DEGRADATION_ERL;
        internal static string ModkKostenName => MyResource.Resource.MODK_LBL_MODULKOSTEN;
        internal static string ModkKostenErl => MyResource.Resource.KI_DLG_MODK_KOSTEN_ERL;
        internal static string ModkWirkungsgradRtName => MyResource.Resource.SP_LABEL_WIRKUNGSGRAD_RT;
        internal static string ModkWirkungsgradRtErl => MyResource.Resource.KI_DLG_MODK_WG_RT_ERL;
        internal static string ModkZyklenName => MyResource.Resource.SP_LABEL_ZYKLEN;
        internal static string ModkZyklenErl => MyResource.Resource.KI_DLG_MODK_ZYKLEN_ERL;
        internal static string ModkVerschleissName => MyResource.Resource.SP_LABEL_VERSCHLEISSKOSTEN;
        internal static string ModkVerschleissErl => MyResource.Resource.KI_DLG_MODK_VERSCHLEISS_ERL;
        internal static string ModkLeistungskostenName => MyResource.Resource.SP_LABEL_LEISTUNGSKOSTEN;
        internal static string ModkLeistungskostenErl => MyResource.Resource.KI_DLG_MODK_LEISTUNGSKOSTEN_ERL;
        internal static string ModkInvestFixName => MyResource.Resource.SP_LABEL_INVESTITION_FIX;
        internal static string ModkInvestFixErl => MyResource.Resource.KI_DLG_MODK_INVEST_FIX_ERL;
        internal static string ModkStandbyName => MyResource.Resource.SP_LABEL_STANDBY;
        internal static string ModkStandbyErl => MyResource.Resource.KI_DLG_MODK_STANDBY_ERL;
        internal static string ModkPmaxName => MyResource.Resource.MODK_LBL_PMAX;
        internal static string ModkPmaxErl => MyResource.Resource.KI_DLG_MODK_PMAX_ERL;
        internal static string ModkWirkungsgradName => MyResource.Resource.MODK_LBL_WIRKUNGSGRAD;
        internal static string ModkWirkungsgradErl => MyResource.Resource.KI_DLG_MODK_WIRKUNGSGRAD_ERL;
        internal static string ModkUMppName => MyResource.Resource.MODK_LBL_UMPP;
        internal static string ModkUMppErl => MyResource.Resource.KI_DLG_MODK_UMPP_ERL;
        internal static string ModkULeerlaufName => MyResource.Resource.MODK_LBL_ULEERLAUF;
        internal static string ModkULeerlaufErl => MyResource.Resource.KI_DLG_MODK_ULEERLAUF_ERL;
        internal static string ModkIMppName => MyResource.Resource.MODK_LBL_IMPP;
        internal static string ModkIMppErl => MyResource.Resource.KI_DLG_MODK_IMPP_ERL;
        internal static string ModkIKurzschlussName => MyResource.Resource.MODK_LBL_IKURZSCHLUSS;
        internal static string ModkIKurzschlussErl => MyResource.Resource.KI_DLG_MODK_IKURZSCHLUSS_ERL;
        internal static string ModkTempkoeffName => MyResource.Resource.MODK_LBL_TEMPKOEFF;
        internal static string ModkTempkoeffErl => MyResource.Resource.KI_DLG_MODK_TEMPKOEFF_ERL;
        internal static string ModkLaengeName => MyResource.Resource.MODK_LBL_LAENGE;
        internal static string ModkLaengeErl => MyResource.Resource.KI_DLG_MODK_LAENGE_ERL;
        internal static string ModkBreiteName => MyResource.Resource.MODK_LBL_BREITE;
        internal static string ModkBreiteErl => MyResource.Resource.KI_DLG_MODK_BREITE_ERL;
        internal static string ModkPvKostenName => MyResource.Resource.MODK_LBL_MODULKOSTEN_PV;
        internal static string ModkPvKostenErl => MyResource.Resource.KI_DLG_MODK_PV_KOSTEN_ERL;
        internal static string ModkTNoctName => MyResource.Resource.PV_MODUL_LABEL_TNOCT;
        internal static string ModkTNoctErl => MyResource.Resource.KI_DLG_MODK_TNOCT_ERL;
        internal static string ModkTechnologieName => MyResource.Resource.PVM_MODUL_LABEL_TECHNOLOGIE;
        internal static string ModkTechnologieErl => MyResource.Resource.KI_DLG_MODK_TECHNOLOGIE_ERL;

        // ---- Der Modulkatalog: Wechselrichter
        internal static string WrkBezeichnerName => MyResource.Resource.WRK_LBL_BEZEICHNER;
        internal static string WrkBezeichnerErl => MyResource.Resource.KI_DLG_WRK_BEZEICHNER_ERL;
        internal static string WrkFirmaName => MyResource.Resource.WRK_LBL_FIRMA;
        internal static string WrkFirmaErl => MyResource.Resource.KI_DLG_WRK_FIRMA_ERL;
        internal static string WrkBeschreibungName => MyResource.Resource.WRK_LBL_BESCHREIBUNG;
        internal static string WrkBeschreibungErl => MyResource.Resource.KI_DLG_WRK_BESCHREIBUNG_ERL;
        internal static string WrkPAcNennName => MyResource.Resource.WRK_LBL_P_AC_NENN;
        internal static string WrkPAcNennErl => MyResource.Resource.KI_DLG_WRK_P_AC_NENN_ERL;
        internal static string WrkSAcMaxName => MyResource.Resource.WRK_LBL_S_AC_MAX;
        internal static string WrkSAcMaxErl => MyResource.Resource.KI_DLG_WRK_S_AC_MAX_ERL;
        internal static string WrkPDcMaxName => MyResource.Resource.WRK_LBL_P_DC_MAX;
        internal static string WrkPDcMaxErl => MyResource.Resource.KI_DLG_WRK_P_DC_MAX_ERL;
        internal static string WrkKostenName => MyResource.Resource.WRK_LBL_KOSTEN;
        internal static string WrkKostenErl => MyResource.Resource.KI_DLG_WRK_KOSTEN_ERL;
        internal static string WrkHerkunftName => MyResource.Resource.WRK_LBL_HERKUNFT;
        internal static string WrkHerkunftErl => MyResource.Resource.KI_DLG_WRK_HERKUNFT_ERL;
        internal static string WrkUMppMinName => MyResource.Resource.WRK_LBL_U_MPP_MIN;
        internal static string WrkUMppMinErl => MyResource.Resource.KI_DLG_WRK_U_MPP_MIN_ERL;
        internal static string WrkUMppMaxName => MyResource.Resource.WRK_LBL_U_MPP_MAX;
        internal static string WrkUMppMaxErl => MyResource.Resource.KI_DLG_WRK_U_MPP_MAX_ERL;
        internal static string WrkUDcMaxName => MyResource.Resource.WRK_LBL_U_DC_MAX;
        internal static string WrkUDcMaxErl => MyResource.Resource.KI_DLG_WRK_U_DC_MAX_ERL;
        internal static string WrkUStartName => MyResource.Resource.WRK_LBL_U_START;
        internal static string WrkUStartErl => MyResource.Resource.KI_DLG_WRK_U_START_ERL;
        internal static string WrkIDcMaxName => MyResource.Resource.WRK_LBL_I_DC_MAX;
        internal static string WrkIDcMaxErl => MyResource.Resource.KI_DLG_WRK_I_DC_MAX_ERL;
        internal static string WrkIScMaxName => MyResource.Resource.WRK_LBL_I_SC_MAX;
        internal static string WrkIScMaxErl => MyResource.Resource.KI_DLG_WRK_I_SC_MAX_ERL;
        internal static string WrkAnzahlMpptName => MyResource.Resource.WRK_LBL_ANZAHL_MPPT;
        internal static string WrkAnzahlMpptErl => MyResource.Resource.KI_DLG_WRK_ANZAHL_MPPT_ERL;
        internal static string WrkStraengeJeMpptName => MyResource.Resource.WRK_LBL_STRAENGE_JE_MPPT;
        internal static string WrkStraengeJeMpptErl => MyResource.Resource.KI_DLG_WRK_STRAENGE_JE_MPPT_ERL;
        internal static string WrkEta05Name => MyResource.Resource.WRK_LBL_ETA05;
        internal static string WrkEta05Erl => MyResource.Resource.KI_DLG_WRK_ETA05_ERL;
        internal static string WrkEta10Name => MyResource.Resource.WRK_LBL_ETA10;
        internal static string WrkEta10Erl => MyResource.Resource.KI_DLG_WRK_ETA10_ERL;
        internal static string WrkEta20Name => MyResource.Resource.WRK_LBL_ETA20;
        internal static string WrkEta20Erl => MyResource.Resource.KI_DLG_WRK_ETA20_ERL;
        internal static string WrkEta30Name => MyResource.Resource.WRK_LBL_ETA30;
        internal static string WrkEta30Erl => MyResource.Resource.KI_DLG_WRK_ETA30_ERL;
        internal static string WrkEta50Name => MyResource.Resource.WRK_LBL_ETA50;
        internal static string WrkEta50Erl => MyResource.Resource.KI_DLG_WRK_ETA50_ERL;
        internal static string WrkEta100Name => MyResource.Resource.WRK_LBL_ETA100;
        internal static string WrkEta100Erl => MyResource.Resource.KI_DLG_WRK_ETA100_ERL;
        internal static string WrkEtaEuroName => MyResource.Resource.WRK_LBL_ETA_EURO;
        internal static string WrkEtaEuroErl => MyResource.Resource.KI_DLG_WRK_ETA_EURO_ERL;
        internal static string WrkEtaMaxName => MyResource.Resource.WRK_LBL_ETA_MAX;
        internal static string WrkEtaMaxErl => MyResource.Resource.KI_DLG_WRK_ETA_MAX_ERL;
        internal static string WrkPStandbyName => MyResource.Resource.WRK_LBL_P_STANDBY;
        internal static string WrkPStandbyErl => MyResource.Resource.KI_DLG_WRK_P_STANDBY_ERL;
        internal static string WrkPNachtName => MyResource.Resource.WRK_LBL_P_NACHT;
        internal static string WrkPNachtErl => MyResource.Resource.KI_DLG_WRK_P_NACHT_ERL;

        // ==================================================== Welle KI-F6: STROM
        //
        // Die Anzeigenamen sind ueberall die BESCHRIFTUNGEN der Masken; nur wo eine
        // Maske ihre Beschriftung im Markup als deutsches Literal traegt
        // (SpeicherZeitreihenDialog) oder zwei Bedienelemente sich eine teilen,
        // steht ein eigener Schluessel.

        internal static string MaskePeakShaving => MyResource.Resource.KI_DLG_MASKE_PEAK;
        internal static string MaskeSpeicherzeitreihen => MyResource.Resource.KI_DLG_MASKE_SZR;
        internal static string MaskeStromganglinieAdmin => MyResource.Resource.KI_DLG_MASKE_SGA;

        // ---- Form_PeakShaving
        internal static string PeakQuelleName => MyResource.Resource.PEAK_GRP_QUELLE;
        internal static string PeakQuelleErl => MyResource.Resource.KI_DLG_PEAK_QUELLE_ERL;
        internal static string PeakGanglinieName => MyResource.Resource.PEAK_OPT_GANGLINIE;
        internal static string PeakGanglinieErl => MyResource.Resource.KI_DLG_PEAK_GANGLINIE_ERL;
        internal static string PeakPName => MyResource.Resource.PEAK_LBL_P;
        internal static string PeakPErl => MyResource.Resource.KI_DLG_PEAK_P_ERL;
        internal static string PeakKapazitaetName => MyResource.Resource.PEAK_LBL_KAPAZITAET;
        internal static string PeakKapazitaetErl => MyResource.Resource.KI_DLG_PEAK_KAPAZITAET_ERL;
        internal static string PeakEtaName => MyResource.Resource.PEAK_LBL_ETA;
        internal static string PeakEtaErl => MyResource.Resource.KI_DLG_PEAK_ETA_ERL;
        internal static string PeakSocMinName => MyResource.Resource.PEAK_LBL_SOCMIN;
        internal static string PeakSocMinErl => MyResource.Resource.KI_DLG_PEAK_SOCMIN_ERL;
        internal static string PeakSocMaxName => MyResource.Resource.PEAK_LBL_SOCMAX;
        internal static string PeakSocMaxErl => MyResource.Resource.KI_DLG_PEAK_SOCMAX_ERL;
        internal static string PeakStartSocName => MyResource.Resource.PEAK_LBL_STARTSOC;
        internal static string PeakStartSocErl => MyResource.Resource.KI_DLG_PEAK_STARTSOC_ERL;
        internal static string PeakAdaptivName => MyResource.Resource.PEAK_CHK_ADAPTIV;
        internal static string PeakAdaptivErl => MyResource.Resource.KI_DLG_PEAK_ADAPTIV_ERL;
        internal static string PeakZielName => MyResource.Resource.PEAK_LBL_ZIEL;
        internal static string PeakZielErl => MyResource.Resource.KI_DLG_PEAK_ZIEL_ERL;
        internal static string PeakLpName => MyResource.Resource.PEAK_LBL_LP;
        internal static string PeakLpErl => MyResource.Resource.KI_DLG_PEAK_LP_ERL;
        internal static string PeakBezugspreisName => MyResource.Resource.PEAK_LBL_BEZUGSPREIS;
        internal static string PeakBezugspreisErl => MyResource.Resource.KI_DLG_PEAK_BEZUGSPREIS_ERL;
        internal static string PeakKompatName => MyResource.Resource.PEAK_CHK_KOMPAT;
        internal static string PeakKompatErl => MyResource.Resource.KI_DLG_PEAK_KOMPAT_ERL;
        internal static string PeakCCapName => MyResource.Resource.PEAK_LBL_CCAP;
        internal static string PeakCCapErl => MyResource.Resource.KI_DLG_PEAK_CCAP_ERL;
        internal static string PeakCPowName => MyResource.Resource.PEAK_LBL_CPOW;
        internal static string PeakCPowErl => MyResource.Resource.KI_DLG_PEAK_CPOW_ERL;
        internal static string PeakIFixName => MyResource.Resource.PEAK_LBL_IFIX;
        internal static string PeakIFixErl => MyResource.Resource.KI_DLG_PEAK_IFIX_ERL;
        internal static string PeakZinsName => MyResource.Resource.PEAK_LBL_ZINS;
        internal static string PeakZinsErl => MyResource.Resource.KI_DLG_PEAK_ZINS_ERL;
        internal static string PeakNutzungsdauerName => MyResource.Resource.PEAK_LBL_NUTZUNGSDAUER;
        internal static string PeakNutzungsdauerErl => MyResource.Resource.KI_DLG_PEAK_NUTZUNGSDAUER_ERL;
        internal static string PeakSocName => MyResource.Resource.PEAK_CHK_SOC;
        internal static string PeakSocErl => MyResource.Resource.KI_DLG_PEAK_SOC_ERL;
        internal static string PeakReiterName => MyResource.Resource.KI_DLG_PEAK_REITER_NAME;
        internal static string PeakReiterErl => MyResource.Resource.KI_DLG_PEAK_REITER_ERL;
        internal static string PeakReiheName => MyResource.Resource.KI_DLG_PEAK_REIHE_NAME;
        internal static string PeakReiheErl => MyResource.Resource.KI_DLG_PEAK_REIHE_ERL;
        internal static string PeakHerkunftName => MyResource.Resource.KI_DLG_PEAK_HERKUNFT_NAME;
        internal static string PeakHerkunftErl => MyResource.Resource.KI_DLG_PEAK_HERKUNFT_ERL;

        // ---- Speicherzeitreihen
        internal static string SzrDateiName => MyResource.Resource.KI_DLG_SZR_DATEI_NAME;
        internal static string SzrDateiErl => MyResource.Resource.KI_DLG_SZR_DATEI_ERL;
        internal static string SzrRolleName => MyResource.Resource.KI_DLG_SZR_ROLLE_NAME;
        internal static string SzrRolleErl => MyResource.Resource.KI_DLG_SZR_ROLLE_ERL;
        internal static string SzrTrennzeichenName => MyResource.Resource.KI_DLG_SZR_TRENNZEICHEN_NAME;
        internal static string SzrTrennzeichenErl => MyResource.Resource.KI_DLG_SZR_TRENNZEICHEN_ERL;
        internal static string SzrDezimalName => MyResource.Resource.KI_DLG_SZR_DEZIMAL_NAME;
        internal static string SzrDezimalErl => MyResource.Resource.KI_DLG_SZR_DEZIMAL_ERL;
        internal static string SzrKodierungName => MyResource.Resource.KI_DLG_SZR_KODIERUNG_NAME;
        internal static string SzrKodierungErl => MyResource.Resource.KI_DLG_SZR_KODIERUNG_ERL;
        internal static string SzrKopfzeileName => MyResource.Resource.KI_DLG_SZR_KOPFZEILE_NAME;
        internal static string SzrKopfzeileErl => MyResource.Resource.KI_DLG_SZR_KOPFZEILE_ERL;
        internal static string SzrUeberspringenName => MyResource.Resource.KI_DLG_SZR_UEBERSPRINGEN_NAME;
        internal static string SzrUeberspringenErl => MyResource.Resource.KI_DLG_SZR_UEBERSPRINGEN_ERL;
        internal static string SzrZeitangabeName => MyResource.Resource.KI_DLG_SZR_ZEITANGABE_NAME;
        internal static string SzrZeitangabeErl => MyResource.Resource.KI_DLG_SZR_ZEITANGABE_ERL;
        internal static string SzrZeitstempelspalteName => MyResource.Resource.KI_DLG_SZR_ZSTEMPELSPALTE_NAME;
        internal static string SzrZeitstempelspalteErl => MyResource.Resource.KI_DLG_SZR_ZSTEMPELSPALTE_ERL;
        internal static string SzrZeitstempelformatName => MyResource.Resource.KI_DLG_SZR_ZSTEMPELFORMAT_NAME;
        internal static string SzrZeitstempelformatErl => MyResource.Resource.KI_DLG_SZR_ZSTEMPELFORMAT_ERL;
        internal static string SzrDatumsspalteName => MyResource.Resource.KI_DLG_SZR_DATUMSPALTE_NAME;
        internal static string SzrDatumsspalteErl => MyResource.Resource.KI_DLG_SZR_DATUMSPALTE_ERL;
        internal static string SzrDatumsformatName => MyResource.Resource.KI_DLG_SZR_DATUMFORMAT_NAME;
        internal static string SzrDatumsformatErl => MyResource.Resource.KI_DLG_SZR_DATUMFORMAT_ERL;
        internal static string SzrUhrzeitspalteName => MyResource.Resource.KI_DLG_SZR_UHRZEITSPALTE_NAME;
        internal static string SzrUhrzeitspalteErl => MyResource.Resource.KI_DLG_SZR_UHRZEITSPALTE_ERL;
        internal static string SzrUhrzeitformatName => MyResource.Resource.KI_DLG_SZR_UHRZEITFORMAT_NAME;
        internal static string SzrUhrzeitformatErl => MyResource.Resource.KI_DLG_SZR_UHRZEITFORMAT_ERL;
        internal static string SzrWertspalteName => MyResource.Resource.KI_DLG_SZR_WERTSPALTE_NAME;
        internal static string SzrWertspalteErl => MyResource.Resource.KI_DLG_SZR_WERTSPALTE_ERL;
        internal static string SzrZeitzoneName => MyResource.Resource.KI_DLG_SZR_ZEITZONE_NAME;
        internal static string SzrZeitzoneErl => MyResource.Resource.KI_DLG_SZR_ZEITZONE_ERL;
        internal static string SzrIntervallName => MyResource.Resource.KI_DLG_SZR_INTERVALL_NAME;
        internal static string SzrIntervallErl => MyResource.Resource.KI_DLG_SZR_INTERVALL_ERL;
        internal static string SzrEinheitName => MyResource.Resource.KI_DLG_SZR_EINHEIT_NAME;
        internal static string SzrEinheitErl => MyResource.Resource.KI_DLG_SZR_EINHEIT_ERL;

        // ---- Form_Stromganglinie_Admin
        internal static string SgaZeitintervallName => MyResource.Resource.IMPORT_LBL_ZEITINTERVAL;
        internal static string SgaZeitintervallErl => MyResource.Resource.KI_DLG_SGA_ZEITINTERVALL_ERL;
        internal static string SgaGewaehltName => MyResource.Resource.KI_DLG_SGA_GEWAEHLT_NAME;
        internal static string SgaGewaehltErl => MyResource.Resource.KI_DLG_SGA_GEWAEHLT_ERL;

        // ---- Station 1 der Stromspeicher-Auslegung: die Einheiten der Flotte
        internal static string FleNameName => MyResource.Resource.FLOTTE_ED_NAME;
        internal static string FleNameErl => MyResource.Resource.KI_DLG_FLE_NAME_ERL;
        internal static string FleKapazitaetName => MyResource.Resource.FLOTTE_ED_KAPAZITAET;
        internal static string FleKapazitaetErl => MyResource.Resource.KI_DLG_FLE_KAPAZITAET_ERL;
        internal static string FleLadeleistungName => MyResource.Resource.FLOTTE_ED_LADELEISTUNG;
        internal static string FleLadeleistungErl => MyResource.Resource.KI_DLG_FLE_LADELEISTUNG_ERL;
        internal static string FleEntladeleistungName => MyResource.Resource.FLOTTE_ED_ENTLADELEISTUNG;
        internal static string FleEntladeleistungErl => MyResource.Resource.KI_DLG_FLE_ENTLADELEISTUNG_ERL;
        internal static string FleLadewirkungsgradName => MyResource.Resource.FLOTTE_ED_LADEWIRKUNGSGRAD;
        internal static string FleLadewirkungsgradErl => MyResource.Resource.KI_DLG_FLE_LADEWIRKUNGSGRAD_ERL;
        internal static string FleEntladewirkungsgradName => MyResource.Resource.FLOTTE_ED_ENTLADEWIRKUNGSGRAD;
        internal static string FleEntladewirkungsgradErl => MyResource.Resource.KI_DLG_FLE_ENTLADEWIRKUNGSGRAD_ERL;
        internal static string FleSocMinName => MyResource.Resource.FLOTTE_ED_SOC_MIN;
        internal static string FleSocMinErl => MyResource.Resource.KI_DLG_FLE_SOC_MIN_ERL;
        internal static string FleSocMaxName => MyResource.Resource.FLOTTE_ED_SOC_MAX;
        internal static string FleSocMaxErl => MyResource.Resource.KI_DLG_FLE_SOC_MAX_ERL;
        internal static string FleSocStartName => MyResource.Resource.FLOTTE_ED_SOC_START;
        internal static string FleSocStartErl => MyResource.Resource.KI_DLG_FLE_SOC_START_ERL;
        internal static string FlePeakReserveName => MyResource.Resource.FLOTTE_ED_PEAK_RESERVE;
        internal static string FlePeakReserveErl => MyResource.Resource.KI_DLG_FLE_PEAK_RESERVE_ERL;
        internal static string FleHilfsverbrauchName => MyResource.Resource.FLOTTE_ED_HILFSVERBRAUCH;
        internal static string FleHilfsverbrauchErl => MyResource.Resource.KI_DLG_FLE_HILFSVERBRAUCH_ERL;
        internal static string FleGrenzverschleissName => MyResource.Resource.FLOTTE_ED_GRENZVERSCHLEISS;
        internal static string FleGrenzverschleissErl => MyResource.Resource.KI_DLG_FLE_GRENZVERSCHLEISS_ERL;
        internal static string FleEigeneKostenName => MyResource.Resource.FLOTTE_ED_EIGENE_KOSTEN;
        internal static string FleEigeneKostenErl => MyResource.Resource.KI_DLG_FLE_EIGENE_KOSTEN_ERL;
        internal static string FleInvestFixName => MyResource.Resource.FLOTTE_ED_INVEST_FIX;
        internal static string FleInvestFixErl => MyResource.Resource.KI_DLG_FLE_INVEST_FIX_ERL;
        internal static string FleInvestKapazitaetName => MyResource.Resource.FLOTTE_ED_INVEST_KAPAZITAET;
        internal static string FleInvestKapazitaetErl => MyResource.Resource.KI_DLG_FLE_INVEST_KAPAZITAET_ERL;
        internal static string FleInvestLeistungName => MyResource.Resource.FLOTTE_ED_INVEST_LEISTUNG;
        internal static string FleInvestLeistungErl => MyResource.Resource.KI_DLG_FLE_INVEST_LEISTUNG_ERL;
        internal static string FleOpexFixName => MyResource.Resource.FLOTTE_ED_OPEX_FIX;
        internal static string FleOpexFixErl => MyResource.Resource.KI_DLG_FLE_OPEX_FIX_ERL;
        internal static string FleOpexKapazitaetName => MyResource.Resource.FLOTTE_ED_OPEX_KAPAZITAET;
        internal static string FleOpexKapazitaetErl => MyResource.Resource.KI_DLG_FLE_OPEX_KAPAZITAET_ERL;
        internal static string FleOpexLeistungName => MyResource.Resource.FLOTTE_ED_OPEX_LEISTUNG;
        internal static string FleOpexLeistungErl => MyResource.Resource.KI_DLG_FLE_OPEX_LEISTUNG_ERL;
        internal static string FleDurchsatzkostenName => MyResource.Resource.FLOTTE_ED_DURCHSATZKOSTEN;
        internal static string FleDurchsatzkostenErl => MyResource.Resource.KI_DLG_FLE_DURCHSATZKOSTEN_ERL;
        internal static string FleErsatzkostenName => MyResource.Resource.FLOTTE_ED_ERSATZKOSTEN;
        internal static string FleErsatzkostenErl => MyResource.Resource.KI_DLG_FLE_ERSATZKOSTEN_ERL;
        internal static string FleErsatzintervallName => MyResource.Resource.FLOTTE_ED_ERSATZINTERVALL;
        internal static string FleErsatzintervallErl => MyResource.Resource.KI_DLG_FLE_ERSATZINTERVALL_ERL;
        internal static string FleRestwertName => MyResource.Resource.FLOTTE_ED_RESTWERT_EINHEIT;
        internal static string FleRestwertErl => MyResource.Resource.KI_DLG_FLE_RESTWERT_ERL;

        // ---- Station 2: Daten und Kosten
        internal static string Spa2LastquelleName => MyResource.Resource.FLOTTE_AUS_LASTQUELLE;
        internal static string Spa2LastquelleErl => MyResource.Resource.KI_DLG_SPA2_LASTQUELLE_ERL;
        internal static string Spa2PvQuelleName => MyResource.Resource.FLOTTE_AUS_PVQUELLE;
        internal static string Spa2PvQuelleErl => MyResource.Resource.KI_DLG_SPA2_PVQUELLE_ERL;
        internal static string Spa2PreisquelleName => MyResource.Resource.FLOTTE_AUS_PREISQUELLE;
        internal static string Spa2PreisquelleErl => MyResource.Resource.KI_DLG_SPA2_PREISQUELLE_ERL;
        internal static string Spa2ModelljahrName => MyResource.Resource.FLOTTE_AUS_MODELLJAHR;
        internal static string Spa2ModelljahrErl => MyResource.Resource.KI_DLG_SPA2_MODELLJAHR_ERL;
        internal static string Spa2InvestquelleName => MyResource.Resource.FLOTTE_AUS_INVESTQUELLE;
        internal static string Spa2InvestquelleErl => MyResource.Resource.KI_DLG_SPA2_INVESTQUELLE_ERL;
        internal static string Spa2BetriebsquelleName => MyResource.Resource.FLOTTE_AUS_BETRIEBSQUELLE;
        internal static string Spa2BetriebsquelleErl => MyResource.Resource.KI_DLG_SPA2_BETRIEBSQUELLE_ERL;
        internal static string Spa2InvestKwName => MyResource.Resource.FLOTTE_AUS_INVEST_KW;
        internal static string Spa2InvestKwErl => MyResource.Resource.KI_DLG_SPA2_INVEST_KW_ERL;
        internal static string Spa2InvestKwhName => MyResource.Resource.FLOTTE_AUS_INVEST_KWH;
        internal static string Spa2InvestKwhErl => MyResource.Resource.KI_DLG_SPA2_INVEST_KWH_ERL;
        internal static string Spa2BetriebKwName => MyResource.Resource.FLOTTE_AUS_BETRIEB_KW;
        internal static string Spa2BetriebKwErl => MyResource.Resource.KI_DLG_SPA2_BETRIEB_KW_ERL;
        internal static string Spa2BetriebKwhName => MyResource.Resource.FLOTTE_AUS_BETRIEB_KWH;
        internal static string Spa2BetriebKwhErl => MyResource.Resource.KI_DLG_SPA2_BETRIEB_KWH_ERL;
        internal static string Spa2BetriebEntladenName => MyResource.Resource.FLOTTE_AUS_BETRIEB_ENTLADEN;
        internal static string Spa2BetriebEntladenErl => MyResource.Resource.KI_DLG_SPA2_BETRIEB_ENTLADEN_ERL;
        internal static string Spa2LeistungspreisName => MyResource.Resource.OPT_LBL_LEISTUNGSPREIS;
        internal static string Spa2LeistungspreisErl => MyResource.Resource.KI_DLG_SPA2_LEISTUNGSPREIS_ERL;
        internal static string Spa2AusgleichName => MyResource.Resource.FLOTTE_ED_AUSGLEICH;
        internal static string Spa2AusgleichErl => MyResource.Resource.KI_DLG_SPA2_AUSGLEICH_ERL;
        internal static string Spa2ZinsName => MyResource.Resource.FLOTTE_ED_ZINS;
        internal static string Spa2ZinsErl => MyResource.Resource.KI_DLG_SPA2_ZINS_ERL;
        internal static string Spa2ProjektionsartName => MyResource.Resource.FLOTTE_ED_PROJEKTIONSART;
        internal static string Spa2ProjektionsartErl => MyResource.Resource.KI_DLG_SPA2_PROJEKTIONSART_ERL;
        internal static string Spa2ProjektjahreName => MyResource.Resource.FLOTTE_ED_PROJEKTJAHRE;
        internal static string Spa2ProjektjahreErl => MyResource.Resource.KI_DLG_SPA2_PROJEKTJAHRE_ERL;
        internal static string Spa2RestwertName => MyResource.Resource.FLOTTE_ED_RESTWERT_GESAMT;
        internal static string Spa2RestwertErl => MyResource.Resource.KI_DLG_SPA2_RESTWERT_ERL;

        // ---- Station 3: Betriebsfuehrung und Netz
        internal static string Spa3VerteilungName => MyResource.Resource.FLOTTE_BETRIEB_LBL_VERTEILUNG;
        internal static string Spa3VerteilungErl => MyResource.Resource.KI_DLG_SPA3_VERTEILUNG_ERL;
        internal static string Spa3PrioritaetName => MyResource.Resource.FLOTTE_BETRIEB_LBL_PRIORITAET;
        internal static string Spa3PrioritaetErl => MyResource.Resource.KI_DLG_SPA3_PRIORITAET_ERL;
        internal static string Spa3ExportName => MyResource.Resource.FLOTTE_BETRIEB_CHK_EXPORT;
        internal static string Spa3ExportErl => MyResource.Resource.KI_DLG_SPA3_EXPORT_ERL;
        internal static string Spa3BezugsgrenzeName => MyResource.Resource.FLOTTE_ED_BEZUGSGRENZE;
        internal static string Spa3BezugsgrenzeErl => MyResource.Resource.KI_DLG_SPA3_BEZUGSGRENZE_ERL;
        internal static string Spa3EinspeisegrenzeName => MyResource.Resource.FLOTTE_ED_EINSPEISEGRENZE;
        internal static string Spa3EinspeisegrenzeErl => MyResource.Resource.KI_DLG_SPA3_EINSPEISEGRENZE_ERL;
        internal static string Spa3InformationsstandName => MyResource.Resource.FLOTTE_ED_INFORMATIONSSTAND;
        internal static string Spa3InformationsstandErl => MyResource.Resource.KI_DLG_SPA3_INFORMATIONSSTAND_ERL;
        internal static string Spa3PlanungshorizontName => MyResource.Resource.FLOTTE_ED_PLANUNGSHORIZONT;
        internal static string Spa3PlanungshorizontErl => MyResource.Resource.KI_DLG_SPA3_PLANUNGSHORIZONT_ERL;
        internal static string Spa3NeuplanungName => MyResource.Resource.FLOTTE_ED_NEUPLANUNG;
        internal static string Spa3NeuplanungErl => MyResource.Resource.KI_DLG_SPA3_NEUPLANUNG_ERL;
        internal static string Spa3EndbedingungName => MyResource.Resource.FLOTTE_ED_ENDBEDINGUNG;
        internal static string Spa3EndbedingungErl => MyResource.Resource.KI_DLG_SPA3_ENDBEDINGUNG_ERL;
        internal static string Spa3FallbackName => MyResource.Resource.FLOTTE_ED_FALLBACK;
        internal static string Spa3FallbackErl => MyResource.Resource.KI_DLG_SPA3_FALLBACK_ERL;

        // ---- Station 4: der Suchraum
        internal static string Spa4VariierenName => MyResource.Resource.FLOTTE_OPT_SP_VARIIEREN;
        internal static string Spa4VariierenErl => MyResource.Resource.KI_DLG_SPA4_VARIIEREN_ERL;
        internal static string Spa4QuelleName => MyResource.Resource.FLOTTE_OPT_QUELLE;
        internal static string Spa4QuelleErl => MyResource.Resource.KI_DLG_SPA4_QUELLE_ERL;
        internal static string Spa4KapazitaetVonName => MyResource.Resource.FLOTTE_ED_KAPAZITAET_VON;
        internal static string Spa4KapazitaetVonErl => MyResource.Resource.KI_DLG_SPA4_KAPAZITAET_VON_ERL;
        internal static string Spa4KapazitaetBisName => MyResource.Resource.FLOTTE_ED_KAPAZITAET_BIS;
        internal static string Spa4KapazitaetBisErl => MyResource.Resource.KI_DLG_SPA4_KAPAZITAET_BIS_ERL;
        internal static string Spa4LeistungVonName => MyResource.Resource.FLOTTE_ED_LEISTUNG_VON;
        internal static string Spa4LeistungVonErl => MyResource.Resource.KI_DLG_SPA4_LEISTUNG_VON_ERL;
        internal static string Spa4LeistungBisName => MyResource.Resource.FLOTTE_ED_LEISTUNG_BIS;
        internal static string Spa4LeistungBisErl => MyResource.Resource.KI_DLG_SPA4_LEISTUNG_BIS_ERL;
        internal static string Spa4AnzahlVonName => MyResource.Resource.FLOTTE_ED_ANZAHL_VON;
        internal static string Spa4AnzahlVonErl => MyResource.Resource.KI_DLG_SPA4_ANZAHL_VON_ERL;
        internal static string Spa4AnzahlBisName => MyResource.Resource.FLOTTE_ED_ANZAHL_BIS;
        internal static string Spa4AnzahlBisErl => MyResource.Resource.KI_DLG_SPA4_ANZAHL_BIS_ERL;

        // ========================== Welle KI-F6: BERICHTE und PROJEKT

        internal static string MaskeBerichteUebersicht => MyResource.Resource.KI_DLG_MASKE_BKU;
        internal static string MaskeBerichtseite => MyResource.Resource.KI_DLG_MASKE_BKB;
        internal static string MaskeProjektKopie => MyResource.Resource.KI_DLG_MASKE_PRK;
        internal static string MaskeProjektVariante => MyResource.Resource.KI_DLG_MASKE_PRV;

        // ---- Reiterblatt „Uebersicht"
        internal static string BkuStammName => MyResource.Resource.BKS_LBL_STAMM;
        internal static string BkuStammErl => MyResource.Resource.KI_DLG_BKU_STAMM_ERL;
        internal static string BkuNurStaemmeName => MyResource.Resource.BKS_LBL_NUR_STAEMME;
        internal static string BkuNurStaemmeErl => MyResource.Resource.KI_DLG_BKU_NURSTAEMME_ERL;
        internal static string BkuVarianteName => MyResource.Resource.BKS_LBL_VARIANTE;
        internal static string BkuVarianteErl => MyResource.Resource.KI_DLG_BKU_VARIANTE_ERL;
        internal static string BkuBezeichnerName => MyResource.Resource.BKS_LBL_BEZEICHNER;
        internal static string BkuBezeichnerErl => MyResource.Resource.KI_DLG_BKU_BEZEICHNER_ERL;
        internal static string BkuSimName => MyResource.Resource.KI_DLG_BKU_SIM_NAME;
        internal static string BkuSimErl => MyResource.Resource.KI_DLG_BKU_SIM_ERL;

        // ---- Reiterblatt „Bericht"
        internal static string BkbAusgabeName => MyResource.Resource.BK_BER_LBL_AUSGABE;
        internal static string BkbAusgabeErl => MyResource.Resource.KI_DLG_BKB_AUSGABE_ERL;
        internal static string BkbZielName => MyResource.Resource.BK_BER_LBL_ZIEL;
        internal static string BkbZielErl => MyResource.Resource.KI_DLG_BKB_ZIEL_ERL;
        internal static string BkbVariantenName => MyResource.Resource.KI_DLG_BKB_VARIANTEN_NAME;
        internal static string BkbVariantenErl => MyResource.Resource.KI_DLG_BKB_VARIANTEN_ERL;
        internal static string BkbBausteineName => MyResource.Resource.BK_BER_LBL_BAUSTEINE;
        internal static string BkbBausteineErl => MyResource.Resource.KI_DLG_BKB_BAUSTEINE_ERL;

        // ---- „Projekt speichern unter"
        internal static string PrkQuelleName => MyResource.Resource.PRJ_KOPIE_LBL_AUSWAHL;
        internal static string PrkQuelleErl => MyResource.Resource.KI_DLG_PRK_QUELLE_ERL;
        internal static string PrkNameName => MyResource.Resource.PRJ_KOPIE_LBL_NEUERNAME;
        internal static string PrkNameErl => MyResource.Resource.KI_DLG_PRK_NAME_ERL;
        internal static string PrkBeschreibungName => MyResource.Resource.PRJ_KOPIE_LBL_BESCHREIBUNG;
        internal static string PrkBeschreibungErl => MyResource.Resource.KI_DLG_PRK_BESCHREIBUNG_ERL;
        internal static string PrkKundeName => MyResource.Resource.PRJ_KOPIE_LBL_KUNDE;
        internal static string PrkKundeErl => MyResource.Resource.KI_DLG_PRK_KUNDE_ERL;
        internal static string PrkBearbeiterName => MyResource.Resource.PRJ_KOPIE_LBL_BEARBEITER;
        internal static string PrkBearbeiterErl => MyResource.Resource.KI_DLG_PRK_BEARBEITER_ERL;

        // ---- „Als Variante speichern"
        internal static string PrvHakenName => MyResource.Resource.VAR_DLG_QUELLE_HAKEN;
        internal static string PrvHakenErl => MyResource.Resource.KI_DLG_PRV_HAKEN_ERL;
        internal static string PrvQuelleErl => MyResource.Resource.KI_DLG_PRV_QUELLE_ERL;
        internal static string PrvBezeichnerName => MyResource.Resource.BK_LBL_BEZEICHNER;
        internal static string PrvBezeichnerErl => MyResource.Resource.KI_DLG_PRV_BEZEICHNER_ERL;
        internal static string PrvZielnameName => MyResource.Resource.KI_DLG_PRV_ZIELNAME_NAME;
        internal static string PrvZielnameErl => MyResource.Resource.KI_DLG_PRV_ZIELNAME_ERL;

        // ---- Welle KI-F6, Schritt 3: die Ansicht „Simulation"
        internal static string SimAutarkieName => MyResource.Resource.SIM_DASH_SPEICHER_INFO;
        internal static string SimAutarkieErl => MyResource.Resource.KI_DLG_SIM_AUTARKIE_ERL;

        // ============================================== Welle #456: die VERWALTUNGEN
        //
        // Die Feldkarte der vier Erzeugerverwaltungen entsteht aus dem PROFIL
        // (KiDialoge.ErzeugerVerwaltung); Anzeigenamen und Einheiten kommen von dort.
        // Hier steht nur, was das Profil nicht fuehrt: die ERLAEUTERUNG je Feld. Wo
        // ein Katalogeditor dasselbe Feld schon erklaert, wird sein Satz genommen -
        // es ist derselbe Wert derselben Stammtabelle; die uebrigen Felder bekommen
        // eigene Saetze.

        /// <summary>Der Uebersetzer des Profils — Schluessel → Text der laufenden Sprache.</summary>
        internal static string Profiltext(string schluessel)
        {
            string text = null;
            try
            {
                text = MyResource.Resource.ResourceManager.GetString(schluessel,
                                                                     MyResource.Resource.Culture);
            }
            catch (System.Exception) { }
            return string.IsNullOrEmpty(text) ? schluessel : text;
        }

        internal static string KbrowSatzName => MyResource.Resource.KI_DLG_KBROW_SATZ_NAME;
        internal static string KbrowSatzErl => MyResource.Resource.KI_DLG_KBROW_SATZ_ERL;

        /// <summary>„Verwerfen" der Verwaltungen (AD-Q6).</summary>
        internal static string KnopfVerwerfen => MyResource.Resource.ADM_BTN_VERWERFEN;

        /// <summary>
        /// Die Absage einer nur zum Ansehen geoeffneten Verwaltung — der Schutzgrund,
        /// den der Katalogbrowser im Lesemodus anmeldet.
        /// </summary>
        internal static string KbrowNurLesen => MyResource.Resource.KI_DLG_KBROW_NURLESEN;

        /// <summary>
        /// Der RUECKFALL eines Profilfeldes ohne eigenen Satz. Er haelt den Katalog am
        /// Leben, wenn dem Profil ein Feld zuwaechst; der Waechter
        /// (<c>KiDialogkatalogTests</c>) verlangt, dass ihn kein Feld traegt.
        /// </summary>
        internal static string KbrowRueckfall(string feldname)
            => string.Format(System.Globalization.CultureInfo.CurrentCulture,
                             MyResource.Resource.KI_DLG_KBROW_FELD_ERL, feldname ?? "");

        /// <summary>
        /// Die Erlaeuterung eines Profilfeldes der Verwaltung <paramref name="art"/>.
        /// </summary>
        /// <param name="feldname">Die Beschriftung ohne Doppelpunkt — fuer die Saetze mit Platzhalter.</param>
        internal static string KbrowErlaeuterung(KatalogBrowserArt art, string schluessel, string feldname)
        {
            string text = Eigener(art, schluessel) ?? Gemeinsam(schluessel, feldname);
            return string.IsNullOrWhiteSpace(text) ? KbrowRueckfall(feldname) : text;
        }

        /// <summary>Der Satz des Katalogeditors derselben Familie; <c>null</c> = keiner.</summary>
        private static string Eigener(KatalogBrowserArt art, string schluessel)
        {
            switch (art)
            {
                case KatalogBrowserArt.Heizkessel:
                    switch (schluessel)
                    {
                        case KatalogBrowserProfil.FeldBeschreibung: return HkBeschreibungErl;
                        case KatalogBrowserProfil.FeldFirma: return HkFirmaErl;
                        case KatalogBrowserProfil.FeldBrennstoff: return HkTraegerErl;
                        case KatalogBrowserProfil.FeldPtherm: return HkLeistungErl;
                        case KatalogBrowserProfil.FeldBrennwert: return HkBrennwertErl;
                        case KatalogBrowserProfil.FeldVorlauf: return HkVorlaufErl;
                        case KatalogBrowserProfil.FeldRuecklauf: return HkRuecklaufErl;
                        case KatalogBrowserProfil.FeldWirkungsgradGas: return HkWgGasErl;
                        case KatalogBrowserProfil.FeldWirkungsgradOel: return HkWgOelErl;
                        case KatalogBrowserProfil.FeldBBVerlust: return HkBbVerlustErl;
                        case KatalogBrowserProfil.FeldWartungskosten:
                            return MyResource.Resource.KI_DLG_KBROW_WARTUNG_ERL;
                        case KatalogBrowserProfil.FeldWartungEinheit:
                            return MyResource.Resource.KI_DLG_KBROW_WARTUNG_EINHEIT_ERL;
                    }
                    return null;

                case KatalogBrowserArt.Bhkw:
                    switch (schluessel)
                    {
                        case KatalogBrowserProfil.FeldBeschreibung: return BhkkBeschreibungErl;
                        case KatalogBrowserProfil.FeldFirma: return BhkkFirmaErl;
                        case KatalogBrowserProfil.FeldMotortyp: return BhkkMotortypErl;
                        case KatalogBrowserProfil.FeldPtherm: return BhkkPthermErl;
                        case KatalogBrowserProfil.FeldPel: return BhkkPelErl;
                        case KatalogBrowserProfil.FeldGrenzleistung: return BhkkGrenzleistungErl;
                        case KatalogBrowserProfil.FeldVorlauf: return BhkkVorlaufErl;
                        case KatalogBrowserProfil.FeldRuecklauf: return BhkkRuecklaufErl;
                        case KatalogBrowserProfil.FeldBrennstoff: return BhkkTraegerErl;
                        case KatalogBrowserProfil.FeldWirkungsgradEl: return BhkkWgElErl;
                        case KatalogBrowserProfil.FeldWirkungsgradTh: return BhkkWgThErl;
                        case KatalogBrowserProfil.FeldWirkungsgrad: return BhkkWgGesamtErl;
                        case KatalogBrowserProfil.FeldInvestitionJeKwel:
                            return MyResource.Resource.KI_DLG_KBROW_INVEST_KWEL_ERL;
                        case KatalogBrowserProfil.FeldWartungJeKwhel:
                            return MyResource.Resource.KI_DLG_KBROW_WARTUNG_KWHEL_ERL;
                    }
                    return null;

                case KatalogBrowserArt.Solarkollektoren:
                    switch (schluessel)
                    {
                        case KatalogBrowserProfil.FeldBeschreibung: return SkkBeschreibungErl;
                        case KatalogBrowserProfil.FeldFirma: return SkkFirmaErl;
                        case KatalogBrowserProfil.FeldKollektortyp: return SkkTypErl;
                        case KatalogBrowserProfil.FeldModulflaeche: return SkkModulflaecheErl;
                        case KatalogBrowserProfil.FeldAperturflaeche: return SkkAperturflaecheErl;
                        case KatalogBrowserProfil.FeldVorlauf: return SkkVorlaufErl;
                        case KatalogBrowserProfil.FeldRuecklauf: return SkkRuecklaufErl;
                        case KatalogBrowserProfil.FeldH0: return SkkH0Erl;
                        case KatalogBrowserProfil.FeldK1: return SkkK1Erl;
                        case KatalogBrowserProfil.FeldK2: return SkkK2Erl;
                        case KatalogBrowserProfil.FeldKdir: return SkkKdirErl;
                        case KatalogBrowserProfil.FeldKdiff: return SkkKdiffErl;
                    }
                    return null;

                case KatalogBrowserArt.Pufferspeicher:
                    switch (schluessel)
                    {
                        case KatalogBrowserProfil.FeldFirma: return PspFirmaErl;
                        case KatalogBrowserProfil.FeldSpeichertyp: return PspSpeichertypErl;
                        case KatalogBrowserProfil.FeldVerluste: return PspVerlusteErl;
                        case KatalogBrowserProfil.FeldVolumen: return PspVolumenErl;
                    }
                    return null;
            }

            return null;
        }

        /// <summary>
        /// Die Saetze, die fuer alle vier Verwaltungen gleich lauten; <c>null</c> = keiner.
        /// </summary>
        private static string Gemeinsam(string schluessel, string feldname)
        {
            switch (schluessel)
            {
                case KatalogBrowserProfil.FeldBezeichner:
                    return MyResource.Resource.KI_DLG_KBROW_BEZEICHNER_ERL;
                case KatalogBrowserProfil.FeldBeschreibung:
                    return HkBeschreibungErl;
                case KatalogBrowserProfil.FeldInvestitionskosten:
                    return MyResource.Resource.KI_DLG_KBROW_INVEST_ERL;
                case KatalogBrowserProfil.FeldRaumbedarf:
                    return MyResource.Resource.KI_DLG_KBROW_RAUMBEDARF_ERL;
                case KatalogBrowserProfil.FeldNutzungsdauer:
                    return MyResource.Resource.KI_DLG_KBROW_NUTZUNGSDAUER_ERL;

                case KatalogBrowserProfil.FeldCo2:
                case KatalogBrowserProfil.FeldSo2:
                case KatalogBrowserProfil.FeldNox:
                case KatalogBrowserProfil.FeldCo:
                case KatalogBrowserProfil.FeldStaub:
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                         MyResource.Resource.KI_DLG_KBROW_EMISSION_ERL, feldname ?? "");

                case KatalogBrowserProfil.FeldKostenModul:
                case KatalogBrowserProfil.FeldKostenMontage:
                case KatalogBrowserProfil.FeldKostenLieferung:
                case KatalogBrowserProfil.FeldKostenSchallschutz:
                case KatalogBrowserProfil.FeldKostenAbgasreinigung:
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                         MyResource.Resource.KI_DLG_KBROW_KOSTENPOSTEN_ERL, feldname ?? "");
            }

            return null;
        }

        // ---- Die Bedarfsverwaltungen: Monatswerte (Welle #456)

        /// <summary>Einheit der Monatswerte eines Bedarfskatalogs.</summary>
        internal const string EINHEIT_MWH = "MWh";

        /// <summary>Der Monatsname 1–12, wie ihn die Monatsfelder der Maske tragen.</summary>
        internal static string Monat(int monat)
        {
            string text = null;
            try
            {
                text = MyResource.Resource.ResourceManager.GetString(
                    "ALLG_MONAT_" + monat.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    MyResource.Resource.Culture);
            }
            catch (System.Exception) { }
            return string.IsNullOrEmpty(text)
                ? monat.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : text;
        }

        /// <summary>Die Erlaeuterung des Monatswertes <paramref name="monat"/> (1–12).</summary>
        internal static string BadmMonatErl(int monat)
            => string.Format(System.Globalization.CultureInfo.CurrentCulture,
                             MyResource.Resource.KI_DLG_BADM_MONAT_ERL, Monat(monat));
    }
}
