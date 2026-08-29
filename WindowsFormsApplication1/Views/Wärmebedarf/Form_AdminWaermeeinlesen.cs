using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    partial class Form_AdminWaermeeinlesen : BaseForm
    {
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        public List<WaermebedarfModel> DateiListe = new List<WaermebedarfModel>();
        private ToolsClass tool = new ToolsClass();
        string filename;
        string filebasename;

        public Form_AdminWaermeeinlesen()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt

            string szPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
            textBox_Ordner.Text = szPath;

            // =================================================================================
            // NOTEBOOK-FIX: Kontrolliertes Layout & Fix für die ListBox-Höhe
            // =================================================================================

            // 1. Verhindert, dass die ListBox wegen der Schriftgröße das Layout sprengt:
            this.listBox_Extern.IntegralHeight = false;

            // 2. Fenstergröße für Notebooks stabilisieren
            this.MinimumSize = new System.Drawing.Size(680, 460);
            this.AutoSize = false;

            // 3. Die linken Buttons bleiben sauber links oben verankert (keine Stauchung)
            this.label1.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.btn_Oeffnen.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.btn_Datei.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.btn_Loeschen.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.btn_Einlesen.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // 4. Die obere Pfad-Eingabe wächst elastisch nach rechts mit
            this.Label2.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.textBox_Name.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // 5. Die ListBox passt sich flexibel an, respektiert aber den Boden!
            this.listBox_Extern.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // 6. WICHTIG: Die unteren Elemente MÜSSEN mit dem Boden nach unten wandern!
            this.label6.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.textBox_Ordner.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.btn_OK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            // =================================================================================
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            result = DialogResult.OK;
            Close();
        }

        public void SetControls()
        {
            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();
            ctrl.ReadAll();

            listBox_Extern.Items.Clear();

            for (int i = 0; i < ctrl.rows; i++)
            {
                WaermebedarfModel model = new WaermebedarfModel();

                model.m_szBezeichner = ctrl.items[i].m_szBezeichner;
                listBox_Extern.Items.Add(model.m_szBezeichner);
                DateiListe.Add(model);
            }
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void btn_Oeffnen_Click(object sender, EventArgs e)
        {
            ToolsClass tool = new ToolsClass();

            //string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "Waermebedarf");
            //szAppDataPath = Path.Combine(szAppDataPath, textBox_Name.Text);
            tool.OpenFileWithDefaultApp(textBox_Name.Text);
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            WaermebedarfStammCtrl ctrl_ganglinie = new WaermebedarfStammCtrl();
            Z_ProjektGebGanglinieCtrl ctrl = new Z_ProjektGebGanglinieCtrl();
            ctrl.ReadAll("Select * from Z_ProjektWaermebedarf where Bezeichner ='" + listBox_Extern.Text + "'");
            if (ctrl.rows > 0)
            {
                MessageBox.Show("Es existiert eine Projektzuordnung, Löschen nicht möglich!");
                return;
            }

            // Delete prueft selbst auf ReadOnly und meldet ggf.
            if (!ctrl_ganglinie.Delete(listBox_Extern.Text)) return;
            SetControls();
        }

        private void btn_Datei_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = textBox_Ordner.Text;
            openFileDialog.Filter = "(*.txt)|*.txt";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;
                filebasename = System.IO.Path.GetFileName(filename);

                try
                {
                    if (!File.Exists(textBox_Ordner.Text + "\\" + filebasename))
                    {
                        File.Copy(filename, textBox_Ordner.Text + "\\" + filebasename, true);
                    }
                    textBox_Name.Text = textBox_Ordner.Text + "\\" + filebasename;
                }
                catch { }
            }
            openFileDialog = null;
        }

        private void btn_Einlesen_Click(object sender, EventArgs e)
        {
            // Datei schon eingelesen?
            if (listBox_Extern.FindString(Path.GetFileNameWithoutExtension(filebasename)) != ListBox.NoMatches) return;

            // Datei in Liste einlesen 
            if (!tool.OpenText(textBox_Name.Text)) return;

            this.Cursor = Cursors.WaitCursor;

            // Import in die STAMM-Tabellen (Kopf + Daten)
            WaermebedarfStammCtrl ctrl_stamm = new WaermebedarfStammCtrl();
            ctrl_stamm.ImportGanglinie(Path.GetFileNameWithoutExtension(filebasename), tool.textList);

            this.Cursor = Cursors.Default;
            SetControls();
        }

        private void listBox_Extern_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
