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

        /// <summary>Einheit einer Zeitspanne in Jahren.</summary>
        internal static string EinheitJahre => MyResource.Resource.KI_DLG_EINHEIT_JAHRE;

        // ====================================================================== Masken

        internal static string MaskeHeizkessel => MyResource.Resource.KI_DLG_MASKE_HEIZKESSEL;
        internal static string MaskePv => MyResource.Resource.KI_DLG_MASKE_PV;
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
        internal static string KvSatzName => MyResource.Resource.KI_DLG_KV_SATZ_NAME;
        internal static string KvSatzErl => MyResource.Resource.KI_DLG_KV_SATZ_ERL;
        internal static string KvNutzungsdauerName => MyResource.Resource.KI_DLG_KV_NUTZUNGSDAUER_NAME;
        internal static string KvNutzungsdauerErl => MyResource.Resource.KI_DLG_KV_NUTZUNGSDAUER_ERL;
        internal static string KvBetragName => MyResource.Resource.KI_DLG_KV_BETRAG_NAME;
        internal static string KvBetragErl => MyResource.Resource.KI_DLG_KV_BETRAG_ERL;

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

        // ====================================================== Pufferspeicher: Felder

        internal static string PspVolumenName => MyResource.Resource.KI_DLG_PSP_VOLUMEN_NAME;
        internal static string PspVolumenErl => MyResource.Resource.KI_DLG_PSP_VOLUMEN_ERL;

        // ========================================================= Waermepumpe: Felder

        internal static string WpModulkostenName => MyResource.Resource.KI_DLG_WP_MODULKOSTEN_NAME;
        internal static string WpModulkostenErl => MyResource.Resource.KI_DLG_WP_MODULKOSTEN_ERL;

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

        // -------------------------------------------- Form_BHKWEing (Projektmaske)

        internal static string BhkwAnlageName => MyResource.Resource.BHKWV_LBL_NAME;
        internal static string BhkwAnlageErl => MyResource.Resource.KI_DLG_BHKW_ANLAGE_ERL;
        internal static string BhkwGrenzleistungName => MyResource.Resource.BHKWV_LBL_GRENZLEISTUNG;
        internal static string BhkwGrenzleistungErl => MyResource.Resource.KI_DLG_BHKW_GRENZLEISTUNG_ERL;
        internal static string BhkwVorlaufName => MyResource.Resource.BHKWV_LBL_VORLAUF;
        internal static string BhkwVorlaufErl => MyResource.Resource.KI_DLG_BHKW_VORLAUF_ERL;
        internal static string BhkwRuecklaufName => MyResource.Resource.BHKWV_LBL_RUECKLAUF;
        internal static string BhkwRuecklaufErl => MyResource.Resource.KI_DLG_BHKW_RUECKLAUF_ERL;

        // -------------------------------------------- Form_PufferSp (Projektmaske)

        internal static string MaskePufferSpProjekt => MyResource.Resource.KI_DLG_MASKE_PUFFERSP_PROJEKT;
        internal static string PspAnlageName => MyResource.Resource.HZK_LBL_NAME;
        internal static string PspAnlageErl => MyResource.Resource.KI_DLG_PSPP_ANLAGE_ERL;

        // ---------------------------------------- Form_Stromspeicher (Projektmaske)

        internal static string MaskeStromspeicherProjekt => MyResource.Resource.KI_DLG_MASKE_STROMSPEICHER;
        internal static string StspAnlageName => MyResource.Resource.HZK_LBL_NAME;
        internal static string StspAnlageErl => MyResource.Resource.KI_DLG_STSP_ANLAGE_ERL;

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

        /// <summary>Ersatztext fuer einen leeren Feldinhalt in der Ergebnisliste.</summary>
        internal static string KeinWert => MyResource.Resource.KI_DLG_KEIN_WERT;
    }
}
