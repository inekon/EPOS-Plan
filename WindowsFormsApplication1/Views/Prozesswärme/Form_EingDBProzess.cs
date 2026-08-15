using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_EingDBProzess : Form
    {
        public string m_szProzessname;
        public string m_szBeschreibung;
        public string m_szProzesstyp;
        public string mode;
        
        public Form_EingDBProzess()
        {
            InitializeComponent();

            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Prozesstyp_STAMM order by Bezeichner");

            while(rs.Next())
            {
                comboBox_Prozesstyp.Items.Add(rs.Read("Bezeichner"));
            }
            rs.Close(); 
 
        }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();

            textBox_Prozessname.Text = m_szProzessname;
            textBox_Beschreibung.Text = m_szBeschreibung;
            comboBox_Prozesstyp.Text = m_szProzesstyp;
            rs.Open("select * from Tab_Prozesswaerme_STAMM where Bezeichner='" + textBox_Prozessname.Text + "'");

            if (rs.Next())
            {
                Wert1.Text = rs.Read("Monat_1").ToString();
                Wert2.Text = rs.Read("Monat_2").ToString();
                Wert3.Text = rs.Read("Monat_3").ToString();
                Wert4.Text = rs.Read("Monat_4").ToString();
                Wert5.Text = rs.Read("Monat_5").ToString();
                Wert6.Text = rs.Read("Monat_6").ToString();
                Wert7.Text = rs.Read("Monat_7").ToString();
                Wert8.Text = rs.Read("Monat_8").ToString();
                Wert9.Text = rs.Read("Monat_9").ToString();
                Wert10.Text = rs.Read("Monat_10").ToString();
                Wert11.Text = rs.Read("Monat_11").ToString();
                Wert12.Text = rs.Read("Monat_12").ToString();
            }
            rs.Close();

            if (mode == "Bearbeiten") btn_Speichern.Enabled = false;
            if (mode == "Neu")
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Ueberschreiben.Enabled = false; 
            }
        }

        /// <summary>
        /// Prueft die zwoelf Monatsfelder am Aktionsknopf (Folgepaket zu ab5bf32) und
        /// liefert die geprueften Werte an den Speicherweg weiter. Das erste ungueltige
        /// Feld meldet sprechend, bekommt den Fokus und liefert false - der Aufrufer
        /// kehrt dann zurueck und laesst den Dialog offen. Leer bleibt unzulaessig,
        /// wie zuvor die Leerpruefung in btn_Speichern.
        /// </summary>
        private bool MonatswertePruefen(out double[] monat)
        {
            monat = new double[12];
            for (int i = 1; i <= 12; i++)
            {
                TextBox tb = this.Controls["Wert" + i.ToString()] as TextBox;
                if (!Program.ZahlPruefen(tb, "Monat " + i.ToString(), out monat[i - 1])) return false;
            }
            return true;
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            double[] monat;
            if (!MonatswertePruefen(out monat)) return;

            // ReadOnly-Schutz: schreibgeschuetzte Stammdatensaetze nicht ueberschreiben.
            if (new ProzesswaermeStammCtrl().IsReadOnly(m_szProzessname))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschuetzt (ReadOnly) und kann nicht ueberschrieben werden.", "Schreibgeschuetzt");
                return;
            }

            string sqlU = "UPDATE Tab_Prozesswaerme_STAMM SET Typ=?, Beschreibung=?, Monat_1=?, Monat_2=?, Monat_3=?, Monat_4=?, Monat_5=?, Monat_6=?, Monat_7=?, Monat_8=?, Monat_9=?, Monat_10=?, Monat_11=?, Monat_12=? WHERE Bezeichner=?";
            System.Data.OleDb.OleDbParameter[] psU = new System.Data.OleDb.OleDbParameter[15];
            psU[0] = new System.Data.OleDb.OleDbParameter("@typ", (object)comboBox_Prozesstyp.Text ?? DBNull.Value);
            psU[1] = new System.Data.OleDb.OleDbParameter("@besch", (object)textBox_Beschreibung.Text ?? DBNull.Value);
            for (int i = 1; i <= 12; i++) psU[1 + i] = new System.Data.OleDb.OleDbParameter("@m" + i, monat[i - 1]);
            psU[14] = new System.Data.OleDb.OleDbParameter("@bez", (object)m_szProzessname ?? DBNull.Value);
            if (DataRepository.ExecuteSQL(sqlU, psU))
                MessageBox.Show("Daten aktualisiert!");
            else
                MessageBox.Show("Fehler beim Aktualisieren der Daten!");
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
             Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            // Zahlen zuerst pruefen - noch bevor der Namensdialog aufgeht.
            double[] monat;
            if (!MonatswertePruefen(out monat)) return;

            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();

            frm.m_szName = textBox_Prozessname.Text;
            frm.SetControl();
            Point p1 = btn_Speichern_Unter.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;

            if (frm.ShowDialog() == DialogResult.OK)
            {
                RecordSet rs = new RecordSet();
                rs.Open("select Bezeichner from Tab_Prozesswaerme_STAMM where Bezeichner='" + frm.m_szName + "'");
                if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); rs.Close(); return; }
                rs.Close(); 
                textBox_Prozessname.Text = frm.m_szName;

                string sqlI = "INSERT INTO Tab_Prozesswaerme_STAMM (Bezeichner, Typ, Beschreibung, Monat_1, Monat_2, Monat_3, Monat_4, Monat_5, Monat_6, Monat_7, Monat_8, Monat_9, Monat_10, Monat_11, Monat_12, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                System.Data.OleDb.OleDbParameter[] psI = new System.Data.OleDb.OleDbParameter[16];
                psI[0] = new System.Data.OleDb.OleDbParameter("@bez", (object)textBox_Prozessname.Text ?? DBNull.Value);
                psI[1] = new System.Data.OleDb.OleDbParameter("@typ", (object)comboBox_Prozesstyp.Text ?? DBNull.Value);
                psI[2] = new System.Data.OleDb.OleDbParameter("@besch", (object)textBox_Beschreibung.Text ?? DBNull.Value);
                for (int i = 1; i <= 12; i++) psI[2 + i] = new System.Data.OleDb.OleDbParameter("@m" + i, monat[i - 1]);
                psI[15] = new System.Data.OleDb.OleDbParameter("@ro", false);
                if (DataRepository.ExecuteSQL(sqlI, psI))
                    MessageBox.Show("Daten gespeichert!");
                else
                    MessageBox.Show("Fehler beim Aktualisieren der Daten!");


            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if(comboBox_Prozesstyp.Text == "" )
            {
                MessageBox.Show("Prozesstyp auswählen!");
                return;
            }
            // Leer- und Zahlpruefung jetzt zentral mit sprechender Meldung; der
            // Speicherweg uebernimmt die geprueften Werte, statt erneut zu parsen.
            double[] monat;
            if (!MonatswertePruefen(out monat)) return;

            string sqlI = "INSERT INTO Tab_Prozesswaerme_STAMM (Bezeichner, Typ, Beschreibung, Monat_1, Monat_2, Monat_3, Monat_4, Monat_5, Monat_6, Monat_7, Monat_8, Monat_9, Monat_10, Monat_11, Monat_12, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            System.Data.OleDb.OleDbParameter[] psI = new System.Data.OleDb.OleDbParameter[16];
            psI[0] = new System.Data.OleDb.OleDbParameter("@bez", (object)textBox_Prozessname.Text ?? DBNull.Value);
            psI[1] = new System.Data.OleDb.OleDbParameter("@typ", (object)comboBox_Prozesstyp.Text ?? DBNull.Value);
            psI[2] = new System.Data.OleDb.OleDbParameter("@besch", (object)textBox_Beschreibung.Text ?? DBNull.Value);
            for (int i = 1; i <= 12; i++) psI[2 + i] = new System.Data.OleDb.OleDbParameter("@m" + i, monat[i - 1]);
            psI[15] = new System.Data.OleDb.OleDbParameter("@ro", false);
            if (DataRepository.ExecuteSQL(sqlI, psI))
                MessageBox.Show("Daten gespeichert!");
            else
                MessageBox.Show("Fehler beim Aktualisieren der Daten!");
        }

 
  
    }
}
