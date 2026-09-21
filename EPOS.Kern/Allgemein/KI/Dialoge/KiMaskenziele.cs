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
    /// Die Zuordnung Katalogmaske → Navigationsschluessel fuer <c>dialog_oeffnen</c>.
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
        /// <b>Ein Ziel, das heute nirgends aufgeht — und das ist die richtige Angabe.</b>
        /// Die Kostenverwaltung haengt an einer gewaehlten KOMPONENTE eines Projekts;
        /// einen kontextfreien Weg dorthin gibt es nicht, und einen zu erfinden hiesse,
        /// die Maske ohne Bezug zu oeffnen. <c>OeffneMaske</c> liefert deshalb
        /// <c>false</c>, und <c>dialog_oeffnen</c> lehnt benannt ab, statt still nichts
        /// zu tun — genau wie bei der Stromspeicher-Ansicht unter Windows. LESEN und
        /// SETZEN erreichen die Maske trotzdem, sobald der Anwender sie offen hat: Dafuer
        /// zaehlt die Anmeldung an der Maskenbruecke, nicht dieses Ziel.
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
        /// <b>Ein Reiterwunsch geht dabei nicht mit.</b> Die <c>AppWurzel</c> fuehrt
        /// zwar einen (<c>Reiterwunsch</c> der <c>Startseite</c>), sie setzt ihn aber
        /// aus ihrem RUECKWEG und nicht aus den Argumenten von <c>OeffneMaske</c>; es
        /// gibt also keinen Schluessel, der die Startseite auf dem Reiter
        /// „Energieerzeuger" aufmachte. Ein solcher waere eine neue Naht durch drei
        /// Schichten und gehoert nicht in diese Welle.
        /// </para>
        /// <para>
        /// <b>Warum die Zeichenkette und nicht <c>Seitenschluessel.Startseite</c>:</b>
        /// dieselbe Begruendung wie bei <see cref="STROMSPEICHER_AUSLEGUNG"/> — jene
        /// Konstante steht in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht.
        /// Ein Waechter in <c>EPOS.UI.Tests</c> haelt beide gegeneinander. Unter
        /// Windows kennt <c>WinFormsNavigation</c> den Schluessel nicht; dort liefert
        /// <c>OeffneMaske</c> <c>false</c>, und <c>dialog_oeffnen</c> lehnt benannt ab,
        /// statt still nichts zu tun. Auf iOS wechselt die Wurzel die Ansicht.
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
        /// Klimadaten haengen am Menuepunkt „Administration → Klimadaten", und die
        /// Windows-Huelle faengt ihn selbst ab. Ein Waechter in <c>EPOS.UI.Tests</c>
        /// haelt beide Fundstellen gegeneinander.
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
        /// die Windows-Huelle faengt ihn im Menueweg selbst ab
        /// (<c>HauptfensterHuelle.Weg</c>), <c>WinFormsNavigation.OeffneMaske</c> kennt
        /// ihn nicht. <c>dialog_oeffnen</c> lehnt dort also benannt ab, statt still
        /// nichts zu tun — genau wie bei der Kostenverwaltung. Auf iOS ist die
        /// Kostenverwaltung insgesamt noch nicht angebunden; die <c>AppWurzel</c>
        /// meldet <c>false</c>. LESEN und SETZEN erreichen die Maske trotzdem, sobald
        /// der Anwender sie offen hat: Dafuer zaehlt die Anmeldung an der
        /// Maskenbruecke, nicht dieses Ziel.
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
        /// bei <see cref="ENERGIETRAEGER_VERWALTUNG"/> — die Windows-Huelle faengt ihn
        /// im Menueweg ab, <c>WinFormsNavigation.OeffneMaske</c> kennt ihn nicht, und
        /// auf iOS ist die Kostenverwaltung noch nicht angebunden. <c>dialog_oeffnen</c>
        /// lehnt deshalb auf beiden Plattformen benannt ab.
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
        /// <see cref="ENERGIETRAEGER_VERWALTUNG"/> — die Windows-Huelle faengt ihn im
        /// Menueweg ab, <c>WinFormsNavigation.OeffneMaske</c> kennt ihn nicht.
        /// </remarks>
        public const string GESETZESKATALOG = "GESETZESKATALOG";

        private static readonly Dictionary<string, string> ZIELE =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Der Heizkesseleditor geht aus der Heizkesselverwaltung auf.
                { KiMaskennamen.HEIZKESSEL,       Masken.HeizkesselAdmin },

                // Der Photovoltaikdialog haengt an der gewaehlten Projektzeile; der
                // kontextfreie Weg ist die Modulverwaltung.
                { KiMaskennamen.PHOTOVOLTAIK,     Masken.PvAdmin },

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
                // Kosten"; ein Reiterwunsch geht dabei nicht mit (dieselbe Lage wie
                // beim Reiterwunsch der Startseite, siehe STARTSEITE). Die Ansicht
                // bedient die AppWurzel auf beiden Plattformen.
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
                // kennt beide; auf iOS uebersetzt IosNavigation keinen von
                // ihnen, die Wurzel antwortet false, und dialog_oeffnen lehnt
                // benannt ab.
                { KiMaskennamen.PEAK_SHAVING,        Masken.PeakShaving },
                { KiMaskennamen.STROMGANGLINIE_ADMIN, Masken.StromganglinieAdmin },

                // Die LESEREGELN einer Speicher-Zeitreihe gehen als Ueberlagerung
                // aus Station 2 der Stromspeicher-Auslegung auf und brauchen eine
                // gewaehlte Datei; kontextfrei gibt es sie nicht. Ihr Ziel ist
                // deshalb die Ansicht, aus der sie aufgehen - dieselbe
                // Begruendung wie bei den Ueberlagerungen der
                // Energietraegerverwaltung.
                { KiMaskennamen.SPEICHER_ZEITREIHEN, STROMSPEICHER_AUSLEGUNG }
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
    }
}
