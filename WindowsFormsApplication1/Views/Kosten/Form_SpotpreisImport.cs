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
    /// Vollständig programmatisch, ohne Designer und ohne eigene <c>.resx</c>.
    /// </para>
    /// </remarks>
    public class Form_SpotpreisImport : Form
    {
        private readonly int _idProjekt;
        private readonly SpotpreisImportCtrl _ctrl = new SpotpreisImportCtrl();
        private SpotpreisImportCtrl.Lauf _lauf;

        private TextBox _tbPfad;
        private TextBox _tbBezeichner;
        private CheckBox _chkStamm;
        private TextBox _tbProtokoll;
        private Button _btnUebernehmen;
        private Label _lblStatus;

        /// <summary>Die zuletzt angelegte <c>Tab_Preisreihe.ID</c>; 0, wenn nichts gespeichert wurde.</summary>
        public int AngelegteReiheId { get; private set; }

        public Form_SpotpreisImport(int idProjekt)
        {
            _idProjekt = idProjekt;
            PreisreiheCtrl.StelleTabellenSicher();
            BaueOberflaeche();
        }

        // ==================================================================
        // Oberfläche
        // ==================================================================

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.PREIS_IMPORT_TITEL;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(720, 520);

            this.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_IMPORT_INFO,
                Location = new Point(12, 10),
                Size = new Size(696, 46),
                AutoSize = false
            });

            // --- Datei ---
            this.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_IMPORT_LABEL_DATEI,
                Location = new Point(12, 66),
                AutoSize = true
            });

            _tbPfad = new TextBox
            {
                Location = new Point(110, 63),
                Width = 480,
                ReadOnly = true,
                BackColor = SystemColors.Control
            };
            this.Controls.Add(_tbPfad);

            Button btnWaehlen = new Button
            {
                Text = MyResource.Resource.PREIS_IMPORT_BTN_DATEI,
                Location = new Point(600, 62),
                Width = 108
            };
            btnWaehlen.Click += btnWaehlen_Click;
            this.Controls.Add(btnWaehlen);

            // --- Bezeichner und Ablageort ---
            this.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_IMPORT_LABEL_BEZEICHNER,
                Location = new Point(12, 98),
                AutoSize = true
            });

            _tbBezeichner = new TextBox
            {
                Location = new Point(110, 95),
                Width = 300
            };
            this.Controls.Add(_tbBezeichner);

            _chkStamm = new CheckBox
            {
                Text = MyResource.Resource.PREIS_IMPORT_CHK_STAMM,
                Location = new Point(430, 96),
                AutoSize = true,
                Checked = true
            };
            this.Controls.Add(_chkStamm);

            // --- Protokoll ---
            this.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_IMPORT_LABEL_PROTOKOLL,
                Location = new Point(12, 128),
                AutoSize = true
            });

            _tbProtokoll = new TextBox
            {
                Location = new Point(12, 150),
                Size = new Size(696, 300),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = SystemColors.Window,
                Font = new Font("Consolas", 9f, FontStyle.Regular)
            };
            this.Controls.Add(_tbProtokoll);

            _lblStatus = new Label
            {
                Location = new Point(12, 458),
                Size = new Size(480, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(_lblStatus);

            // --- Knöpfe ---
            _btnUebernehmen = new Button
            {
                Text = MyResource.Resource.PREIS_IMPORT_BTN_UEBERNEHMEN,
                Location = new Point(this.ClientSize.Width - 230, 484),
                Width = 120,
                Enabled = false
            };
            _btnUebernehmen.Click += btnUebernehmen_Click;

            Button btnSchliessen = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 100, 484),
                Width = 88
            };

            this.Controls.Add(_btnUebernehmen);
            this.Controls.Add(btnSchliessen);
            this.CancelButton = btnSchliessen;
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
