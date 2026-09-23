// WOHIN dialog_oeffnen FUEHRT (Auftrag #201, Stufe S3, Punkt 3).
//
// Der Dialogkatalog nennt seine Masken unter ihren KATALOGSCHLUESSELN
// (Form_Heizkessel_Bearbeiten, Form_PV, Form_PufferSp_Bearbeiten, Form_WP,
// StromspeicherAuslegung, …). Dienste.Navigation kennt dagegen NAVIGATIONSSCHLUESSEL
// (Masken.* bzw. die Seitenschluessel der AppWurzel). Beide Namensraeume gibt es aus
// gutem Grund - der eine steht im Werkzeugvertrag des Modells und im Protokoll, der
// andere in der Navigationstabelle der jeweiligen Huelle -, und zwischen ihnen fehlte
// bis hierher die Zuordnung.
//
// SIE IST DATEN UND KEINE LOGIK. Eine Tabelle, kein switch mit Sonderfaellen: Ein
// Waechter (EPOS.Kern.Tests) haelt sie gegen den Katalog und verlangt, dass JEDER
// Katalogeintrag ein Ziel hat. Waechst dem Katalog eine sechste Maske zu, faellt die
// fehlende Zeile im Test auf und nicht beim Anwender.
//
// WARUM DIE ZIELE DIE VERWALTUNGSMASKEN SIND. Drei der vier Katalogmasken sind EDITOREN,
// die aus einer Liste heraus aufgehen (Heizkessel, Pufferspeicher, Photovoltaik) - sie
// brauchen einen gewaehlten Satz und lassen sich nicht kontextfrei oeffnen (dieselbe
// Begruendung, aus der maske_oeffnen nie ins Register kam, KiAktionen). Der Assistent
// fuehrt den Anwender deshalb DORTHIN, wo er den Satz waehlt; das ist der Weg, den er
// auch von Hand ginge.

