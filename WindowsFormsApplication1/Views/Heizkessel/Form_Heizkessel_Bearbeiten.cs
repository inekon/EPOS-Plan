using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel_Bearbeiten : BaseForm
    {
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public string m_szKessel = "";
        private int m_mode = MODE_EDIT;

        // Beim Knopfdruck geprüft (EingabenPruefen) und von InitDatensatzUpdate
        // unverändert ins Modell übernommen - so kommt "12.5" wie "12,5" als 12,5 an.
        private double m_dPtherm, m_dWirkungsgradGas, m_dWirkungsgradOel, m_dBBVerlust;
        private double m_dInvestitionskosten, m_dNutzungsdauer, m_dRaumbedarf;
        private double m_dNOx, m_dCO2, m_dCO, m_dSO2, m_dStaub;
        private int m_nVorlauf, m_nRuecklauf;

        public Form_Heizkessel_Bearbeiten(int mode)
        {
            InitializeComponent();
            m_mode = mode;

            if (mode == MODE_EDIT)
            {
                btn_Speichern.Enabled = false;
                btn_Speichern_Unter.Enabled = true;
                btn_Ueberschreiben.Enabled = true;
            }
            else
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Ueberschreiben.Enabled = false;

                textBox_Beschreibung.Text = "";
                textBox_Hersteller.Text = "";
                tb_th_Leistung.Text = "0";
                tb_Wirkungsgrad.Text = "0.94";
                tb_Wirkungsgrad_Öl.Text = "0";
                tb_B_Verlust.Text = "0";
                tb_Investitionskosten.Text = "0";
                tb_Nutzungsdauer.Text = "0";
                tb_Raumbedarf.Text = "0";
                tb_NOx.Text = "0";
                tb_CO2.Text = "0";
                tb_CO.Text = "0";
                tb_SO2.Text = "0";
                tb_Staub.Text = "0";
                checkBox_Brennwert.Checked = false;
            }

            HeizkesselStammCtrl ctrl = new HeizkesselStammCtrl();
            comboBox_Brennstoff.DataSource = ctrl.Brennstoffart;
        }

        public void SetControls(string szName, string szBeschreibung)
        {
            RecordSet rs = new RecordSet();

            textBox_Name.Text = szName;
            m_szKessel = szName;
            textBox_Beschreibung.Text = szBeschreibung;

            rs.Open("select * from [Tab_Heizkessel_STAMM] where Bezeichner='" + szName + "'");
            if (!rs.Next()) { rs.Close(); return; }

            textBox_Hersteller.Text = rs.GetString("Firma");
            tb_th_Leistung.Text = rs.Read("Ptherm").ToString();
            tb_Wirkungsgrad.Text = rs.Read("Wirkungsgrad_Gas").ToString();
            tb_Wirkungsgrad_Öl.Text = rs.Read("Wirkungsgrad_Öl").ToString();
            tb_B_Verlust.Text = ((double)rs.Read("Betriebsbereitschaftverlust")).ToString("F2");
            tb_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
            tb_Nutzungsdauer.Text = rs.Read("Nutzungsdauer").ToString();
            tb_Raumbedarf.Text = rs.Read("Raumbedarf").ToString();
            tb_NOx.Text = rs.Read("NOx").ToString();
            tb_CO2.Text = rs.Read("CO2").ToString();
            tb_CO.Text = rs.Read("CO").ToString();
            tb_SO2.Text = rs.Read("SO2").ToString();
            tb_Staub.Text = rs.Read("Staub").ToString();
            checkBox_Brennwert.Checked = (bool)rs.Read("Brennwert");
            textBox_Vorlauf.Text = rs.Read("Vorlauf").ToString();
            textBox_Ruecklauf.Text = rs.Read("Ruecklauf").ToString();

            if (rs.Read("Brennstoff") != DBNull.Value)
            {
                int brennstoff = (int)rs.Read("Brennstoff");
                comboBox_Brennstoff.SelectedIndex = brennstoff >= 1 ? brennstoff - 1 : 1;
            }
            rs.Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            // Erst prüfen, dann schreiben: bei ungültiger Eingabe bleibt der Dialog offen
            if (!EingabenPruefen()) return;

            HeizkesselModel model = new HeizkesselModel();
            HeizkesselStammCtrl ctrl = new HeizkesselStammCtrl();

            try
            {
                InitDatensatzUpdate(ctrl);

                if (ctrl.Update())
                {
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch
            {
                MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
            }
        }

        public bool Insert(HeizkesselModel model)
        {
            HeizkesselStammCtrl ctrl = new HeizkesselStammCtrl();
            if (ctrl.Exists(model.Name)) return false;

            ctrl.Name = model.Name;
            ctrl.Beschreibung = model.Beschreibung;
            ctrl.Firma = model.Firma;
            ctrl.Ptherm = model.Ptherm;
            ctrl.Brennstoff = model.Brennstoff;
            ctrl.Wirkungsgrad_Gas = model.Wirkungsgrad_Gas;
            ctrl.Wirkungsgrad_Oel = model.Wirkungsgrad_Oel;
            ctrl.Investitionskosten = model.Investitionskosten;
            ctrl.Raumbedarf = model.Raumbedarf;
            ctrl.Wartungskosten = model.Wartungskosten;
            ctrl.Nutzungsdauer = model.Nutzungsdauer;
            ctrl.CO2 = model.CO2;
            ctrl.SO2 = model.SO2;
            ctrl.NOx = model.NOx;
            ctrl.CO = model.CO;
            ctrl.Staub = model.Staub;
            ctrl.Betriebsbereitschaftverlust = model.Betriebsbereitschaftverlust;
            ctrl.Brennwert = model.Brennwert;
            ctrl.Vorlauf = model.Vorlauf;
            ctrl.Ruecklauf = model.Ruecklauf;

            return ctrl.Insert();
        }

        // Folgepaket zu ab5bf32: Die TextChanged-Handler färben nur noch. Gemeldet wird
        // erst beim Speichern (EingabenPruefen), damit keine Zwischeneingabe modal stört
        // und das früher hier stehende Undo() nicht mehr zwischen Fehleingabe und
        // Leerstand pendeln kann. Das Auffüllen leerer Felder mit "0" entfällt; leer
        // gilt beim Speichern weiterhin als 0.
        private void tb_th_Leistung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Wirkungsgrad_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Wirkungsgrad_Öl_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_B_Verlust_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Investitionskosten_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Raumbedarf_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Nutzungsdauer_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_CO2_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_SO2_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_NOx_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_CO_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Staub_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void btn_CO2_Click(object sender, EventArgs e)
        {
            // Wir holen uns den Namen aus der Liste der BrennstoffCtrl
            string name = comboBox_Brennstoff.Text;

            // Logik für CO2-Werte basierend auf dem Namen
            if (name.ToUpper().Contains("ÖL"))
            {
                tb_CO2.Text = "290880";
            }
            else if (name.ToUpper().Contains("GAS") && !name.Contains("Flüssiggas"))
            {
                tb_CO2.Text = "201600";
            }
            else if (name.Contains("Flüssiggas"))
            {
                tb_CO2.Text = "238680";
            }
            else tb_CO2.Text = "0";
        }

        /// <summary>
        /// Prüft alle Zahlenfelder beim Knopfdruck (Folgepaket zu ab5bf32): sprechende
        /// Meldung, Fokus ins Feld, Dialog bleibt offen. Leer gilt wie bisher als 0 -
        /// früher füllte der TextChanged leere Felder sofort mit "0" auf.
        /// </summary>
        private bool EingabenPruefen()
        {
            if (!Program.ZahlPruefen(tb_th_Leistung, "Thermische Leistung", out m_dPtherm, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Wirkungsgrad, "Wirkungsgrad Gas, Biogas, Holz und Sonstiges", out m_dWirkungsgradGas, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Wirkungsgrad_Öl, "Wirkungsgrad Öl", out m_dWirkungsgradOel, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_B_Verlust, "Betriebsbereitschaftsverluste", out m_dBBVerlust, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Investitionskosten, "Investitionskosten", out m_dInvestitionskosten, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Raumbedarf, "Raumbedarf", out m_dRaumbedarf, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Nutzungsdauer, "Nutzungsdauer", out m_dNutzungsdauer, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_CO2, "CO2", out m_dCO2, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_SO2, "SO2", out m_dSO2, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_NOx, "NOx", out m_dNOx, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_CO, "CO", out m_dCO, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Staub, "Staub", out m_dStaub, leerErlaubt: true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Vorlauf, "Vorlauf", out m_nVorlauf, leerErlaubt: true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Ruecklauf, "Rücklauf", out m_nRuecklauf, leerErlaubt: true)) return false;

            return true;
        }

        HeizkesselModel InitDatensatzUpdate(HeizkesselStammCtrl model = null)
        {
            if (model == null) model = new HeizkesselStammCtrl();

            // Strings sind unkritisch, wir nutzen aber .Trim() gegen versehentliche Leerzeichen
            model.Name = textBox_Name.Text.Trim();
            model.Firma = textBox_Hersteller.Text.Trim();
            model.Beschreibung = textBox_Beschreibung.Text.Trim();

            // Zahlen kommen fertig geparst aus EingabenPruefen
            model.Ptherm = m_dPtherm;

            // Brennstoff: Sicherstellen, dass ein gültiger Index gewählt wurde
            // Falls nichts gewählt ist (-1), wird hier die ID 1 gesetzt
            model.Brennstoff = comboBox_Brennstoff.SelectedIndex >= 0
                               ? comboBox_Brennstoff.SelectedIndex + 1
                               : 1;

            model.Wirkungsgrad_Gas = m_dWirkungsgradGas;
            model.Wirkungsgrad_Oel = m_dWirkungsgradOel;
            model.Betriebsbereitschaftverlust = m_dBBVerlust;
            model.Investitionskosten = m_dInvestitionskosten;
            model.Nutzungsdauer = m_dNutzungsdauer;
            model.Raumbedarf = m_dRaumbedarf;
            model.NOx = m_dNOx;
            model.CO2 = m_dCO2;
            model.CO = m_dCO;
            model.SO2 = m_dSO2;
            model.Staub = m_dStaub;
            model.Brennwert = checkBox_Brennwert.Checked;
            model.Vorlauf = m_nVorlauf;
            model.Ruecklauf = m_nRuecklauf;

            return model;
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            // Prüfung vor der Namensabfrage, damit kein Name für einen Datensatz
            // vergeben wird, der anschließend an der Zahlenprüfung scheitert
            if (!EingabenPruefen()) return;

            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                HeizkesselModel model = new HeizkesselModel();

                // Zuerst das Model mit den UI-Daten füllen
                model = InitDatensatzUpdate();

                // Den neuen Namen aus dem Dialog setzen
                model.Name = frmLabel.m_szName;

                // Alles in einem Rutsch speichern
                if (Insert(model))
                {
                    textBox_Name.Text = frmLabel.m_szName;
                    m_szKessel = frmLabel.m_szName;

                    MessageBox.Show("Datensatz erfolgreich neu angelegt.");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Fehler: Name existiert bereits oder Datenbankfehler!");
                }
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            // Erst prüfen, dann anlegen: bei ungültiger Eingabe bleibt der Dialog offen
            if (!EingabenPruefen()) return;

            try
            {
                HeizkesselModel model = new HeizkesselModel();
                model = InitDatensatzUpdate();

                // Alles in einem Rutsch speichern
                if (Insert(model))
                {
                    MessageBox.Show("Datensatz gespeichert");
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                    this.DialogResult = DialogResult.Cancel;
                }
                Close();
            }
            catch
            {
                MessageBox.Show("Fehler beim Speichern des Datensatzes!");
            }
        }

        // Vorlauf/Rücklauf werden als ganze Grad gespeichert (Modellfelder int),
        // deshalb hier die Ganzzahl-Färbung.
        private void textBox_Vorlauf_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_Ruecklauf_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }
    }
}