using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    public partial class FormTest : Form
    {
        public FormTest()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal; 
        }

        private void butt_WP_Click(object sender, EventArgs e)
        {
            WPTestCtrl wpclass = new WPTestCtrl();
            wpclass.ReadAll();
            wpclass.FillListBox(listBoxWP);
            wpclass = null;
        }

        private void listBoxWP_SelectedIndexChanged(object sender, EventArgs e)
        {
            textWPName.Text = listBoxWP.Text;
            WPTestCtrl wpclass = new WPTestCtrl();
            wpclass.ReadSingle("select * from Tab_WP where WPName='" + textWPName.Text + "' order by ID_WP");
            textBeschreibung.Text = wpclass.Beschreibung;
            wpclass = null;
        }

        private void butt_Speichern_Click(object sender, EventArgs e)
        {
            if (listBoxWP.SelectedIndex != -1)
            {
                WPTestCtrl wpclass = new WPTestCtrl();
                wpclass.ReadSingle("select * from Tab_WP where WPName='" + textWPName.Text + "' order by ID_WP");
                wpclass.Beschreibung = textBeschreibung.Text;
                wpclass.Update();
                wpclass = null;
            }
            else
            {
                MessageBox.Show("Wärmepumpe auswählen", "Hinweis");
            }
        }

        private void butt_Delete_Click(object sender, EventArgs e)
        {
            if (listBoxWP.SelectedIndex != -1)
            {
                WPTestCtrl wpclass = new WPTestCtrl();
                //wpclass.ReadSingle("select * from Tab_WP where WPName='" + textWPName.Text + "' order by ID_WP");
                wpclass.WPName = textWPName.Text;
                wpclass.Delete();
                //listBoxWP.Items.Remove(textWPName.Text);
                wpclass.ReadAll();
                wpclass.FillListBox(listBoxWP);
                wpclass = null;
            }
            else
            {
                MessageBox.Show("Wärmepumpe auswählen", "Hinweis");
            }
        }

        private void butt_Neu_Click(object sender, EventArgs e)
        {
            WPTestCtrl wpclass = new WPTestCtrl();
            wpclass.WPName = textWPName.Text;
            wpclass.Beschreibung = textBeschreibung.Text;
            wpclass.Add();
            wpclass.ReadAll();
            wpclass.FillListBox(listBoxWP);
            wpclass = null;

        }

        private void butt_ExcelDatei_Click(object sender, EventArgs e)
        {
            FileDlgClass dlg = new FileDlgClass();
            string file = dlg.FileDlg();
        }
    }
}
