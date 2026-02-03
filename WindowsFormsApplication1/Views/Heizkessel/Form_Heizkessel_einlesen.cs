using System;
using System.Data.Odbc;
using System.IO;
using System.Web;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel_einlesen : Form
    {
        private HeizkesselImport ctrl = new HeizkesselImport();

        string szBrennstoffIndex = string.Empty;
        string szBrennstoffart = string.Empty;
        string szCO2 = string.Empty;
        string szNOx = string.Empty;
        string szCO = string.Empty;

        public Form_Heizkessel_einlesen ()
        {
            InitializeComponent();
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_VDI3805_Click(object sender, EventArgs e)
        {
            string filename = "";

            Liste_Heizkessel.Items.Clear();

            string szAppDataPath = Path.Combine(Program.ApplicationPath_User, "VDI_Heizkessel");

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = szAppDataPath;
            openFileDialog.Filter = "(*.vdi)|*.vdi";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;

                ctrl.Import(filename);
                for (int i = 0; i < ctrl._list.Count; i++)
                {
                    Liste_Heizkessel.Items.Add(ctrl._list[i].m_szName);
                }
            }
        }

        private void Liste_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
          //  for (int i = 0; i < ctrl._list.Count; i++)
            {
               // if (Liste_Heizkessel.Text == ctrl._list[i].m_szName)
                {

                    int i= Liste_Heizkessel.SelectedIndex;
                    textBox_Name.Text = Liste_Heizkessel.Text;
                    textBox_Firma.Text = ctrl._list[i].m_szFirma;
                    textBox_Bauart.Text = ctrl._list[i].m_szBauart;
                    textBox_ThLeistung.Text = ctrl._list[i].m_szThLeistung;
                    textBox_Brennstoff.Text = ctrl._list[i].m_szBrennstoff;
                    textBox_Versluste.Text = ctrl._list[i].m_szVerluste;
                    textBox__Wirkungsgrad.Text = ctrl._list[i].m_szWirkungsgrad;
                    szBrennstoffIndex = ctrl._list[i].m_szBrennstoffIndex;
                    szCO2 = ctrl._list[i].m_szCO2;
                    szNOx = ctrl._list[i].m_szNOX;
                    szCO = ctrl._list[i].m_szCO;
       //             index = i;
       
                }
            }
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            if (textBox_Name.Text == "")
            {
                MessageBox.Show("Bitte einen Heizkessel selektieren!");
                return;
            }

            rs.Open("select * from [DB-Heizung] where Name='" + textBox_Name.Text + "'");
            if (rs.Next()) { MessageBox.Show("Daten bereits eingelesen!"); rs.Close(); return; }
            rs.Close();

            try
            {
                transaction = Program.DBConnection.BeginTransaction();
                rs.DBCommand.Transaction = transaction;
                rs.Insert("INSERT INTO [DB-Heizung] (Name) SELECT '" + textBox_Name.Text + "' AS Ausdr1");
                rs.Close();

                BrennstoffCtrl ctrl = new BrennstoffCtrl();
                ctrl.model = InitDatensatzUpdate();
                ctrl.DBCommand.Transaction = transaction;

                if (ctrl.Update())
                {
                    transaction.Commit();
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    transaction.Rollback();
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                try
                {
                    // Attempt to roll back the transaction.
                    transaction.Rollback();
                }
                catch
                {
                    // Do nothing here; transaction is not active.
                }
            }
        }

        BrennstoffModel InitDatensatzUpdate()
        {
            BrennstoffModel model = new BrennstoffModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Firma.Text;
            model.Beschreibung = textBox_Bauart.Text;
            model.Ptherm = Program.convertTxt2Double(textBox_ThLeistung.Text);
            int nBrennstoffart = Program.convertTxt2Int(szBrennstoffart);
            if(nBrennstoffart == 0) model.Wirkungsgrad_Gas = Program.convertTxt2Double(textBox__Wirkungsgrad.Text) /100;
            else if(nBrennstoffart == 1) model.Wirkungsgrad_Oel = Program.convertTxt2Double(textBox__Wirkungsgrad.Text) / 100;
            else
            {
                model.Wirkungsgrad_Gas = model.Wirkungsgrad_Oel = Program.convertTxt2Double(textBox__Wirkungsgrad.Text) / 100;
            }
            if(model.Wirkungsgrad_Gas == 0 && model.Wirkungsgrad_Oel == 0)
                model.Wirkungsgrad_Gas = model.Wirkungsgrad_Oel = 1;

            model.Betriebsbereitschaftverlust = Program.convertTxt2Double(textBox_Versluste.Text);
            int Brennstoffindex = Program.convertTxt2Int(szBrennstoffIndex);
            if(Brennstoffindex > 22) Brennstoffindex = 23;
            model.Brennstoff = Brennstoffindex;
            model.NOx = Program.convertTxt2Double(szNOx);
            model.CO2 = Program.convertTxt2Double(szCO2);
            model.CO = Program.convertTxt2Double(szCO);

            return model;
        }

    }
}