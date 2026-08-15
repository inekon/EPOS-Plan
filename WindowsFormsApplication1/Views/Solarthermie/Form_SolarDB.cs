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

        // TextChanged faerbt nur noch das Feld (Program.ZahlFaerben/GanzzahlFaerben),
        // gemeldet wird erst beim Speichern-Knopf. Das alte checkDouble()+Undo()
        // konnte den Dialog in einer Endlosmeldung festhalten - ausfuehrliche
        // Begruendung in Program.cs (Folgepaket zu ab5bf32).
        private void textBox_Modul_A_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Absorber_A_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_h0_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_k1_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_k2_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_C_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Kdir_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Kdiff_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Ertrag_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Kosten_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void btn_Überschreiben_Click(object sender, EventArgs e)
        {
            // Zahlenfelder pruefen, bevor irgendetwas geschrieben wird; null heisst:
            // Meldung ist raus, Dialog bleibt offen.
            SolarkollektorenModel m = InitDatensatzUpdate();
            if (m == null) return;

            try
            {
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

        /// <summary>
        /// Prueft alle Zahlenfelder und baut daraus den Datensatz. Bei ungueltiger
        /// Eingabe meldet Program.ZahlPruefen/GanzzahlPruefen sprechend, setzt den
        /// Fokus und liefert null - der Aufrufer kehrt dann zurueck, ohne zu
        /// speichern, und der Dialog bleibt offen.
        /// Leer ist nur bei Vorlauf/Ruecklauf erlaubt (dort galt "" schon bisher als
        /// 0); die Dezimalfelder wurden ungeprueft mit double.Parse gelesen und
        /// haetten bei leer oder Buchstaben eine FormatException geworfen.
        /// </summary>
        SolarkollektorenModel InitDatensatzUpdate()
        {
            double modulflaeche, aperturflaeche, h0, k1, k2, kdir, kdiff, kosten;
            int vorlauf, ruecklauf;

            if (!Program.ZahlPruefen(textBox_Modul_A, "Modulfläche", out modulflaeche)) return null;
            if (!Program.ZahlPruefen(textBox_Absorber_A, "Aperturfläche", out aperturflaeche)) return null;
            if (!Program.ZahlPruefen(textBox_h0, "h0", out h0)) return null;
            if (!Program.ZahlPruefen(textBox_k1, "k1", out k1)) return null;
            if (!Program.ZahlPruefen(textBox_k2, "k2", out k2)) return null;
            if (!Program.ZahlPruefen(textBox_Kdir, "Kdir", out kdir)) return null;
            if (!Program.ZahlPruefen(textBox_Kdiff, "Kdiff", out kdiff)) return null;
            if (!Program.ZahlPruefen(textBox_Kosten, "Investitionskosten", out kosten)) return null;
            if (!Program.GanzzahlPruefen(textBox_Vorlauf, "Vorlauf", out vorlauf, true)) return null;
            if (!Program.GanzzahlPruefen(textBox_Ruecklauf, "Rücklauf", out ruecklauf, true)) return null;

            SolarkollektorenModel model = new SolarkollektorenModel();
            model.m_szKollektorname = textBox_Name.Text;
            model.m_szFirma = textBox_Firma.Text;
            model.m_szBeschreibung = textBox_Beschreibung.Text;
            model.m_szKollektortyp = textBox_Typ.Text;
            model.m_Modulfläche = modulflaeche;
            model.m_Aperturfläche = aperturflaeche;
            model.m_h0 = h0;
            model.m_k1 = k1;
            model.m_k2 = k2;
            model.m_Kdir = kdir;
            model.m_Kdfu = kdiff;
            model.m_Kosten = kosten;
            model.m_Vorlauf = vorlauf;
            model.m_Ruecklauf = ruecklauf;

            return model;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_Name.Text))
            {
                MessageBox.Show("Bitte einen Kollektorname eingeben!");
                return;
            }

            // Zahlenfelder vor dem Schreiben pruefen (siehe InitDatensatzUpdate).
            SolarkollektorenModel m = InitDatensatzUpdate();
            if (m == null) return;

            try
            {
                SolarkollektorenStammCtrl ctrl = new SolarkollektorenStammCtrl();
                if (ctrl.Exists(textBox_Name.Text)) { MessageBox.Show("Name existiert bereits!"); return; }

                if (ctrl.InsertFrom(m))
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

                    // Erst pruefen, dann den neuen Namen uebernehmen - so bleibt bei
                    // einer Fehleingabe auch das Namensfeld unveraendert.
                    SolarkollektorenModel m = InitDatensatzUpdate();
                    if (m == null) return;

                    textBox_Name.Text = frmLabel.m_szName;
                    m.m_szKollektorname = frmLabel.m_szName;

                    if (ctrl.InsertFrom(m))
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

        // Ganzzahlfelder (Speicherweg parst mit Int32). Das automatische Auffuellen
        // eines leeren Feldes mit "0" entfaellt - leer wird beim Speichern als 0
        // uebernommen (GanzzahlPruefen mit leerErlaubt).
        private void textBox_Vorlauf_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_Ruecklauf_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }
    }
}