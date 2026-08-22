using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog „Als Variante speichern…" — fragt den Bezeichner ab und legt aus dem
    /// geöffneten Projekt eine Variante an. Einstieg ist das MDI-Menü
    /// (Projekte › Als Variante speichern…), gerechnet wird in
    /// <see cref="VariantenCtrl.AnlegenAusStamm"/>; dieser Dialog enthält bewusst
    /// keine eigene Anlegelogik.
    ///
    /// <para>
    /// IST DAS GEÖFFNETE PROJEKT SELBST EINE VARIANTE, wird ihr Stammprojekt
    /// verwendet: Eine Variante hängt immer am Stamm, nie an einer anderen Variante —
    /// sonst wäre die Vergleichsgruppe keine Gruppe mehr, sondern eine Kette, und die
    /// Differenz-Kennzahlen der Wirtschaftlichkeit hätten keinen gemeinsamen Bezug.
    /// </para>
    ///
    /// <para>
    /// Die Oberfläche steht vollständig im Code (Muster <see cref="UcBkUebersicht"/>),
    /// damit weder eine Designer- noch eine <c>.resx</c>-Datei entsteht; alle sichtbaren
    /// Texte kommen aus <c>MyResource</c>.
    /// </para>
    /// </summary>
    public class Form_AlsVariante : Form
    {
        private readonly TableLayoutPanel tl = new TableLayoutPanel();
        private readonly Label lblHinweis = new Label();
        private readonly Label lblBez = new Label();
        private readonly TextBox txtBezeichner = new TextBox();
        private readonly FlowLayoutPanel pnlKnoepfe = new FlowLayoutPanel();
        private readonly Button btnAnlegen = new Button();
        private readonly Button btnAbbrechen = new Button();

        /// <summary>Der eingegebene Bezeichner (nach OK).</summary>
        public string Bezeichner { get { return (txtBezeichner.Text ?? "").Trim(); } }

        private Form_AlsVariante(string stammName)
        {
            this.Text = MyResource.Resource.VAR_DLG_TITEL;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.ClientSize = new Size(470, 190);

            tl.Dock = DockStyle.Fill;
            tl.Padding = new Padding(14);
            tl.ColumnCount = 1;
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tl.RowCount = 4;
            tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Hinweis
            tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));   // lblBez
            tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));   // txtBezeichner
            tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));   // Knöpfe

            lblHinweis.Dock = DockStyle.Fill;
            lblHinweis.Margin = new Padding(0, 0, 0, 10);
            lblHinweis.Text = string.Format(MyResource.Resource.VAR_DLG_HINWEIS, stammName);

            lblBez.Dock = DockStyle.Fill;
            lblBez.Margin = new Padding(0);
            lblBez.Text = MyResource.Resource.BK_LBL_BEZEICHNER;

            txtBezeichner.Dock = DockStyle.Fill;
            txtBezeichner.Margin = new Padding(0, 0, 0, 10);
            txtBezeichner.TextChanged += new EventHandler(this.txtBezeichner_TextChanged);

            btnAnlegen.Text = MyResource.Resource.BK_BTN_ANLEGEN;
            btnAnlegen.DialogResult = DialogResult.OK;
            btnAnlegen.AutoSize = true;
            btnAnlegen.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnAnlegen.Margin = new Padding(8, 0, 0, 0);
            btnAnlegen.Enabled = false;               // erst mit Bezeichner anklickbar

            btnAbbrechen.Text = MyResource.Resource.SIM_BTN_ABBRECHEN;
            btnAbbrechen.DialogResult = DialogResult.Cancel;
            btnAbbrechen.AutoSize = true;
            btnAbbrechen.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnAbbrechen.Margin = new Padding(8, 0, 0, 0);

            pnlKnoepfe.Dock = DockStyle.Fill;
            pnlKnoepfe.FlowDirection = FlowDirection.RightToLeft;
            pnlKnoepfe.Margin = new Padding(0);
            pnlKnoepfe.WrapContents = false;
            pnlKnoepfe.Controls.Add(btnAbbrechen);
            pnlKnoepfe.Controls.Add(btnAnlegen);

            tl.Controls.Add(lblHinweis, 0, 0);
            tl.Controls.Add(lblBez, 0, 1);
            tl.Controls.Add(txtBezeichner, 0, 2);
            tl.Controls.Add(pnlKnoepfe, 0, 3);
            this.Controls.Add(tl);

            this.AcceptButton = btnAnlegen;
            this.CancelButton = btnAbbrechen;
        }

        private void txtBezeichner_TextChanged(object sender, EventArgs e)
        {
            btnAnlegen.Enabled = Bezeichner.Length > 0;
        }

        /// <summary>
        /// Zeigt den Dialog und legt bei OK die Variante an. <paramref name="idProjekt"/>
        /// ist das in Form_Start geöffnete Projekt (Stamm oder Variante),
        /// <paramref name="projektname"/> dessen Name.
        /// </summary>
        public static void Zeige(IWin32Window besitzer, int idProjekt, string projektname)
        {
            if (idProjekt <= 0)
            {
                MessageBox.Show(besitzer,
                    MyResource.Resource.VAR_MSG_KEIN_PROJEKT,
                    MyResource.Resource.VAR_DLG_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            VariantenCtrl ctrl = new VariantenCtrl();

            // Stamm bestimmen: ist das geöffnete Projekt eine Variante, deren Stamm nehmen.
            int idStamm = ctrl.StammRefDerVariante(idProjekt);
            bool istVariante = idStamm > 0;
            if (!istVariante) idStamm = idProjekt;

            string stammName = istVariante ? LiesProjektname(idStamm) : (projektname ?? "");
            if (string.IsNullOrWhiteSpace(stammName)) stammName = LiesProjektname(idStamm);
            if (string.IsNullOrWhiteSpace(stammName))
            {
                MessageBox.Show(besitzer, MyResource.Resource.BK_MSG_KEIN_STAMM,
                    MyResource.Resource.VAR_DLG_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (Form_AlsVariante dlg = new Form_AlsVariante(stammName))
            {
                if (dlg.ShowDialog(besitzer) != DialogResult.OK) return;

                string bezeichner = dlg.Bezeichner;
                Cursor alt = Cursor.Current;
                try
                {
                    Cursor.Current = Cursors.WaitCursor;

                    string fehler;
                    int neueId = ctrl.AnlegenAusStamm(idStamm, stammName, bezeichner, out fehler);
                    if (neueId <= 0)
                    {
                        MessageBox.Show(besitzer,
                            string.IsNullOrEmpty(fehler)
                                ? MyResource.Resource.BK_MSG_ANLEGEN_FEHLGESCHLAGEN : fehler,
                            MyResource.Resource.VAR_DLG_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Startseite nachziehen: Variantenauswahl und – falls schon aufgebaut –
                    // der Reiter „Berichte & Kosten" kennen die neue Variante sonst nicht.
                    Program.startfrm?.VariantenAnzeigeAktualisieren();

                    MessageBox.Show(besitzer,
                        string.Format(MyResource.Resource.BK_MSG_VARIANTE_ANGELEGT, bezeichner),
                        MyResource.Resource.VAR_DLG_TITEL,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(besitzer,
                        string.Format(MyResource.Resource.BK_MSG_ANLEGEFEHLER, ex.Message),
                        MyResource.Resource.VAR_DLG_TITEL,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally { Cursor.Current = alt; }
            }
        }

        // Liest den Projektnamen zu einer ID (leer, wenn nicht gefunden) – wie in Form_Start.
        private static string LiesProjektname(int idProjekt)
        {
            ProjektCtrl pc = new ProjektCtrl();
            pc.ReadAll();
            foreach (ProjektModel p in pc.items)
                if (p.m_ID == idProjekt) return p.m_szProjektname;
            return "";
        }
    }
}
