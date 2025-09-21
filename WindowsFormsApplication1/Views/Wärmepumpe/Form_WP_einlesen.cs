using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_WP_einlesen : Form
    {
        WaermepumpenImport ctrl = new WaermepumpenImport();

        int index = 0;

        public Form_WP_einlesen()
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

            Liste_WP.Items.Clear();

            string szAppDataPath = Path.Combine(Program.ApplicationPath_User, "vdi");

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
                    Liste_WP.Items.Add(ctrl._list[i].szName);
                }
            }

        }

        private void Liste_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < ctrl._list.Count; i++)
            {
                if (Liste_WP.Text == ctrl._list[i].szName)
                {
                    textBox_Name.Text = Liste_WP.Text;
                    textBox_Firma.Text = ctrl._list[i].szFirma;
                    textBox_Typ.Text = ctrl._list[i].szWPTyp;
                    textBox_Zusatzheizung.Text = ctrl._list[i].szElektrZuheizung;
                    textBox_ThLeistung.Text = ctrl._list[i].szThLeistung;
                    textBox_Aufstellung.Text = ctrl._list[i].szAufstellung;
                    textBox_Stufen.Text = ctrl._list[i].szStufen;
                    textBox_MaxVorlauf.Text = ctrl._list[i].szMaxVorlauf;
                    textBox__Wirkungsgrad.Text = ctrl._list[i].szCOP;
                    textBox_Kuehlleistung.Text = ctrl._list[i].szKuehlleistung;  
                    index = i;
                    break;
                }

            }
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            WPCtrl wpctrl = new WPCtrl();
            WPModel model = new WPModel();
            KenndatenCtrl datctrl = new KenndatenCtrl();
            KenndatenKuehlungCtrl datkuehlctrl = new KenndatenKuehlungCtrl();
            
            if (Liste_WP.Text == "") return;
            
            wpctrl.WPName = ctrl._list[index].szName;

            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_WP where WPName='" + wpctrl.WPName + "'");
            if(rs.Next()) { MessageBox.Show("Daten bereits eingelesen!"); rs.Close(); return; }    
            rs.Close(); 

            int nStufen = Program.convertTxt2Int(ctrl._list[index].szStufen);
            if (nStufen == 0) wpctrl.Regelung = "stetig";
            else if (nStufen == 1) wpctrl.Regelung = "einstufig";
            else if (nStufen == 2) wpctrl.Regelung = "zweistufig";
            else wpctrl.Regelung = "mehrstufig";

            wpctrl.Aufstellung = ctrl._list[index].szAufstellung;
            wpctrl.Firma = ctrl._list[index].szFirma;
            double pd = Program.convertTxt2Double(ctrl._list[index].szThLeistung);
            wpctrl.Nennleistung = (int)pd;
            wpctrl.Typ = ctrl._list[index].szWPTyp;

            if (ctrl._list[index].szElektrZuheizung != "")
            {
                double heizstab = Program.convertTxt2Double(ctrl._list[index].szElektrZuheizung);
                wpctrl.Heizung = (int)heizstab;
                double kuehlung = 0;
                kuehlung = Program.convertTxt2Double(ctrl._list[index].szKuehlleistung);
                wpctrl.Kuehlleistung = kuehlung; 
            }
            
            if(!wpctrl.Insert()) return;

            string vorlauf = "";
            string last = "";
            bool anfang=false;
            bool anfang_kuehl = false;
            List<string> datlines = ctrl._list[index].x;
            
            for (int i = 0; i < datlines.Count; i++)
            {
                string[] token = datlines[i].Split(';');
                if (token[0] == "710.09" && token[2] == "1")
                {
                    vorlauf = token[3];
                    anfang = true;
                }
                else if (token[0] == "710.09" && token[2] == "2")
                {
                    vorlauf = token[3];
                    anfang_kuehl = true;
                    anfang = false;
                    last = token[7];
                }
                else if(anfang && (token[0] == "710.91"))
                {
                    string cop=token[5];
                    string p=token[3];
                    string t=token[2];
            
                    datctrl.m_ID_WP = wpctrl.ID;
                    datctrl.m_nCOP = Program.convertTxt2Double(cop);
                    datctrl.m_nTemperatur = Program.convertTxt2Int(t);
                    datctrl.m_nPTherm = Program.convertTxt2Double(p);
                    datctrl.m_nVorlauf = Program.convertTxt2Int(vorlauf);
                                        
                    if(!datctrl.Insert()) return;
                }
                else if (anfang_kuehl && (token[0] == "710.91"))
                {
                    string cop = token[5];
                    string p = token[3];
                    string t = token[2];
                    
                    datkuehlctrl.m_ID_WP = wpctrl.ID;
                    datkuehlctrl.m_nCOP = Program.convertTxt2Double(cop);
                    datkuehlctrl.m_nTemperatur = Program.convertTxt2Int(t);
                    datkuehlctrl.m_nPkuehl = Program.convertTxt2Double(p);
                    datkuehlctrl.m_nVorlauf = Program.convertTxt2Int(vorlauf); ;
                    if(last.ToUpper()  == "MAX") datkuehlctrl.m_nLast = 100;
                    else datkuehlctrl.m_nLast = Program.convertTxt2Int(last);
                    
                    if (!datkuehlctrl.Insert()) return;
                }

            }

            MessageBox.Show("Daten gespeichert!");
        }


    }
}