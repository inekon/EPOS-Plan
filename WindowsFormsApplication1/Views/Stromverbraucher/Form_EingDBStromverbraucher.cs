using System;
using System.Data;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_EingDBStromverbraucher : Form
    {
        public string m_szStromname;
        public string m_szBeschreibung;
        public string m_szStromtyp;
        public string mode;

        // Beschriftung der Felder Wert1..Wert12 (Monat_1..Monat_12) fuer die Pruefmeldung.
        private static readonly string[] m_szMonate =
        {
            "Januar", "Februar", "März", "April", "Mai", "Juni",
            "Juli", "August", "September", "Oktober", "November", "Dezember"
        };

        public Form_EingDBStromverbraucher()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt

            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Stromverbrauchertyp_STAMM order by Typname");

            while (rs.Next())
            {
                comboBox_Stromtyp.Items.Add(rs.Read("Typname"));
            }
            rs.Close();

        }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();

            textBox_Stromname.Text = m_szStromname;
            textBox_Beschreibung.Text = m_szBeschreibung;
            comboBox_Stromtyp.Text = m_szStromtyp;
            rs.Open("select * from Tab_Stromverbraucher_STAMM where Bezeichner ='" + textBox_Stromname.Text + "'");

            if (rs.Next())
            {
                Wert1.Text = Convert.ToDouble(rs.Read("Monat_1")).ToString("F4");
                Wert2.Text = Convert.ToDouble(rs.Read("Monat_2")).ToString("F4");
                Wert3.Text = Convert.ToDouble(rs.Read("Monat_3")).ToString("F4");
                Wert4.Text = Convert.ToDouble(rs.Read("Monat_4")).ToString("F4");
                Wert5.Text = Convert.ToDouble(rs.Read("Monat_5")).ToString("F4");
                Wert6.Text = Convert.ToDouble(rs.Read("Monat_6")).ToString("F4");
                Wert7.Text = Convert.ToDouble(rs.Read("Monat_7")).ToString("F4");
                Wert8.Text = Convert.ToDouble(rs.Read("Monat_8")).ToString("F4");
                Wert9.Text = Convert.ToDouble(rs.Read("Monat_9")).ToString("F4");
                Wert10.Text = Convert.ToDouble(rs.Read("Monat_10")).ToString("F4");
                Wert11.Text = Convert.ToDouble(rs.Read("Monat_11")).ToString("F4");
                Wert12.Text = Convert.ToDouble(rs.Read("Monat_12")).ToString("F4");
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
        /// Prueft die zwoelf Monatswerte am Aktionsknopf (Folgepaket zu ab5bf32).
        /// Das erste ungueltige oder leere Feld meldet sprechend, bekommt den Fokus
        /// und liefert false - der Aufrufer kehrt zurueck und laesst den Dialog offen.
        /// Leer meldet wie zuvor, weil die Speicherwege einen Wert je Monat brauchen.
        /// </summary>
        private bool MonatswertePruefen(out double[] monat)
        {
            monat = new double[12];
            for (int i = 1; i <= 12; i++)
            {
                TextBox tb = this.Controls["Wert" + i.ToString()] as TextBox;
                if (!Program.ZahlPruefen(tb, "Monatswert " + m_szMonate[i - 1], out monat[i - 1])) return false;
            }
            return true;
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            double[] monat;
            if (!MonatswertePruefen(out monat)) return;

            StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
            // SaveHead prueft selbst auf ReadOnly und meldet ggf.
            if (ctrl.SaveHead(m_szStromname, comboBox_Stromtyp.Text, textBox_Beschreibung.Text, monat, false))
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

            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();

            frm.m_szName = textBox_Stromname.Text;
            frm.SetControl();

            if (frm.ShowDialog() == DialogResult.OK)
            {
                StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
                if (ctrl.Exists(frm.m_szName)) { MessageBox.Show("Name existiert bereits!"); return; }

                textBox_Stromname.Text = frm.m_szName;

                if (ctrl.SaveHead(textBox_Stromname.Text, comboBox_Stromtyp.Text, textBox_Beschreibung.Text, monat, true))
                    MessageBox.Show("Daten gespeichert!");
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (comboBox_Stromtyp.Text == "")
            {
                MessageBox.Show("Verbrauchertyp auswählen!");
                return;
            }

            // Ersetzt die frueher hier stehende Leerpruefung: MonatswertePruefen meldet
            // leere UND ungueltige Felder sprechend und setzt den Fokus.
            double[] monat;
            if (!MonatswertePruefen(out monat)) return;

            StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
            if (ctrl.SaveHead(textBox_Stromname.Text, comboBox_Stromtyp.Text, textBox_Beschreibung.Text, monat, true))
                MessageBox.Show("Daten gespeichert!");
        }
    }
}
