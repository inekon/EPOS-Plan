using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_AdminPV : Form
    {
        PhotovoltaikModel model = new PhotovoltaikModel();
        public List<WErzeugerModel> list_pvmodel = new List<WErzeugerModel>();
        public bool m_bItemBearbeiten = false;
        private bool m_Neu = false;

        public Form_AdminPV ()
        {
            InitializeComponent();
        }
        
        public void SetControls(string projekt)
        {
            listBox_PV.Items.Clear();
            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                listBox_PV.Items.Add(list_pvmodel[i].Bezeichner);
            }
            if (listBox_PV.Items.Count > 0) listBox_PV.SelectedIndex = 0;
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Abbruch_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_AdminPV_Load(object sender, EventArgs e)
        {
            if (m_bItemBearbeiten) return;

            RecordSet rs = new RecordSet();
            rs.Open("SELECT * FROM Tab_PV_STAMM");   
            
            while (rs.Next())
            {
                string bezeichner = rs.Read("Bezeichner").ToString();
                Console.WriteLine("Bezeichner: {bezeichner}");
                listBox_PV.Items.Add(bezeichner);
            }
            if(listBox_PV.Items.Count > 0)  listBox_PV.SelectedIndex = 0;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            // Folgepaket zu ab5bf32: Zahlen erst hier pruefen, nicht mehr beim Verlassen
            // des Feldes. Bewusst VOR dem try: der Catch unten wuerde einen Parse-Fehler
            // sonst als "Fehler beim Speichern" melden und mit InitControls saemtliche
            // Eingaben leeren. Bei ungueltiger Eingabe bleibt der Dialog offen und es
            // wird nichts geschrieben.
            // Alle Felder sind im PhotovoltaikModel double (Tab_PV_STAMM ebenso), daher
            // durchgaengig ZahlPruefen - auch Laenge und Breite, deren alte Pruefung
            // checkInt war, obwohl sie in Metern mit Nachkommastellen gespeichert werden.
            double wirkungsgrad, leistung, uLeerlauf, uMpp, iMpp, iKurzschluss;
            double tempKoeff, laenge, breite, modulkosten;

            if (!Program.ZahlPruefen(textBox_Wirkungsgrad, "Wirkungsgrad", out wirkungsgrad, true)) return;
            // Leistung: ein leeres Feld meldete bisher schon beim Verlassen
            // ("Leistungseingabe überprüfen!") - die Meldung kommt jetzt hier,
            // leer bleibt also unzulaessig.
            if (!Program.ZahlPruefen(textBox_Leistung, "Nennleistung (Pmax)", out leistung)) return;
            if (!Program.ZahlPruefen(textBox_ULeerlauf, "Leerlaufspannung (Uoc)", out uLeerlauf, true)) return;
            if (!Program.ZahlPruefen(textBox_UMpp, "Spannung im MPP (Umpp)", out uMpp, true)) return;
            if (!Program.ZahlPruefen(textBox_IMpp, "Strom im MPP (Impp)", out iMpp, true)) return;
            if (!Program.ZahlPruefen(textBox_IKurzschluss, "Kurzschlussstrom (Isc)", out iKurzschluss, true)) return;
            if (!Program.ZahlPruefen(textBox_TempKoeff, "Temp.-Koeffizient Pmax", out tempKoeff, true)) return;
            if (!Program.ZahlPruefen(textBox_Laenge, "Länge", out laenge, true)) return;
            if (!Program.ZahlPruefen(textBox_Breite, "Breite", out breite, true)) return;
            if (!Program.ZahlPruefen(textBox_Modulkosten, "Modulkosten", out modulkosten, true)) return;

            try
            {
                model.m_szName = textBox_Bezeichner.Text;
                model.m_szBeschreibung = textBox_Beschreibung.Text;
                model.m_Wirkungsgrad = wirkungsgrad;
                model.m_Leistung = leistung;
                model.m_U_Leerlauf = uLeerlauf;
                model.m_U_Mpp = uMpp;
                model.m_I_Mpp = iMpp;
                model.m_I_Kurzschluss = iKurzschluss;
                model.m_Temp_Coeff_Pmax = tempKoeff;
                model.m_Laenge = laenge;
                model.m_Breite = breite;
                model.m_Modulkosten = modulkosten;

                if (m_Neu)
                {
                    PhotovoltaikStammCtrl ctrl = new PhotovoltaikStammCtrl();
                    if (ctrl.Exists(model.m_szName)) { MessageBox.Show("Name existiert bereits!"); return; }

                    if (ctrl.InsertFrom(model))
                    {
                        listBox_PV.Items.Add(textBox_Bezeichner.Text);
                        listBox_PV.SelectedIndex = listBox_PV.Items.Count - 1;
                        m_Neu = false;
                        MessageBox.Show("Datensatz gespeichert!");
                    }
                    else { MessageBox.Show("Fehler beim Speichern des Datensatzes!"); }
                }
                else
                {
                    PhotovoltaikStammCtrl ctrl = new PhotovoltaikStammCtrl();
                    if (ctrl.UpdateFrom(model, listBox_PV.Text))
                    {
                        MessageBox.Show("Datensatz gespeichert!");
                    }
                }
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                m_Neu = false;
                InitControls();
                return;
            }
            return;
        }

        private void listBox_PV_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            textBox_Bezeichner.Text = listBox_PV.Text;
            model.m_szName = textBox_Bezeichner.Text;
            
            rs.Open("SELECT * FROM Tab_PV_STAMM where Bezeichner='" + listBox_PV.Text + "'");

            if (!rs.EOF())
            {
                textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                model.m_szBeschreibung = textBox_Beschreibung.Text;
                textBox_Firma.Text = (string)rs.Read("Firma");
                model.m_szFirma = textBox_Firma.Text;
                textBox_Wirkungsgrad.Text = Convert.ToDouble(rs.Read("Wirkungsgrad")).ToString("F2");
                model.m_Wirkungsgrad = Program.convertTxt2Double(textBox_Wirkungsgrad.Text);
                textBox_Leistung.Text = Convert.ToDouble(rs.Read("Leistung")).ToString("F2");
                model.m_Leistung = Program.convertTxt2Double(textBox_Leistung.Text);
               
                textBox_ULeerlauf.Text = rs.Read("U_Leerlauf").ToString();
                model.m_U_Leerlauf = Program.convertTxt2Double(textBox_ULeerlauf.Text);
                textBox_UMpp.Text = rs.Read("U_Mpp").ToString();
                model.m_U_Mpp = Program.convertTxt2Double(textBox_UMpp.Text);
                textBox_IMpp.Text = rs.Read("I_Mpp").ToString();
                model.m_I_Mpp = Program.convertTxt2Double(textBox_IMpp.Text);
                textBox_IKurzschluss.Text = rs.Read("I_Kurzschluss").ToString();
                model.m_I_Kurzschluss = Program.convertTxt2Double(textBox_IKurzschluss.Text);
                textBox_TempKoeff.Text = rs.Read("gamma_PMP").ToString();
                model.m_Temp_Coeff_Pmax = Program.convertTxt2Double(textBox_TempKoeff.Text);
                textBox_Laenge.Text = rs.Read("Laenge").ToString();
                model.m_Laenge = Program.convertTxt2Double(textBox_Laenge.Text);
                textBox_Breite.Text = rs.Read("Breite").ToString();
                model.m_Breite = Program.convertTxt2Double(textBox_Breite.Text);
                textBox_Modulkosten.Text = rs.Read("Modulkosten").ToString();
                model.m_Modulkosten = Program.convertTxt2Double(textBox_Modulkosten.Text);

            }
            rs.Close();
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            InitControls();
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();
            
            Point p1 = btn_Neu.Location;  
            p1 = this.PointToScreen(p1); 
            frm.Location = p1;  

            if (frm.ShowDialog() == DialogResult.OK)
            {
                m_Neu = true;
                textBox_Bezeichner.Text = frm.m_szName;
                textBox_Firma.Text = "";
                textBox_Beschreibung.Text = "";
                textBox_ULeerlauf.Text = "0";
                textBox_UMpp.Text = "0";
                textBox_Leistung.Text = "0";
                textBox_Wirkungsgrad.Text = "0";
                textBox_IMpp.Text = "0";
                textBox_IKurzschluss.Text = "0";
                textBox_TempKoeff.Text = "0";
                textBox_Laenge.Text = "0";
                textBox_Breite.Text = "0";
                textBox_Modulkosten.Text = "0"; 
            }
            return;
        }

        private void InitControls()
        {
            m_Neu = false;
            textBox_Bezeichner.Text = "";
            textBox_Firma.Text = "";
            textBox_Beschreibung.Text = "";
            textBox_UMpp.Text = "";
            textBox_ULeerlauf.Text = "";
            textBox_Wirkungsgrad.Text = "";
            textBox_Leistung.Text = "";
            textBox_Firma.Text = "";
            textBox_IMpp.Text = "";
            textBox_IKurzschluss.Text = "";
            textBox_TempKoeff.Text = "";
            textBox_Laenge.Text = "";
            textBox_Breite.Text = "";
            textBox_Modulkosten.Text = "0";
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close(); 
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listBox_PV.SelectedIndex == -1)
            {
                MessageBox.Show("Modul in Liste auswählen!");
                return;
            }
            try
            {
                PhotovoltaikStammCtrl ctrl = new PhotovoltaikStammCtrl();
                if (!ctrl.Delete(textBox_Bezeichner.Text)) return;
                listBox_PV.Items.Remove(textBox_Bezeichner.Text);
                listBox_PV.SelectedIndex = listBox_PV.Items.Count - 1;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return;
            }

        }

        // Die Eingabefelder faerben nur noch (Folgepaket zu ab5bf32): kein modales
        // Melden und kein Undo() mehr beim Verlassen des Feldes - so bleibt auch das
        // Durchklicken des Katalogs und der Weg zu Abbrechen/Beenden meldungsfrei.
        // Geprueft wird in btn_Speichern_Click. Gefaerbt wird nach dem Speichertyp:
        // alle Felder sind double, daher durchgaengig ZahlFaerben.
        private void textBox_Leistung_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Wirkungsgrad_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_UMpp_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_ULeerlauf_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_IMpp_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }
        private void textBox_IKurzschluss_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }
        private void textBox_TempKoeff_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }
        // Laenge und Breite: bewusst Zahl-Faerbung, obwohl die alte Pruefung checkInt
        // war - gespeichert werden beide als double (Meterangabe mit Nachkommastellen).
        private void textBox_Laenge_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }
        private void textBox_Breite_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Modulkosten_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }
    }
}
