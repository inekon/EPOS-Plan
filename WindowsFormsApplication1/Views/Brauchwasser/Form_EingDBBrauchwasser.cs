using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_EingDBBrauchwasser : Form
    {
        public string m_szBezeichner;
        public string m_szBeschreibung;
        public string m_szBrauchwassertyp;
        public string mode;
        
        public Form_EingDBBrauchwasser()
        {
            InitializeComponent();

            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Brauchwassertyp_STAMM order by Bezeichner");

            while(rs.Next())
            {
                comboBox_Brauchwassertyp.Items.Add(rs.Read("Bezeichner"));
            }
            rs.Close(); 
         }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();

            textBox_Bezeichner.Text = m_szBezeichner;
            textBox_Beschreibung.Text = m_szBeschreibung;
            comboBox_Brauchwassertyp.Text = m_szBrauchwassertyp;
            rs.Open("select * from Tab_Brauchwasser_STAMM where Bezeichner='" + textBox_Bezeichner.Text + "'");

            if (rs.Next())
            {
                Wert1.Text = ((double)rs.Read("Monat_1")).ToString("F4");
                Wert2.Text = ((double)rs.Read("Monat_2")).ToString("F4");
                Wert3.Text = ((double)rs.Read("Monat_3")).ToString("F4");
                Wert4.Text = ((double)rs.Read("Monat_4")).ToString("F4");
                Wert5.Text = ((double)rs.Read("Monat_5")).ToString("F4");
                Wert6.Text = ((double)rs.Read("Monat_6")).ToString("F4");
                Wert7.Text = ((double)rs.Read("Monat_7")).ToString("F4");
                Wert8.Text = ((double)rs.Read("Monat_8")).ToString("F4");
                Wert9.Text = ((double)rs.Read("Monat_9")).ToString("F4");
                Wert10.Text = ((double)rs.Read("Monat_10")).ToString("F4");
                Wert11.Text = ((double)rs.Read("Monat_11")).ToString("F4");
                Wert12.Text = ((double)rs.Read("Monat_12")).ToString("F4");
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

            BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
            // SaveHead prueft selbst auf ReadOnly und meldet ggf.
            if (ctrl.SaveHead(m_szBezeichner, comboBox_Brauchwassertyp.Text, textBox_Beschreibung.Text, monat, false))
                MessageBox.Show("Daten aktualisiert!");
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

            // iU9-W2.1: Namensabfrage ueber NamensDialogHuelle statt
            // Form_Sp_ItemNeu (mittig statt an der Knopfposition - die
            // Blazor-Huelle kennt kein PointToScreen; Name kommt getrimmt).
            string szName = NamensDialogHuelle.Bezeichner(this, textBox_Bezeichner.Text);

            if (szName != null)
            {
                BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
                if (ctrl.Exists(szName)) { MessageBox.Show("Name existiert bereits!"); return; }
                textBox_Bezeichner.Text = szName;

                if (ctrl.SaveHead(textBox_Bezeichner.Text, comboBox_Brauchwassertyp.Text, textBox_Beschreibung.Text, monat, true))
                    MessageBox.Show("Daten gespeichert!");
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if(comboBox_Brauchwassertyp.Text == "" )
            {
                MessageBox.Show("Brauchwassertyp auswählen!");
                return;
            }
            // Leer- und Zahlpruefung jetzt zentral mit sprechender Meldung; der
            // Speicherweg uebernimmt die geprueften Werte, statt erneut zu parsen.
            double[] monat;
            if (!MonatswertePruefen(out monat)) return;

            BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
            if (ctrl.SaveHead(textBox_Bezeichner.Text, comboBox_Brauchwassertyp.Text, textBox_Beschreibung.Text, monat, true))
                MessageBox.Show("Daten gespeichert!");
        }

 
  
    }
}
