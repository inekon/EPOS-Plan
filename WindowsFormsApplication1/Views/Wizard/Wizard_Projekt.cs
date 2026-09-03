using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using Json.Schema.Generation.Intents;

namespace WindowsFormsApplication1
{
    public partial class Wizard_Projekt : Form
    {
        public int m_ID_Klimaregion = 0;

        public Wizard_Projekt()
        {
            InitializeComponent();
            comboBox_Klima.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox_Klima.AutoCompleteSource = AutoCompleteSource.ListItems;
            HandhabungEinrichten();
        }

        // ------------------------------------------------------------------
        //  Handhabung (Nutzerauftrag 02.09.2026): Pflichtfelder sichtbar, Namens-
        //  doppel live gemeldet, sinnvolle Vorbelegungen, Fokus im Namensfeld.
        // ------------------------------------------------------------------

        private Label lblNameHinweis;
        private bool _neuModus;
        private readonly HashSet<string> _vorhandeneNamen = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        // Ressourcen-Helfer mit deutschem Fallback (Drei-Schichten-Regel; die
        // generierten Resource-Eigenschaften entstehen erst im VS-Designer).
        private static string TWz(string key, string fallback)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(key);
                return string.IsNullOrEmpty(s) ? fallback : s;
            }
            catch { return fallback; }
        }

        private void HandhabungEinrichten()
        {
            // Pflichtfelder markieren — Beschriftungen kommen aus den .resx, der
            // Stern wird sprachneutral angehängt.
            label1.Text = label1.Text.TrimEnd() + " *";
            label8.Text = label8.Text.TrimEnd() + " *";
            label6.AutoSize = true;
            label6.Text = label6.Text.TrimEnd() + "   " + TWz("WZP_PFLICHT", "(* = Pflichtfeld)");
            textBox_Beschreibung.PlaceholderText =
                TWz("WZP_BESCHREIBUNG_HINT", "Kurzbeschreibung: Vorhaben, Standort, Besonderheiten …");

            // Live-Hinweis unter dem Namensfeld (Doppel / leer).
            lblNameHinweis = new Label
            {
                AutoSize = true,
                ForeColor = Color.Firebrick,
                Font = new Font(Font.FontFamily, 8.25f),
                Location = new Point(textBox_Name.Left, textBox_Name.Bottom + 2),
                Visible = false
            };
            Controls.Add(lblNameHinweis);
            lblNameHinweis.BringToFront();
            textBox_Name.TextChanged += (s, e) => NameHinweisNachziehen();
            VisibleChanged += (s, e) => { if (Visible && textBox_Name.Enabled) textBox_Name.Focus(); };
        }

        private void NameHinweisNachziehen()
        {
            string grund;
            bool ok = NamePruefen(out grund);
            lblNameHinweis.Text = ok ? "" : grund;
            // Ein leeres Feld wird erst beim „Weiter" gemeldet — nicht schon beim Tippen.
            lblNameHinweis.Visible = !ok && (textBox_Name.Text ?? "").Trim().Length > 0;
        }

        private bool NamePruefen(out string grund)
        {
            grund = "";
            string name = (textBox_Name.Text ?? "").Trim();
            if (name.Length == 0)
            { grund = TWz("WZP_NAME_LEER", "Bitte einen Projektnamen eingeben."); return false; }
            if (_neuModus && _vorhandeneNamen.Contains(name))
            { grund = TWz("WZP_NAME_VORHANDEN", "Ein Projekt mit diesem Namen existiert bereits."); return false; }
            return true;
        }

        /// <summary>
        /// Prüfung beim Verlassen der Seite (Assistent „Weiter"): Projektname
        /// gefüllt und — im Neu-Modus — noch nicht vergeben; Klimaregion gewählt.
        /// </summary>
        public bool Pruefe(out string meldung)
        {
            if (!NamePruefen(out meldung)) return false;
            if ((comboBox_Klima.Text ?? "").Trim().Length == 0)
            { meldung = TWz("WZP_KLIMA_LEER", "Bitte eine Klimaregion wählen."); return false; }
            return true;
        }

        // Klimaregion des zuletzt aktiven Projekts als Vorbelegung (Neu-Modus) —
        // Tab_Applikation -> Tab_Projekt.ID_Klimaregion -> Projektkopie.Bezeichner.
        private void KlimaregionVorbelegen()
        {
            try
            {
                RecordSet rs = new RecordSet();
                int idProjekt = 0, idKlima = 0;
                rs.Open("select * from Tab_Applikation");
                if (rs.Next())
                {
                    object o = rs.Read("ID_Projekt");
                    if (o != null && o != DBNull.Value) idProjekt = Convert.ToInt32(o);
                }
                rs.Close();
                if (idProjekt <= 0) return;

                rs.Open("select * from Tab_Projekt where ID = " + idProjekt);
                if (rs.Next())
                {
                    object o = rs.Read("ID_Klimaregion");
                    if (o != null && o != DBNull.Value) idKlima = Convert.ToInt32(o);
                }
                rs.Close();
                if (idKlima <= 0) return;

                string name = "";
                rs.Open("select * from Tab_Klimaregion where ID = " + idKlima);
                if (rs.Next()) name = Convert.ToString(rs.Read("Bezeichner")) ?? "";
                rs.Close();
                if (name.Length > 0 && comboBox_Klima.Items.Contains(name)) comboBox_Klima.Text = name;
            }
            catch { /* Vorbelegung ist Komfort, kein Muss */ }
        }

        public void SetProjektbezeichner(String Projektname)
        {
            // Klimaregion-Auswahl (Namen aus den Stammdaten) ZUERST befuellen, damit ein
            // anschliessend gesetzter Text auch bei DropDownList sicher angezeigt wird.
            comboBox_Klima.Items.Clear();
            KlimaregionStammCtrl ctrl = new KlimaregionStammCtrl();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                comboBox_Klima.Items.Add(ctrl.items[i].m_szName);
            }

            ProjektCtrl projctrl = new ProjektCtrl();
            if (Projektname != "")
            {
                projctrl.ReadSingle(Projektname);
                textBox_Name.Text = Projektname;
                textBox_Bearbeiter.Text = projctrl.m_szBearbeiter;
                textBox_Beschreibung.Text = projctrl.m_szBeschreibung;
                textBox_Kunde.Text = projctrl.m_szKunde;
                textBox_Aenderungsdatum.Text = projctrl.m_Aenderungsdatum.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                textBox_Erstelldatum.Text = projctrl.m_Erstelldatum.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                m_ID_Klimaregion = projctrl.m_ID_Klimaregion;

                // Regionsnamen anzeigen. Neue Speicherweise: ID der Projekt-Kopie (Tab_Klimaregion.ID,
                // auf dieses Projekt eingeschraenkt). Fallback: aeltere Projekte mit STAMM-ID.
                if (m_ID_Klimaregion != 0)
                {
                    string szName = "";
                    RecordSet rs = new RecordSet();
                    rs.Open("select * from Tab_Klimaregion where ID=" + m_ID_Klimaregion + " and ID_Projekt=" + projctrl.m_ID);
                    if (rs.Next())
                    {
                        szName = (string)rs.Read("Bezeichner");
                    }
                    rs.Close();

                    if (szName == "")
                    {
                        rs.Open("select * from Tab_Klimaregion_STAMM where ID_Klimaregion=" + m_ID_Klimaregion);
                        if (rs.Next())
                        {
                            szName = (string)rs.Read("Name");
                        }
                        rs.Close();
                    }

                    comboBox_Klima.Text = szName;
                }
            }
            else
            {
                textBox_Aenderungsdatum.Text = DateTime.Now.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                textBox_Erstelldatum.Text = DateTime.Now.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));

                // Vorbelegungen (Nutzerauftrag 02.09.2026) — nur in leere Felder, damit
                // Eingaben beim erneuten Betreten der Seite erhalten bleiben.
                if ((textBox_Bearbeiter.Text ?? "").Trim().Length == 0)
                    textBox_Bearbeiter.Text = Environment.UserName;
                if ((comboBox_Klima.Text ?? "").Trim().Length == 0)
                    KlimaregionVorbelegen();

                // Vorhandene Namen einmal lesen — die Doppelprüfung läuft dann beim
                // Tippen ohne Datenbankzugriff.
                _vorhandeneNamen.Clear();
                try
                {
                    projctrl.ReadAll();
                    for (int i = 0; i < projctrl.rows; i++)
                        if (!string.IsNullOrEmpty(projctrl.items[i].m_szProjektname))
                            _vorhandeneNamen.Add(projctrl.items[i].m_szProjektname.Trim());
                }
                catch { }
                NameHinweisNachziehen();
            }
            projctrl = null;
        }

        public void SetEditProjektName(bool value)
        {
            textBox_Name.Enabled = value;
            _neuModus = value;   // nur ein frei benennbares Projekt ist ein neues
        }
        public string GetProjektName() { return textBox_Name.Text; }
        public string GetBeschreibung() { return textBox_Beschreibung.Text; }
        public string GetBearbeiter() { return textBox_Bearbeiter.Text; }
        public string GetKunde() { return textBox_Kunde.Text; }
        public DateTime GetDatum() { return DateTime.Now ; }
        public DateTime GetErstellDatum() { return DateTime.Parse(textBox_Erstelldatum.Text); }
        public int GetIDKlimaregion() { return m_ID_Klimaregion; }
        public string GetKlimaname() { return comboBox_Klima.Text; }

        private void comboBox_Klima_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Klimaregion_STAMM where Name='" + comboBox_Klima.Text + "'");
            if (rs.Next())
            {
                m_ID_Klimaregion = (int)rs.Read("ID_Klimaregion");
            }
            rs.Close();
        }
    }
}
