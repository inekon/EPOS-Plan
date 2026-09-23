using System;
using System.Collections.Generic;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die zwölf globalen Anwendungseinstellungen als Wertesatz (iU9-W14c.0i).
    ///
    /// <para>Die Namen sind die Schlüssel aus <c>Properties.Settings</c> — sie sind
    /// eingefroren und stehen so auch in der <c>user.config</c> des Anwenders.</para>
    ///
    /// <para><b>Die Rubrik „Klimadaten"</b> führt drei Adressen: die PVGIS-Schnittstelle
    /// (<c>PVGISUrl</c>, aus der Rubrik „Web-Schnittstellen" hierher gewandert), das
    /// Portal der DWD-Testreferenzjahre (<c>TRYPortalUrl</c>) und die Ablage der
    /// TRY-Regionaldaten (<c>TRYRegionalUrl</c>).</para>
    ///
    /// <para><b>Die Rubrik „Diagramme"</b> führt den zwölften Wert
    /// (<c>DiagrammFarben</c>): die vom Anwender geänderten Farbrollen als kompakten
    /// Text <c>ROLLE=#RRGGBB;…</c>. Leer heißt Hausfarben. Format, Rollenliste und das
    /// Übernehmen in <c>Farbpalette.Aktuell</c> stehen in
    /// <see cref="Zeichnung.Diagrammfarben"/>.</para>
    ///
    /// <para><b>Dazu ein Wert, der NICHT in <c>Properties.Settings</c> liegt:</b> die
    /// Programmeinstellung „Neue Projekte mit Kühlung anlegen"
    /// (<see cref="NeueProjekteMitKuehlung"/>, E27/K10). Sie liegt in
    /// <c>Dienste.Einstellungen</c>, damit der Kern sie auf jeder Plattform und ohne
    /// Windows-Ablage lesen kann (Kühlkonzept 7.2).</para>
    /// </summary>
    public sealed class Einstellungensatz
    {
        public string VdiPfad = "";
        public string DbExportPfad = "";
        public string DbImportPfad = "";
        public string DbPfad = "";
        public string DbName = "";
        public string WikiUrl = "";
        public string PvgisUrl = "";
        public string GeokodierungUrl = "";
        public string TryPortalUrl = "";
        public string TryRegionalUrl = "";
        public string AllgemeinPfad = "";

        /// <summary>
        /// Die geänderten Diagrammfarben als <c>ROLLE=#RRGGBB;…</c>; leer = Hausfarben
        /// (<c>Zeichnung.Diagrammfarben.SCHLUESSEL</c>).
        /// </summary>
        public string DiagrammFarben = "";

        /// <summary>
        /// „Neue Projekte mit Kühlung anlegen" (E27, K10; Kühlkonzept 7.2, 8.3) —
        /// <b>Vorgabe aus</b>. Bestimmt allein den Anfangswert der Projekteinstellung
        /// <c>Tab_Einstellungen.Kuehlbetrieb</c> eines NEU angelegten Projekts und schaltet
        /// sonst nichts: kein vorhandenes Projekt, keinen Lauf. Gelesen und geschrieben über
        /// <c>Dienste.Einstellungen</c> (<see cref="EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG"/>),
        /// nicht über <c>Properties.Settings</c>.
        /// </summary>
        public bool NeueProjekteMitKuehlung;
    }

    /// <summary>Das Ergebnis des Speicherns: gelungen oder mit Grund gescheitert.</summary>
    public sealed class SpeicherBefund
    {
        public SpeicherBefund(bool ok, string meldung)
        {
            Ok = ok;
            Meldung = meldung ?? "";
        }

        public bool Ok { get; }

        /// <summary>Der Fehlertext; leer, wenn alles gelungen ist.</summary>
        public string Meldung { get; }
    }

    /// <summary>
    /// Liest und <b>schreibt</b> die zwölf globalen Anwendungseinstellungen
    /// (iU9-W14c.0i).
    ///
    /// <para><b>Warum es das gibt</b> (Befund W14c-B57): Bis hierher gab es
    /// <b>keinen schreibenden Weg</b> zu <c>Properties.Settings</c> außerhalb der
    /// Maske <c>Form_AdminSettings</c>. <c>IEinstellungen</c> ersetzt laut eigenem
    /// Kopfkommentar nur die LESENDEN Zugriffe, und
    /// <c>SettingsEinstellungen.Schreib</c> schreibt in die Registry, nicht nach
    /// <c>Properties.Settings</c>. Eine Razor-Komponente kennt weder das eine noch
    /// das andere — sie braucht einen Controller.</para>
    ///
    /// <para><b>Kein <c>SpecialFolder</c></b> (Befund W14c-B55, Wächter!): Die vier
    /// Vorgabepfade der Maske griffen unmittelbar auf
    /// <c>Environment.GetFolderPath</c> zu; im Kern ist das verboten. Hier stehen sie
    /// über <c>Dienste.Pfade.BenutzerLokal</c> und <c>Dienste.Pfade.Gemeinsam</c> —
    /// dieselben Ordner, über die Plattformschnittstelle geholt.</para>
    ///
    /// <para><b>Der Parameter <c>szPath</c> ist weg</b> (Befund W14c-B54): Er wurde
    /// nie gelesen, beide Aufrufer übergaben <c>""</c>.</para>
    ///
    /// <para><b>Der DB-Name landet im Namensfeld, nicht im Pfadfeld</b> (Befund
    /// W14c-B53, A-12): <c>btn_Standardwerte_Click</c> überschrieb den gerade
    /// gesetzten DB-PFAD mit dem DB-NAMEN; gemeint war das Namensfeld.</para>
    /// </summary>
    public static class EinstellungenCtrl
    {
        /// <summary>Der Anwendungsordner unterhalb von <c>LocalApplicationData</c>.</summary>
        private const string ORDNER_WP_PLAN = "WP-Plan";

        /// <summary>Der Datenbankordner unterhalb von <c>CommonApplicationData</c>.</summary>
        private const string ORDNER_EPOS_PLAN = "EPOS_PLAN";

        /// <summary>Der Unterordner der Datenbanksicherungen unter dem VDI-Pfad.</summary>
        private const string UNTERORDNER_BACKUP = "Backup";

        /// <summary>Der Unterordner der Datenbankimporte unter dem VDI-Pfad.</summary>
        private const string UNTERORDNER_IMPORT = "Import";

        // =====================================================================
        // Die Programmeinstellung „Neue Projekte mit Kühlung anlegen" (E27, K10)
        // =====================================================================

        /// <summary>
        /// Der Schlüssel in <c>Dienste.Einstellungen</c> — ASCII und <b>eingefroren</b> wie
        /// jeder Persistenzwert (Kühlkonzept 7.2, B-K11): unter Windows ein DWord unter
        /// <c>HKCU\Software\wp-plan</c>, auf iOS <c>wp-plan.NeueProjekteMitKuehlung</c> in den
        /// Preferences, ohne Oberfläche die flüchtige Ablage (dort „aus"). Er ist
        /// <b>kein</b> <c>Properties.Settings</c>-Schlüssel.
        /// </summary>
        public const string SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG = "NeueProjekteMitKuehlung";

        /// <summary>Die Werksvorgabe: aus (E27).</summary>
        public const bool NEUE_PROJEKTE_MIT_KUEHLUNG_VORGABE = false;

        /// <summary>
        /// Liest die Programmeinstellung; nichts hinterlegt, unlesbar oder eine Ausnahme
        /// der Ablage heißen „aus".
        ///
        /// <para><b>Zwei Leser, und nur diese zwei</b> (Wächter
        /// <c>KuehlbetriebProgrammeinstellungTests</c>): <see cref="Lesen"/> für den
        /// Einstellungsdialog und <see cref="KonfigurationCtrl.KuehlbetriebAnfangswertSetzen"/>
        /// beim Anlegen eines Projekts. Kein Lauf, kein Lesen der Projektkonfiguration und kein
        /// Schemaschritt fragt sie — ein Projekt ohne Einstellungssatz ist „aus", nicht „wie
        /// die Programmeinstellung" (Kühlkonzept 7.2).</para>
        /// </summary>
        public static bool NeueProjekteMitKuehlungLesen()
        {
            try
            {
                return Dienste.Einstellungen.LiesZahl(SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG,
                                                      NEUE_PROJEKTE_MIT_KUEHLUNG_VORGABE ? 1 : 0) != 0;
            }
            catch
            {
                return NEUE_PROJEKTE_MIT_KUEHLUNG_VORGABE;
            }
        }

        /// <summary>Schreibt die Programmeinstellung als 0/1 (<c>SchreibZahl</c>).</summary>
        public static void NeueProjekteMitKuehlungSchreiben(bool an)
        {
            Dienste.Einstellungen.SchreibZahl(SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG, an ? 1 : 0);
        }

        // =====================================================================
        // Lesen (Form_AdminSettings_Load)
        // =====================================================================

        /// <summary>
        /// Der gespeicherte Stand, ergänzt um die Vorgaben. <b>Die Reihenfolge zählt:</b>
        /// erst der VDI-Pfad, dann Export und Import AUF IHM AUFBAUEND, dann DB-Pfad,
        /// DB-Name, Allgemein-Pfad und zuletzt die URLs — wörtlich wie <c>Load</c>.
        ///
        /// <para>Die zwei TRY-Adressen kommen zuletzt; sie haben keinen Vorgabeweg über
        /// <c>Dienste.Pfade</c>, sondern nur die Werksvorgabe der Einstellung.</para>
        /// </summary>
        public static Einstellungensatz Lesen()
        {
            var s = new Einstellungensatz();
            s.VdiPfad = VdiPfadOderVorgabe();
            s.DbExportPfad = ExportPfadOderVorgabe(s.VdiPfad);
            s.DbImportPfad = ImportPfadOderVorgabe(s.VdiPfad);
            s.DbPfad = DbPfadOderVorgabe();
            s.DbName = Properties.Settings.Default.DBName ?? "";
            s.AllgemeinPfad = AllgemeinPfadOderVorgabe();
            s.WikiUrl = Properties.Settings.Default.WordPressUrl ?? "";
            s.PvgisUrl = Properties.Settings.Default.PVGISUrl ?? "";
            s.GeokodierungUrl = Properties.Settings.Default.GeoKodierung ?? "";
            s.TryPortalUrl = Properties.Settings.Default.TRYPortalUrl ?? "";
            s.TryRegionalUrl = Properties.Settings.Default.TRYRegionalUrl ?? "";
            s.DiagrammFarben = Properties.Settings.Default.DiagrammFarben ?? "";
            s.NeueProjekteMitKuehlung = NeueProjekteMitKuehlungLesen();
            return s;
        }

        /// <summary>
        /// Die Werksstandards: <c>Properties.Settings.Default.Reset()</c> setzt alles im
        /// Speicher zurück, danach greifen dieselben Vorgabewege wie beim Laden.
        ///
        /// <para><b>Zurücksetzen SPEICHERT NICHT</b> — wörtlich wie der Vorläufer: „Die
        /// Standardwerte wurden geladen. Mit ‚Speichern' werden sie übernommen."</para>
        /// </summary>
        public static Einstellungensatz Zuruecksetzen()
        {
            Properties.Settings.Default.Reset();
            Einstellungensatz s = Lesen();

            // Der Wert aus Dienste.Einstellungen kennt kein Reset() - die Werksvorgabe wird
            // hier gesetzt und wie alles andere erst mit „OK" gespeichert.
            s.NeueProjekteMitKuehlung = NEUE_PROJEKTE_MIT_KUEHLUNG_VORGABE;
            return s;
        }

        // =====================================================================
        // Schreiben (Btn_Speichern_Click)
        // =====================================================================

        /// <summary>
        /// Schreibt die zwölf Werte und legt die fünf Ordner an, falls sie fehlen.
        ///
        /// <para><b>Die Reihenfolge ist die des Vorläufers:</b> erst die Werte in die
        /// Settings, dann die Ordner — und nur wenn die Ordner stehen, wird
        /// <c>Save()</c> gerufen. Schlägt das Anlegen fehl, bleibt der gespeicherte
        /// Stand, was er war.</para>
        ///
        /// <para><b>Zuletzt wird die Diagrammpalette neu geladen</b>
        /// (<c>Zeichnung.Diagrammfarben.Uebernehmen</c>): Damit trägt schon das nächste
        /// gezeichnete Bild die eingestellten Farben, und der Bericht ebenso — beide
        /// malen über denselben <c>SkiaMaler</c>. Ein Neustart ist nicht nötig. Es steht
        /// hier und nicht in der Windows-Hülle, weil jede Schale denselben Weg nimmt.</para>
        /// </summary>
        public static SpeicherBefund Speichern(Einstellungensatz s)
        {
            if (s == null) return new SpeicherBefund(false, "");

            Properties.Settings.Default.VDI3805Path = s.VdiPfad ?? "";
            Properties.Settings.Default.DBExportPath = s.DbExportPfad ?? "";
            Properties.Settings.Default.DBImportPath = s.DbImportPfad ?? "";
            Properties.Settings.Default.DBPath = s.DbPfad ?? "";
            Properties.Settings.Default.DBName = s.DbName ?? "";
            Properties.Settings.Default.WordPressUrl = s.WikiUrl ?? "";
            Properties.Settings.Default.PVGISUrl = s.PvgisUrl ?? "";
            Properties.Settings.Default.GeoKodierung = s.GeokodierungUrl ?? "";
            Properties.Settings.Default.TRYPortalUrl = s.TryPortalUrl ?? "";
            Properties.Settings.Default.TRYRegionalUrl = s.TryRegionalUrl ?? "";
            Properties.Settings.Default.AllgemeinPath = s.AllgemeinPfad ?? "";
            Properties.Settings.Default.DiagrammFarben = s.DiagrammFarben ?? "";

            try
            {
                foreach (string pfad in new[] { s.VdiPfad, s.DbImportPfad, s.DbPfad,
                                                s.DbExportPfad, s.AllgemeinPfad })
                    if (!string.IsNullOrWhiteSpace(pfad) && !Directory.Exists(pfad))
                        Directory.CreateDirectory(pfad);
            }
            catch (Exception ex)
            {
                return new SpeicherBefund(false,
                    string.Format(MyResource.Resource.ADM_SET_MSG_ORDNER_FEHLER, ex.Message));
            }

            // Die Programmeinstellung „Neue Projekte mit Kühlung anlegen" (E27, K10) geht
            // über Dienste.Einstellungen - VOR dem Save(), damit ein Fehler hier den
            // gespeicherten Stand der Settings so lässt, wie er war.
            try
            {
                NeueProjekteMitKuehlungSchreiben(s.NeueProjekteMitKuehlung);
            }
            catch (Exception ex)
            {
                return new SpeicherBefund(false,
                    string.Format(MyResource.Resource.ADM_SET_MSG_KUEHLUNG_FEHLER, ex.Message));
            }

            Properties.Settings.Default.Save();

            // Die Palette der Diagramme traegt den neuen Stand ab dem naechsten Bild -
            // ohne Neustart und ohne dass eine Huelle daran denken muss.
            Zeichnung.Diagrammfarben.Uebernehmen();

            return new SpeicherBefund(true, "");
        }

        // =====================================================================
        // Die vier Vorgabewege (GetConfiguredOrDefault…)
        // =====================================================================

        /// <summary>Gespeicherter VDI-Pfad, sonst <c>LocalApplicationData\WP-Plan</c>.</summary>
        public static string VdiPfadOderVorgabe()
        {
            string wert = Properties.Settings.Default.VDI3805Path;
            return string.IsNullOrWhiteSpace(wert)
                ? Dienste.Pfade.Verbinde(Dienste.Pfade.BenutzerLokalBasis, ORDNER_WP_PLAN)
                : wert;
        }

        /// <summary>
        /// <b>Der Herstellerdatenpfad</b> — der Startordner der Dateiwähler der
        /// Importmasken (Anwenderentscheid <b>W6‑O‑9</b> vom 06.09.2026).
        ///
        /// <para>Drei Stufen, in dieser Reihenfolge:</para>
        /// <list type="number">
        /// <item>der <b>gespeicherte</b> <c>VDI3805Path</c> — was der Anwender in den
        /// Einstellungen eingetragen hat, gilt; daran ändert die Auslieferung
        /// nichts;</item>
        /// <item>sonst der <b>ausgelieferte</b> Ordner <c>Dienste.Pfade.Herstellerdaten</c>
        /// — beim Anwender <c>{app}\VDI-3805-Daten</c>, im Entwicklungsstand die
        /// Repowurzel. <b>Das ist die Wirkung von W6‑O‑9:</b> Nach einer frischen
        /// Installation macht der Dateiwähler ohne Zutun im mitgelieferten Bestand
        /// auf;</item>
        /// <item>sonst der bisherige Vorgabeweg <see cref="VdiPfadOderVorgabe"/>
        /// (<c>LocalApplicationData\WP-Plan</c>) — für Stände ohne den Ordner.</item>
        /// </list>
        ///
        /// <para><b>Warum das NICHT dasselbe ist wie <see cref="VdiPfadOderVorgabe"/>.</b>
        /// Jener Pfad steht in der Einstellungsmaske und wird auch BESCHRIEBEN — die
        /// Solarganglinien-Verwaltung legt gewählte Dateien darunter ab
        /// (<c>…\Solarthermie</c>), und <see cref="Speichern"/> erzeugt den Ordner, wenn
        /// er fehlt. Der Auslieferungsordner liegt in „Programme" und ist für den
        /// Anwender schreibgeschützt; ihn zur Vorgabe des SCHREIBENDEN Pfades zu machen,
        /// brächte einen Fehlschlag beim ersten Ablegen. Deshalb zwei Wege: lesen aus der
        /// Auslieferung, schreiben ins Benutzerprofil.</para>
        /// </summary>
        public static string HerstellerdatenpfadOderVorgabe()
        {
            string wert = Properties.Settings.Default.VDI3805Path;
            if (!string.IsNullOrWhiteSpace(wert)) return wert;

            string ausgeliefert = Dienste.Pfade.Herstellerdaten;
            return string.IsNullOrWhiteSpace(ausgeliefert)
                ? VdiPfadOderVorgabe()
                : ausgeliefert;
        }

        /// <summary>Gespeicherter Export-Ordner, sonst <c>&lt;VDI-Pfad&gt;\Backup</c>.</summary>
        public static string ExportPfadOderVorgabe(string vdiPfad)
        {
            string wert = Properties.Settings.Default.DBExportPath;
            return string.IsNullOrWhiteSpace(wert)
                ? Dienste.Pfade.Verbinde(vdiPfad ?? "", UNTERORDNER_BACKUP)
                : wert;
        }

        /// <summary>Gespeicherter Import-Ordner, sonst <c>&lt;VDI-Pfad&gt;\Import</c>.</summary>
        public static string ImportPfadOderVorgabe(string vdiPfad)
        {
            string wert = Properties.Settings.Default.DBImportPath;
            return string.IsNullOrWhiteSpace(wert)
                ? Dienste.Pfade.Verbinde(vdiPfad ?? "", UNTERORDNER_IMPORT)
                : wert;
        }

        /// <summary>
        /// Gespeicherter Datenbankordner, sonst <c>CommonApplicationData\EPOS_PLAN</c>.
        /// <b>Nicht</b> <c>Dienste.Pfade.Gemeinsam</c>: Das ist
        /// <c>CommonApplicationData\WP-Plan</c>, und der Datenbankordner heißt seit je
        /// <c>EPOS_PLAN</c>.
        /// </summary>
        public static string DbPfadOderVorgabe()
        {
            string wert = Properties.Settings.Default.DBPath;
            if (!string.IsNullOrWhiteSpace(wert)) return wert;

            // Gemeinsam ist "<CommonApplicationData>\WP-Plan" - eine Ebene hoeher liegt
            // der Ordner, unter dem EPOS_PLAN steht.
            string gemeinsam = Dienste.Pfade.Gemeinsam ?? "";
            string basis = Path.GetDirectoryName(gemeinsam);
            return Dienste.Pfade.Verbinde(string.IsNullOrEmpty(basis) ? gemeinsam : basis,
                                          ORDNER_EPOS_PLAN);
        }

        /// <summary>
        /// Gespeicherter Allgemein-Ordner, sonst <c>LocalApplicationData\WP-Plan</c>.
        /// <b>Ohne Parameter</b> — der frühere <c>szPath</c> wurde nie gelesen
        /// (Befund W14c-B54).
        /// </summary>
        public static string AllgemeinPfadOderVorgabe()
        {
            string wert = Properties.Settings.Default.AllgemeinPath;
            return string.IsNullOrWhiteSpace(wert)
                ? Dienste.Pfade.Verbinde(Dienste.Pfade.BenutzerLokalBasis, ORDNER_WP_PLAN)
                : wert;
        }
    }
}
