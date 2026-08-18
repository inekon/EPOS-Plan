using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Auswahldialog des Knopfes „Planwert übernehmen…": legt <b>je Anlage</b> fest,
    /// welcher Technikwert als Investition gilt, und zeigt die Nebenkosten, die als
    /// eigene Zeilen entstehen.
    ///
    /// <para>
    /// Umsetzung der Nutzerentscheidungen 1 und 2 vom 18.08.2026. Der Anwender sieht zu
    /// jeder Zeile, WOHER die Zahl kommt (Spalte „Herkunft": Feldname bzw. die Rechnung
    /// „653,60 €/kWel × 250,00 kWel"), damit die Wahl nicht raten heißt.
    /// </para>
    ///
    /// <para>
    /// Bewusst ohne Designer-Datei aufgebaut — wie <see cref="UcBkKosten"/>: die
    /// Maske ist eine Tabelle mit zwei Knöpfen, und der WinForms-Designer brächte drei
    /// weitere Dateien (<c>.Designer.cs</c>, zwei <c>.resx</c>) ohne Gegenwert. Alle
    /// Anzeigetexte kommen aus <c>MyResource</c>, die Steuerwerte der Auswahlspalte sind
    /// die sprachneutralen Schlüssel aus <see cref="TechnikPlanwertCtrl"/>.
    /// </para>
    /// </summary>
    internal class Form_PlanwertUebernahme : Form
    {
        private readonly List<TechnikPlanwertCtrl.Anlage> _anlagen;
        private readonly string _komponente;

        private DataGridView grid;
        private Label lblKopf;
        private Label lblNeben;
        private Label lblSumme;
        private Button btnOk;
        private Button btnAbbruch;

        /// <summary>Gewählte Kostenbasis je GerätID (Schlüssel aus <see cref="TechnikPlanwertCtrl"/>).</summary>
        internal Dictionary<int, string> Wahl { get; private set; }

        /// <summary>Summe der Hauptposition nach der Auswahl.</summary>
        internal double Hauptsumme { get; private set; }

        /// <summary>Nebenkosten, je Bezeichnung zusammengefasst.</summary>
        internal List<TechnikPlanwertCtrl.Nebenposten> Nebenkosten { get; private set; }

        internal Form_PlanwertUebernahme(string komponente, List<TechnikPlanwertCtrl.Anlage> anlagen)
        {
            _komponente = komponente ?? "";
            _anlagen = anlagen ?? new List<TechnikPlanwertCtrl.Anlage>();
            Wahl = new Dictionary<int, string>();
            Nebenkosten = TechnikPlanwertCtrl.Nebensummen(_anlagen);

            Aufbauen();
            Fuellen();
            SummeAktualisieren();
        }

        // ------------------------------------------------------------------- Aufbau

        private void Aufbauen()
        {
            Text = string.Format(MyResource.Resource.KOSTEN_PLANWERT_TITEL, _komponente);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(760, 380);
            MinimumSize = new Size(560, 300);
            Font = new Font("Segoe UI", 9f);

            lblKopf = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(10, 8, 10, 0),
                Text = MyResource.Resource.KOSTEN_PLANWERT_KOPF
            };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnEnter,
                BackgroundColor = Color.White,
                Margin = new Padding(10)
            };
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            var cAnlage = new DataGridViewTextBoxColumn
            {
                HeaderText = MyResource.Resource.KOSTEN_PLANWERT_SP_ANLAGE,
                ReadOnly = true,
                FillWeight = 130
            };
            var cBasis = new DataGridViewComboBoxColumn
            {
                HeaderText = MyResource.Resource.KOSTEN_PLANWERT_SP_BASIS,
                FillWeight = 110,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };
            var cBetrag = new DataGridViewTextBoxColumn
            {
                HeaderText = MyResource.Resource.KOSTEN_PLANWERT_SP_BETRAG,
                ReadOnly = true,
                FillWeight = 80
            };
            cBetrag.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            var cHerkunft = new DataGridViewTextBoxColumn
            {
                HeaderText = MyResource.Resource.KOSTEN_PLANWERT_SP_HERLEITUNG,
                ReadOnly = true,
                FillWeight = 150
            };

            grid.Columns.AddRange(cAnlage, cBasis, cBetrag, cHerkunft);
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.CellValueChanged += (s, e) => { if (e.RowIndex >= 0) ZeileNachziehen(e.RowIndex); };

            lblNeben = new Label { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10, 6, 10, 0) };
            lblSumme = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(10, 4, 10, 0),
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold)
            };

            btnOk = new Button
            {
                Text = MyResource.Resource.KOSTEN_PLANWERT_BTN_OK,
                DialogResult = DialogResult.OK,
                Size = new Size(120, 28),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnAbbruch = new Button
            {
                Text = MyResource.Resource.KOSTEN_PLANWERT_BTN_ABBRUCH,
                DialogResult = DialogResult.Cancel,
                Size = new Size(120, 28),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            var fuss = new Panel { Dock = DockStyle.Bottom, Height = 42 };
            btnOk.Location = new Point(fuss.Width - 260, 7);
            btnAbbruch.Location = new Point(fuss.Width - 132, 7);
            fuss.Controls.Add(btnOk);
            fuss.Controls.Add(btnAbbruch);
            fuss.Resize += (s, e) =>
            {
                btnOk.Left = fuss.Width - 260;
                btnAbbruch.Left = fuss.Width - 132;
            };

            var mitte = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
            mitte.Controls.Add(grid);

            var unten = new Panel { Dock = DockStyle.Bottom, Height = 70 };
            unten.Controls.Add(lblSumme);
            unten.Controls.Add(lblNeben);

            Controls.Add(mitte);
            Controls.Add(unten);
            Controls.Add(fuss);
            Controls.Add(lblKopf);

            AcceptButton = btnOk;
            CancelButton = btnAbbruch;
        }

        // -------------------------------------------------------------------- Daten

        private void Fuellen()
        {
            foreach (TechnikPlanwertCtrl.Anlage a in _anlagen)
            {
                int i = grid.Rows.Add();
                DataGridViewRow r = grid.Rows[i];
                r.Tag = a;

                r.Cells[0].Value = a.Bezeichner;

                var zelle = (DataGridViewComboBoxCell)r.Cells[1];
                foreach (TechnikPlanwertCtrl.Basiswert b in a.Basiswerte)
                    zelle.Items.Add(TechnikPlanwertCtrl.BasisName(b.Schluessel));

                if (a.Basiswerte.Count == 0)
                {
                    // Nichts gepflegt: keine Auswahl anbieten, Zeile trägt 0 bei.
                    zelle.Items.Add(TechnikPlanwertCtrl.BasisName(TechnikPlanwertCtrl.BASIS_KEINE));
                    zelle.Value = zelle.Items[0];
                    zelle.ReadOnly = true;
                }
                else
                {
                    if (a.Mehrdeutig)
                        zelle.Items.Add(TechnikPlanwertCtrl.BasisName(TechnikPlanwertCtrl.BASIS_KEINE));

                    // Vorauswahl: der einzige gepflegte Wert, sonst der ERSTE (Modulpreis) —
                    // eine Vorauswahl ist nötig, damit die Maske eine Summe zeigt; sie ist
                    // sichtbar und änderbar, also keine stille Festlegung.
                    zelle.Value = TechnikPlanwertCtrl.BasisName(a.Basiswerte[0].Schluessel);
                }

                ZeileNachziehen(i);
            }
        }

        /// <summary>Betrag/Herkunft der Zeile aus der gewählten Basis nachziehen.</summary>
        private void ZeileNachziehen(int index)
        {
            if (index < 0 || index >= grid.Rows.Count) return;
            DataGridViewRow r = grid.Rows[index];

            var a = r.Tag as TechnikPlanwertCtrl.Anlage;
            if (a == null) return;

            string angezeigt = Convert.ToString(r.Cells[1].Value);
            TechnikPlanwertCtrl.Basiswert treffer = null;
            foreach (TechnikPlanwertCtrl.Basiswert b in a.Basiswerte)
                if (string.Equals(TechnikPlanwertCtrl.BasisName(b.Schluessel), angezeigt, StringComparison.Ordinal))
                { treffer = b; break; }

            Wahl[a.GeraetID] = (treffer != null) ? treffer.Schluessel : TechnikPlanwertCtrl.BASIS_KEINE;

            r.Cells[2].Value = (treffer != null)
                ? treffer.Betrag.ToString("N2", BerichtTexte.Kultur) : "0,00";
            r.Cells[3].Value = (treffer != null) ? treffer.Herleitung : "";

            SummeAktualisieren();
        }

        private void SummeAktualisieren()
        {
            Hauptsumme = TechnikPlanwertCtrl.Hauptsumme(_anlagen, Wahl);

            if (lblSumme != null)
                lblSumme.Text = string.Format(MyResource.Resource.KOSTEN_PLANWERT_SUMME,
                                              Hauptsumme.ToString("N2", BerichtTexte.Kultur));

            if (lblNeben == null) return;

            if (Nebenkosten.Count == 0) { lblNeben.Text = ""; return; }

            var teile = new List<string>();
            foreach (TechnikPlanwertCtrl.Nebenposten n in Nebenkosten)
                teile.Add(n.Bezeichnung + " " + n.Betrag.ToString("N2", BerichtTexte.Kultur) + " €");

            lblNeben.Text = MyResource.Resource.KOSTEN_PLANWERT_NEBENKOSTEN + " " +
                            string.Join("  ·  ", teile.ToArray());
        }
    }
}
