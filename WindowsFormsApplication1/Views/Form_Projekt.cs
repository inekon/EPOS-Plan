using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Projekt : Form
    {
        public Form_Projekt()
        {
            InitializeComponent();
        }

        private void Form_Projekt_Load(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadAll();
            ctrl.FillListBox(listBox_Projekte);
    
        }

        private void button_Speichern_Click(object sender, EventArgs e)
        {
            if (listBox_Projekte.SelectedIndex != -1)
            {
                ProjektCtrl ctrl = new ProjektCtrl();
                ctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Name.Text + "' order by Projektname");
                ctrl.Bearbeiter = textBox_Bearbeiter.Text;
                ctrl.Beschreibung = textBox_Beschreibung.Text;
                ctrl.Kunde = textBox_Kunde.Text;
                ctrl.Aenderung = DateTime.Now;
                ctrl.Update();
                ctrl = null;
            }
            else
            {
                MessageBox.Show("Projekt auswählen", "Hinweis");
            }
        }

        private void listBox_Projekte_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_Projekte.Text;
            ProjektCtrl wpclass = new ProjektCtrl();
            wpclass.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Name.Text + "' order by Projektname");
            textBox_Bearbeiter.Text = wpclass.Bearbeiter;
            textBox_Beschreibung.Text = wpclass.Beschreibung;
            textBox_Kunde.Text = wpclass.Kunde;
            textBox_Aenderung.Text = wpclass.Aenderung.ToString();
            wpclass = null;
        }

        private void button_Loeschen_Click(object sender, EventArgs e)
        {
            if (listBox_Projekte.SelectedIndex != -1)
            {
                ProjektCtrl wpclass = new ProjektCtrl();
                //wpclass.ReadSingle("select * from Tab_WP where WPName='" + textWPName.Text + "' order by ID_WP");
                wpclass.Projektname = textBox_Name.Text;
                wpclass.Delete();
                //listBoxWP.Items.Remove(textWPName.Text);
                wpclass.ReadAll();
                wpclass.FillListBox(listBox_Projekte);
                wpclass = null;
            }
            else
            {
                MessageBox.Show("Projekt auswählen", "Hinweis");
            }
        }

        private void button_Neu_Click(object sender, EventArgs e)
        {
            ProjektCtrl wpclass = new ProjektCtrl();
            wpclass.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Name.Text + "' order by Projektname");
            if (wpclass.rows == 0)
            {
                wpclass.Projektname = textBox_Name.Text;
                wpclass.Beschreibung = textBox_Beschreibung.Text;
                wpclass.Bearbeiter = textBox_Bearbeiter.Text;
                wpclass.Kunde = textBox_Kunde.Text;
                wpclass.Aenderung = DateTime.Now;

                wpclass.Add();
                wpclass.ReadAll();
                wpclass.FillListBox(listBox_Projekte);
            }
            else
            {
                MessageBox.Show("Projekt bereits vorhanden", "Hinweis");
            }
            wpclass = null;
        }
    }
}
