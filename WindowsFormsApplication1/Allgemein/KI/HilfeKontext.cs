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

        /// <summary>Ersatzwert, wenn sich der Bereich nicht sicher zuordnen laesst.</summary>
        public const string BEREICH_UNBEKANNT = "Unbekannter Bereich";

        private const string B_ADMIN = "Administration";
        // E5 (Projektdialoge, 29.08.2026): der Bereich heisst wie das Fenster
        // "Projektassistent". Der Wert ist zugleich Schluessel in
        // WikiWissen.SEITE_JE_BEREICH - beide Stellen nur gemeinsam aendern.
        private const string B_ASSISTENT = "Projektassistent";
        private const string B_BERICHT = "Bericht";
        private const string B_BHKW = "BHKW";
        private const string B_BRAUCHWASSER = "Brauchwasser";
        private const string B_GEBAEUDE = "Gebäude";
        private const string B_HAUPTFENSTER = "Hauptfenster";
        private const string B_HEIZKESSEL = "Heizkessel";
        private const string B_HILFE = "Hilfe";
        private const string B_KLIMADATEN = "Klimadaten";
        private const string B_KOSTEN = "Kosten und Preise";
        private const string B_LIZENZ = "Lizenz";
        private const string B_PHOTOVOLTAIK = "Photovoltaik";
        private const string B_PROJEKT = "Projektverwaltung";
        private const string B_PROZESSWAERME = "Prozesswärme";
        private const string B_PUFFERSPEICHER = "Pufferspeicher";
        private const string B_SIMULATION = "Simulation";
        private const string B_SOLARTHERMIE = "Solarthermie";
        private const string B_STROMSPEICHER = "Stromspeicher";
        private const string B_STROMVERBRAUCHER = "Stromverbraucher";
        private const string B_VARIANTEN = "Varianten";
        private const string B_WAERMEBEDARF = "Wärmebedarf";
        private const string B_WAERMEPUMPE = "Wärmepumpe";
        private const string B_WIRTSCHAFT = "Wirtschaftlichkeit";

        // Von Masken ueber SetzeBereich() gesetzte, bewusst feinere Bezeichnungen.
        // Sie enthalten nur Fach- und Bedienbegriffe, keine Projektdaten.
        private const string B_QUELLE_ERDREICH =
            "Wärmequelle Erdreich (Quellsystem, Bodentyp, Auslegungsprüfung VDI 4640)";
        private const string B_SIM_KONFIG =
            "Simulation Konfiguration (Erzeuger definieren, Pufferspeicher zuordnen)";
        private const string B_SIM_DETAIL = "Detaillierte Simulation";

        /// <summary>
        /// Alle Zeichenketten, die als Bereichsangabe den Rechner verlassen duerfen.
        /// Was hier nicht steht, wird zu <see cref="BEREICH_UNBEKANNT"/>.
        /// </summary>
        private static readonly HashSet<string> POSITIVLISTE = new HashSet<string>(StringComparer.Ordinal)
        {
            BEREICH_UNBEKANNT,
            B_ADMIN, B_ASSISTENT, B_BERICHT, B_BHKW, B_BRAUCHWASSER, B_GEBAEUDE,
            B_HAUPTFENSTER, B_HEIZKESSEL, B_HILFE, B_KLIMADATEN, B_KOSTEN, B_LIZENZ,
            B_PHOTOVOLTAIK, B_PROJEKT, B_PROZESSWAERME, B_PUFFERSPEICHER, B_SIMULATION,
            B_SOLARTHERMIE, B_STROMSPEICHER, B_STROMVERBRAUCHER, B_VARIANTEN,
            B_WAERMEBEDARF, B_WAERMEPUMPE, B_WIRTSCHAFT,
            B_QUELLE_ERDREICH, B_SIM_KONFIG, B_SIM_DETAIL
        };

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
            { "Form_LizenzVerwaltung",       B_LIZENZ },
            { "Form_Lizenz",                 B_LIZENZ },

            { "Form_BHKWAdmin",              B_BHKW },

            { "Form_Brauchwasser",           B_BRAUCHWASSER },
            { "Form_Brauchwasser_Admin",     B_BRAUCHWASSER },
            { "Form_EingBrauchwasserTyp",    B_BRAUCHWASSER },
            { "Form_EingDBBrauchwasser",     B_BRAUCHWASSER },

            { "Form_EingGebTyp",             B_GEBAEUDE },
            { "Form_GebWohnflaeche",         B_GEBAEUDE },
            { "Form_Gebaeude",               B_GEBAEUDE },
            { "Form_Gebaeude1",              B_GEBAEUDE },
            { "Form_Gebaeude2",              B_GEBAEUDE },

            { "Form_Heizkessel_Admin",       B_HEIZKESSEL },
            { "Form_Heizkessel_einlesen",    B_HEIZKESSEL },

            { "Form_HelpPopup",              B_HILFE },
            { "Form_KiChat",                 B_HILFE },

            { "Form_Klimadaten",             B_KLIMADATEN },

            { "Form_Kosten_Auswahl",         B_KOSTEN },

            { "Form_AdminPV",                B_PHOTOVOLTAIK },
            { "Form_CECImport",              B_PHOTOVOLTAIK },

            // Nachgetragen mit H7: Die Datei heisst Form_CECImport.cs, die KLASSE
            // aber Main_PV_Test - nachgeschlagen wird der Typname, der Eintrag
            // darueber griff also nie. Er bleibt stehen, falls die Klasse einmal
            // wie ihre Datei heisst.
            { "Main_PV_Test",                B_PHOTOVOLTAIK },

            // P6 nachgetragen: die Huellform "Projekt oeffnen" aus Paket P3. Ohne
            // Eintrag griff erst die Kennungsstufe ("projekt" im Typnamen) - das
            // Ergebnis war zwar dasselbe, aber unbeabsichtigt.
            { "Form_ProjektAuswahl",         B_PROJEKT },
            { "Form_ProjektDelete",          B_PROJEKT },
            { "Form_ProjektSpeichernUnter",  B_PROJEKT },

            { "Form_EingDBProzess",          B_PROZESSWAERME },
            { "Form_EingProzTyp",            B_PROZESSWAERME },
            { "Form_Prozesswaerme",          B_PROZESSWAERME },
            { "Form_Prozesswaerme_Admin",    B_PROZESSWAERME },

            { "Form_PufferSp_Admin",         B_PUFFERSPEICHER },
            { "Form_PufferSp_Bearbeiten",    B_PUFFERSPEICHER },
            { "Form_PufferSp_Projekt",       B_PUFFERSPEICHER },
            { "Form_PufferSp_einlesen",      B_PUFFERSPEICHER },

            { "DashboardForm",               B_SIMULATION },
            { "ErzeugerKarte",               B_SIMULATION },
            { "SpeicherKarte",               B_SIMULATION },
            { "NavigatorStrom",              B_SIMULATION },
            { "NavigatorUebersicht",         B_SIMULATION },
            { "NavigatorWaerme",             B_SIMULATION },
            { "Form_QuelleErdreich",         B_QUELLE_ERDREICH },
            { "Form_QuellePufferspeicher",   B_SIMULATION },
            { "Form_Quellprofil",            B_SIMULATION },
            { "Form_Simulation_Config",      B_SIM_KONFIG },
            { "Form_Simulation_Detail",      B_SIM_DETAIL },
            { "Form_Waermesenke",            B_SIMULATION },

            { "Form_SolarKollektorenAdmin",  B_SOLARTHERMIE },
            { "Form_SolarKollektoren_einlesen", B_SOLARTHERMIE },
            { "Form_Solarganglinie_Admin",   B_SOLARTHERMIE },

            { "Form_AdminStromspeicher",     B_STROMSPEICHER },
            { "Form_PeakShaving",            B_STROMSPEICHER },
            { "Form_SpeicherOptimierung",    B_STROMSPEICHER },
            { "Form_SpeicherVariantenVergleich", B_STROMSPEICHER },

            // Nachgetragen mit H7: Entwicklermaske hinter dem unbeschrifteten Knopf
            // "SP" auf FormMain - sie ordnet dem Projekt einen Stromspeicher zu.
            { "Form_StromTest",              B_STROMSPEICHER },

            { "Form_EingDBStromverbraucher", B_STROMVERBRAUCHER },
            { "Form_EingStromTyp",           B_STROMVERBRAUCHER },
            { "Form_GanglinieImportOptionen",B_STROMVERBRAUCHER },
            { "Form_GanglinieProtokoll",     B_STROMVERBRAUCHER },
            { "Form_Stromganglinie",         B_STROMVERBRAUCHER },
            { "Form_Stromganglinie_Admin",   B_STROMVERBRAUCHER },
            { "Form_Stromverbraucher",       B_STROMVERBRAUCHER },
            { "Form_Stromverbraucher_Admin", B_STROMVERBRAUCHER },

            { "Form_AdminWaermeeinlesen",    B_WAERMEBEDARF },   // H7 nachgetragen
            { "Form_Waermebedarf",           B_WAERMEBEDARF },

            // Kenndaten ist das Kennfeld EINER Waermepumpe (Stuetzstellen
            // Vorlauftemperatur / Ptherm / COP), aufgerufen aus Form_WP - der
            // Klassenname sagt das nicht, deshalb der Eintrag (H7).
            { "Form_WP_einlesen",            B_WAERMEPUMPE },


            { "WizardParent",                B_ASSISTENT },
            { "Wizard_Projekt",              B_ASSISTENT },
            { "Wizard_Komponenten",          B_ASSISTENT },
            { "Wizard_Stromlastgang",        B_ASSISTENT }
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
            if (string.IsNullOrWhiteSpace(bereich)) return BEREICH_UNBEKANNT;
            string b = bereich.Trim();
            return POSITIVLISTE.Contains(b) ? b : BEREICH_UNBEKANNT;
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
