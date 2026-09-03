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
    /// „kW", „%", „€", „m³", „°C", „l", „°" und „g / MWh" sind Einheitenzeichen und keine
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

        /// <summary>Einheit eines Rauminhalts in Kubikmetern.</summary>
        internal const string EINHEIT_M3 = "m³";

        /// <summary>Einheit eines Rauminhalts in Litern.</summary>
        internal const string EINHEIT_LITER = "l";

        /// <summary>Einheit einer Temperatur.</summary>
        internal const string EINHEIT_GRAD_C = "°C";

        /// <summary>Einheit eines Winkels.</summary>
        internal const string EINHEIT_GRAD = "°";

        /// <summary>Einheit einer massenbezogenen Emission.</summary>
        internal const string EINHEIT_G_MWH = "g / MWh";

        /// <summary>Einheit einer Zeitspanne in Jahren.</summary>
        internal static string EinheitJahre => MyResource.Resource.KI_DLG_EINHEIT_JAHRE;

        // ====================================================================== Masken

        internal static string MaskeHeizkessel => MyResource.Resource.KI_DLG_MASKE_HEIZKESSEL;
        internal static string MaskePv => MyResource.Resource.KI_DLG_MASKE_PV;
        internal static string MaskePufferSp => MyResource.Resource.KI_DLG_MASKE_PUFFERSP;
        internal static string MaskeWp => MyResource.Resource.KI_DLG_MASKE_WP;

        // ====================================================================== Knoepfe

        internal static string KnopfSpeichern => MyResource.Resource.KI_DLG_KNOPF_SPEICHERN;
        internal static string KnopfSpeichernUnter => MyResource.Resource.KI_DLG_KNOPF_SPEICHERN_UNTER;
        internal static string KnopfUeberschreiben => MyResource.Resource.KI_DLG_KNOPF_UEBERSCHREIBEN;
        internal static string KnopfAbbrechen => MyResource.Resource.KI_DLG_KNOPF_ABBRECHEN;
        internal static string KnopfOk => MyResource.Resource.KI_DLG_KNOPF_OK;

        // ========================================================== Heizkessel: Felder

        internal static string HkLeistungName => MyResource.Resource.KI_DLG_HK_LEISTUNG_NAME;
        internal static string HkLeistungErl => MyResource.Resource.KI_DLG_HK_LEISTUNG_ERL;
        internal static string HkWgGasName => MyResource.Resource.KI_DLG_HK_WG_GAS_NAME;
        internal static string HkWgGasErl => MyResource.Resource.KI_DLG_HK_WG_GAS_ERL;
        internal static string HkWgOelName => MyResource.Resource.KI_DLG_HK_WG_OEL_NAME;
        internal static string HkWgOelErl => MyResource.Resource.KI_DLG_HK_WG_OEL_ERL;
        internal static string HkBbVerlustName => MyResource.Resource.KI_DLG_HK_BB_VERLUST_NAME;
        internal static string HkBbVerlustErl => MyResource.Resource.KI_DLG_HK_BB_VERLUST_ERL;
        internal static string HkInvestName => MyResource.Resource.KI_DLG_HK_INVEST_NAME;
        internal static string HkInvestErl => MyResource.Resource.KI_DLG_HK_INVEST_ERL;

        /// <summary>
        /// Der Anzeigename des Wartungsfeldes kommt aus dem Bestand: Genau diesen Text
        /// traegt die Beschriftung, die <c>WartungsfeldAufbauen</c> zur Laufzeit setzt, und
        /// genau ihn nennt auch die Pruefmeldung von <c>EingabenPruefen</c>. Ein zweiter
        /// Eintrag waere eine zweite Pflegestelle fuer dasselbe Wort.
        /// </summary>
        internal static string HkWartungName => MyResource.Resource.KESSEL_WARTUNG_LBL;
        internal static string HkWartungErl => MyResource.Resource.KI_DLG_HK_WARTUNG_ERL;

        internal static string HkRaumbedarfName => MyResource.Resource.KI_DLG_HK_RAUMBEDARF_NAME;
        internal static string HkRaumbedarfErl => MyResource.Resource.KI_DLG_HK_RAUMBEDARF_ERL;
        internal static string HkNutzungsdauerName => MyResource.Resource.KI_DLG_HK_NUTZUNGSDAUER_NAME;
        internal static string HkNutzungsdauerErl => MyResource.Resource.KI_DLG_HK_NUTZUNGSDAUER_ERL;
        internal static string HkCo2Name => MyResource.Resource.KI_DLG_HK_CO2_NAME;
        internal static string HkCo2Erl => MyResource.Resource.KI_DLG_HK_CO2_ERL;
        internal static string HkSo2Name => MyResource.Resource.KI_DLG_HK_SO2_NAME;
        internal static string HkSo2Erl => MyResource.Resource.KI_DLG_HK_SO2_ERL;
        internal static string HkNoxName => MyResource.Resource.KI_DLG_HK_NOX_NAME;
        internal static string HkNoxErl => MyResource.Resource.KI_DLG_HK_NOX_ERL;
        internal static string HkCoName => MyResource.Resource.KI_DLG_HK_CO_NAME;
        internal static string HkCoErl => MyResource.Resource.KI_DLG_HK_CO_ERL;
        internal static string HkStaubName => MyResource.Resource.KI_DLG_HK_STAUB_NAME;
        internal static string HkStaubErl => MyResource.Resource.KI_DLG_HK_STAUB_ERL;
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

        // ====================================================== Pufferspeicher: Felder

        internal static string PspVolumenName => MyResource.Resource.KI_DLG_PSP_VOLUMEN_NAME;
        internal static string PspVolumenErl => MyResource.Resource.KI_DLG_PSP_VOLUMEN_ERL;

        // ========================================================= Waermepumpe: Felder

        internal static string WpModulkostenName => MyResource.Resource.KI_DLG_WP_MODULKOSTEN_NAME;
        internal static string WpModulkostenErl => MyResource.Resource.KI_DLG_WP_MODULKOSTEN_ERL;

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

        /// <summary>Ersatztext fuer einen leeren Feldinhalt in der Ergebnisliste.</summary>
        internal static string KeinWert => MyResource.Resource.KI_DLG_KEIN_WERT;
    }
}