using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zuordnung Katalogmaske → Navigationsschluessel und ARGUMENT fuer
    /// <c>dialog_oeffnen</c> — zwei Datenspalten, <see cref="Ziel"/> und
    /// <see cref="Argument"/>.
    /// </summary>
    public static class KiMaskenziele
    {
        /// <summary>
        /// Der Seitenschluessel der Stromspeicher-Ansicht.
        /// </summary>
        /// <remarks>
        /// <b>Warum die Zeichenkette und nicht <c>Seitenschluessel.StromspeicherAuslegung</c>.</b>
        /// Jene Konstante steht in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht
        /// (die Abhaengigkeit laeuft in die andere Richtung). Die AppWurzel setzt dieselbe
        /// Zeichenkette; ein Waechter in <c>EPOS.UI.Tests</c> haelt beide gegeneinander,
        /// damit aus zwei Fundstellen nicht zwei Wahrheiten werden — dasselbe Muster, mit
        /// dem <c>Masken.KiAssistent</c> und <c>Seitenschluessel.KiAssistent</c> seit #199
        /// zusammengehalten werden.
        /// </remarks>
        public const string STROMSPEICHER_AUSLEGUNG = "STROMSPEICHER_AUSLEGUNG";

        /// <summary>
        /// Der Seitenschluessel der Kostenverwaltung.
        /// </summary>
        /// <remarks>
        /// <b>Der Weg ist der des Menues</b> („Administration → Kosten →
        /// Kostenverwaltung…"). Unter Windows reicht <c>WinFormsNavigation.OeffneMaske</c>
        /// den Schluessel an den Ablauf der Hauptfensterhuelle weiter, und der oeffnet
        /// dieselbe Maske wie der Menuepunkt — ein Weg, eine Wahrheit. Die Kostenzeilen
        /// einer KOMPONENTE waehlt der Anwender dann dort; das ist der Weg, den er auch
        /// von Hand ginge. Auf iOS ist die Kostenverwaltung nicht angebunden, die
        /// <c>AppWurzel</c> antwortet <c>false</c>, und <c>dialog_oeffnen</c> lehnt
        /// benannt ab. LESEN und SETZEN erreichen die Maske ohnehin, sobald der Anwender
        /// sie offen hat: Dafuer zaehlt die Anmeldung an der Maskenbruecke, nicht dieses
        /// Ziel.
        /// </remarks>
        public const string KOSTENVERWALTUNG = "KOSTENVERWALTUNG";

        /// <summary>
        /// Der Seitenschluessel der STARTSEITE — das Ziel der Erzeugermasken des
        /// Projekts (Welle KI‑F1).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum die Startseite und nicht die Maske selbst.</b> Die sechs
        /// Projektmasken gehen aus der Erzeugerkarte der Startseite auf (Reiter
        /// „Energieerzeuger") und brauchen eine gewaehlte Anlage; kontextfrei lassen
        /// sie sich nicht oeffnen — dieselbe Lage wie bei den Katalogeditoren, deren
        /// Ziel die Verwaltung ist. Der Assistent fuehrt den Anwender deshalb dorthin,
        /// wo er die Anlage waehlt: auf die Startseite. Das ist der Weg, den er auch
        /// von Hand ginge.
        /// </para>
        /// <para>
        /// <b>Der REITER geht als Argument mit</b> (Anwenderentscheid KI‑D‑Q8). Die
        /// Zuordnung steht in <see cref="ARGUMENTE"/> und ist DATEN wie die Zieltabelle
        /// selbst: Die sechs Erzeugermasken nennen <see cref="REITER_ERZEUGER"/>, die
        /// Gebaeude- und Ganglinienmasken ihren eigenen Reiter. <c>dialog_oeffnen</c>
        /// reicht ihn an <c>Dienste.Navigation.OeffneMaske(ziel, argument)</c> durch,
        /// und die <c>AppWurzel</c> verbraucht ihn als Reiterwunsch — derselbe Weg, den
        /// ihr Rueckweg „Projekt angelegt" schon ging. Wo die Startseite keinen eigenen
        /// Reiter fuehrt (die Bedarfsprofile stehen auf zweien), geht kein Argument mit.
        /// </para>
        /// <para>
        /// <b>Warum die Zeichenkette und nicht <c>Seitenschluessel.Startseite</c>:</b>
        /// dieselbe Begruendung wie bei <see cref="STROMSPEICHER_AUSLEGUNG"/> — jene
        /// Konstante steht in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht.
        /// Ein Waechter in <c>EPOS.UI.Tests</c> haelt beide gegeneinander. Unter
        /// Windows reicht <c>WinFormsNavigation.OeffneMaske</c> den Schluessel samt
        /// Reiter an die gezeichnete Wurzel weiter; auf iOS tut die Wurzel dasselbe.
        /// </para>
        /// </remarks>
        public const string STARTSEITE = "STARTSEITE";

        /// <summary>
        /// Der Seitenschluessel der KLIMADATENVERWALTUNG (Welle KI‑F3).
        /// </summary>
        /// <remarks>
        /// <b>Warum die Zeichenkette und nicht <c>Seitenschluessel.Klimadaten</c>:</b>
        /// dieselbe Begruendung wie bei <see cref="STARTSEITE"/> — jene Konstante steht
        /// in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht. Dieser
        /// Schluessel hat als einziger der Welle KEINEN <c>Masken.*</c>-Zwilling: Die
        /// Klimadaten haengen am Menuepunkt „Administration → Klimadaten", und unter
        /// Windows reicht <c>WinFormsNavigation.OeffneMaske</c> ihn an den Ablauf der
        /// Hauptfensterhuelle weiter, der denselben Menueweg geht. Auf iOS zeigt die
        /// <c>AppWurzel</c> denselben Dialog als Ansicht — ihre Datenseite liegt
        /// plattformfrei in <c>EPOS.UI.Daten</c>. Ein Waechter in
        /// <c>EPOS.UI.Tests</c> haelt beide Fundstellen gegeneinander.
        /// </remarks>
        public const string KLIMADATEN = "KLIMADATEN";

        /// <summary>
        /// Der Seitenschluessel der ENERGIETRAEGERVERWALTUNG (Welle KI‑F4).
        /// </summary>
        /// <remarks>
        /// <b>Warum die Zeichenkette und nicht <c>Seitenschluessel.EnergietraegerVerwaltung</c>:</b>
        /// dieselbe Begruendung wie bei <see cref="KLIMADATEN"/> — jene Konstante steht
        /// in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht. Der Schluessel
        /// gehoert zum Menuepunkt „Administration → Kosten → Energietraegerverwaltung…";
        /// unter Windows reicht <c>WinFormsNavigation.OeffneMaske</c> ihn an den Ablauf
        /// der Hauptfensterhuelle weiter (<c>HauptfensterHuelle.Ablauf</c>), der denselben
        /// Menueweg geht — genau wie bei der Kostenverwaltung. Auf iOS ist die
        /// Kostenverwaltung insgesamt nicht angebunden; die <c>AppWurzel</c>
        /// meldet <c>false</c>, und <c>dialog_oeffnen</c> lehnt benannt ab. LESEN und
        /// SETZEN erreichen die Maske trotzdem, sobald der Anwender sie offen hat:
        /// Dafuer zaehlt die Anmeldung an der Maskenbruecke, nicht dieses Ziel.
        /// </remarks>
        public const string ENERGIETRAEGER_VERWALTUNG = "ENERGIETRAEGER_VERWALTUNG";

        /// <summary>
        /// Der Seitenschluessel des Dialogs „Energietraeger-Variante anlegen"
        /// (Welle KI‑F4).
        /// </summary>
        /// <remarks>
        /// Er ist einer der wenigen dieser Welle, den die <c>AppWurzel</c> WIRKLICH
        /// bedient (<c>Seitenschluessel.Energietraeger</c>): Auf iOS geht der Dialog
        /// damit auf. Unter Windows kennt <c>WinFormsNavigation</c> ihn nicht — dort
        /// steht er als Ueberlagerung in den Erzeugerdialogen, und
        /// <c>dialog_oeffnen</c> lehnt benannt ab.
        /// </remarks>
        public const string VARIANTE_ANLEGEN = "ENERGIETRAEGER_VARIANTE";

        /// <summary>
        /// Der Seitenschluessel der NUTZUNGSDAUERVERWALTUNG (Welle KI‑F4).
        /// </summary>
        /// <remarks>
        /// Der dritte Punkt der Rubrik „Administration → Kosten"; dieselbe Lage wie
        /// bei <see cref="ENERGIETRAEGER_VERWALTUNG"/> — unter Windows geht der Weg
        /// ueber den Ablauf der Hauptfensterhuelle auf, auf iOS ist die
        /// Kostenverwaltung nicht angebunden, und <c>dialog_oeffnen</c> lehnt dort
        /// benannt ab.
        /// </remarks>
        public const string NUTZUNGSDAUER_VERWALTUNG = "NUTZUNGSDAUER_VERWALTUNG";

        /// <summary>
        /// Der Seitenschluessel des Dialogs „BHKW-Wirtschaftlichkeit" (Welle KI‑F4).
        /// </summary>
        /// <remarks>
        /// Wie <see cref="VARIANTE_ANLEGEN"/> einer der wenigen Schluessel dieser Welle,
        /// den die <c>AppWurzel</c> WIRKLICH bedient
        /// (<c>Seitenschluessel.BhkwWirtschaftlichkeit</c>): Auf iOS geht der Dialog
        /// damit auf. Unter Windows kennt <c>WinFormsNavigation</c> ihn nicht — dort
        /// steht er als Ueberlagerung der Wirtschaftlichkeitsseite und in einem eigenen
        /// Fenster, und <c>dialog_oeffnen</c> lehnt benannt ab.
        /// </remarks>
        public const string BHKW_WIRTSCHAFTLICHKEIT = "BHKW_WIRTSCHAFTLICHKEIT";

        /// <summary>
        /// Der Seitenschluessel der GESETZLICHEN PARAMETER (Welle KI‑F4).
        /// </summary>
        /// <remarks>
        /// Menuepunkt „Administration → Gesetzliche Parameter"; dieselbe Lage wie bei
        /// <see cref="ENERGIETRAEGER_VERWALTUNG"/> — unter Windows reicht
        /// <c>WinFormsNavigation.OeffneMaske</c> ihn an den Ablauf der
        /// Hauptfensterhuelle weiter, auf iOS antwortet die <c>AppWurzel</c>
        /// <c>false</c>.
        /// </remarks>
        public const string GESETZESKATALOG = "GESETZESKATALOG";

        /// <summary>
        /// Der Seitenschluessel des Dialogs „Als Variante speichern" (Welle KI‑F6).
        /// </summary>
        /// <remarks>
        /// <b>Warum die Zeichenkette und nicht <c>Seitenschluessel.ProjektAlsVariante</c>:</b>
        /// dieselbe Begruendung wie bei <see cref="KLIMADATEN"/> — jene Konstante steht
        /// in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht. Der Schluessel
        /// gehoert zum Menuepunkt „Projekt → Als Variante speichern…"; unter Windows
        /// reicht <c>WinFormsNavigation.OeffneMaske</c> ihn an den Ablauf der
        /// Hauptfensterhuelle weiter (<c>HauptfensterHuelle.Ablauf</c>), der denselben
        /// Menueweg geht; auf iOS zeigt die <c>AppWurzel</c> denselben Dialog als
        /// Ansicht. Die Maske gilt dem AKTIVEN Projekt — den Stamm bestimmt der Weg
        /// selbst, denn eine Variante haengt immer am Stamm. LESEN und SETZEN
        /// erreichen sie ohnehin, sobald der Anwender sie offen hat: Dafuer zaehlt die
        /// Anmeldung an der Maskenbruecke, nicht dieses Ziel.
        /// </remarks>
        public const string PROJEKT_VARIANTE = "PROJEKT_ALS_VARIANTE";

        /// <summary>
        /// Der Seitenschluessel der PROGRAMMEINSTELLUNGEN (Welle #458, Stufe 2).
        /// </summary>
        /// <remarks>
        /// Derselbe Menueweg wie bei <see cref="KLIMADATEN"/>: Unter Windows reicht
        /// <c>WinFormsNavigation.OeffneMaske</c> ihn an den Ablauf der
        /// Hauptfensterhuelle weiter, der das Fenster des Menuepunkts oeffnet. Auf iOS
        /// fuehrt die <c>AppWurzel</c> die Einstellungen nicht; <c>dialog_oeffnen</c>
        /// lehnt dort benannt ab. Ein Waechter in <c>EPOS.UI.Tests</c> haelt die
        /// Zeichenkette gegen <c>Seitenschluessel.Einstellungen</c>.
        /// </remarks>
        public const string EINSTELLUNGEN = "EINSTELLUNGEN";

        // =================================================================
        //  Die ARGUMENTE - zweite Datenspalte neben dem Ziel (KI-D-Q8)
        // =================================================================

        /// <summary>
        /// Der Reiter „Energieerzeuger" der Startseite — das Argument der sechs
        /// Erzeugermasken des Projekts.
        /// </summary>
        /// <remarks>
        /// <b>Warum die Zeichenkette und nicht <c>Reiterschluessel.Erzeuger</c>:</b>
        /// dieselbe Begruendung wie bei <see cref="STARTSEITE"/> — jene Konstante steht
        /// in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht. Ein Waechter in
        /// <c>EPOS.UI.Tests</c> haelt beide Fundstellen gegeneinander.
        /// </remarks>
        public const string REITER_ERZEUGER = "ERZEUGER";

        /// <summary>
        /// Der Reiter „Wärmebedarf" der Startseite; dieselbe Lage wie bei
        /// <see cref="REITER_ERZEUGER"/>.
        /// </summary>
        public const string REITER_WAERMEBEDARF = "WAERMEBEDARF";

        /// <summary>
        /// Das Blatt „Übersicht" der Ansicht „Berichte und Kosten".
        /// </summary>
        /// <remarks>
        /// Dieselbe Lage wie bei <see cref="REITER_ERZEUGER"/>: Die vier Blattschluessel
        /// stehen in <c>EPOS.UI</c> (<c>BerichteKostenSeite.SEITE_*</c>), und ein
        /// Waechter in <c>EPOS.UI.Tests</c> haelt sie gegen diese vier Zeichenketten.
        /// </remarks>
        public const string BLATT_UEBERSICHT = "UEBERSICHT";

        /// <summary>Das Blatt „Kosten" der Ansicht „Berichte und Kosten".</summary>
        public const string BLATT_KOSTEN = "KOSTEN";

        /// <summary>Das Blatt „Wirtschaftlichkeit" der Ansicht „Berichte und Kosten".</summary>
        public const string BLATT_WIRTSCHAFT = "WIRTSCHAFT";

        /// <summary>Das Blatt „Bericht" der Ansicht „Berichte und Kosten".</summary>
        public const string BLATT_BERICHT = "BERICHT";

        private static readonly Dictionary<string, string> ZIELE =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Der Heizkesseleditor geht aus der Heizkesselverwaltung auf.
                { KiMaskennamen.HEIZKESSEL,       Masken.HeizkesselAdmin },

                // Der Photovoltaikdialog haengt an der gewaehlten Projektzeile; der
                // kontextfreie Weg ist die Modulverwaltung.
                { KiMaskennamen.PHOTOVOLTAIK,     Masken.PvAdmin },

                // Die Ueberlagerung „Anlagenwerte" geht aus dem Strangabschnitt von
                // Form_PV auf und hat keinen eigenen Weg im Menue - ihr Ziel ist
                // deshalb dasselbe wie das ihres Wirts (Welle KI-F7).
                { KiMaskennamen.PV_ANLAGENWERTE,  Masken.PvAdmin },

                // Ebenso der Pufferspeichereditor.
                { KiMaskennamen.PUFFERSPEICHER,   Masken.PufferSpAdmin },

                // Die Waermepumpenverwaltung IST die Maske des Katalogeintrags - hier
                // fallen Katalogschluessel und Navigationsschluessel zusammen.
                { KiMaskennamen.WAERMEPUMPE,      Masken.WpAdministration },

                // Die Stromspeicher-Ansicht ist eine freie ANSICHT der AppWurzel und
                // keine Maske der WinForms-Navigationstabelle. Unter Windows braucht sie
                // einen gerechneten Simulationslauf und geht deshalb ueber den Knopf der
                // Ergebnisseite auf; dort liefert OeffneMaske false, und die Aktion lehnt
                // benannt ab. Auf iOS wechselt die Wurzel die Ansicht.
                { KiMaskennamen.STROMSPEICHER_AUSLEGUNG, STROMSPEICHER_AUSLEGUNG },

                // Die Ansicht „Simulation" ist eine freie ANSICHT der AppWurzel und
                // zugleich ein Maskenschluessel der Windows-Navigationstabelle (SIM-Q3,
                // #207) - beide Wege fuehren ueber denselben Schluessel, und deshalb
                // steht hier Masken.Simulation und keine zweite Zeichenkette.
                { KiMaskennamen.SIMULATION,       Masken.Simulation },

                // Die Kostenverwaltung geht nur AUS einer gewaehlten Komponente auf;
                // siehe KOSTENVERWALTUNG.
                { KiMaskennamen.KOSTENVERWALTUNG, KOSTENVERWALTUNG },

                // Welle KI-F1: Die Erzeugermasken des PROJEKTS gehen aus der
                // Erzeugerkarte der Startseite auf und brauchen eine gewaehlte Anlage;
                // ihr Ziel ist deshalb die Startseite - siehe STARTSEITE.
                { KiMaskennamen.HEIZKESSEL_PROJEKT,     STARTSEITE },
                { KiMaskennamen.BHKW_PROJEKT,           STARTSEITE },
                { KiMaskennamen.PUFFERSPEICHER_PROJEKT, STARTSEITE },
                { KiMaskennamen.STROMSPEICHER_PROJEKT,  STARTSEITE },
                { KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT, STARTSEITE },

                // Die Waermepumpen-ANLAGE geht ebenfalls aus der Erzeugerkarte auf -
                // anders als die Stammverwaltung Form_WP, die ihre eigene Maske hat.
                { KiMaskennamen.WAERMEPUMPE_ANLAGE,     STARTSEITE },

                // Welle KI-F2: Die Masken der SIMULATIONSKONFIGURATION gehen aus
                // Schritt ① der Ansicht "Simulation" auf und brauchen eine gewaehlte
                // Komponente - die Pufferverwaltung ueber den Knopf der Speicherkarte,
                // die Quellen- und Senkenmasken ueber die Karte ihrer Waermepumpe, die
                // Komponentenkonfiguration ueber den Knopf ihrer Karte. Kontextfrei
                // laesst sich keine davon oeffnen; das Ziel ist deshalb die ANSICHT,
                // auf der der Anwender die Komponente waehlt - dieselbe Begruendung wie
                // bei den Katalogeditoren und den Erzeugermasken. Masken.Simulation
                // kennt die Windows-Navigationstabelle, und dieselbe Zeichenkette ist
                // der Seitenschluessel der AppWurzel (SIM-Q3, #207).
                { KiMaskennamen.PUFFERSPEICHER_VERWALTUNG, Masken.Simulation },
                { KiMaskennamen.QUELLE_ERDREICH,           Masken.Simulation },
                { KiMaskennamen.QUELLE_PUFFERSPEICHER,     Masken.Simulation },
                { KiMaskennamen.QUELLPROFIL,               Masken.Simulation },
                { KiMaskennamen.WAERMESENKE,               Masken.Simulation },
                { KiMaskennamen.KOMPONENTENKONFIGURATION,  Masken.Simulation },

                // Welle KI-F3: Die GEBAEUDEMASKE ist zugleich die Gebaeudeverwaltung -
                // Masken.GebaeudeAdmin oeffnet dieselbe Razor-Komponente in der
                // Betriebsart Admin. Hier fuehrt ein Ziel also auf die Maske selbst und
                // nicht bloss in ihre Naehe; welche Betriebsart offen ist, sagt ihr Feld
                // „verwaltung". Der Gebaeude-KATALOGEDITOR geht aus dieser Maske auf und
                // braucht einen gewaehlten Satz; sein Ziel ist deshalb dieselbe
                // Verwaltung - dieselbe Begruendung wie bei den Katalogeditoren der
                // Erzeuger.
                { KiMaskennamen.GEBAEUDE,         Masken.GebaeudeAdmin },
                { KiMaskennamen.GEBAEUDE_KATALOG, Masken.GebaeudeAdmin },

                // Die Wohn-/Nutzflaechenangabe haengt an einer gewaehlten PROJEKTZEILE
                // und geht ueber den Knopf „Aendern…" auf; kontextfrei gibt es sie
                // nicht. Ihr Weg beginnt auf der Startseite, Reiter „Waermebedarf",
                // Kachel „Gebaeudedaten eingeben" - siehe STARTSEITE.
                { KiMaskennamen.GEBAEUDE_WOHNFLAECHE, STARTSEITE },

                // Der WAERMEBEDARF eines Gebaeudes geht aus der Gebaeudemaske auf und
                // braucht eine gewaehlte Projektzeile samt gerechnetem Bedarf.
                { KiMaskennamen.GEBAEUDE_BEDARF, STARTSEITE },

                // Die Gebaeudetypen-Verwaltung IST die Maske des Navigationsschluessels -
                // hier fallen Katalogschluessel und Navigationsschluessel zusammen.
                { KiMaskennamen.GEBAEUDETYP, Masken.GebaeudetypenAdmin },

                // Profil und Kopfsatz eines Bedarfstyps gehen als Ueberlagerung aus den
                // drei Bedarfsverwaltungen auf. Eine Komponente bedient alle drei
                // Auspraegungen; der Katalogschluessel ist die Stromfassung, und das
                // Ziel ist deshalb die Stromverbraucher-Verwaltung - der Weg, den der
                // Anwender von Hand ginge.
                { KiMaskennamen.TYPPROFIL, Masken.StromverbraucherAdmin },
                { KiMaskennamen.TYPSTAMM,  Masken.StromverbraucherAdmin },

                // Die BEDARFSPROFILE eines Projekts gehen aus den Kacheln der
                // Startseite auf (Reiter „Waermebedarf" und „Strombedarf") und
                // brauchen ein offenes Projekt.
                { KiMaskennamen.BEDARFSPROFILE, STARTSEITE },

                // Die drei KATALOGVERWALTUNGEN sind selbst Navigationsziele des
                // Menues - hier fallen Katalogschluessel und Navigationsschluessel
                // zusammen, wie bei der Waermepumpenverwaltung.
                { KiMaskennamen.PROZESSWAERME_ADMIN,    Masken.ProzesswaermeAdmin },
                { KiMaskennamen.STROMVERBRAUCHER_ADMIN, Masken.StromverbraucherAdmin },
                { KiMaskennamen.BRAUCHWASSER_ADMIN,     Masken.BrauchwasserAdmin },

                // Die ERGEBNISANZEIGE eines Bedarfs geht aus dem Bedarfsreiter der
                // Ansicht „Simulation" auf und braucht einen gerechneten Lauf.
                { KiMaskennamen.BEDARF_ERGEBNIS, Masken.Simulation },

                // Die externen WAERMEBEDARFSGANGLINIEN und die SOLARGANGLINIEN eines
                // Projekts gehen aus den Kacheln der Startseite auf und brauchen ein
                // offenes Projekt; die Verwaltung ihrer Kataloge steht hinter dem
                // Knopf „Bearbeiten…" IN diesen Masken.
                { KiMaskennamen.WAERMEBEDARF_EXTERN, STARTSEITE },
                { KiMaskennamen.SOLARGANGLINIE,      STARTSEITE },

                // Die KLIMADATEN haengen am Menuepunkt „Administration → Klimadaten" -
                // siehe KLIMADATEN.
                { KiMaskennamen.KLIMADATEN, KLIMADATEN },

                // Welle KI-F4: Die ENERGIETRAEGERVERWALTUNG haengt am Menuepunkt
                // „Administration → Kosten → Energietraegerverwaltung…" - siehe
                // ENERGIETRAEGER_VERWALTUNG. Die saisonalen Leistungspreis-Saetze und
                // das Kostenprofil gehen als UEBERLAGERUNG aus ihr auf und brauchen
                // einen gewaehlten Traeger; kontextfrei gibt es beide nicht, und ihr
                // Ziel ist deshalb die Maske, aus der sie aufgehen - dieselbe
                // Begruendung wie bei den Katalogeditoren der Erzeuger.
                { KiMaskennamen.ENERGIETRAEGER,        ENERGIETRAEGER_VERWALTUNG },
                { KiMaskennamen.LEISTUNGSPREISREIHE,   ENERGIETRAEGER_VERWALTUNG },
                { KiMaskennamen.KOSTENPROFIL,          ENERGIETRAEGER_VERWALTUNG },

                // „Energietraeger-Variante anlegen" ist eine eigene Ansicht der
                // AppWurzel - siehe VARIANTE_ANLEGEN.
                { KiMaskennamen.ENERGIETRAEGER_VARIANTE, VARIANTE_ANLEGEN },

                // Der EMISSIONSKATALOG geht als Ueberlagerung aus der Traegerkarte
                // auf („Emissionsarten & Katalog verwalten…") und braucht einen
                // gewaehlten Traeger; sein Ziel ist deshalb die Verwaltung.
                { KiMaskennamen.EMISSIONSKATALOG, ENERGIETRAEGER_VERWALTUNG },

                // Die NUTZUNGSDAUERN sind selbst ein Menuepunkt - hier fallen
                // Katalogschluessel und Navigationsschluessel zusammen, wie bei der
                // Waermepumpenverwaltung.
                { KiMaskennamen.NUTZUNGSDAUER, NUTZUNGSDAUER_VERWALTUNG },

                // Der KOSTENFAKTOREN-Katalog, der ZEILENEDITOR einer Position und die
                // Worst-/Best-Case-Eingabe gehen alle drei als Ueberlagerung aus der
                // Kostenverwaltung auf und brauchen eine gewaehlte Komponente bzw.
                // Zeile; ihr Ziel ist deshalb die Maske, aus der sie aufgehen.
                { KiMaskennamen.KOSTENFAKTOR_KATALOG, KOSTENVERWALTUNG },
                { KiMaskennamen.VORLAGENPOSITION,     KOSTENVERWALTUNG },
                { KiMaskennamen.CASE_EINGABE,         KOSTENVERWALTUNG },

                // Welle KI-F4: Die WIRTSCHAFTLICHKEITSMASKEN gehen aus der Fussleiste
                // der Wirtschaftlichkeitsseite auf; die ist das dritte Reiterblatt der
                // Ansicht „Berichte und Kosten" (Ansichten.BerichteKosten), und die
                // bedient die AppWurzel auf beiden Plattformen. Anders als bei den
                // Kostenmasken fuehrt hier also ein Ziel wirklich irgendwohin.
                { KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, Ansichten.BerichteKosten },
                { KiMaskennamen.TARIFSTRUKTUR,                Ansichten.BerichteKosten },
                { KiMaskennamen.PV_VERGUETUNG,                Ansichten.BerichteKosten },

                // Die BHKW-Wirtschaftlichkeit ist zusaetzlich eine eigene Ansicht der
                // AppWurzel (Seitenschluessel.BhkwWirtschaftlichkeit) - der einzige
                // Dialog dieser Welle, den die Wurzel unmittelbar zeigt.
                { KiMaskennamen.BHKW_WIRTSCHAFTLICHKEIT, BHKW_WIRTSCHAFTLICHKEIT },

                // Die GESETZLICHEN PARAMETER haengen am Menuepunkt „Administration →
                // Gesetzliche Parameter"; der Zeileneditor geht als Ueberlagerung aus
                // ihnen auf und braucht eine gewaehlte Zeile.
                { KiMaskennamen.GESETZESKATALOG,       GESETZESKATALOG },
                { KiMaskennamen.GESETZESKATALOG_ZEILE, GESETZESKATALOG },

                // Die zwei REITERBLAETTER sind Teile der Ansicht „Berichte und
                // Kosten"; welches vorn steht, sagt das Argument (siehe ARGUMENTE).
                // Die Ansicht bedient die AppWurzel auf beiden Plattformen.
                { KiMaskennamen.KOSTENSEITE,              Ansichten.BerichteKosten },
                { KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, Ansichten.BerichteKosten },

                // ---- Welle KI-F5: die ERZEUGERKATALOGE -----------------------
                //
                // DIE KATALOGEDITOREN fuehren auf ihre VERWALTUNG - dieselbe
                // Begruendung wie beim Heizkessel und beim Pufferspeicher: Sie gehen
                // aus einer Liste heraus auf, brauchen einen gewaehlten Satz und
                // lassen sich nicht kontextfrei oeffnen. Der Assistent fuehrt den
                // Anwender dorthin, wo er den Satz waehlt.
                { KiMaskennamen.BHKW,           Masken.BhkwAdmin },
                { KiMaskennamen.SOLARKOLLEKTOR, Masken.SolarkollektorenAdmin },

                // ---- Welle #456: die VERWALTUNGEN der vier Erzeugerkataloge ----
                //
                // Der Katalogbrowser in seinen vier Auspraegungen IST die Maske
                // hinter diesen Navigationsschluesseln - Katalog- und
                // Navigationsschluessel fallen zusammen wie bei der
                // Waermepumpenverwaltung. Die Katalogeditoren darueber fuehren auf
                // dasselbe Ziel; die Absage nennt deshalb die Verwaltung
                // (KiAktionenDialog.Gemeint). WinFormsNavigation kennt die vier
                // Schluessel; auf iOS laufen sie unuebersetzt an die Wurzel, die
                // Katalogverwaltungen dort benannt ablehnt (KI-D-Q10) - dieselbe Lage
                // wie fuer die Katalogeditoren, es braucht nichts Neues.
                { KiMaskennamen.HEIZKESSEL_ADMIN,        Masken.HeizkesselAdmin },
                { KiMaskennamen.BHKW_ADMIN,              Masken.BhkwAdmin },
                { KiMaskennamen.SOLARKOLLEKTOREN_ADMIN,  Masken.SolarkollektorenAdmin },
                { KiMaskennamen.PUFFERSPEICHER_ADMIN,    Masken.PufferSpAdmin },

                // DIE DREI MODULKATALOGE sind selbst die Verwaltung (Browser und
                // Editor in einem) und haben deshalb je einen EIGENEN Weg im Menue
                // „Administration"; ihre Navigationsschluessel sind zugleich ihre
                // Katalogschluessel.
                { KiMaskennamen.PV_MODULKATALOG,        Masken.PvAdmin },
                { KiMaskennamen.STROMSPEICHER_KATALOG,  Masken.StromspeicherAdmin },
                { KiMaskennamen.WECHSELRICHTER_KATALOG, Masken.WechselrichterAdmin },

                // ---- Welle KI-F6: STROM -------------------------------------
                //
                // Die LASTSPITZENKAPPUNG und die STROMGANGLINIEN-VERWALTUNG sind
                // eigene Fenster mit einem Weg im Menue; ihre Katalogschluessel
                // sind zugleich ihre Navigationsschluessel - dieselbe Lage wie
                // bei der Waermepumpenverwaltung. WinFormsNavigation.OeffneMaske
                // kennt beide; auf iOS zeigt die AppWurzel dieselben Dialoge als
                // Ansicht, weil ihre Datenseite plattformfrei liegt.
                { KiMaskennamen.PEAK_SHAVING,        Masken.PeakShaving },
                { KiMaskennamen.STROMGANGLINIE_ADMIN, Masken.StromganglinieAdmin },

                // Die LESEREGELN einer Speicher-Zeitreihe gehen als Ueberlagerung
                // aus Station 2 der Stromspeicher-Auslegung auf und brauchen eine
                // gewaehlte Datei; kontextfrei gibt es sie nicht. Ihr Ziel ist
                // deshalb die Ansicht, aus der sie aufgehen - dieselbe
                // Begruendung wie bei den Ueberlagerungen der
                // Energietraegerverwaltung.
                { KiMaskennamen.SPEICHER_ZEITREIHEN, STROMSPEICHER_AUSLEGUNG },

                // ---- Welle KI-F6: BERICHTE und PROJEKT -----------------------
                //
                // Die zwei REITERBLAETTER gehoeren zur Ansicht „Berichte und
                // Kosten" - wie schon Kostenseite und Wirtschaftlichkeitsseite
                // der Welle KI-F4; welches vorn steht, sagt das Argument. Die
                // Ansicht bedient die AppWurzel auf beiden Plattformen.
                { KiMaskennamen.BERICHTE_UEBERSICHT, Ansichten.BerichteKosten },
                { KiMaskennamen.BERICHTSEITE,        Ansichten.BerichteKosten },

                // „Projekt speichern unter" IST eine Maske der Windows-
                // Navigationstabelle - hier fallen Katalogschluessel und
                // Navigationsschluessel zusammen, wie bei der
                // Waermepumpenverwaltung. Auf iOS zeigt die AppWurzel denselben
                // Dialog als Ansicht.
                { KiMaskennamen.PROJEKT_KOPIE, Masken.ProjektSpeichernUnter },

                // „Als Variante speichern" haengt am Menuepunkt „Projekt → Als
                // Variante speichern…" - siehe PROJEKT_VARIANTE.
                { KiMaskennamen.PROJEKT_VARIANTE, PROJEKT_VARIANTE },

                // ---- Welle #458, Stufe 2 --------------------------------------
                //
                // Der KENNLINIENEDITOR geht als Ueberlagerung aus der
                // Waermepumpen-Verwaltung und aus der Waermepumpen-Anlage auf und
                // braucht eine gewaehlte Waermepumpe; kontextfrei gibt es ihn nicht.
                // Sein Ziel ist deshalb die Verwaltung - dieselbe Bauart wie die
                // Ueberlagerung „Anlagenwerte" der Photovoltaik.
                { KiMaskennamen.KENNLINIEN, Masken.WpAdministration },

                // Der PROJEKTKOPF ist Schritt 1 des Assistenten „Neues Projekt" -
                // Masken.Assistent ohne Argument oeffnet ihn in der Betriebsart Neu
                // (AssistentCtrl.BETRIEBSART_NEU = 0), dem Weg des Menues.
                { KiMaskennamen.PROJEKTKOPF, Masken.Assistent },

                // Die STARTSEITE ist ihr eigenes Ziel; ohne Argument entscheidet sie
                // selbst, welcher Reiter vorn steht.
                { KiMaskennamen.STARTSEITE, STARTSEITE },

                // Die PROGRAMMEINSTELLUNGEN haengen am Menuepunkt „Administration →
                // Einstellungen" - siehe EINSTELLUNGEN.
                { KiMaskennamen.EINSTELLUNGEN, EINSTELLUNGEN }
            };

        /// <summary>
        /// Die zweite Datenspalte: das ARGUMENT, mit dem das Ziel aufmacht
        /// (Anwenderentscheid KI‑D‑Q8, 21.09.2026).
        ///
        /// <para><b>Auch sie ist DATEN und keine Logik</b> — eine Tabelle neben der
        /// Zieltabelle, kein <c>switch</c> mit Sonderfaellen. Wer hier fehlt, oeffnet
        /// sein Ziel so, wie es von selbst aufmacht; das ist kein Fehlerzustand,
        /// sondern die Angabe „kein eigener Platz".</para>
        ///
        /// <para><b>Zwei Arten von Argument.</b> Das Ziel <see cref="STARTSEITE"/>
        /// nimmt einen REITER, die Ansicht „Berichte und Kosten" ein BLATT. Beide
        /// Ziele fuehren mehrere Masken; ohne das Argument landete der Anwender auf
        /// dem ersten Reiter bzw. der Uebersicht und muesste selbst weitersuchen.</para>
        /// </summary>
        private static readonly Dictionary<string, string> ARGUMENTE =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Die sechs Erzeugermasken des Projekts gehen aus der Erzeugerkarte
                // der Startseite auf - Reiter 4.
                { KiMaskennamen.HEIZKESSEL_PROJEKT,       REITER_ERZEUGER },
                { KiMaskennamen.BHKW_PROJEKT,             REITER_ERZEUGER },
                { KiMaskennamen.PUFFERSPEICHER_PROJEKT,   REITER_ERZEUGER },
                { KiMaskennamen.STROMSPEICHER_PROJEKT,    REITER_ERZEUGER },
                { KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT, REITER_ERZEUGER },
                { KiMaskennamen.WAERMEPUMPE_ANLAGE,       REITER_ERZEUGER },

                // Die SOLARGANGLINIEN haengen an der Kachel „Solarthermie" - sie
                // steht ebenfalls auf dem Erzeugerreiter, und welche der zwei
                // Masken aufgeht, entscheidet die Weiche der Kachel.
                { KiMaskennamen.SOLARGANGLINIE,           REITER_ERZEUGER },

                // Gebaeudedaten, Wohnflaeche und der gerechnete Bedarf gehen aus der
                // Kachel „Gebaeudedaten eingeben" auf, die externen Ganglinien aus
                // „Daten importieren" - beide auf Reiter 2.
                { KiMaskennamen.GEBAEUDE_WOHNFLAECHE,     REITER_WAERMEBEDARF },
                { KiMaskennamen.GEBAEUDE_BEDARF,          REITER_WAERMEBEDARF },
                { KiMaskennamen.WAERMEBEDARF_EXTERN,      REITER_WAERMEBEDARF },

                // KiMaskennamen.BEDARFSPROFILE steht hier BEWUSST NICHT: Dieselbe
                // Maske geht aus den Kacheln ZWEIER Reiter auf („Waermebedarf" mit
                // Prozesswaerme und Brauchwasser, „Strombedarf" mit den drei
                // Stromprofilen). Einen davon zu nennen hiesse, die Haelfte der
                // Aufrufe an den falschen Platz zu fuehren.

                // Die Ansicht „Berichte und Kosten" fuehrt vier Blaetter; jede
                // Maske nennt das ihre.
                { KiMaskennamen.KOSTENSEITE,                  BLATT_KOSTEN },
                { KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE,     BLATT_WIRTSCHAFT },
                { KiMaskennamen.BERICHTE_UEBERSICHT,          BLATT_UEBERSICHT },
                { KiMaskennamen.BERICHTSEITE,                 BLATT_BERICHT },

                // Die drei Wirtschaftlichkeitsmasken gehen aus der Fussleiste des
                // Blattes „Wirtschaftlichkeit" auf.
                { KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, BLATT_WIRTSCHAFT },
                { KiMaskennamen.TARIFSTRUKTUR,                BLATT_WIRTSCHAFT },
                { KiMaskennamen.PV_VERGUETUNG,                BLATT_WIRTSCHAFT }
            };

        /// <summary>Alle zugeordneten Katalogmasken.</summary>
        public static IReadOnlyCollection<string> Masken_ => ZIELE.Keys;

        /// <summary>
        /// Der Navigationsschluessel zur Katalogmaske; leer, wenn es keinen gibt.
        /// </summary>
        public static string Ziel(string maskenname)
        {
            if (string.IsNullOrWhiteSpace(maskenname)) return "";
            string ziel;
            return ZIELE.TryGetValue(maskenname.Trim(), out ziel) ? ziel : "";
        }

        /// <summary>Gibt es fuer diese Katalogmaske ein Ziel?</summary>
        public static bool Kennt(string maskenname) => Ziel(maskenname).Length > 0;

        /// <summary>
        /// Das ARGUMENT, mit dem das Ziel dieser Katalogmaske aufmacht — der Reiter
        /// der Startseite bzw. das Blatt der Ansicht „Berichte und Kosten"; leer,
        /// wenn keines mitgeht.
        /// </summary>
        /// <remarks>
        /// Leer statt <c>null</c> — dieselbe Form wie bei <see cref="Ziel"/>: Ein
        /// Aufrufer muss nicht zwei Abwesenheiten unterscheiden, und der Kern laeuft
        /// ohne Nullbarkeitsannotation.
        /// </remarks>
        public static string Argument(string maskenname)
        {
            if (string.IsNullOrWhiteSpace(maskenname)) return "";
            string argument;
            return ARGUMENTE.TryGetValue(maskenname.Trim(), out argument) ? argument : "";
        }
    }
}
