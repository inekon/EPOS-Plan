using System;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Editor der BHKW-STAMMDATEN. Liest/schreibt/loescht ausschliesslich Tab_BHKW_STAMM
    // (nicht mehr die Projekt-Tabelle Tab_BHKW). Schreibgeschuetzte Datensaetze (ReadOnly=true)
    // koennen nur nach ausdruecklicher Rueckfrage ueberschrieben werden. Neue
    // Datensaetze werden mit ReadOnly=false angelegt.
    public partial class Form_DBBHKW : Form
    {
        public BHKWStammModel model = new BHKWStammModel();
        public bool m_bNeu = false;
        public bool m_bAdmin = false;
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public int m_mode = MODE_EDIT;
        public string m_szName = "";

        // true, wenn der geladene Datensatz aus dem Auslieferungskatalog stammt (ReadOnly).
        // Ueberschreiben bleibt moeglich, verlangt dann aber eine ausdrueckliche Bestaetigung.
        private bool m_bKatalogsatz = false;

        public Form_DBBHKW()
        {
            InitializeComponent();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_DBBHKW_Load(object sender, EventArgs e)
        {
            comboBox_Name.Items.Add(model.m_szBezeichner);
            textBox_Hersteller.Text = model.m_szFirma;
            textBox_Beschreibung.Text = model.m_szBeschreibung;
        }


        public void SetControls(string szName)
        {
            BHKWStammCtrl ctrl = new BHKWStammCtrl();
            RecordSet rs = new RecordSet();

            ctrl.ReadAll();
            ctrl.FillComboBox(comboBox_Name);

            if (m_mode == MODE_EDIT)
            {
                comboBox_Name.Text = szName;
                ctrl.ReadAll("Bezeichner='" + szName + "'");
                model = ctrl.items[0];

                // Katalogsaetze (ReadOnly) bleiben ueberschreibbar; die Rueckfrage stellt
                // btn_Ueberschreiben_Click. Damit folgt der Dialog dem Hausmuster von
                // Heizkessel, Gebaeude und Brauchwasser: Knopf aktiv, Pruefung beim Speichern.
                m_bKatalogsatz = model.m_bReadOnly;
                btn_Speichern.Enabled = false;
                btn_Speichern_Unter.Enabled = true;   // "Speichern unter" legt eine neue Kopie an -> immer erlaubt
                btn_Überschreiben.Enabled = true;
            }
            else
            {
                m_bKatalogsatz = false;
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Überschreiben.Enabled = false;
                model = new BHKWStammModel();
                model.m_szBezeichner = szName;
                comboBox_Name.Text = szName;
            }

            textBox_Beschreibung.Text = model.m_szBeschreibung;
            textBox_Motortyp.Text = model.m_szMotortyp;
            textBox_NOx.Text = model.m_NOx.ToString();
            textBox_CO.Text = model.m_CO.ToString();
            textBox_CO2.Text = model.m_CO2.ToString();
            textBox_SO2.Text = model.m_SO2.ToString();
            textBox_Staub.Text = model.m_Staub.ToString();
            textBox_el_Leistung.Text = model.m_Pel.ToString("F2");
            textBox_th_Leistung.Text = model.m_Ptherm.ToString("F2");
            textBox_Wartungskosten.Text = model.m_Wartungskosten_kWhel.ToString("F2");
            textBox_Hersteller.Text = model.m_szFirma;
            textBox_Wirkungsgrad.Text = model.m_Wirkungsgrad.ToString("F2");
            textBox_Investitionskosten.Text = model.m_Investition_KWel.ToString("F2");
            textBox_Nutzungsdauer.Text = model.m_Nutzungsdauer.ToString();
            textBox_Raumbedarf.Text = model.m_Raumbedarf.ToString("F2");
            textBox_Grenzleistung.Text = model.m_Grenzleistung.ToString("F2");
            textBox_Modul.Text = model.m_Kosten_Modul.ToString("F2");
            textBox_Montage.Text = model.m_Kosten_Montage.ToString("F2");
            textBox_Lieferung.Text = model.m_Kosten_Lieferung.ToString("F2");
            textBox_Schallschutzhaube.Text = model.m_Kosten_Schallschutzhaube.ToString("F2");
            textBox_Abgasreinigung.Text = model.m_Kosten_Abgasreinigung.ToString("F2");
            textBox_Vorlauf.Text = model.m_Vorlauf.ToString();
            textBox_Ruecklauf.Text = model.m_Ruecklauf.ToString();

            comboBox_Brennstoff.Items.Add("Alle");
            for (int i = 0; i < ctrl.Brennstoffart.Count; i++)
            {
                comboBox_Brennstoff.Items.Add(ctrl.Brennstoffart[i]);
            }

            rs.Open("select * from [Tab_BHKW_STAMM] where Bezeichner='" + szName + "'");
            if (!rs.Next()) { rs.Close(); return; }
            if (rs.Read("Brennstoff") != DBNull.Value)
            {
                int brennstoff = (int)rs.Read("Brennstoff");
                comboBox_Brennstoff.SelectedIndex = brennstoff >= 1 ? brennstoff : 1;
            }
            rs.Close();
        }

        private void btn_Überschreiben_Click(object sender, EventArgs e)
        {
            // Zahlen erst hier pruefen: bei ungueltiger Eingabe bleibt der Dialog offen
            // und es wird nichts geschrieben.
            Eingabewerte werte;
            if (!EingabenPruefen(out werte)) return;

            BHKWStammCtrl ctrl = new BHKWStammCtrl();

            try
            {
                ctrl.model = InitDatensatzUpdate(werte);

                // Katalogsatz: einmal ausdruecklich nachfragen und den Schutz nur fuer
                // genau diesen Schreibvorgang aufheben.
                if (m_bKatalogsatz)
                {
                    if (MessageBox.Show(
                            "Dieser Datensatz stammt aus dem Auslieferungskatalog und ist schreibgeschützt."
                            + Environment.NewLine + Environment.NewLine
                            + "Soll er trotzdem überschrieben werden?",
                            "Schreibgeschützter Datensatz",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                        return;

                    ctrl.SchreibschutzUebergehen = true;
                }

                // Ohne diese Freigabe prueft ctrl.Update() selbst erneut auf ReadOnly.
                if (ctrl.Update())
                {
                    MessageBox.Show("Datensatz gespeichert");
                    this.DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    // Kann schreibgeschuetzt sein oder ein echter Fehler -> kein Close, Nutzer kann reagieren.
                    this.DialogResult = DialogResult.Cancel;
                }
            }
            catch
            {
                MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
            }
        }

        /// <summary>
        /// Die geprueften Zahlenwerte der Eingabefelder. Gefuellt von EingabenPruefen,
        /// verbraucht von InitDatensatzUpdate.
        /// </summary>
        private struct Eingabewerte
        {
            public double Pel, Ptherm, Wirkungsgrad, Grenzleistung;
            public double Investition, Raumbedarf, Wartungskosten;
            public double Modul, Montage, Lieferung, Schallschutzhaube, Abgasreinigung;
            public int Nutzungsdauer, NOx, CO2, CO, SO2, Staub, Vorlauf, Ruecklauf;
        }

        /// <summary>
        /// Prueft saemtliche Zahlenfelder beim Aktionsknopf (Folgepaket zu ab5bf32).
        /// Das erste ungueltige Feld meldet sprechend, bekommt den Fokus und liefert
        /// false - der Aufrufer kehrt dann zurueck und laesst den Dialog offen.
        /// Leere Felder gelten als 0, wie zuvor das Auffuellen im TextChanged.
        /// Bewusst keine Bereichspruefungen: der Katalogbestand muss speicherbar bleiben.
        /// </summary>
        private bool EingabenPruefen(out Eingabewerte werte)
        {
            werte = new Eingabewerte();

            if (!Program.ZahlPruefen(textBox_th_Leistung, "thermische Leistung", out werte.Ptherm, true)) return false;
            if (!Program.ZahlPruefen(textBox_el_Leistung, "elektrische Leistung", out werte.Pel, true)) return false;
            if (!Program.ZahlPruefen(textBox_Wirkungsgrad, "Gesamtwirkungsgrad", out werte.Wirkungsgrad, true)) return false;
            if (!Program.ZahlPruefen(textBox_Grenzleistung, "untere Grenzleistung", out werte.Grenzleistung, true)) return false;

            if (!Program.ZahlPruefen(textBox_Investitionskosten, "Investitionskosten", out werte.Investition, true)) return false;
            if (!Program.ZahlPruefen(textBox_Raumbedarf, "Raumbedarf", out werte.Raumbedarf, true)) return false;
            if (!Program.ZahlPruefen(textBox_Wartungskosten, "Wartungskosten", out werte.Wartungskosten, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Nutzungsdauer, "Nutzungsdauer", out werte.Nutzungsdauer, true)) return false;

            if (!Program.ZahlPruefen(textBox_Modul, "Kosten Modul", out werte.Modul, true)) return false;
            if (!Program.ZahlPruefen(textBox_Montage, "Kosten Montage und Inbetriebnahme", out werte.Montage, true)) return false;
            if (!Program.ZahlPruefen(textBox_Lieferung, "Kosten Lieferung", out werte.Lieferung, true)) return false;
            if (!Program.ZahlPruefen(textBox_Schallschutzhaube, "Kosten Schallschutzhaube", out werte.Schallschutzhaube, true)) return false;
            if (!Program.ZahlPruefen(textBox_Abgasreinigung, "Kosten Abgasreinigung", out werte.Abgasreinigung, true)) return false;

            if (!Program.GanzzahlPruefen(textBox_NOx, "NOx-Emission", out werte.NOx, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_CO2, "CO2-Emission", out werte.CO2, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_CO, "CO-Emission", out werte.CO, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_SO2, "SO2-Emission", out werte.SO2, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Staub, "Staub-Emission", out werte.Staub, true)) return false;

            if (!Program.GanzzahlPruefen(textBox_Vorlauf, "Vorlauftemperatur", out werte.Vorlauf, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Ruecklauf, "Rücklauftemperatur", out werte.Ruecklauf, true)) return false;

            return true;
        }

        // Nimmt die bereits geprueften Werte entgegen; kein Parse mehr auf Benutzertext.
        BHKWStammModel InitDatensatzUpdate(Eingabewerte werte)
        {
            BHKWStammModel model = new BHKWStammModel();
            model.m_szBezeichner = comboBox_Name.Text;
            model.m_szFirma = textBox_Hersteller.Text;
            model.m_szBeschreibung = textBox_Beschreibung.Text;
            model.m_Pel = werte.Pel;
            model.m_Ptherm = werte.Ptherm;
            model.m_Wirkungsgrad = werte.Wirkungsgrad;
            model.m_Investition_KWel = werte.Investition;
            model.m_Nutzungsdauer = werte.Nutzungsdauer;
            model.m_Raumbedarf = werte.Raumbedarf;
            model.m_NOx = werte.NOx;
            model.m_CO2 = werte.CO2;
            model.m_CO = werte.CO;
            model.m_SO2 = werte.SO2;
            model.m_Staub = werte.Staub;
            model.m_Grenzleistung = werte.Grenzleistung;
            model.m_Wartungskosten_kWhel = werte.Wartungskosten;
            model.m_szMotortyp = textBox_Motortyp.Text;
            model.m_Kosten_Modul = werte.Modul;
            model.m_Kosten_Montage = werte.Montage;
            model.m_Kosten_Lieferung = werte.Lieferung;
            model.m_Kosten_Schallschutzhaube = werte.Schallschutzhaube;
            model.m_Kosten_Abgasreinigung = werte.Abgasreinigung;
            model.m_Vorlauf = werte.Vorlauf;
            model.m_Ruecklauf = werte.Ruecklauf;

            // Brennstoff: Sicherstellen, dass ein gültiger Index gewählt wurde
            // Falls nichts gewählt ist (-1), wird hier die ID 1 gesetzt
            model.m_Brennstoff = comboBox_Brennstoff.SelectedIndex >= 0
                               ? comboBox_Brennstoff.SelectedIndex
                               : 1;

            return model;
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            // Zahlen zuerst pruefen - noch bevor der Namensdialog aufgeht.
            Eingabewerte werte;
            if (!EingabenPruefen(out werte)) return;

            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            RecordSet rs = new RecordSet();
            OleDbTransaction transaction = null;

            System.Drawing.Point p1 = btn_Speichern_Unter.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(frmLabel.m_szName))
                {
                    MessageBox.Show("Bitte einen gültigen Namen eingeben!");
                    return;
                }

                try
                {
                    // 1. Eine saubere OleDb-Verbindung über das DataRepository öffnen
                    using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                    {
                        conn.Open();

                        // 2. Transaktion auf der OleDbConnection starten
                        transaction = conn.BeginTransaction();

                        // 3. Dem RecordSet mitteilen, welche Connection und welche Transaktion es nutzen soll
                        rs.DBCommand.Connection = conn;
                        rs.DBCommand.Transaction = transaction;

                        // Existenzprüfung in der STAMM-Tabelle
                        rs.Open("select Bezeichner from Tab_BHKW_STAMM where Bezeichner='" + frmLabel.m_szName + "'");
                        if (!rs.EOF())
                        {
                            MessageBox.Show("Name existiert bereits!");
                            rs.Close();
                            transaction.Rollback();
                            return;
                        }
                        rs.Close();

                        comboBox_Name.Text = frmLabel.m_szName;
                        comboBox_Name.Items.Add(frmLabel.m_szName);

                        // INSERT in die STAMM-Tabelle inkl. ReadOnly=false (Feld ist NOT NULL)
                        rs.Insert("INSERT INTO Tab_BHKW_STAMM (Bezeichner, ReadOnly) VALUES ('" + frmLabel.m_szName + "', False)");
                        rs.Close();

                        // 4. Controller verarbeiten (Stammdaten)
                        BHKWStammCtrl ctrl = new BHKWStammCtrl();
                        ctrl.model = InitDatensatzUpdate(werte);

                        // Dem Controller die aktive Verbindung und Transaktion übergeben
                        ctrl.DBCommand.Connection = conn;
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler bei BHKW Speichern Unter: " + ex.Message);
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");

                    if (transaction != null && transaction.Connection != null)
                    {
                        try { transaction.Rollback(); } catch { }
                    }
                }
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            // Zahlen zuerst pruefen, danach erst Name und Datenbank.
            Eingabewerte werte;
            if (!EingabenPruefen(out werte)) return;

            OleDbTransaction transaction = null;

            if (string.IsNullOrEmpty(comboBox_Name.Text))
            {
                MessageBox.Show("Bitte einen gültigen Namen eingeben!");
                return;
            }

            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    // 1. Existenzprüfung via COUNT in der STAMM-Tabelle
                    string checkSql = "SELECT COUNT(*) FROM Tab_BHKW_STAMM WHERE Bezeichner = ?";
                    using (OleDbCommand checkCmd = conn.CreateCommand())
                    {
                        checkCmd.Transaction = transaction;
                        checkCmd.CommandText = checkSql;
                        checkCmd.Parameters.Add(new OleDbParameter("?", comboBox_Name.Text));

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Name existiert bereits!");
                            transaction.Rollback();
                            return;
                        }
                    }

                    // 2. Parametrisierter INSERT in die STAMM-Tabelle inkl. ReadOnly=false (NOT NULL)
                    string insertSql = "INSERT INTO Tab_BHKW_STAMM (Bezeichner, ReadOnly) VALUES (?, ?)";
                    using (OleDbCommand insertCmd = conn.CreateCommand())
                    {
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = insertSql;
                        insertCmd.Parameters.Add(new OleDbParameter("?", comboBox_Name.Text));
                        insertCmd.Parameters.Add(new OleDbParameter("?", false));
                        insertCmd.ExecuteNonQuery();
                    }

                    // 3. Controller verarbeiten (Stammdaten)
                    BHKWStammCtrl ctrl = new BHKWStammCtrl();
                    ctrl.model = InitDatensatzUpdate(werte);
                    ctrl.DBCommand.Connection = conn;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei BHKW Speichern: " + ex.Message);
                MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                if (transaction != null && transaction.Connection != null)
                {
                    try { transaction.Rollback(); } catch { }
                }
            }
        }
        // TextChanged faerbt nur noch (Folgepaket zu ab5bf32): kein modales Melden,
        // kein Undo() und kein Auffuellen mit "0" mehr - gemeldet wird erst beim
        // Aktionsknopf ueber EingabenPruefen. Faerbung nach dem Speichertyp in
        // InitDatensatzUpdate: ZahlFaerben fuer double, GanzzahlFaerben fuer Int32.
        private void textBox_Investitionskosten_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_th_Leistung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_el_Leistung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Wirkungsgrad_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Grenzleistung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Raumbedarf_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Wartungskosten_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Nutzungsdauer_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_Modul_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Montage_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Lieferung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Schallschutzhaube_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Abgasreinigung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_CO2_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_SO2_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_NOx_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_CO_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_Staub_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void btn_Eintragen_Click(object sender, EventArgs e)
        {
            //Wenn Heizöl aktiviert ist, trage die entsprechenden Werte ein
            if (comboBox_Brennstoff.Text.ToUpper().Contains("HEIZÖL"))
            {
                textBox_SO2.Text = "270";
                textBox_CO2.Text = "265000";
                // Wenn Checkbox "mit SCR" aktiviert ist
                if (checkBox_SCR.Checked)
                {
                    textBox_NOx.Text = "450";
                    textBox_CO.Text = "280";
                    textBox_Staub.Text = "80";
                }
                // Wenn Checkbox "mit SCR" n i c h t aktiviert ist
                else
                {
                    textBox_NOx.Text = "4400";
                    textBox_CO.Text = "140";
                    textBox_Staub.Text = "80";
                }
            }

            //Wenn Gas oder Biogas aktiviert ist, trage die entsprechenden Werte ein
            if (comboBox_Brennstoff.Text.ToUpper().Contains("STADTGAS") || comboBox_Brennstoff.Text.ToUpper().Contains("ERDGAS")
                || comboBox_Brennstoff.Text.ToUpper().Contains("BIOGAS"))
            {
                textBox_SO2.Text = "0";
                textBox_CO2.Text = "200000";
                // Stiller Parser statt double.Parse: das Feld kann ungueltigen oder
                // leeren Text enthalten, beides zaehlt hier wie 0 - keine Meldung.
                double dPtherm;
                Program.ZahlParsen(textBox_th_Leistung.Text, out dPtherm);
                //Wenn die thermische Leistung größer als 1.000 kW ist
                if (dPtherm > 1000)
                {
                    textBox_NOx.Text = "250";
                    textBox_CO.Text = "250";
                    textBox_Staub.Text = "0";
                }
                else
                {
                    //Wenn die thermische Leistung n i c h t größer als 1.000 kW ist
                    textBox_NOx.Text = "285";
                    textBox_CO.Text = "370";
                    textBox_Staub.Text = "0";
                }
            }
        }

        private void btn_CO2_Click(object sender, EventArgs e)
        {
            //Wenn Heizöl aktiviert wurde, trage den CO2-Wert für Heizöl ein
            if (comboBox_Brennstoff.Text.ToUpper().Contains("HEIZÖL")) textBox_CO2.Text = "290880";
            //Wenn Gas aktiviert wurde, trage den CO2-Wert für Gas ein
            if (comboBox_Brennstoff.Text.ToUpper().Contains("GAS")) textBox_CO2.Text = "201600";
            //Wenn Flüssiggas aktiviert wurde, trage den CO2-Wert für Flüssiggas ein
            if (comboBox_Brennstoff.Text.ToUpper().Contains("FLÜSSIGGAS")) textBox_CO2.Text = "238680";
        }

        // Vorlauf/Ruecklauf: hier bewusst Ganzzahl-Faerbung, obwohl die alte Pruefung
        // checkDouble war - gespeichert werden die Temperaturen als Int32.
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
