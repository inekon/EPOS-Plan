using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Gemeinsamer Konfliktdialog aller Importpfade (Konzept 4.1): EIN Dialog fuer die
    /// ganze Auswahl statt einer Meldung je Satz. Je Zeile Befund und waehlbare Aktion
    /// (Importieren/Auslassen/Ueberschreiben/Umbenennen); beim Umbenennen wird die
    /// Namenszelle editierbar. Reine Code-Form ohne Designer (Hausregel, Muster
    /// Form_Gesetzesparameter), Texte ueber MyResource.
    ///
    /// <para>Seit iU9-W12.0b holt diese Maske ihre Entscheidungsregeln aus
    /// <see cref="ImportKonfliktModell"/> im Kern; hier steht nur noch das
    /// Steuerelementgeruest. Mit iU9-W12.3 tritt
    /// <c>EPOS.UI.Dialoge.Import.ImportKonflikteDialog</c> an ihre Stelle.</para>
    /// </summary>
    public class Form_ImportKonflikte : Form
    {
        private readonly List<ImportPruefung> _pruefungen;
        private readonly HashSet<string> _vergebeneNamen;

        private Label _lblKopf;
        private DataGridView _grid;
        private Button _btnAlleAuslassen;
        private Button _btnOk;
        private Button _btnAbbrechen;
        private Button btn_Help;

        private const string SPALTE_NAME = "NAME";
        private const string SPALTE_BEFUND = "BEFUND";
        private const string SPALTE_AKTION = "AKTION";

        /// <summary>
        /// Zeigt den Dialog. Rueckgabe: je Pruefung eine Entscheidung, oder null bei
        /// Abbruch (dann wird gar nichts importiert).
        /// </summary>
        /// <param name="pruefungen">Ergebnis von DublettenPruefung.PruefeKandidaten.</param>
        /// <param name="vergebeneNamen">Normalisierte Bestandsnamen (DublettenPruefung.VergebeneNamen).</param>
        public static List<KonfliktEntscheidung> Zeigen(IWin32Window owner,
            List<ImportPruefung> pruefungen, HashSet<string> vergebeneNamen)
        {
            using (Form_ImportKonflikte frm = new Form_ImportKonflikte(pruefungen, vergebeneNamen))
            {
                if (frm.ShowDialog(owner) != DialogResult.OK) return null;
                return frm.Entscheidungen();
            }
        }

        private Form_ImportKonflikte(List<ImportPruefung> pruefungen, HashSet<string> vergebeneNamen)
        {
            _pruefungen = pruefungen ?? new List<ImportPruefung>();
            _vergebeneNamen = vergebeneNamen ?? new HashSet<string>(StringComparer.Ordinal);
            AufbauControls();
            TexteSetzen();
            ZeilenFuellen();
        }

        // ------------------------------------------------------------- Aufbau ------

        private void AufbauControls()
        {
            SuspendLayout();

            Text = "";
            // Der Formularname ist das Praefix in help_mapping.txt; ohne ihn
            // findet die Hilfeautomatik dieses Fenster nicht (HilfeAutomatik, F5).
            Name = "Form_ImportKonflikte";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(860, 420);
            MinimumSize = new Size(640, 300);
            AutoScaleMode = AutoScaleMode.None;   // Anwendung ist faktisch DpiUnaware

            _lblKopf = new Label
            {
                AutoSize = false,
                Location = new Point(12, 12),
                Size = new Size(ClientSize.Width - 24, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _grid = new DataGridView
            {
                Location = new Point(12, 38),
                Size = new Size(ClientSize.Width - 24, ClientSize.Height - 38 - 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn
            {
                Name = SPALTE_NAME,
                FillWeight = 34,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            DataGridViewTextBoxColumn colBefund = new DataGridViewTextBoxColumn
            {
                Name = SPALTE_BEFUND,
                FillWeight = 44,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colBefund.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            DataGridViewComboBoxColumn colAktion = new DataGridViewComboBoxColumn
            {
                Name = SPALTE_AKTION,
                FillWeight = 22,
                FlatStyle = FlatStyle.Flat,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            _grid.Columns.AddRange(new DataGridViewColumn[] { colName, colBefund, colAktion });

            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grid.CellBeginEdit += Grid_CellBeginEdit;
            _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            _grid.CellValueChanged += Grid_CellValueChanged;
            _grid.DataError += (s, e) => { e.ThrowException = false; };

            _btnAlleAuslassen = new Button
            {
                Size = new Size(180, 28),
                Location = new Point(12, ClientSize.Height - 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnAlleAuslassen.Click += BtnAlleAuslassen_Click;

            _btnOk = new Button
            {
                Size = new Size(110, 28),
                Location = new Point(ClientSize.Width - 236, ClientSize.Height - 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _btnOk.Click += BtnOk_Click;

            _btnAbbrechen = new Button
            {
                Size = new Size(110, 28),
                Location = new Point(ClientSize.Width - 122, ClientSize.Height - 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel
            };

            // Infobutton (H4). Zuordnung: help_mapping.txt - ohne Zeile dort
            // bleibt der Knopf grau statt wirkungslos anklickbar (F3).
            btn_Help = new Button
            {
                Name = "btn_Help",
                Size = new Size(28, 28),
                Location = new Point(210, ClientSize.Height - 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.Transparent,
                BackgroundImage = Properties.Resources.help_icon,
                BackgroundImageLayout = ImageLayout.Zoom,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                UseVisualStyleBackColor = false
            };
            btn_Help.FlatAppearance.BorderSize = 0;

            Controls.Add(btn_Help);
            Controls.Add(_lblKopf);
            Controls.Add(_grid);
            Controls.Add(_btnAlleAuslassen);
            Controls.Add(_btnOk);
            Controls.Add(_btnAbbrechen);
            CancelButton = _btnAbbrechen;

            ResumeLayout(false);
        }

        private void TexteSetzen()
        {
            Text = MyResource.Resource.IMP_KONFLIKT_TITEL;
            _grid.Columns[SPALTE_NAME].HeaderText = MyResource.Resource.IMP_KONFLIKT_SPALTE_NAME;
            _grid.Columns[SPALTE_BEFUND].HeaderText = MyResource.Resource.IMP_KONFLIKT_SPALTE_BEFUND;
            _grid.Columns[SPALTE_AKTION].HeaderText = MyResource.Resource.IMP_KONFLIKT_SPALTE_AKTION;
            _btnAlleAuslassen.Text = MyResource.Resource.IMP_KONFLIKT_ALLE_AUSLASSEN;
            _btnOk.Text = MyResource.Resource.IMP_KONFLIKT_OK;
            _btnAbbrechen.Text = MyResource.Resource.IMP_KONFLIKT_ABBRECHEN;
        }

        // ------------------------------------------------------------- Inhalt ------

        private static string AktionText(KonfliktAktion a)
            => ImportKonfliktModell.AktionText(a);

        private void ZeilenFuellen()
        {
            _lblKopf.Text = ImportKonfliktModell.KopfText(
                _pruefungen.Count, ImportKonfliktModell.Konflikte(_pruefungen));

            foreach (ImportPruefung p in _pruefungen)
            {
                int zeile = _grid.Rows.Add();
                DataGridViewRow row = _grid.Rows[zeile];
                row.Tag = p;
                row.Cells[SPALTE_NAME].Value = p.Kandidat.Name;
                row.Cells[SPALTE_BEFUND].Value = ImportKonfliktModell.BefundText(p);

                KonfliktAktion vorbelegung;
                List<KonfliktAktion> erlaubt = ImportKonfliktModell.ErlaubteAktionen(p, out vorbelegung);

                DataGridViewComboBoxCell combo = (DataGridViewComboBoxCell)row.Cells[SPALTE_AKTION];
                foreach (KonfliktAktion a in erlaubt) combo.Items.Add(AktionText(a));
                combo.Value = AktionText(vorbelegung);
            }
        }

        // ------------------------------------------------------------ Verhalten ------

        private KonfliktAktion ZeilenAktion(DataGridViewRow row)
        {
            string wert = Convert.ToString(row.Cells[SPALTE_AKTION].Value);
            if (wert == MyResource.Resource.IMP_KONFLIKT_AKTION_IMPORTIEREN) return KonfliktAktion.Importieren;
            if (wert == MyResource.Resource.IMP_KONFLIKT_AKTION_UEBERSCHREIBEN) return KonfliktAktion.Ueberschreiben;
            if (wert == MyResource.Resource.IMP_KONFLIKT_AKTION_UMBENENNEN) return KonfliktAktion.Umbenennen;
            return KonfliktAktion.Auslassen;
        }

        private void Grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Der Name ist nur editierbar, wenn die Zeile auf "Umbenennen" steht.
            if (e.ColumnIndex == _grid.Columns[SPALTE_NAME].Index &&
                ZeilenAktion(_grid.Rows[e.RowIndex]) != KonfliktAktion.Umbenennen)
                e.Cancel = true;
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Combo-Aenderungen sofort uebernehmen, damit CellValueChanged feuert.
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewComboBoxCell)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != _grid.Columns[SPALTE_AKTION].Index) return;

            DataGridViewRow row = _grid.Rows[e.RowIndex];
            ImportPruefung p = (ImportPruefung)row.Tag;

            if (ZeilenAktion(row) == KonfliktAktion.Umbenennen)
            {
                // Namensvorschlag, solange noch der Originalname steht.
                string aktuell = Convert.ToString(row.Cells[SPALTE_NAME].Value);
                if (string.Equals(aktuell, p.Kandidat.Name, StringComparison.Ordinal))
                    row.Cells[SPALTE_NAME].Value = NamensVorschlag(p.Kandidat.Name);
            }
            else
            {
                row.Cells[SPALTE_NAME].Value = p.Kandidat.Name;
            }
        }

        private string NamensVorschlag(string name)
            => ImportKonfliktModell.NamensVorschlag(name, _vergebeneNamen);

        private void BtnAlleAuslassen_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                ImportPruefung p = (ImportPruefung)row.Tag;
                if (ImportKonfliktModell.IstKonflikt(p))
                {
                    row.Cells[SPALTE_AKTION].Value = AktionText(KonfliktAktion.Auslassen);
                    row.Cells[SPALTE_NAME].Value = p.Kandidat.Name;
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            _grid.EndEdit();

            // Die Pruefregel (Konzept 4.3) steht seit iU9-W12.0b im Kern.
            List<KonfliktEntscheidung> entscheidungen = Entscheidungen();
            ImportKonfliktModell.Beanstandung befund =
                ImportKonfliktModell.Pruefe(entscheidungen, _vergebeneNamen);
            if (befund != null)
            {
                MessageBox.Show(this,
                    ImportKonfliktModell.BeanstandungsText(befund),
                    MyResource.Resource.IMP_KONFLIKT_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _grid.CurrentCell = _grid.Rows[befund.Zeile].Cells[SPALTE_NAME];
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private List<KonfliktEntscheidung> Entscheidungen()
        {
            var liste = new List<KonfliktEntscheidung>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                ImportPruefung p = (ImportPruefung)row.Tag;
                KonfliktAktion aktion = ZeilenAktion(row);
                liste.Add(new KonfliktEntscheidung
                {
                    Pruefung = p,
                    Aktion = aktion,
                    NeuerName = aktion == KonfliktAktion.Umbenennen
                        ? (Convert.ToString(row.Cells[SPALTE_NAME].Value) ?? "").Trim()
                        : null
                });
            }
            return liste;
        }
    }
}
