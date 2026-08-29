using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Import einer Spotmarktpreis-Datei (Fachkonzept Stromspeicher 4.1 a,
    /// Arbeitspaket AP4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zwei Schritte, mit Absicht getrennt.</b> „Datei prüfen" liest und bereitet
    /// auf, ohne zu speichern; erst „Übernehmen" legt die Reihe an. Der Anwender sieht
    /// also das vollständige Validierungsprotokoll — übersprungene Schalttagszeilen,
    /// gemittelte Doppelstunde, ergänzte Umstellungsstunde, Wertebereich, negative
    /// Preise — BEVOR 8.760 Zeilen in die Datenbank gehen.
    /// </para>
    /// <para>
    /// <b>Der Dialog rechnet nichts.</b> Zerlegen macht <c>SpotpreisLeser</c>, den
    /// Kalender die Engine (<c>SpotreihenAufbereitung</c>), das Zusammenfügen
    /// <c>SpotpreisImportCtrl</c>. Hier stehen nur Dateiauswahl, Anzeige und der
    /// Speichern-Knopf — deshalb hängt die Verifikation der Zeitzonen- und
    /// Schaltjahrbehandlung nicht an dieser Maske.
    /// </para>
    /// <para>
    /// Die Oberfläche steht in <c>Form_SpotpreisImport.Designer.cs</c>, weiterhin ohne
    /// eigene <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und
    /// werden in <see cref="TexteSetzen"/> gesetzt; im Designer stehen dieselben Texte
    /// nur als Entwurfsbild.
    /// </para>
    /// </remarks>
    public partial class Form_SpotpreisImport : Form
    {
        private readonly int _idProjekt;
        private readonly SpotpreisImportCtrl _ctrl = new SpotpreisImportCtrl();
        private SpotpreisImportCtrl.Lauf _lauf;

        /// <summary>Die zuletzt angelegte <c>Tab_Preisreihe.ID</c>; 0, wenn nichts gespeichert wurde.</summary>
        public int AngelegteReiheId { get; private set; }

        public Form_SpotpreisImport(int idProjekt)
        {
            _idProjekt = idProjekt;
            PreisreiheCtrl.StelleTabellenSicher();

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske ist ein FixedDialog mit fest
            // gerechneten Pixelpositionen, und die Anwendung läuft DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). Vor der Designer-Umstellung
            // wurde AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls
            // keine Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteSetzen();
        }

        // ==================================================================
        // Oberfläche — Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen seit der Designer-Umstellung in
        // Form_SpotpreisImport.Designer.cs. Designer-Code trägt keine Kommentare;
        // die Pixelentscheidungen stehen deshalb hier.
        //
        // Design-Politur 21.08.2026 — Echttexte im Designer, geprüfte Abstände,
        // einheitliche Fußknöpfe. Alle Breiten mit TextRenderer gemessen (Segoe UI 9 pt,
        // deutsch und englisch); die Maske skaliert nicht (AutoScaleMode.None,
        // DpiUnaware).
        //
        // * Jede Beschriftung trägt im Designer jetzt den deutschen Echttext aus
        //   MyResource statt des Feldnamens — der Anwender sieht im VS-Designer das
        //   Bild der laufenden Maske. Die Anzeige selbst kommt unverändert aus
        //   TexteSetzen(); die Designer-Texte werden im Betrieb sofort überschrieben.
        //   _lblStatus bleibt bewusst LEER: Diese Zeile ist auch zur Laufzeit leer,
        //   bis eine Datei geprüft wurde.
        // * Fußknöpfe einheitlich 30 px hoch (vorher 23): _btnUebernehmen 120 x 30
        //   (468/486), _btnSchliessen 110 x 30 (598/486). Die rechte Kante der Gruppe
        //   bleibt bei x = 708 — dieselben 12 px Rand wie links; zwischen den Knöpfen
        //   liegen 10 px, darüber 8 px zur Unterkante von _lblStatus (478).
        // * ClientSize 720 x 520 -> 720 x 528: Die 7 px höheren Knöpfe ließen unten nur
        //   noch 6 px Luft. Mit 528 bleiben 12 px, passend zum seitlichen Rand; die
        //   Breite ist unverändert.
        // * _btnWaehlen 1 px tiefer (600/62 -> 600/63): gleiche Oberkante wie das
        //   Pfadfeld _tbPfad (110/63), beide 23 px hoch. Die Breite 108 bleibt — der
        //   deutsche Text misst 93 px, der englische 69 px.
        // * Gemessen und deshalb NICHT geändert: _lblInfo (696 x 46) trägt den
        //   Hinweistext in beiden Sprachen mit zwei Zeilen (deutsch 671 x 30, englisch
        //   653 x 30). _chkStamm (AutoSize ab x = 430) endet bei rund 654 und bleibt
        //   damit im Formular. _lblStatus (480 x 20) fasst die längste Statusmeldung
        //   (fett gemessen 277 px).
        //
        // Zwei Fachknöpfe statt OK/Abbrechen: „Uebernehmen" ist keine Bestätigung,
        // sondern der Schreibvorgang selbst (8.760 Zeilen) und erst nach erfolgreicher
        // Prüfung freigeschaltet — eine Beschriftung „OK" würde genau das verdecken.
        // Der zweite Knopf trägt bereits SIM_BTN_ABBRECHEN und ist CancelButton.

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Entwurfstexte.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.PREIS_IMPORT_TITEL;
            _lblInfo.Text = MyResource.Resource.PREIS_IMPORT_INFO;
            _lblDatei.Text = MyResource.Resource.PREIS_IMPORT_LABEL_DATEI;
            _btnWaehlen.Text = MyResource.Resource.PREIS_IMPORT_BTN_DATEI;
            _lblBezeichner.Text = MyResource.Resource.PREIS_IMPORT_LABEL_BEZEICHNER;
            _chkStamm.Text = MyResource.Resource.PREIS_IMPORT_CHK_STAMM;
            _lblProtokoll.Text = MyResource.Resource.PREIS_IMPORT_LABEL_PROTOKOLL;
            _btnUebernehmen.Text = MyResource.Resource.PREIS_IMPORT_BTN_UEBERNEHMEN;
            _btnSchliessen.Text = MyResource.Resource.SIM_BTN_ABBRECHEN;
        }

        // ==================================================================
        // Ereignisse
        // ==================================================================

        private void btnWaehlen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = MyResource.Resource.PREIS_IMPORT_DATEIFILTER;
                dlg.CheckFileExists = true;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                _tbPfad.Text = dlg.FileName;
                if (_tbBezeichner.Text.Trim().Length == 0)
                    _tbBezeichner.Text = Path.GetFileNameWithoutExtension(dlg.FileName);

                DateiPruefen(dlg.FileName);
            }
        }

        private void DateiPruefen(string pfad)
        {
            _btnUebernehmen.Enabled = false;
            _lauf = null;

            Cursor vorher = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _lauf = _ctrl.Pruefe(pfad);
                _tbProtokoll.Text = _lauf.Protokoll.Replace("\n", Environment.NewLine);

                _btnUebernehmen.Enabled = _lauf.Erfolgreich;
                _lblStatus.Text = _lauf.Erfolgreich
                    ? string.Format(MyResource.Resource.PREIS_IMPORT_STATUS_BEREIT, _lauf.Jahr)
                    : MyResource.Resource.PREIS_IMPORT_STATUS_UNBRAUCHBAR;
                _lblStatus.ForeColor = _lauf.Erfolgreich ? Color.DarkGreen : Color.Firebrick;
            }
            catch (Exception ex)
            {
                _tbProtokoll.Text = ex.Message;
                _lblStatus.Text = MyResource.Resource.PREIS_IMPORT_STATUS_UNBRAUCHBAR;
                _lblStatus.ForeColor = Color.Firebrick;
            }
            finally
            {
                this.Cursor = vorher;
            }
        }

        private void btnUebernehmen_Click(object sender, EventArgs e)
        {
            if (_lauf == null || !_lauf.Erfolgreich) return;

            Cursor vorher = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            _btnUebernehmen.Enabled = false;
            try
            {
                int ziel = _chkStamm.Checked ? 0 : _idProjekt;

                int id = _ctrl.Speichere(_lauf, _tbBezeichner.Text.Trim(), ziel, Fortschritt);
                if (id <= 0)
                {
                    _lblStatus.Text = MyResource.Resource.PREIS_IMPORT_STATUS_NICHT_GESPEICHERT;
                    _lblStatus.ForeColor = Color.Firebrick;
                    _btnUebernehmen.Enabled = true;
                    return;
                }

                AngelegteReiheId = id;
                _lblStatus.Text = string.Format(MyResource.Resource.PREIS_IMPORT_STATUS_GESPEICHERT,
                                                id, _lauf.Reihe.StundenreiheCtKwh.Length);
                _lblStatus.ForeColor = Color.DarkGreen;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            finally
            {
                this.Cursor = vorher;
            }
        }

        /// <summary>
        /// Fortschrittsanzeige beim Schreiben. 8.760 Einzel-INSERTs dauern auf einer
        /// Netzwerkdatenbank sichtbar lange; ohne Rückmeldung wirkt das Programm
        /// eingefroren.
        /// </summary>
        private void Fortschritt(int geschrieben)
        {
            _lblStatus.Text = string.Format(MyResource.Resource.PREIS_IMPORT_STATUS_SCHREIBT,
                                            geschrieben.ToString("N0", CultureInfo.CurrentCulture));
            _lblStatus.ForeColor = Color.Black;
            _lblStatus.Refresh();
        }
    }
}
