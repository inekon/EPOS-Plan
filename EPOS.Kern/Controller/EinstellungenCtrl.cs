using System;
using System.Collections.Generic;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die neun globalen Anwendungseinstellungen als Wertesatz (iU9-W14c.0i).
    ///
    /// <para>Die Namen sind die Schlüssel aus <c>Properties.Settings</c> — sie sind
    /// eingefroren und stehen so auch in der <c>user.config</c> des Anwenders.</para>
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
        public string AllgemeinPfad = "";
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
    /// Liest und <b>schreibt</b> die neun globalen Anwendungseinstellungen
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
        // Lesen (Form_AdminSettings_Load)
        // =====================================================================

        /// <summary>
        /// Der gespeicherte Stand, ergänzt um die Vorgaben. <b>Die Reihenfolge zählt:</b>
        /// erst der VDI-Pfad, dann Export und Import AUF IHM AUFBAUEND, dann DB-Pfad,
        /// DB-Name, Allgemein-Pfad und zuletzt die drei URLs — wörtlich wie <c>Load</c>.
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
            return Lesen();
        }

        // =====================================================================
        // Schreiben (Btn_Speichern_Click)
        // =====================================================================

        /// <summary>
        /// Schreibt die neun Werte und legt die fünf Ordner an, falls sie fehlen.
        ///
        /// <para><b>Die Reihenfolge ist die des Vorläufers:</b> erst die Werte in die
        /// Settings, dann die Ordner — und nur wenn die Ordner stehen, wird
        /// <c>Save()</c> gerufen. Schlägt das Anlegen fehl, bleibt der gespeicherte
        /// Stand, was er war.</para>
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
            Properties.Settings.Default.AllgemeinPath = s.AllgemeinPfad ?? "";

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

            Properties.Settings.Default.Save();
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
