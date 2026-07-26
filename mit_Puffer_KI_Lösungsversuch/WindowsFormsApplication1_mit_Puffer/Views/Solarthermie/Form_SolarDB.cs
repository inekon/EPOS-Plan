using System;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_SolarDB : Form
    {
        public SolarkollektorenModel model = new SolarkollektorenModel();
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public int m_mode = MODE_EDIT;
        public string m_szName = "";

        public Form_SolarDB()
        {
            InitializeComponent();
        }
        public void SetControls(string szName)
        {
            SolarkollektorenStammCtrl ctrl = new SolarkollektorenStammCtrl();

            if (m_mode == MODE_EDIT)
            {
                btn_Speichern.Enabled = false;
                btn_Speichern_Unter.Enabled = true;
                btn_Überschreiben.Enabled = true;
                ctrl.ReadAll("Bezeichner='" + szName + "'");
                model = ctrl.items[0];
            }
            else
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Überschreiben.Enabled = false;
                model = new SolarkollektorenModel();
                model.m_szKollektorname = szName;
            }

            textBox_Name.Text = model.m_szKollektorname;
            textBox_Firma.Text = model.m_szFirma;
            textBox_Beschreibung.Text = model.m_szBeschreibung;
            textBox_Typ.Text = model.m_szKollektortyp;
            textBox_Modul_A.Text = model.m_Modulfläche.ToString();
            textBox_Absorber_A.Text = model.m_Aperturfläche.ToString();
            textBox_h0.Text = model.m_h0.ToString();
            textBox_k1.Text = model.m_k1.ToString();
            textBox_k2.Text = model.m_k2.ToString();
            textBox_Kdir.Text = model.m_Kdir.ToString();
            textBox_Kdiff.Text = model.m_Kdfu.ToString();
            textBox_Kosten.Text = model.m_Kosten.ToString();
            textBox_Vorlauf.Text = model.m_Vorlauf.ToString();
            textBox_Ruecklauf.Text = model.m_Ruecklauf.ToString();  
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void textBox_Modul_A_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Absorber_A_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_h0_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_k1_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_k2_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_C_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Kdir_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Kdiff_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Ertrag_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Kosten_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void btn_Überschreiben_Click(object sender, EventArgs e)
        {
            try
            {
                SolarkollektorenModel m = InitDatensatzUpdate();
                SolarkollektorenStammCtrl ctrl = new SolarkollektorenStammCtrl();
                if (ctrl.UpdateFrom(m))
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Überschreiben des Solarkollektors: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
                this.DialogResult = DialogResult.Cancel;
            }
        }

        SolarkollektorenModel InitDatensatzUpdate()
        {
            SolarkollektorenModel model = new SolarkollektorenModel();
            model.m_szKollektorname = textBox_Name.Text;
            model.m_szFirma = textBox_Firma.Text;
            model.m_szBeschreibung = textBox_Beschreibung.Text;
            model.m_szKollektortyp = textBox_Typ.Text;
            model.m_Modulfläche = double.Parse(textBox_Modul_A.Text);
            model.m_Aperturfläche = double.Parse(textBox_Absorber_A.Text);
            model.m_h0 = double.Parse(textBox_h0.Text);
            model.m_k1 = double.Parse(textBox_k1.Text);
            model.m_k2 = double.Parse(textBox_k2.Text);
            model.m_Kdir = double.Parse(textBox_Kdir.Text);
            model.m_Kdfu = double.Parse(textBox_Kdiff.Text);
            model.m_Kosten = double.Parse(textBox_Kosten.Text);
            model.m_Vorlauf = textBox_Vorlauf.Text == "" ? 0 : Int32.Parse(textBox_Vorlauf.Text);
            model.m_Ruecklauf = textBox_Ruecklauf.Text == "" ? 0 : Int32.Parse(textBox_Ruecklauf.Text);    

            return model;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_Name.Text))
            {
                MessageBox.Show("Bitte einen Kollektorname eingeben!");
                return;
            }

            try
            {
                SolarkollektorenStammCtrl ctrl = new SolarkollektorenStammCtrl();
                if (ctrl.Exists(textBox_Name.Text)) { MessageBox.Show("Name existiert bereits!"); return; }

                if (ctrl.InsertFrom(InitDatensatzUpdate()))
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern des Solarkollektors: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
                this.DialogResult = DialogResult.Cancel;
            }
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();

            System.Drawing.Point p1 = btn_Speichern_Unter.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(frmLabel.m_szName))
                {
                    MessageBox.Show("Bitte einen gültigen Kollektorname eingeben!");
                    return;
                }

                try
                {
                    SolarkollektorenStammCtrl ctrl = new SolarkollektorenStammCtrl();
                    if (ctrl.Exists(frmLabel.m_szName)) { MessageBox.Show("Name existiert bereits!"); return; }

                    textBox_Name.Text = frmLabel.m_szName;

                    if (ctrl.InsertFrom(InitDatensatzUpdate()))
                    {
                        this.DialogResult = DialogResult.OK;
                        MessageBox.Show("Datensatz gespeichert");
                    }
                    else
                    {
                        this.DialogResult = DialogResult.Cancel;
                        MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                    }
                    Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler bei 'Speichern unter' des Solarkollektors: " + ex.Message);
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                    this.DialogResult = DialogResult.Cancel;
                }
            }
        }

        private void textBox_Vorlauf_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Ruecklauf_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }
    }
}