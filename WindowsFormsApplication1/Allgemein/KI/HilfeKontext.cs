using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ermittelt, in welchem Bereich der Anwendung sich der Benutzer gerade
    /// befindet. Der KI-Assistent bekommt diesen Kontext mitgeliefert und kann
    /// dadurch gezielt zur aktuellen Maske antworten.
    ///
    /// DATENSCHUTZ (Sicherheitsmassnahme A5): Der Kontext verlaesst den Rechner
    /// und darf deshalb ausschliesslich generische Bereichsbezeichnungen
    /// enthalten. Fruehere Fassungen haben den rohen Fenstertitel uebernommen -
    /// mehrere Titel fuehren jedoch Klarnamen ("... - Projekt: Muster GmbH").
    ///
    /// Seither gilt: jeder Bestandteil des gelieferten Textes stammt aus einer
    /// festen Positivliste (<see cref="POSITIVLISTE"/>). Passt nichts, wird
    /// <see cref="BEREICH_UNBEKANNT"/> geliefert. Neue Masken, die einen
    /// sprechenden Bereichsnamen setzen wollen, muessen ihren Text hier
    /// eintragen - genau das ist der Sinn der Liste.
    /// </summary>
    public static class HilfeKontext
    {
        // ------------------------------------------------------------------
        //  Positivliste der Bereichsbezeichnungen
        // ------------------------------------------------------------------

        // ------------------------------------------------------------------
        //  iU9-W15b.0f: Die Positivliste und die Freigabeschranke stehen seit
        //  dieser Welle im Kern (KiChatKontext) - sie sind reine Zeichenarbeit
        //  und werden auf iOS ebenso gebraucht (Befund W15b-B19, Entscheid E-9).
        //  Hier stehen nur noch die Kurznamen, damit die drei Nachschlagetabellen
        //  unten unveraendert lesbar bleiben. EINE Liste, EIN Ort.
        // ------------------------------------------------------------------

        /// <summary>Ersatzwert, wenn sich der Bereich nicht sicher zuordnen laesst.</summary>
        public const string BEREICH_UNBEKANNT = KiChatKontext.BEREICH_UNBEKANNT;

        private const string B_ADMIN = KiChatKontext.B_ADMIN;
        private const string B_ASSISTENT = KiChatKontext.B_ASSISTENT;
        private const string B_BERICHT = KiChatKontext.B_BERICHT;
        private const string B_BHKW = KiChatKontext.B_BHKW;
        private const string B_BRAUCHWASSER = KiChatKontext.B_BRAUCHWASSER;
        private const string B_GEBAEUDE = KiChatKontext.B_GEBAEUDE;
        private const string B_HAUPTFENSTER = KiChatKontext.B_HAUPTFENSTER;
        private const string B_HEIZKESSEL = KiChatKontext.B_HEIZKESSEL;
        private const string B_HILFE = KiChatKontext.B_HILFE;
        private const string B_KLIMADATEN = KiChatKontext.B_KLIMADATEN;
        private const string B_KOSTEN = KiChatKontext.B_KOSTEN;
        private const string B_LIZENZ = KiChatKontext.B_LIZENZ;
        private const string B_PHOTOVOLTAIK = KiChatKontext.B_PHOTOVOLTAIK;
        private const string B_PROJEKT = KiChatKontext.B_PROJEKT;
        private const string B_PROZESSWAERME = KiChatKontext.B_PROZESSWAERME;
        private const string B_PUFFERSPEICHER = KiChatKontext.B_PUFFERSPEICHER;
        private const string B_SIMULATION = KiChatKontext.B_SIMULATION;
        private const string B_SOLARTHERMIE = KiChatKontext.B_SOLARTHERMIE;
        private const string B_STROMSPEICHER = KiChatKontext.B_STROMSPEICHER;
        private const string B_STROMVERBRAUCHER = KiChatKontext.B_STROMVERBRAUCHER;
        private const string B_VARIANTEN = KiChatKontext.B_VARIANTEN;
        private const string B_WAERMEBEDARF = KiChatKontext.B_WAERMEBEDARF;
        private const string B_WAERMEPUMPE = KiChatKontext.B_WAERMEPUMPE;
        private const string B_WIRTSCHAFT = KiChatKontext.B_WIRTSCHAFT;

        private const string B_QUELLE_ERDREICH = KiChatKontext.B_QUELLE_ERDREICH;
        private const string B_SIM_KONFIG = KiChatKontext.B_SIM_KONFIG;
        private const string B_SIM_DETAIL = KiChatKontext.B_SIM_DETAIL;

        /// <summary>
        /// Detailangaben, die ueber ErgaenzeDetail() mitgesendet werden duerfen.
        /// Bewusst leer: heute ruft nichts ErgaenzeDetail() auf. Wer die Methode
        /// benutzen will, traegt seinen generischen Text zuerst hier ein.
        /// </summary>
        private static readonly HashSet<string> ERLAUBTE_DETAILS = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Zuordnung Formulartyp -&gt; Bereichsbezeichnung (exakter Typname).</summary>
        private static readonly Dictionary<string, string> BEREICH_JE_TYP =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "MDIMainForm",                 B_HAUPTFENSTER },
            { "FormMain",                    B_HAUPTFENSTER },
            { "Form_Start",                  B_HAUPTFENSTER },

            { "Form_AdminSettings",          B_ADMIN },
            { "Form_Gesetzesparameter",      B_ADMIN },
            { "Form_GesetzparameterZeile",   B_ADMIN },
            { "Form_KatalogDubletten",       B_ADMIN },
            // iU9-W15c: Beide Lizenzmasken sind Razor-Komponenten. Die
            // Klassennamen bleiben hier stehen - sie sind der Schluessel, unter dem
            // HilfeAutomatik ein geoeffnetes Fenster erkennt -, und die zwei
            // Komponenten kommen daneben. Dieselbe Praxis wie bei KiChatDialog
            // (W15b.7) und BedarfAdminDialog (W14b.1).
            { "Form_LizenzVerwaltung",       B_LIZENZ },
            { "Form_Lizenz",                 B_LIZENZ },
            { "LizenzVerwaltungDialog",      B_LIZENZ },
            { "LizenzDialog",                B_LIZENZ },

            // iU9-W14a.1: Die vier Erzeuger-Katalogbrowser sind EINE Razor-Komponente
            // (KatalogBrowserDialog) mit vier Auspraegungen. Die Maskennamen bleiben
            // hier stehen: Sie sind der Schluessel, unter dem HilfeAutomatik ein
            // geoeffnetes Fenster erkennt, und die Blazor-Huelle traegt weiterhin den
            // Titel der Auspraegung. Ein EINZIGER Eintrag fuer die Komponente ginge
            // nicht - sie bedient vier verschiedene Bereiche.
            { "Form_BHKWAdmin",              B_BHKW },

            // iU9-W14b.1: Die DREI Bedarfs-Katalogverwaltungen sind EINE
            // Razor-Komponente mit drei Auspraegungen (BedarfAdminDialog). Der
            // Bereich haengt damit nicht mehr am Klassennamen - er kaeme fuer alle
            // drei gleich heraus; eingetragen ist der haeufigste Wirt. Die
            // Schluessel des InfoKnopfes in help_mapping.txt heissen weiter nach
            // den drei Masken, denn sie sind die Adresse des HILFETEXTES und nicht
            // der Klasse (Praxis seit W12).
            { "BedarfAdminDialog",           B_BRAUCHWASSER },


            { "Form_Heizkessel_Admin",       B_HEIZKESSEL },

            { "Form_HelpPopup",              B_HILFE },
            { "Form_KiChat",                 B_HILFE },
            // iU9-W15b.7: Der Chat ist eine Razor-Komponente. Der Klassenname bleibt
            // hier stehen - er ist der Schluessel, unter dem HilfeAutomatik ein
            // geoeffnetes Fenster erkennt -, und die Komponente kommt daneben. Der
            // Titelweg (BEREICH_JE_TITELANFANG, "Hilfe-Assistent") traegt den
            // Windows-Fall ohnehin; auf iOS bildet KiChatKontext den Seitenschluessel
            // KI_ASSISTENT auf denselben Bereich ab (Praxis seit W12).
            { "KiChatDialog",                B_HILFE },

            { "Form_Klimadaten",             B_KLIMADATEN },

            { "Form_Kosten_Auswahl",         B_KOSTEN },

            { "Form_AdminPV",                B_PHOTOVOLTAIK },

            // iU9-W13.3: Der PV-Modulimport ist die Razor-Komponente
            // PvModulImportDialog. Damit fallen ZWEI Eintraege weg: der tote
            // "Form_CECImport" (der Dateiname, nachgeschlagen wird der TYPNAME -
            // Befund W13-B37) und der wirksame "Main_PV_Test". Der Bereich bleibt
            // derselbe; die Zeile in help_mapping.txt heisst weiter nach der
            // Maske, denn sie ist die Adresse des HILFETEXTES (Praxis seit W12).
            { "PvModulImportDialog",         B_PHOTOVOLTAIK },

            // P6 nachgetragen: die Huellform "Projekt oeffnen" aus Paket P3. Ohne
            // Eintrag griff erst die Kennungsstufe ("projekt" im Typnamen) - das
            // Ergebnis war zwar dasselbe, aber unbeabsichtigt.
            { "Form_ProjektAuswahl",         B_PROJEKT },
            { "Form_ProjektDelete",          B_PROJEKT },
            { "Form_ProjektSpeichernUnter",  B_PROJEKT },

            // iU9-W14b.1: Form_Prozesswaerme_Admin ist geloescht - sie ist eine
            // Auspraegung von BedarfAdminDialog (Eintrag oben bei B_BRAUCHWASSER).

            { "Form_PufferSp_Admin",         B_PUFFERSPEICHER },
            { "Form_PufferSp_Bearbeiten",    B_PUFFERSPEICHER },
            // iU9-W14a.2: derselbe Bereich fuer die Razor-Fassung des Katalogeditors.
            { "PufferSpKatalogDialog",       B_PUFFERSPEICHER },
            // iU9-W10a.4: Form_PufferSp_Projekt ist geloescht (Razor-Komponente).

            { "ErzeugerKarte",               B_SIMULATION },
            { "SpeicherKarte",               B_SIMULATION },
            // iU9-W10a.3: Form_QuelleErdreich ist geloescht. Der Bereich
            // B_QUELLE_ERDREICH bleibt - die HUELLE QuelleErdreichHuelle setzt ihn
            // ueber SetzeBereich, solange der Blazor-Dialog steht, und nimmt ihn
            // danach mit Zuruecksetzen wieder weg.
            // iU9-W10a.5: Form_QuellePufferspeicher ist geloescht (Razor-Komponente).
            // iU9-W10a.6: Form_Quellprofil ist geloescht (Razor-Komponente).
            // iU9-W10b.1: Form_Simulation_Config ist geloescht (Razor-Seite). Den
            // Bereich meldet jetzt SimulationKonfigHuelle beim Aktivieren des
            // Fensters - derselbe Text, nur ohne Formularklasse dahinter.
            // iU9-W10a.7: Form_Waermesenke ist geloescht (Razor-Komponente).
            // iU9-W11b.13: Form_Simulation_Detail, DashboardForm, NavigatorUebersicht,
            // NavigatorStrom, NavigatorWaerme und Form_SpeicherVariantenVergleich sind
            // geloescht (Razor-Seite SimulationErgebnisSeite). Den Bereich
            // B_SIM_DETAIL meldet jetzt SimulationErgebnisHuelle beim Aktivieren des
            // Fensters - derselbe Text, nur ohne Formularklasse dahinter; die fuenf
            // Nebenmasken sind Reiter derselben Seite und brauchen keinen eigenen
            // Eintrag mehr.

            { "Form_SolarKollektorenAdmin",  B_SOLARTHERMIE },
            // iU9-W14b.2: Die Verwaltung der Solarthermieganglinien ist die
            // Razor-Komponente SolarganglinieAdminDialog; der Bereich bleibt.
            { "SolarganglinieAdminDialog",   B_SOLARTHERMIE },
            { "SolarganglinieDialog",        B_SOLARTHERMIE },

            { "Form_AdminStromspeicher",     B_STROMSPEICHER },
            // iU9-W12.6: Form_PeakShaving ist die Razor-Komponente
            // PeakShavingDialog; der Bereich bleibt derselbe.
            { "PeakShavingDialog",           B_STROMSPEICHER },
            { "Form_SpeicherOptimierung",    B_STROMSPEICHER },

            // Nachgetragen mit H7: Entwicklermaske hinter dem unbeschrifteten Knopf
            // "SP" auf FormMain - sie ordnet dem Projekt einen Stromspeicher zu.
            { "Form_StromTest",              B_STROMSPEICHER },

            // BEFUND W12-B20, nachgetragen (iU9-W12.3): Der Konfliktdialog hatte als
            // einzige der sechs Masken der Welle 12 KEINEN Bereich, obwohl
            // help_mapping.txt seit H1/H2 eine Zeile fuer ihn fuehrt. Er gehoert zum
            // Uebernehmen fremder Projekte - dasselbe Ziel wie dort.
            { "ImportKonflikteDialog",       B_PROJEKT },

            // iU9-W12.1/W12.2/W12.4/W12.5: Die vier Ganglinienmasken sind
            // Razor-Komponenten; der Bereich bleibt derselbe. Die Schluessel des
            // InfoKnopfes in help_mapping.txt heissen weiter nach den Masken -
            // sie sind die Adresse des HILFETEXTES, nicht der Klasse.
            { "GanglinieImportOptionenDialog", B_STROMVERBRAUCHER },
            { "GanglinieProtokollDialog",    B_STROMVERBRAUCHER },
            { "StromganglinieDialog",        B_STROMVERBRAUCHER },
            { "StromganglinieAdminDialog",   B_STROMVERBRAUCHER },

            // iU9-W13.2: Die Verwaltung der externen Waermebedarfsganglinien ist
            // die Razor-Komponente WaermebedarfAdminDialog; der Bereich bleibt.
            { "WaermebedarfAdminDialog",     B_WAERMEBEDARF },

            // iU9-W13.1: Die VIER VDI-3805-Einlesemasken sind EINE Razor-Komponente
            // mit vier Auspraegungen (KatalogImportDialog). Der Bereich haengt
            // damit nicht mehr am Klassennamen - er kaeme fuer alle vier gleich
            // heraus. Nachgeschlagen wird deshalb der Bereich des WIRTES; die
            // Schluessel des InfoKnopfes in help_mapping.txt heissen weiter nach
            // den vier Masken, denn sie sind die Adresse des HILFETEXTES und
            // nicht der Klasse (Praxis seit W12).
            { "KatalogImportDialog",         B_HEIZKESSEL },


            // iU9-W16a.1/.3: Wizard_Stromlastgang und Wizard_Komponenten sind
            // gefallen. Die Stromlastgangseite ist seither DIESELBE Komponente wie
            // der Dialog der Startkachel (StromganglinieDialog, :207, Bereich
            // Stromverbraucher); der Komponentenschritt ist KomponentenauswahlDialog
            // und gehoert zum Assistenten. Wizard_Projekt (W15a.6) steht hier
            // ebenfalls nicht mehr; ihr Nachfolger ProjektKopfSeite laeuft im Rahmen
            // und wird ueber dessen Fenstertyp nachgeschlagen.
            { "WizardParent",                B_ASSISTENT },
            { "KomponentenauswahlDialog",    B_ASSISTENT }
        };

        /// <summary>
        /// Ersatzweg fuer Fenster, deren Typ nicht in der Tabelle steht: der
        /// normierte Titelanfang (alles vor dem ersten Trennzeichen). Auch hier
        /// wird ausschliesslich der Wert der Positivliste geliefert, nie der Titel.
        /// </summary>
        private static readonly Dictionary<string, string> BEREICH_JE_TITELANFANG =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bericht erstellen",                                B_BERICHT },
            { "Bericht",                                          B_BERICHT },
            { "Kapitalwert-Verlauf über den Nutzungszeitraum",    B_WIRTSCHAFT },
            { "Wirtschaftlichkeit",                               B_WIRTSCHAFT },
            { "Simulation",                                       B_SIMULATION },
            { "Hilfe-Assistent",                                  B_HILFE },
            { "KI-Assistent",                                     B_HILFE }
        };

        /// <summary>
        /// Letzte Stufe: Kennung im Typnamen -&gt; Bereich. Die Reihenfolge ist
        /// bewusst von speziell nach allgemein. Ausgegeben wird immer nur die
        /// hinterlegte Konstante, nie ein Teil des Typ- oder Fensternamens.
        /// </summary>
        private static readonly string[][] BEREICH_JE_KENNUNG =
        {
            new[] { "wirtschaftlichkeit", B_WIRTSCHAFT },
            new[] { "bericht",            B_BERICHT },
            new[] { "lizenz",             B_LIZENZ },
            new[] { "wizard",             B_ASSISTENT },
            new[] { "brauchwasser",       B_BRAUCHWASSER },
            new[] { "prozess",            B_PROZESSWAERME },
            new[] { "puffer",             B_PUFFERSPEICHER },
            new[] { "stromspeicher",      B_STROMSPEICHER },
            new[] { "peakshaving",        B_STROMSPEICHER },
            new[] { "stromverbraucher",   B_STROMVERBRAUCHER },
            new[] { "stromganglinie",     B_STROMVERBRAUCHER },
            new[] { "solar",              B_SOLARTHERMIE },
            new[] { "heizkessel",         B_HEIZKESSEL },
            new[] { "bhkw",               B_BHKW },
            new[] { "photovoltaik",       B_PHOTOVOLTAIK },
            new[] { "waermebedarf",       B_WAERMEBEDARF },
            new[] { "wärmebedarf",        B_WAERMEBEDARF },
            new[] { "waermepumpe",        B_WAERMEPUMPE },
            new[] { "wärmepumpe",         B_WAERMEPUMPE },
            new[] { "gebaeude",           B_GEBAEUDE },
            new[] { "gebäude",            B_GEBAEUDE },
            new[] { "klima",              B_KLIMADATEN },
            new[] { "kosten",             B_KOSTEN },
            new[] { "tarif",              B_KOSTEN },
            new[] { "preis",              B_KOSTEN },
            new[] { "variante",           B_VARIANTEN },
            new[] { "simulation",         B_SIMULATION },
            new[] { "quelle",             B_SIMULATION },
            new[] { "senke",              B_SIMULATION },
            new[] { "projekt",            B_PROJEKT },
            new[] { "admin",              B_ADMIN },
            new[] { "help",               B_HILFE },
            new[] { "hilfe",              B_HILFE }
        };

        /// <summary>Trennzeichen, an denen ein Fenstertitel abgeschnitten wird.</summary>
        private static readonly string[] TITEL_TRENNER = { " — ", " – ", " - ", " | ", ":", "(" };

        /// <summary>Hoechstens so viele Registerkarten-Ebenen werden gemeldet.</summary>
        private const int MAX_REGISTER_EBENEN = 3;

        /// <summary>Hoechstlaenge einer gemeldeten Registerkartenbeschriftung.</summary>
        private const int MAX_REGISTER_LAENGE = 40;

        // ------------------------------------------------------------------
        //  Von den Masken gesetzter Zustand
        // ------------------------------------------------------------------

        /// <summary>Optional gesetzter Bereichsname (ueberschreibt die Automatik).</summary>
        private static string _bereich = "";

        /// <summary>Zusaetzliche Angaben der Maske (nur Werte aus ERLAUBTE_DETAILS).</summary>
        private static readonly List<string> _details = new List<string>();

        /// <summary>
        /// Setzt den Bereichsnamen, in dem sich der Benutzer befindet.
        /// Aufruf z. B. im Konstruktor oder beim Aktivieren einer Maske.
        ///
        /// Es werden nur Bezeichnungen aus der Positivliste uebernommen; alles
        /// andere wird verworfen, damit ueber diesen Weg keine Projekt- oder
        /// Kundendaten in den Prompt gelangen koennen.
        /// </summary>
        public static void SetzeBereich(string bereich)
        {
            string geprueft = Freigegeben(bereich);
            _bereich = geprueft == BEREICH_UNBEKANNT ? "" : geprueft;
            _details.Clear();
        }

        /// <summary>
        /// Ergaenzt eine Detailangabe zum aktuellen Bereich. Uebernommen wird nur,
        /// was in ERLAUBTE_DETAILS steht - freier Text wird stillschweigend verworfen.
        /// </summary>
        public static void ErgaenzeDetail(string detail)
        {
            if (string.IsNullOrWhiteSpace(detail)) return;
            string d = detail.Trim();
            if (!ERLAUBTE_DETAILS.Contains(d))
            {
                System.Diagnostics.Debug.WriteLine(
                    "[KI] Detailangabe nicht in der Positivliste - verworfen.");
                return;
            }
            if (!_details.Contains(d)) _details.Add(d);
        }

        /// <summary>
        /// Meldet die Windows-Ermittlung des Bereichs beim Kern an (iU9-W15b.0f).
        /// Aufruf einmalig beim Programmstart (<c>Program.Main</c>).
        /// </summary>
        /// <remarks>
        /// Ohne diesen Aufruf bleibt <c>KiChatKontext.AktuellerBereich()</c> bei
        /// „Unbekannter Bereich" - genau der Zustand, in dem der Aktionsharnisch, die
        /// Konsolenwerkzeuge und (bis iU11) die iOS-Huelle laufen. Der Assistent
        /// antwortet dann ohne Bereichsangabe: unschaerfer, aber nicht falsch.
        /// </remarks>
        public static void Einhaengen()
        {
            KiChatKontext.AktiverBereich =
                () => !string.IsNullOrEmpty(_bereich) ? _bereich : AktivesFenster();
        }

        /// <summary>Loescht den gesetzten Kontext (z. B. beim Schliessen einer Maske).</summary>
        public static void Zuruecksetzen()
        {
            _bereich = "";
            _details.Clear();
        }

        // ------------------------------------------------------------------
        //  Ausgabe
        // ------------------------------------------------------------------

        /// <summary>
        /// Liefert eine kurze Beschreibung des aktuellen Kontexts fuer den
        /// KI-Assistenten - bewusst knapp, da jedes Token Kosten verursacht.
        ///
        /// Jeder Bestandteil stammt aus der Positivliste bzw. aus fest
        /// vergebenen Beschriftungen der Oberflaeche. Projekt-, Kunden- und
        /// Simulationsdaten sind ausgeschlossen; zur Sicherheit laeuft der
        /// fertige Text zusaetzlich durch <see cref="OhneKlarnamen"/>.
        /// </summary>
        public static string Beschreibung()
        {
            StringBuilder sb = new StringBuilder();

            string bereich = !string.IsNullOrEmpty(_bereich) ? _bereich : AktivesFenster();
            if (!string.IsNullOrEmpty(bereich)) sb.Append("Bereich: ").Append(bereich);

            string tabs = AktiveRegisterkarten();
            if (!string.IsNullOrEmpty(tabs))
            {
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append("Registerkarte: ").Append(tabs);
            }

            foreach (string d in _details)
            {
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append(d);
            }

            return OhneKlarnamen(sb.ToString());
        }

        /// <summary>
        /// Bereichsbezeichnung des aktiven Fensters - abgebildet auf die
        /// Positivliste. Der Fenstertitel selbst wird NIE zurueckgegeben.
        /// </summary>
        private static string AktivesFenster()
        {
            try
            {
                Form frm = Form.ActiveForm;
                if (frm == null) return "";

                // In der MDI-Oberflaeche ist das aktive Fenster das Rahmenfenster;
                // aussagekraeftig ist das darin aktive Kindfenster.
                Form ziel = frm.ActiveMdiChild ?? frm;
                return BereichFuer(ziel.GetType().Name, ziel.Text);
            }
            catch { return ""; }
        }

        /// <summary>
        /// Bildet Typname und Fenstertitel auf die Positivliste ab. Bewusst
        /// öffentlich und frei von Oberflächenzustand, damit sich die Abbildung
        /// ohne aktives Fenster nachweisen lässt (Selbstprüfung/Prüfharnisch).
        /// Rückgabe ist immer ein Eintrag der Positivliste.
        /// </summary>
        public static string BereichFuer(string typname, string fenstertitel)
        {
            string treffer = BereichAusTyp(typname);
            if (treffer != null) return treffer;

            treffer = BereichAusTitel(fenstertitel);
            if (treffer != null) return treffer;

            treffer = BereichAusKennung(typname);
            if (treffer != null) return treffer;

            return BEREICH_UNBEKANNT;
        }

        private static string BereichAusTyp(string typname)
        {
            if (string.IsNullOrEmpty(typname)) return null;
            string wert;
            return BEREICH_JE_TYP.TryGetValue(typname, out wert) ? Freigegeben(wert) : null;
        }

        private static string BereichAusTitel(string titel)
        {
            if (string.IsNullOrWhiteSpace(titel)) return null;

            string anfang = titel.Trim();
            foreach (string trenner in TITEL_TRENNER)
            {
                int p = anfang.IndexOf(trenner, StringComparison.Ordinal);
                if (p > 0) anfang = anfang.Substring(0, p).Trim();
            }

            string wert;
            return BEREICH_JE_TITELANFANG.TryGetValue(anfang, out wert) ? Freigegeben(wert) : null;
        }

        private static string BereichAusKennung(string typname)
        {
            if (string.IsNullOrEmpty(typname)) return null;
            string t = typname.ToLowerInvariant();
            foreach (string[] regel in BEREICH_JE_KENNUNG)
                if (t.Contains(regel[0])) return Freigegeben(regel[1]);
            return null;
        }

        /// <summary>
        /// Letzte Schranke: nur Eintraege der Positivliste duerfen hinaus.
        /// Alles andere wird zu <see cref="BEREICH_UNBEKANNT"/>.
        /// </summary>
        private static string Freigegeben(string bereich)
        {
            return KiChatKontext.Freigegeben(bereich);
        }

        /// <summary>
        /// Sammelt die Beschriftungen der aktuell gewaehlten Registerkarten
        /// (auch verschachtelt), z. B. "Simulation &gt; Wärmepumpe".
        ///
        /// Registerkartenbeschriftungen sind im gesamten Projekt fest vergeben
        /// (Designer bzw. MyResource) - es gibt keine Stelle, die sie aus
        /// Projektdaten zusammensetzt. Zusaetzlich begrenzen Anzahl und Laenge
        /// den moeglichen Schaden, falls das einmal jemand aendert.
        /// </summary>
        private static string AktiveRegisterkarten()
        {
            try
            {
                Form frm = Form.ActiveForm;
                if (frm == null) return "";
                Form ziel = frm.ActiveMdiChild ?? frm;

                List<string> namen = new List<string>();
                SucheTabs(ziel, namen);
                return string.Join(" > ", namen);
            }
            catch { return ""; }
        }

        private static void SucheTabs(Control parent, List<string> namen)
        {
            foreach (Control c in parent.Controls)
            {
                if (namen.Count >= MAX_REGISTER_EBENEN) return;

                TabControl tc = c as TabControl;
                if (tc != null && tc.SelectedTab != null)
                {
                    string text = tc.SelectedTab.Text;
                    if (!string.IsNullOrWhiteSpace(text) &&
                        text.IndexOf('\n') < 0 &&
                        text.Length <= MAX_REGISTER_LAENGE &&
                        !namen.Contains(text))
                    {
                        namen.Add(text);
                    }
                    SucheTabs(tc.SelectedTab, namen);   // verschachtelte TabControls
                    continue;
                }

                if (c.Controls.Count > 0) SucheTabs(c, namen);
            }
        }

        /// <summary>
        /// Sicherheitsnetz: entfernt den Namen des gerade geoeffneten Projekts,
        /// falls er wider Erwarten doch in den Text geraten ist.
        /// </summary>
        private static string OhneKlarnamen(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            try
            {
                string projekt = Dienste.Projekt.Name;
                if (!string.IsNullOrWhiteSpace(projekt) && projekt.Trim().Length >= 3)
                    text = text.Replace(projekt.Trim(), "(entfernt)");
            }
            catch { }
            return text;
        }
    }
}
