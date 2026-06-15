using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Config : Form
    {
        public KonfigurationModel Konfiguration = new KonfigurationModel();
        public int m_ID_Projekt;
        private ComboBox comboBox;
        private int index = -1;
        private List<string> listErzeuger = new List<string>();
        private List<string> listPufferSp = new List<string>();
        private Timer statusTimer = new Timer();

        public class LanguageItem
        {
            public string DisplayName { get; set; } // Das, was der User sieht (übersetzt)
            public string DbValue { get; set; }    // Das, was in die DB kommt (z.B. "STATUS_OPEN")
        }

        private readonly List<LanguageItem> _waermeerzeugerItems;

        public Form_Simulation_Config()
        {
            InitializeComponent();

            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.View = View.Details;
            listView1.Columns.Add("Wärmeerzeuger", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Pufferspeicher", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("", -2, HorizontalAlignment.Left);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Handle double-click for editing
            listView1.MouseDoubleClick += ListView_MouseDoubleClick;

            // Initialize ComboBox (hidden by default) für die Bearbeitung der "Pufferspeicher"-Spalte  
            comboBox = new ComboBox
            {
                Visible = false,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _waermeerzeugerItems = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_BHKW, DbValue = "BHKW" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_HEIZKESSEL, DbValue = "Heizkessel" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_SOLARTHERMIE, DbValue = "Solarthermie" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_WAERMEPUMPE, DbValue = "Wärmepumpe" },
            };

            var items_PV = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_PHOTOVOLTAIK, DbValue = "Photovoltaik" },
            };

            var items_SP = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_STROMSPEICHER, DbValue = "Stromspeicher" },
            };

            // Das Array mit deinen 4 ComboBoxen (Namen anpassen)
            ComboBox[] myComboBoxes = { comboBox1, comboBox2, comboBox3, comboBox4 };

            // Über das Array iterieren
            foreach (var cb in myComboBoxes)
            {
                // WICHTIG: Erst Member setzen, dann DataSource
                cb.DisplayMember = "DisplayName";
                cb.ValueMember = "DbValue";

                // Eine Kopie der Liste oder ToList() nutzen, falls die Boxen 
                // unabhängig voneinander selektieren sollen
                cb.DataSource = _waermeerzeugerItems.ToList();

                // Auswahl auf leer setzen
                cb.SelectedIndex = -1;
            }

            // Erst JETZT das Event abonnieren
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged += comboBox3_SelectedIndexChanged;
            comboBox4.SelectedIndexChanged += comboBox4_SelectedIndexChanged;

            comboBox5.DisplayMember = "DisplayName";
            comboBox5.ValueMember = "DbValue";
            comboBox5.DataSource = items_PV.ToList();
            comboBox5.SelectedIndex = -1;
            comboBox6.DisplayMember = "DisplayName";
            comboBox6.ValueMember = "DbValue";
            comboBox6.DataSource = items_SP.ToList();
            comboBox6.SelectedIndex = -1;

            comboBox5.SelectedIndexChanged += comboBox5_SelectedIndexChanged;
            comboBox6.SelectedIndexChanged += comboBox6_SelectedIndexChanged;

            // Aufruf im Konstruktor:
            SetGroupBoxFontBold(groupBox_Tools);

            // Timer konfigurieren
            statusTimer.Interval = 3000; // 3 Sekunden Sichtbarkeit
            statusTimer.Tick += (s, e) => {
                lblStatus.Visible = false;
                statusTimer.Stop();
            };
        }

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            ListViewItem item = listView1.SelectedItems[0];
            item.SubItems[1].Text = comboBox.SelectedItem.ToString();
            comboBox.Visible = false;
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            ListViewItem item = listView1.SelectedItems[0];
            ListViewHitTestInfo hit = listView1.HitTest(e.Location);
            int subItemIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            index = subItemIndex;

            // Die Bounds des SubItems relativ zum ListView holen
            Rectangle subItemBounds = hit.SubItem.Bounds;

            // Umrechnung der Position: Absolut zur Form, egal in welcher GroupBox das ListView liegt
            Point screenPoint = listView1.PointToScreen(subItemBounds.Location);
            Point formPoint = this.PointToClient(screenPoint);
            Rectangle displayBounds = new Rectangle(formPoint, subItemBounds.Size);

            if (subItemIndex == 1) // Spalte "Pufferspeicher"
            {
                // WICHTIG: Alte Events entfernen, um Mehrfach-Aufrufe zu verhindern
                comboBox.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
                comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;

                // Sicherstellen, dass die ComboBox auf der Form liegt
                if (!this.Controls.Contains(comboBox)) this.Controls.Add(comboBox);

                comboBox.Bounds = displayBounds;
                comboBox.Text = item.SubItems[subItemIndex].Text;
                comboBox.Visible = true;
                comboBox.BringToFront();
                comboBox.Focus();
                comboBox.DroppedDown = true; // Öffnet die Liste sofort beim Doppelklick
            }
            else if (subItemIndex == 2 || subItemIndex == 3) // "Vorlauf" oder "Rücklauf"
            {
                TextBox textBox = new TextBox { Bounds = displayBounds, Text = item.SubItems[subItemIndex].Text };

                // Verhindert die Eingabe von Buchstaben
                textBox.KeyPress += (s, ev) => {
                    if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar))
                    {
                        ev.Handled = true;
                    }
                };

                // Event beim Verlassen der TextBox
                textBox.LostFocus += (s, ev) =>
                {
                    // Validierung: Nur speichern, wenn es eine Zahl ist (optional)
                    item.SubItems[subItemIndex].Text = textBox.Text;
                    textBox.Dispose();
                };

                // Enter-Taste zum Bestätigen unterstützen
                textBox.KeyDown += (s, ev) =>
                {
                    if (ev.KeyCode == Keys.Enter)
                    {
                        listView1.Focus(); // Löst LostFocus aus
                        ev.SuppressKeyPress = true;
                    }
                };

                this.Controls.Add(textBox);
                textBox.BringToFront();
                textBox.Focus();
            }
            else if (subItemIndex == 4) // Spalte mit dem "📂" Symbol
            {
                Form_PufferSp_Admin frm = new Form_PufferSp_Admin();
                frm.m_bReadOnly = true;
                frm.ShowDialog();
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1)
            {
                checkBox1.Checked = true;
                // listBox1.Items.Add(comboBox1.Text);
                AddErzeuger();
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex != -1)
            {
                checkBox2.Checked = true;
                //  listBox1.Items.Add(comboBox2.Text);
                AddErzeuger();
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex != -1)
            {
                checkBox3.Checked = true;
                // listBox1.Items.Add(comboBox3.Text);
                AddErzeuger();
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex != -1)
            {
                checkBox4.Checked = true;
                // listBox1.Items.Add(comboBox4.Text);
                AddErzeuger();
            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox5.SelectedIndex != -1) { checkBox5.Checked = true; }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox6.SelectedIndex != -1) { checkBox6.Checked = true; }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked) { comboBox1.Text = ""; }
            AddErzeuger();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox2.Checked) { comboBox2.Text = ""; }
            AddErzeuger();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox3.Checked) { comboBox3.Text = ""; }
            AddErzeuger();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox4.Checked) { comboBox4.Text = ""; }
            AddErzeuger();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox5.Checked) { comboBox5.Text = ""; comboBox5.SelectedIndex = -1; }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox6.Checked) { comboBox6.Text = ""; comboBox6.SelectedIndex = -1; }
        }

        public void SetControls(int ID_Projekt)
        {
            var items = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_BHKW, DbValue = "BHKW" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_HEIZKESSEL, DbValue = "Heizkessel" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_SOLARTHERMIE, DbValue = "Solarthermie" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_WAERMEPUMPE, DbValue = "Wärmepumpe" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_GESAMTSYSTEM, DbValue = "Gesamtsystem" },
            };

            m_ID_Projekt = ID_Projekt;
            comboBox1.SelectedValue = Konfiguration.m_Tool_1;
            comboBox2.SelectedValue = Konfiguration.m_Tool_2;
            comboBox3.SelectedValue = Konfiguration.m_Tool_3;
            comboBox4.SelectedValue = Konfiguration.m_Tool_4;
            comboBox5.SelectedValue = Konfiguration.m_Tool_5;
            comboBox6.SelectedValue = Konfiguration.m_Tool_6;
            checkBox_Heizstab.Checked = Konfiguration.m_WP_Heizstab;
            textBox_Netzverluste.Text = Konfiguration.m_Netzverluste.ToString();
            comboBox_NetzvEinheit.Text = Konfiguration.m_szNetzverlusteEinheit;
            textBox_untere_PGrenze.Text = Konfiguration.m_BHKW_Grenzleistung.ToString();
            comboBox_Bereitschaft.Text = Konfiguration.m_Kessel_Betriebsbereitschaft.ToString();

            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();
            ctrlpsp.ReadAll("ID_Projekt= " + m_ID_Projekt);
            for (int i = 0; i < ctrlpsp.rows; i++)
            {
                var match = items.FirstOrDefault(x => x.DbValue == ctrlpsp.items[i].Erzeuger);
                listView1.Items.Add(new ListViewItem(new[] { match.DisplayName, ctrlpsp.items[i].PufferSp, ctrlpsp.items[i].Vorlauf.ToString(), ctrlpsp.items[i].Ruecklauf.ToString(), "📂" }));
            }
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            PufferSpCtrl ctrl = new PufferSpCtrl();
            ctrl.ReadAll("");
            for (int i = 0; i < ctrl.rows; i++) listPufferSp.Add(ctrl.items[i].Name);
            comboBox.Items.AddRange(listPufferSp.ToArray());
        }

        // Hilfsmethode, um den DB-Wert sicher zu extrahieren
        private string GetDbValue(ComboBox cb)
        {
            // Variante A: Über SelectedValue (setzt voraus, dass ValueMember="DbValue" korrekt ist)
            // return cb.SelectedValue?.ToString() ?? "";

            // Variante B: Über das Objekt selbst (sicherste Methode)
            if (cb.SelectedItem is LanguageItem item)
            {
                return item.DbValue;
            }
            return ""; // Falls nichts ausgewählt ist
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();

            Konfiguration.m_Tool_1 = checkBox1.Checked ? GetDbValue(comboBox1) : "";
            Konfiguration.m_Tool_2 = checkBox2.Checked ? GetDbValue(comboBox2) : "";
            Konfiguration.m_Tool_3 = checkBox3.Checked ? GetDbValue(comboBox3) : "";
            Konfiguration.m_Tool_4 = checkBox4.Checked ? GetDbValue(comboBox4) : "";
            Konfiguration.m_Tool_5 = checkBox5.Checked ? GetDbValue(comboBox5) : "";
            Konfiguration.m_Tool_6 = checkBox6.Checked ? GetDbValue(comboBox6) : "";
            Konfiguration.m_WP_Heizstab = checkBox_Heizstab.Checked;

            // Statt: Konfiguration.m_Netzverluste = double.Parse(textBox_Netzverluste.Text);
            if (double.TryParse(textBox_Netzverluste.Text, out double netzVerluste))
            {
                Konfiguration.m_Netzverluste = netzVerluste;
            }
            else
            {
                Konfiguration.m_Netzverluste = 0; // Standardwert bei Fehlern
            }
            Konfiguration.m_szNetzverlusteEinheit = comboBox_NetzvEinheit.Text;

            if (double.TryParse(textBox_untere_PGrenze.Text, out double untere_PGrenze))
            {
                Konfiguration.m_BHKW_Grenzleistung = untere_PGrenze;
            }
            else
            {
                Konfiguration.m_BHKW_Grenzleistung = 0; // Standardwert bei Fehlern
            }

            if (int.TryParse(comboBox_Bereitschaft.Text, out int bereitschaft))
            {
                Konfiguration.m_Kessel_Betriebsbereitschaft = bereitschaft;
            }
            else
            {
                Konfiguration.m_Kessel_Betriebsbereitschaft = 0; // Standardwert bei Fehlern
            }

            ctrl.model = Konfiguration;
            if (!ctrl.Delete(m_ID_Projekt)) return;
            if (ctrl.Insert(m_ID_Projekt)) ShowStatus("✔ Konfiguration erfolgreich gespeichert", Color.ForestGreen);

            int prioritaet = 1;

            ctrlpsp.ID_Projekt = m_ID_Projekt;

            if (!ctrlpsp.Delete()) return;
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                ListViewItem item = listView1.Items[i];
                ctrlpsp.PufferSp = item.SubItems[1].Text;

                var items = new List<LanguageItem>
                {
                    new LanguageItem { DisplayName = MyResource.Resource.KONFIG_BHKW, DbValue = "BHKW" },
                    new LanguageItem { DisplayName = MyResource.Resource.KONFIG_HEIZKESSEL, DbValue = "Heizkessel" },
                    new LanguageItem { DisplayName = MyResource.Resource.KONFIG_SOLARTHERMIE, DbValue = "Solarthermie" },
                    new LanguageItem { DisplayName = MyResource.Resource.KONFIG_WAERMEPUMPE, DbValue = "Wärmepumpe" },
                    new LanguageItem { DisplayName = MyResource.Resource.KONFIG_GESAMTSYSTEM, DbValue = "Gesamtsystem" },
                };
                var match = items.FirstOrDefault(x => x.DisplayName == item.SubItems[0].Text);
                ctrlpsp.Erzeuger = match?.DbValue ?? item.SubItems[0].Text;

                ctrlpsp.Vorlauf = Int32.Parse(item.SubItems[2].Text);
                ctrlpsp.Ruecklauf = Int32.Parse(item.SubItems[3].Text);
                ctrlpsp.Prioritaet = prioritaet++;
                ctrlpsp.Insert();
            }


        }

        private void AddErzeuger()
        {
            listErzeuger.Clear(); // Liste leeren, wir bauen sie neu auf

            // Wir erstellen ein Array von Paaren: Checkbox + zugehörige ComboBox
            // Das ist viel sauberer als 4 separate Abfragen.
            var controlPairs = new[]
            {
                new { CheckBox = checkBox1, ComboBox = comboBox1 },
                new { CheckBox = checkBox2, ComboBox = comboBox2 },
                new { CheckBox = checkBox3, ComboBox = comboBox3 },
                new { CheckBox = checkBox4, ComboBox = comboBox4 }
            };

            foreach (var pair in controlPairs)
            {
                // SCHRITT 1: Prüfen, ob die Checkbox überhaupt aktiv ist!
                if (pair.CheckBox.Checked)
                {
                    // SCHRITT 2: Wenn ja, prüfen wir die ComboBox
                    if (pair.ComboBox.SelectedItem is LanguageItem selectedItem)
                    {
                        string valueToSave = selectedItem.DbValue;

                        if (!string.IsNullOrEmpty(valueToSave) && !listErzeuger.Contains(valueToSave))
                        {
                            listErzeuger.Add(valueToSave);
                        }
                    }
                }
                // WENN die Checkbox nicht aktiv ist (Checked == false), 
                // wird ihr ComboBox-Inhalt einfach ignoriert und nicht zur Liste hinzugefügt.
                // Ein explizites "Löschen" ist nicht nötig, da wir listErzeuger.Clear() oben machen.
            }

            // "Gesamtsystem" immer hinzufügen (außer es ist schon drin)
            if (!listErzeuger.Contains("Gesamtsystem"))
            {
                listErzeuger.Add("Gesamtsystem");
            }
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            Form_KonfigPufferspeicher frm = new Form_KonfigPufferspeicher();

            // Transformation in eine Liste von Strings (nur die Anzeigenamen)
            List<string> displayListe = listErzeuger
                .Select(dbVal => _waermeerzeugerItems.FirstOrDefault(refItem => refItem.DbValue == dbVal)?.DisplayName)
                .Where(name => name != null) // Falls ein DB-Wert nicht in der Referenzliste war
                .ToList();
            displayListe.Add(MyResource.Resource.KONFIG_GESAMTSYSTEM);

            frm.listErzeuger = displayListe;
            frm.listPufferSp = listPufferSp;
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls();

            DialogResult result = frm.ShowDialog();
            if (result == DialogResult.OK)
            {

                listView1.Items.Add(new ListViewItem(new[] { frm.model.Erzeuger, frm.model.PufferSp, frm.model.Vorlauf.ToString(), frm.model.Ruecklauf.ToString(), "📂" }));
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            ListViewItem item = listView1.SelectedItems[0];
            listView1.Items.Remove(item);
        }

        private void checkBox_PufferSp_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_PufferSp.Checked)
            {
                groupBox_PufferSp.Visible = true;
            }
            else
            {
                groupBox_PufferSp.Visible = false;
            }
        }

        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            // Hintergrund Hellblau
            using (SolidBrush pb = new SolidBrush(Color.LightBlue))
            {
                e.Graphics.FillRectangle(pb, e.Bounds);
            }

            // Text zeichnen
            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, Color.Black,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

            // Rahmen (Optional)
            e.Graphics.DrawRectangle(Pens.LightGray, e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
        }

        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true; // Nutzt das Standard-Zeichnen für die Zeile
        }

        private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true; // Nutzt das Standard-Zeichnen für die Zellen (inkl. Symbole)
        }

        private void SetGroupBoxFontBold(GroupBox gb)
        {
            // Titel der GroupBox fett machen
            gb.Font = new Font(gb.Font, FontStyle.Bold);

            // Alle Kinder in der GroupBox wieder auf normal setzen
            foreach (Control c in gb.Controls)
            {
                c.Font = new Font(c.Font, FontStyle.Regular);
            }
            gb.Invalidate(); 
        }

        private void ShowStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            lblStatus.Visible = true;

            statusTimer.Stop(); // Falls er noch lief, zurücksetzen
            statusTimer.Start();
        }
    }
}
