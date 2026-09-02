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

        // =============================================================================
        // T_NOCT und der Erhalt der nicht editierten Katalogfelder (Paket A, E1.2)
        // =============================================================================
        //
        // DAS FELD. Bis Paket A war T_NOCT die einzige Ertragsgroesse des Modulkatalogs
        // ohne Eingabemoeglichkeit - die Simulation rechnete mit fest verdrahteten
        // 45 Grad C. Seit E1.2 liest sie den Katalogwert; damit braucht er eine Maske.
        // Programmatisch, weil Designer und .resx dieses Formulars nicht von Hand
        // editiert werden (CLAUDE.md des Hauptprojekts).
        //
        // DER BESTANDSFEHLER, DER DABEI GESCHLOSSEN WIRD. btn_Speichern_Click fuellte
        // ein FRISCHES PhotovoltaikModel nur aus den Maskenfeldern; alpha_SC, beta_OC
        // und T_NOCT blieben auf 0 und wurden von PhotovoltaikStammCtrl.Update
        // mitgeschrieben. Jedes Speichern eines CEC-Moduls loeschte damit genau die
        // drei Katalogwerte, die der Import geliefert hatte. T_NOCT ist jetzt
        // editierbar, alpha_SC und beta_OC werden aus dem GELADENEN Datensatz erhalten.

        private const int TNOCT_LABEL_LINKS = 11;
        private const int TNOCT_LABEL_BREITE = 150;
        private const int TNOCT_FELD_LINKS = 165;
        private const int TNOCT_FELD_BREITE = 55;
        private const int TNOCT_OBEN = 319;

        private TextBox textBox_TNoct;
        private ToolTip _tipAdminPv;

        /// <summary>alpha_SC des gerade geladenen Katalogsatzes - nicht editierbar, aber zu erhalten.</summary>
        private double _alphaScGeladen;

        /// <summary>beta_OC des gerade geladenen Katalogsatzes - nicht editierbar, aber zu erhalten.</summary>
        private double _betaOcGeladen;

        public Form_AdminPV ()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TNoctFeldAnlegen();
        }

        /// <summary>
        /// Legt Beschriftung, Eingabefeld und Einheit fuer <c>T_NOCT</c> an - in der
        /// linken Spalte unter den Knoepfen „Loeschen"/„Neu…", wo als einziger Bereich
        /// der Maske Platz frei ist (die rechte Wertespalte beginnt bei x = 253 und ist
        /// bis zu den Knoepfen bei y = 449 durchgehend belegt).
        /// </summary>
        private void TNoctFeldAnlegen()
        {
            _tipAdminPv = new ToolTip();

            Label lbl = new Label();
            lbl.Text = MyResource.Resource.PV_MODUL_LABEL_TNOCT;
            lbl.AutoSize = false;
            lbl.Size = new Size(TNOCT_LABEL_BREITE, 19);
            lbl.Location = new Point(TNOCT_LABEL_LINKS, TNOCT_OBEN + 3);
            lbl.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            Controls.Add(lbl);

            textBox_TNoct = new TextBox();
            textBox_TNoct.Location = new Point(TNOCT_FELD_LINKS, TNOCT_OBEN);
            textBox_TNoct.Size = new Size(TNOCT_FELD_BREITE, 25);
            textBox_TNoct.Font = new Font("Segoe UI", 10f);
            textBox_TNoct.TextAlign = HorizontalAlignment.Right;
            textBox_TNoct.TextChanged += (s, e) => Program.ZahlFaerben(s);
            Controls.Add(textBox_TNoct);

            Label einheit = new Label();
            einheit.Text = "°C";
            einheit.AutoSize = true;
            einheit.Location = new Point(TNOCT_FELD_LINKS + TNOCT_FELD_BREITE + 6, TNOCT_OBEN + 3);
            einheit.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            Controls.Add(einheit);

            _tipAdminPv.SetToolTip(lbl, MyResource.Resource.PV_MODUL_TIP_TNOCT);
            _tipAdminPv.SetToolTip(textBox_TNoct, MyResource.Resource.PV_MODUL_TIP_TNOCT);
        }

        /// <summary>Zahlenwert einer Katalogspalte; NULL und Unlesbares gelten als 0.</summary>
        private static double ZuZahl(object wert)
        {
            if (wert == null || wert == DBNull.Value) return 0.0;
            try { return Convert.ToDouble(wert); } catch { return 0.0; }
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

            // E1.2: neu editierbar. Leer gilt wie bei den uebrigen Feldern als 0 - und
            // 0 liegt ausserhalb des Plausibilitaetsfensters, die Simulation faellt dann
            // auf 45 Grad C zurueck und sagt das im Protokoll.
            double tNoct;
            if (!Program.ZahlPruefen(textBox_TNoct, MyResource.Resource.PV_MODUL_FELD_TNOCT, out tNoct, true)) return;

            try
            {
                model.m_szName = textBox_Bezeichner.Text;
                model.m_szBeschreibung = textBox_Beschreibung.Text;
                model.m_szFirma = textBox_Firma.Text;
                model.m_Wirkungsgrad = wirkungsgrad;
                model.m_Leistung = leistung;
                model.m_U_Leerlauf = uLeerlauf;
                model.m_U_Mpp = uMpp;
                model.m_I_Mpp = iMpp;
                model.m_I_Kurzschluss = iKurzschluss;
                model.m_Temp_Coeff_Pmax = tempKoeff;
                model.m_T_NOCT = tNoct;
                model.m_Laenge = laenge;
                model.m_Breite = breite;
                model.m_Modulkosten = modulkosten;

                // NICHT EDITIERTE FELDER AUS DEM GELADENEN DATENSATZ ERHALTEN.
                // alpha_SC und beta_OC haben keine Maske; ohne diese beiden Zeilen
                // schriebe der Speicherweg sie mit 0 zurueck und loeschte damit die
                // Werte des CEC-Imports (Bestandsfehler, siehe Kopf der Klasse).
                model.m_alpha_SC = _alphaScGeladen;
                model.m_beta_OC = _betaOcGeladen;

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
            
            rs.Open("SELECT * FROM Tab_PV_STAMM where Bezeichner='" + listBox_PV.Text + "'");

            if (!rs.EOF())
            {
                textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Firma.Text = (string)rs.Read("Firma");
                // Hier wird nur die Anzeige besetzt. Das Model fuellt
                // ausschliesslich btn_Speichern_Click aus den mit ZahlPruefen
                // geprueften Feldern - die DB-Werte stehen hier im Format der
                // Systemkultur (z.B. "17,50") und duerfen nicht kulturinvariant
                // zurueckgelesen werden.
                textBox_Wirkungsgrad.Text = Convert.ToDouble(rs.Read("Wirkungsgrad")).ToString("F2");
                textBox_Leistung.Text = Convert.ToDouble(rs.Read("Leistung")).ToString("F2");
               
                textBox_ULeerlauf.Text = rs.Read("U_Leerlauf").ToString();
                textBox_UMpp.Text = rs.Read("U_Mpp").ToString();
                textBox_IMpp.Text = rs.Read("I_Mpp").ToString();
                textBox_IKurzschluss.Text = rs.Read("I_Kurzschluss").ToString();
                textBox_TempKoeff.Text = rs.Read("gamma_PMP").ToString();
                textBox_TNoct.Text = rs.Read("T_NOCT").ToString();
                textBox_Laenge.Text = rs.Read("Laenge").ToString();
                textBox_Breite.Text = rs.Read("Breite").ToString();
                textBox_Modulkosten.Text = rs.Read("Modulkosten").ToString();

                // Merken, was die Maske NICHT zeigt - damit das Speichern es nicht
                // ueberschreibt (siehe Kopf der Klasse).
                _alphaScGeladen = ZuZahl(rs.Read("alpha_SC"));
                _betaOcGeladen = ZuZahl(rs.Read("beta_OC"));
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
                textBox_TNoct.Text = "0";
                textBox_Laenge.Text = "0";
                textBox_Breite.Text = "0";
                textBox_Modulkosten.Text = "0";
                // Ein NEUER Katalogsatz hat nichts zu erhalten.
                _alphaScGeladen = 0.0;
                _betaOcGeladen = 0.0;
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
            textBox_IMpp.Text = "";
            textBox_IKurzschluss.Text = "";
            textBox_TempKoeff.Text = "";
            textBox_TNoct.Text = "";
            textBox_Laenge.Text = "";
            textBox_Breite.Text = "";
            textBox_Modulkosten.Text = "0";
            _alphaScGeladen = 0.0;
            _betaOcGeladen = 0.0;
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
