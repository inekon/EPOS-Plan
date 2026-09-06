using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welcher der drei Modulkataloge gemeint ist (iU9-W14a.0a; die dritte
    /// Auspraegung kam mit dem Wechselrichterkatalog, W6-E-2 vom 06.09.2026).
    ///
    /// <para>Familie C der Vermessung: <c>Form_AdminStromspeicher</c> und
    /// <c>Form_AdminPV</c> sind BROWSER UND EDITOR IN EINEM — Liste links, editierbare
    /// Felder rechts, „Speichern" schreibt unmittelbar. Dieselbe Bauart, zwei sehr
    /// verschiedene Pflegezustaende: Der Stromspeicher ist die gepflegte Fassung (AP3,
    /// <c>MyResource</c>, Rueckfrage beim Loeschen, fachliche Vorgaben), die
    /// Photovoltaik die liegengebliebene.</para>
    /// </summary>
    public enum ModulKatalogArt
    {
        /// <summary><c>Tab_Stromspeicher_STAMM</c> — Vorlaeufer <c>Form_AdminStromspeicher</c>.</summary>
        Stromspeicher,

        /// <summary><c>Tab_PV_STAMM</c> — Vorlaeufer <c>Form_AdminPV</c>.</summary>
        Photovoltaik,

        /// <summary>
        /// <c>Tab_Wechselrichter_STAMM</c> — <b>ohne Vorlaeufer</b>: Der
        /// Wechselrichter war die einzige Geraetefamilie ohne Katalog (Konzept
        /// Wechselrichter 1.4, Anwenderentscheid W6-E-2 vom 06.09.2026, Stufe S1.4).
        /// Die dritte Auspraegung ist damit die erste, die nicht eine WinForms-Maske
        /// abloest, sondern eine Luecke schliesst.
        /// </summary>
        Wechselrichter
    }

    /// <summary>
    /// Ein Eingabefeld des Modulkatalogs.
    /// </summary>
    /// <remarks>
    /// <para><b><see cref="LeerErlaubt"/> ist BITGLEICH aus dem Bestand uebernommen</b>
    /// und keine Vereinheitlichung: Bei der Photovoltaik sind NEUN von zehn Zahlfeldern
    /// leer erlaubt, allein <c>textBox_Leistung</c> nicht („ein leeres Feld meldete
    /// bisher schon beim Verlassen", <c>Form_AdminPV.cs:72-74</c>). Beim Stromspeicher
    /// ist es umgekehrt: KEINES der fuenf Bestandsfelder darf leer sein, ALLE SECHS
    /// AP3-Gerätefelder duerfen es („Leer ist hier ERLAUBT und heisst nicht gepflegt",
    /// <c>Form_AdminStromspeicher.cs:117-119</c>). Der Katalog enthaelt Altdatensaetze,
    /// die diese Groessen nie hatten, und ein Pflichtfeld wuerde deren Bearbeitung
    /// sperren.</para>
    /// </remarks>
    public sealed class ModulKatalogFeld
    {
        public ModulKatalogFeld(string schluessel, string bezeichnung, string einheit = "",
                                BrowserFeldArt art = BrowserFeldArt.Zahl,
                                bool leerErlaubt = true, int gruppe = 0,
                                bool gesperrt = false, string vorgabe = "0",
                                IReadOnlyList<(string Wert, string Text)> optionen = null)
        {
            Optionen = optionen ?? Array.Empty<(string, string)>();
            Schluessel = schluessel;
            Bezeichnung = bezeichnung;
            Einheit = einheit ?? "";
            Art = art;
            LeerErlaubt = leerErlaubt;
            Gruppe = gruppe;
            Gesperrt = gesperrt;
            Vorgabe = vorgabe ?? "";
        }

        /// <summary>Sprachneutraler ASCII-Schluessel — zugleich der Zugriff auf den Wert.</summary>
        public string Schluessel { get; }

        /// <summary>Beschriftung, bereits uebersetzt.</summary>
        public string Bezeichnung { get; }

        /// <summary>Einheit hinter dem Feld, sprachneutral.</summary>
        public string Einheit { get; }

        /// <summary>Textfeld, Zahlenfeld oder Ganzzahlfeld.</summary>
        public BrowserFeldArt Art { get; }

        /// <summary>Darf das Feld beim Speichern leer sein? Siehe Klassenkommentar.</summary>
        public bool LeerErlaubt { get; }

        /// <summary>0 = Bestandsfelder, 1 = AP3-Gerätetechnik (nur Stromspeicher).</summary>
        public int Gruppe { get; }

        /// <summary>Nur lesbar — beim Bezeichner, dem Schluessel des UPDATE.</summary>
        public bool Gesperrt { get; }

        /// <summary>
        /// Vorbelegung nach „Neu…", als TEXT und in der Reihenfolge des Vorlaeufers.
        /// Leere Zeichenkette heisst „leer".
        /// </summary>
        public string Vorgabe { get; }

        /// <summary>Der Feldname, den eine Pruefmeldung nennt — die Beschriftung ohne „:".</summary>
        public string Feldname => (Bezeichnung ?? "").TrimEnd(' ', ':');

        /// <summary>
        /// Die Optionen eines <see cref="BrowserFeldArt.Auswahl"/>-Feldes: Wert = der Code in
        /// der Datenbank (leer = NULL), Text = die uebersetzte Beschriftung. Leer bei allen
        /// anderen Feldarten.
        /// </summary>
        public IReadOnlyList<(string Wert, string Text)> Optionen { get; }
    }

    /// <summary>
    /// <b>Die Auspraegung eines Modulkatalogs</b> (iU9-W14a.0a) — alles, worin sich
    /// <c>Form_AdminStromspeicher</c> und <c>Form_AdminPV</c> unterscheiden, als DATEN.
    ///
    /// <para>Zwilling zu <see cref="KatalogBrowserProfil"/>, dieselbe Bauart und
    /// dieselbe Begruendung: Der Bauplan gibt es einmal, die Werte je Katalog.</para>
    ///
    /// <para><b>Der AP3-Layoutblock entfaellt ersatzlos.</b>
    /// <c>Form_AdminStromspeicher</c> baut sechs Steuerelemente ZUR LAUFZEIT mit
    /// gerechneten Koordinaten auf (Z. 391-461) und vergroessert dabei das Fenster von
    /// 614 auf 1 036 px (Befund W14-B43). In Razor ist eine zweite Feldspalte CSS; die
    /// sechs Felder stehen hier als <see cref="ModulKatalogFeld.Gruppe"/> 1.</para>
    /// </summary>
    public sealed class ModulKatalogProfil
    {
        /// <summary>Welche der beiden Auspraegungen.</summary>
        public ModulKatalogArt Art { get; private set; }

        /// <summary>Die Stammtabelle.</summary>
        public string Stammtabelle { get; private set; }

        /// <summary>Fenstertitel, bereits uebersetzt.</summary>
        public string Titel { get; private set; }

        /// <summary>Beschriftung ueber der Liste, bereits uebersetzt.</summary>
        public string Listenbeschriftung { get; private set; }

        /// <summary>Ueberschrift der ersten Feldgruppe, bereits uebersetzt.</summary>
        public string GruppeBestand { get; private set; }

        /// <summary>
        /// Ueberschrift der zweiten Feldgruppe (AP3-Gerätetechnik); leer, wenn es sie
        /// nicht gibt.
        /// </summary>
        public string GruppeZwei { get; private set; }

        /// <summary>
        /// Ueberschrift der DRITTEN Feldgruppe; leer, wenn es sie nicht gibt.
        /// </summary>
        /// <remarks>
        /// Sie kam mit dem Wechselrichterkatalog (W6-E-2, Konzept 6): Seine Felder
        /// zerfallen fachlich in drei Bloecke - Geraet, Eingang und Wirkungsgrad -,
        /// und ein Block mit zwanzig Feldern waere nicht lesbar. Die zwei aelteren
        /// Auspraegungen lassen sie leer.
        /// </remarks>
        public string GruppeDrei { get; private set; }

        /// <summary>Die Eingabefelder in der Reihenfolge der Maske (13 / 13 / 21).</summary>
        public IReadOnlyList<ModulKatalogFeld> Felder { get; private set; }

        /// <summary>Meldung, wenn ein Knopf ohne Auswahl gedrueckt wird.</summary>
        public string MeldungOhneAuswahl { get; private set; }

        /// <summary>
        /// Der Schluessel des Infoknopfs — die ZEILE LINKS in <c>help_mapping.txt</c>
        /// (<c>Form_X.btn_Help</c>), nicht das Ziel rechts.
        /// </summary>
        public string HilfeSchluessel { get; private set; }

        /// <summary>Fuehrt der Katalog einen Herstellerfilter? Nur die Photovoltaik.</summary>
        public bool HatHerstellerfilter { get; private set; }

        /// <summary>Beschriftung des Herstellerfilters, bereits uebersetzt.</summary>
        public string FilterBezeichnung { get; private set; }

        // ==================================================================
        // Die Schluessel der Felder
        // ==================================================================

        public const string FeldBezeichner = "BEZEICHNER";
        public const string FeldTyp = "TYP";
        public const string FeldEnergie = "ENERGIE";
        public const string FeldLeistung = "LEISTUNG";
        public const string FeldDegradation = "DEGRADATION";
        public const string FeldLadezustand = "LADEZUSTAND";
        public const string FeldModulkosten = "MODULKOSTEN";
        public const string FeldWirkungsgradRt = "WIRKUNGSGRAD_RT";
        public const string FeldZyklen = "ZYKLEN";
        public const string FeldVerschleisskosten = "VERSCHLEISSKOSTEN";
        public const string FeldLeistungskosten = "LEISTUNGSKOSTEN";
        public const string FeldInvestitionFix = "INVESTITION_FIX";
        public const string FeldStandby = "STANDBY";

        public const string FeldFirma = "FIRMA";
        public const string FeldBeschreibung = "BESCHREIBUNG";
        public const string FeldWirkungsgrad = "WIRKUNGSGRAD";
        public const string FeldUMpp = "U_MPP";
        public const string FeldULeerlauf = "U_LEERLAUF";
        public const string FeldIMpp = "I_MPP";
        public const string FeldIKurzschluss = "I_KURZSCHLUSS";
        public const string FeldTempKoeff = "GAMMA_PMP";
        public const string FeldLaenge = "LAENGE";
        public const string FeldBreite = "BREITE";
        /// <summary>Paket A/B des PV-Ertragsmodells (Merge 5): Zelltemperatur NOCT und Zelltechnologie.</summary>
        public const string FeldTNoct = "T_NOCT";
        public const string FeldTechnologie = "TECHNOLOGIE";

        // --- Wechselrichter (W6-E-2, Stufe S1.4). Der Schluessel ist der SPALTENNAME
        //     der Stammtabelle: Anders als bei den zwei aelteren Auspraegungen gibt es
        //     hier keinen WinForms-Vorlaeufer, dessen Feldnamen zu erben waeren - und
        //     eine zweite Schreibweise neben WechselrichterSchema waere eine zweite
        //     Wahrheit. Die Huelle bildet damit ohne Zuordnungstabelle ab.
        public const string FeldPAcNenn = WechselrichterSchema.SPALTE_P_AC_NENN;
        public const string FeldSAcMax = WechselrichterSchema.SPALTE_S_AC_MAX;
        public const string FeldPDcMax = WechselrichterSchema.SPALTE_P_DC_MAX;
        public const string FeldUMppMin = WechselrichterSchema.SPALTE_U_MPP_MIN;
        public const string FeldUMppMax = WechselrichterSchema.SPALTE_U_MPP_MAX;
        public const string FeldUDcMax = WechselrichterSchema.SPALTE_U_DC_MAX;
        public const string FeldUStart = WechselrichterSchema.SPALTE_U_START;
        public const string FeldIDcMax = WechselrichterSchema.SPALTE_I_DC_MAX;
        public const string FeldAnzahlMppt = WechselrichterSchema.SPALTE_ANZAHL_MPPT;
        public const string FeldStraengeJeMppt = WechselrichterSchema.SPALTE_STRAENGE_JE_MPPT;
        public const string FeldEta05 = WechselrichterSchema.SPALTE_ETA05;
        public const string FeldEta10 = WechselrichterSchema.SPALTE_ETA10;
        public const string FeldEta20 = WechselrichterSchema.SPALTE_ETA20;
        public const string FeldEta30 = WechselrichterSchema.SPALTE_ETA30;
        public const string FeldEta50 = WechselrichterSchema.SPALTE_ETA50;
        public const string FeldEta100 = WechselrichterSchema.SPALTE_ETA100;
        public const string FeldEtaEuro = WechselrichterSchema.SPALTE_ETA_EURO;
        public const string FeldEtaMax = WechselrichterSchema.SPALTE_ETA_MAX;
        public const string FeldPStandby = WechselrichterSchema.SPALTE_P_STANDBY;
        public const string FeldPNacht = WechselrichterSchema.SPALTE_P_NACHT;
        public const string FeldKosten = WechselrichterSchema.SPALTE_KOSTEN;
        public const string FeldHerkunft = WechselrichterSchema.SPALTE_HERKUNFT;

        // ==================================================================
        // Die zwei Auspraegungen
        // ==================================================================

        /// <summary>
        /// Die Auspraegung zu einer Katalogart. <paramref name="text"/> uebersetzt einen
        /// Beschriftungsschluessel; <c>null</c> liefert den Schluessel selbst zurueck.
        /// </summary>
        public static ModulKatalogProfil Finde(ModulKatalogArt art, Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            switch (art)
            {
                case ModulKatalogArt.Stromspeicher:
                    return new ModulKatalogProfil
                    {
                        Art = art,
                        Stammtabelle = StromspeicherStammCtrl.TABLE,
                        Titel = t("MODK_TITEL_STROMSPEICHER"),
                        Listenbeschriftung = t("MODK_LISTE_STROMSPEICHER"),
                        GruppeBestand = t("MODK_GRUPPE_SPEICHER"),
                        GruppeZwei = t("SP_GRUPPE_GERAETETECHNIK"),
                        GruppeDrei = "",
                        MeldungOhneAuswahl = t("MODK_MSG_AUSWAHL_SPEICHER"),
                        HilfeSchluessel = "Form_AdminStromspeicher.btn_Help",
                        HatHerstellerfilter = false,
                        FilterBezeichnung = "",
                        Felder = new[]
                        {
                            new ModulKatalogFeld(FeldBezeichner, t("MODK_LBL_BEZEICHNER"), "",
                                                 BrowserFeldArt.Text, true, 0, gesperrt: true, vorgabe: ""),
                            new ModulKatalogFeld(FeldTyp, t("MODK_LBL_TYP"), "",
                                                 BrowserFeldArt.Text, false, 0, false,
                                                 // W14a.0f: der Persistenzwert steht jetzt in DbWerte.
                                                 vorgabe: DbWerte.SP_TYP_LITHIUM_IONEN),

                            // Die drei Einheiten sind die BERICHTIGTEN (Abnahmebefund 1 zum
                            // ersten App-Start, AP0-Entscheid 16.08.2026): kWh statt kW an
                            // der Kapazitaet, €/kWh statt € an den Modulkosten. Der
                            // Vorlaeufer schrieb sie zur Laufzeit ueber die Designer-Werte
                            // (Befund W14-B40); hier stehen sie gleich richtig.
                            new ModulKatalogFeld(FeldEnergie, t("SP_LABEL_ENERGIE_KURZ"), "kWh",
                                                 BrowserFeldArt.Zahl, leerErlaubt: false),
                            new ModulKatalogFeld(FeldLeistung, t("MODK_LBL_LEISTUNG"), "kW",
                                                 BrowserFeldArt.Zahl, leerErlaubt: false),
                            new ModulKatalogFeld(FeldLadezustand, t("MODK_LBL_LADEZUSTAND"), "%",
                                                 BrowserFeldArt.Zahl, leerErlaubt: false),
                            new ModulKatalogFeld(FeldDegradation, t("MODK_LBL_DEGRADATION"), "%",
                                                 BrowserFeldArt.Zahl, leerErlaubt: false),
                            new ModulKatalogFeld(FeldModulkosten, t("MODK_LBL_MODULKOSTEN"), "€/kWh",
                                                 BrowserFeldArt.Zahl, leerErlaubt: false),

                            // AP3-Gerätetechnik (Fachkonzept Stromspeicher 5.1) - alle sechs
                            // duerfen leer bleiben und heissen dann „nicht gepflegt" (0).
                            new ModulKatalogFeld(FeldWirkungsgradRt, t("SP_LABEL_WIRKUNGSGRAD_RT"), "-",
                                                 BrowserFeldArt.Zahl, true, 1, false,
                                                 Zahl(StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE)),
                            new ModulKatalogFeld(FeldZyklen, t("SP_LABEL_ZYKLEN"), "-",
                                                 BrowserFeldArt.Ganzzahl, true, 1),
                            new ModulKatalogFeld(FeldVerschleisskosten, t("SP_LABEL_VERSCHLEISSKOSTEN"),
                                                 t("SP_EINHEIT_ZYKLUSKOSTEN"),
                                                 BrowserFeldArt.Zahl, true, 1, false,
                                                 Zahl(StromspeicherModel.C_VER_VORGABE)),
                            new ModulKatalogFeld(FeldLeistungskosten, t("SP_LABEL_LEISTUNGSKOSTEN"), "€/kW",
                                                 BrowserFeldArt.Zahl, true, 1),
                            new ModulKatalogFeld(FeldInvestitionFix, t("SP_LABEL_INVESTITION_FIX"), "€",
                                                 BrowserFeldArt.Zahl, true, 1),
                            new ModulKatalogFeld(FeldStandby, t("SP_LABEL_STANDBY"), "W",
                                                 BrowserFeldArt.Zahl, true, 1)
                        }
                    };

                case ModulKatalogArt.Photovoltaik:
                    return new ModulKatalogProfil
                    {
                        Art = art,
                        Stammtabelle = PhotovoltaikStammCtrl.TABLE,
                        Titel = t("MODK_TITEL_PV"),
                        Listenbeschriftung = t("MODK_LISTE_PV"),
                        GruppeBestand = t("MODK_GRUPPE_PV"),
                        GruppeZwei = "",
                        GruppeDrei = "",
                        MeldungOhneAuswahl = t("MODK_MSG_AUSWAHL_MODUL"),
                        HilfeSchluessel = "Form_AdminPV.btn_Help",
                        HatHerstellerfilter = false,
                        FilterBezeichnung = "",
                        Felder = new[]
                        {
                            new ModulKatalogFeld(FeldBezeichner, t("MODK_LBL_BEZEICHNER_PV"), "",
                                                 BrowserFeldArt.Text, true, 0, gesperrt: true, vorgabe: ""),
                            new ModulKatalogFeld(FeldFirma, t("MODK_LBL_FIRMA"), "",
                                                 BrowserFeldArt.Text, true, 0, false, vorgabe: ""),
                            new ModulKatalogFeld(FeldBeschreibung, t("MODK_LBL_BESCHREIBUNG"), "",
                                                 BrowserFeldArt.Mehrzeilig, true, 0, false, vorgabe: ""),

                            // BITGLEICH: neun von zehn Zahlfeldern duerfen leer sein, die
                            // Nennleistung nicht (Form_AdminPV.cs:72-75).
                            new ModulKatalogFeld(FeldLeistung, t("MODK_LBL_PMAX"), "W",
                                                 BrowserFeldArt.Zahl, leerErlaubt: false),
                            new ModulKatalogFeld(FeldWirkungsgrad, t("MODK_LBL_WIRKUNGSGRAD"), "%"),
                            new ModulKatalogFeld(FeldUMpp, t("MODK_LBL_UMPP"), "V"),
                            new ModulKatalogFeld(FeldULeerlauf, t("MODK_LBL_ULEERLAUF"), "V"),
                            new ModulKatalogFeld(FeldIMpp, t("MODK_LBL_IMPP"), "A"),
                            new ModulKatalogFeld(FeldIKurzschluss, t("MODK_LBL_IKURZSCHLUSS"), "A"),
                            new ModulKatalogFeld(FeldTempKoeff, t("MODK_LBL_TEMPKOEFF"), "%/K"),
                            new ModulKatalogFeld(FeldLaenge, t("MODK_LBL_LAENGE"), "m"),
                            new ModulKatalogFeld(FeldBreite, t("MODK_LBL_BREITE"), "m"),
                            new ModulKatalogFeld(FeldModulkosten, t("MODK_LBL_MODULKOSTEN_PV"), "€"),
                            // PAKET A/B des PV-Ertragsmodells (mit Merge 5 aus Form_AdminPV
                            // nachgezogen): die NOCT-Zelltemperatur (leer = 0 = nicht gepflegt)
                            // und die Zelltechnologie als Auswahl (leer = NULL).
                            new ModulKatalogFeld(FeldTNoct, t("PV_MODUL_LABEL_TNOCT"), "°C"),
                            new ModulKatalogFeld(FeldTechnologie, t("PVM_MODUL_LABEL_TECHNOLOGIE"), "",
                                                 BrowserFeldArt.Auswahl, true, 0, false, vorgabe: "",
                                                 optionen: Technologien(t))
                        }
                    };

                case ModulKatalogArt.Wechselrichter:
                    return new ModulKatalogProfil
                    {
                        Art = art,
                        Stammtabelle = SchemaKatalog.TAB_WECHSELRICHTER_STAMM,
                        Titel = t("WRK_TITEL_VERWALTUNG"),
                        Listenbeschriftung = t("WRK_LISTE"),
                        GruppeBestand = t("WRK_GRUPPE_GERAET"),
                        GruppeZwei = t("WRK_GRUPPE_EINGANG"),
                        GruppeDrei = t("WRK_GRUPPE_WIRKUNGSGRAD"),
                        MeldungOhneAuswahl = t("WRK_MSG_AUSWAHL"),
                        HilfeSchluessel = "Form_AdminWechselrichter.btn_Help",
                        // ALS EINZIGE der drei Auspraegungen mit Herstellerfilter
                        // (Konzept 6): Die CEC-Liste bringt ueber zweitausend Geraete
                        // von 152 Herstellern; ohne Einengung ist die Liste nicht
                        // bedienbar.
                        HatHerstellerfilter = true,
                        FilterBezeichnung = t("WRK_LBL_FIRMA"),
                        Felder = new[]
                        {
                            // --- Gruppe 0: Geraet ---------------------------------
                            new ModulKatalogFeld(FeldBezeichner, t("WRK_LBL_BEZEICHNER"), "",
                                                 BrowserFeldArt.Text, true, 0, gesperrt: true, vorgabe: ""),
                            new ModulKatalogFeld(FeldFirma, t("WRK_LBL_FIRMA"), "",
                                                 BrowserFeldArt.Text, true, 0, false, vorgabe: ""),
                            new ModulKatalogFeld(FeldBeschreibung, t("WRK_LBL_BESCHREIBUNG"), "",
                                                 BrowserFeldArt.Mehrzeilig, true, 0, false, vorgabe: ""),
                            // Das EINE Pflichtfeld - wie bei der Photovoltaik allein
                            // die Nennleistung (Konzept 6).
                            new ModulKatalogFeld(FeldPAcNenn, t("WRK_LBL_P_AC_NENN"), "kW",
                                                 BrowserFeldArt.Zahl, leerErlaubt: false),
                            new ModulKatalogFeld(FeldSAcMax, t("WRK_LBL_S_AC_MAX"), "kVA"),
                            new ModulKatalogFeld(FeldPDcMax, t("WRK_LBL_P_DC_MAX"), "kW"),
                            new ModulKatalogFeld(FeldKosten, t("WRK_LBL_KOSTEN"), "€"),
                            // Woher der Satz stammt - Auskunft, nicht Eingabe: Der
                            // Import setzt sie, die Handpflege bekommt "HAND".
                            new ModulKatalogFeld(FeldHerkunft, t("WRK_LBL_HERKUNFT"), "",
                                                 BrowserFeldArt.Text, true, 0, gesperrt: true,
                                                 vorgabe: DbWerte.WR_HERKUNFT_HAND),

                            // --- Gruppe 1: Eingang --------------------------------
                            new ModulKatalogFeld(FeldUMppMin, t("WRK_LBL_U_MPP_MIN"), "V",
                                                 BrowserFeldArt.Zahl, true, 1),
                            new ModulKatalogFeld(FeldUMppMax, t("WRK_LBL_U_MPP_MAX"), "V",
                                                 BrowserFeldArt.Zahl, true, 1),
                            new ModulKatalogFeld(FeldUDcMax, t("WRK_LBL_U_DC_MAX"), "V",
                                                 BrowserFeldArt.Zahl, true, 1),
                            new ModulKatalogFeld(FeldUStart, t("WRK_LBL_U_START"), "V",
                                                 BrowserFeldArt.Zahl, true, 1),
                            // JE MPPT, nicht je Geraet - so fuehrt es die CEC-Liste,
                            // und so braucht es die Auslegungspruefung P4.
                            new ModulKatalogFeld(FeldIDcMax, t("WRK_LBL_I_DC_MAX"), "A",
                                                 BrowserFeldArt.Zahl, true, 1),
                            new ModulKatalogFeld(FeldAnzahlMppt, t("WRK_LBL_ANZAHL_MPPT"), "",
                                                 BrowserFeldArt.Ganzzahl, true, 1),
                            new ModulKatalogFeld(FeldStraengeJeMppt, t("WRK_LBL_STRAENGE_JE_MPPT"), "",
                                                 BrowserFeldArt.Ganzzahl, true, 1),

                            // --- Gruppe 2: Wirkungsgrad ---------------------------
                            // Faktoren 0…1, nicht Prozent - so, wie sie
                            // PvErweitertesModell.EtaWechselrichter und die drei
                            // Anlagenspalten PV_WrEta10/50/100 fuehren.
                            new ModulKatalogFeld(FeldEta05, t("WRK_LBL_ETA05"), "-",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldEta10, t("WRK_LBL_ETA10"), "-",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldEta20, t("WRK_LBL_ETA20"), "-",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldEta30, t("WRK_LBL_ETA30"), "-",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldEta50, t("WRK_LBL_ETA50"), "-",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldEta100, t("WRK_LBL_ETA100"), "-",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldEtaEuro, t("WRK_LBL_ETA_EURO"), "-",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldEtaMax, t("WRK_LBL_ETA_MAX"), "-",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldPStandby, t("WRK_LBL_P_STANDBY"), "W",
                                                 BrowserFeldArt.Zahl, true, 2),
                            new ModulKatalogFeld(FeldPNacht, t("WRK_LBL_P_NACHT"), "W",
                                                 BrowserFeldArt.Zahl, true, 2)
                        }
                    };
            }

            throw new ArgumentOutOfRangeException(nameof(art));
        }

        /// <summary>
        /// Die Optionen der Zelltechnologie (Paket B, Stufe E2.3): der Datenbankcode aus
        /// <see cref="DbWerte"/> und die uebersetzte Beschriftung; der erste Eintrag ist
        /// "nicht gepflegt" (leer = NULL). Reihenfolge wie in Form_AdminPV.TECHNOLOGIE_WERTE.
        /// </summary>
        public static IReadOnlyList<(string Wert, string Text)> Technologien(Func<string, string> t)
        {
            return new[]
            {
                ("", t("PVM_TECHNOLOGIE_LEER")),
                (DbWerte.PV_TECHNOLOGIE_C_SI, t("PVM_TECHNOLOGIE_C_SI")),
                (DbWerte.PV_TECHNOLOGIE_CIS, t("PVM_TECHNOLOGIE_CIS")),
                (DbWerte.PV_TECHNOLOGIE_CDTE, t("PVM_TECHNOLOGIE_CDTE")),
                (DbWerte.PV_TECHNOLOGIE_A_SI, t("PVM_TECHNOLOGIE_A_SI")),
                (DbWerte.PV_TECHNOLOGIE_SONSTIGE, t("PVM_TECHNOLOGIE_SONSTIGE"))
            };
        }

        /// <summary>Beide Auspraegungen — fuer Stapelpruefungen.</summary>
        public static IEnumerable<ModulKatalogArt> AlleArten
        {
            get
            {
                yield return ModulKatalogArt.Stromspeicher;
                yield return ModulKatalogArt.Photovoltaik;
                yield return ModulKatalogArt.Wechselrichter;
            }
        }

        /// <summary>
        /// Vorbelegung als Text in der Kultur des Anwenders — damit die Zahl so aussieht
        /// wie eine selbst getippte (Fachkonzept 8.5: UI in CurrentCulture, Datei und
        /// Datenbank invariant; woertlich <c>Form_AdminStromspeicher.ZahlAnzeigen</c>).
        /// </summary>
        private static string Zahl(double wert)
        {
            return wert.ToString(System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
