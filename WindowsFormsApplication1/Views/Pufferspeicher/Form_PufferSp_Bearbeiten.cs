using System;
using System.Data;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_Bearbeiten : Form
    {
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public string m_szPufferSp = "";
        private int m_mode = MODE_EDIT;

        /// <summary>
        /// DB-Werte der drei Einträge von <c>comboBox_Speichertyp</c>, in der
        /// Reihenfolge der <c>.resx</c>-Schlüssel <c>Items</c>, <c>Items1</c>,
        /// <c>Items2</c> (Solarspeicher, Pufferspeicher, Kombispeicher).
        ///
        /// <para>
        /// <b>Befund L0-1.</b> Bis Paket 9 stand hier
        /// <c>model.Speichertyp = comboBox_Speichertyp.Text</c> — also der
        /// LOKALISIERTE Text. Auf englischer Oberfläche landeten damit
        /// „Solar storage", „Buffer storage", „Combination storage" in
        /// <c>Tab_Pufferspeicher_STAMM.Speichertyp</c>, und beim nächsten Öffnen
        /// (oder in einer deutschen Sitzung) traf der Wert keinen Katalogeintrag mehr.
        /// Derselbe Fehlertyp wie B0-9/B0-10/B0-11 und ein Verstoß gegen die
        /// Drei-Schichten-Regel. Gespeichert wird jetzt über den AUSWAHLINDEX, der
        /// sprachfrei ist.
        /// </para>
        /// </summary>
        private static readonly string[] SPEICHERTYP_DB_WERTE =
        {
            DbWerte.PSP_SPEICHERTYP_SOLAR,
            DbWerte.PSP_SPEICHERTYP_PUFFER,
            DbWerte.PSP_SPEICHERTYP_KOMBI
        };

        /// <summary>
        /// Bestandstoleranz zu Befund L0-1: Datensätze, die vor der Behebung auf
        /// englischer Oberfläche gespeichert wurden, tragen diese Texte in der
        /// Speichertyp-Spalte. Sie werden beim LESEN auf den richtigen Eintrag
        /// zurückgeführt, damit der Dialog nicht leer aufgeht; beim nächsten Speichern
        /// steht dann wieder der deutsche Persistenzwert in der Datenbank.
        /// Die Zeichenketten stammen aus <c>Form_PufferSp_Bearbeiten.en-US.resx</c>
        /// und sind bewusst hier eingefroren — sie beschreiben Altdaten, nicht die
        /// heutige Oberfläche, und dürfen sich mit einer Übersetzungskorrektur NICHT
        /// mitändern.
        /// </summary>
        private static readonly string[] SPEICHERTYP_ALTWERTE_EN =
        {
            "Solar storage",
            "Buffer storage",
            "Combination storage"
        };

        public Form_PufferSp_Bearbeiten(int mode)
        {
            InitializeComponent();

            // Dezenter Einstieg in den Assistenten, oben rechts im Client-Bereich
            // (Fachkonzept 11.8). Programmatisch, damit Designer und .resx
            // unberuehrt bleiben.
            KiAufrufKnopf.Anbringen(this);

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

                comboBox_Speichertyp.Text = "";
                textBox_Hersteller.Text = "";
                textBox_Verluste.Text = "0";
                textBox_Investitionskosten.Text = "0";
                textBox_Volumen.Text = "0";
            }
        }

        public void SetControls(string szName)
        {
            textBox_Name.Text = szName;
            m_szPufferSp = szName;

            // 1. Daten über das DataRepository mittels DataTable abfragen (Ersetzt RecordSet)
            string sql = "SELECT * FROM Tab_Pufferspeicher_STAMM WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("?", szName ?? (object)DBNull.Value));

            if (dt == null || dt.Rows.Count == 0) return;

            DataRow row = dt.Rows[0];

            // Zuordnung ueber Spaltennamen statt ueber Ordinalzahlen. Die frueheren
            // row[2]..row[6] waren an die aktuelle Spaltenreihenfolge von
            // Tab_Pufferspeicher_STAMM gebunden - die ist kein Vertrag. Die
            // SchemaMigration haengt neue Spalten zwar immer hinten an, aber ein
            // Tabellenumbau (Import aus einer Vorlage, "Komprimieren und reparieren"
            // nach manuellen Aenderungen) verschoebe die Zuordnung stillschweigend.
            SetzeText(textBox_Hersteller, row, "Hersteller");
            SpeichertypAnzeigen(row);
            SetzeText(textBox_Volumen, row, "Gesamtvolumen");
            SetzeZahl(textBox_Verluste, row, "Bereitschaftsverluste");
            SetzeZahl(textBox_Investitionskosten, row, "Investitionskosten");
        }

        /// <summary>
        /// LESEWEG des Speichertyps (Befund L0-1): DB-Wert → Auswahleintrag.
        ///
        /// Erkannt werden der deutsche Persistenzwert, der aktuell angezeigte Text und
        /// — als Bestandstoleranz — die englischen Werte, die vor der Behebung
        /// gespeichert wurden. Trifft nichts davon zu (Fremdimport, Freitext), bleibt
        /// der Rohwert im Feld stehen; er wäre sonst kommentarlos verschwunden.
        /// </summary>
        private void SpeichertypAnzeigen(DataRow row)
        {
            if (!row.Table.Columns.Contains("Speichertyp")) return;
            object v = row["Speichertyp"];
            if (v == DBNull.Value) return;

            string wert = (v.ToString() ?? "").Trim();
            int index = SpeichertypIndex(wert);

            if (index >= 0 && index < comboBox_Speichertyp.Items.Count)
                comboBox_Speichertyp.SelectedIndex = index;
            else
                comboBox_Speichertyp.Text = wert;
        }

        /// <summary>
        /// Auswahlindex zu einem Speichertyp-Text; -1, wenn keiner passt.
        /// Geprüft wird in dieser Reihenfolge: DB-Wert, angezeigter Text der aktuellen
        /// Sprache, englischer Altwert (Bestandstoleranz L0-1).
        /// </summary>
        private int SpeichertypIndex(string text)
        {
            if (string.IsNullOrEmpty(text)) return -1;

            for (int i = 0; i < SPEICHERTYP_DB_WERTE.Length; i++)
                if (string.Equals(text, SPEICHERTYP_DB_WERTE[i], StringComparison.OrdinalIgnoreCase))
                    return i;

            for (int i = 0; i < comboBox_Speichertyp.Items.Count && i < SPEICHERTYP_DB_WERTE.Length; i++)
                if (string.Equals(text, comboBox_Speichertyp.Items[i].ToString(),
                                  StringComparison.OrdinalIgnoreCase))
                    return i;

            for (int i = 0; i < SPEICHERTYP_ALTWERTE_EN.Length; i++)
                if (string.Equals(text, SPEICHERTYP_ALTWERTE_EN[i], StringComparison.OrdinalIgnoreCase))
                    return i;

            return -1;
        }

        /// <summary>
        /// SCHREIBWEG des Speichertyps (Befund L0-1): Auswahl → deutscher DB-Wert.
        ///
        /// Maßgeblich ist der Auswahlindex — er ist sprachfrei. Nur wenn nichts
        /// ausgewählt ist (die ComboBox lässt Freitext zu), wird der Text ausgewertet;
        /// passt auch der nicht, geht er unverändert durch, damit eine bewusste
        /// Freitexteingabe nicht stillschweigend umgeschrieben wird.
        /// </summary>
        private string SpeichertypDbWert()
        {
            int index = comboBox_Speichertyp.SelectedIndex;
            if (index >= 0 && index < SPEICHERTYP_DB_WERTE.Length)
                return SPEICHERTYP_DB_WERTE[index];

            string text = (comboBox_Speichertyp.Text ?? "").Trim();
            int ausText = SpeichertypIndex(text);
            return ausText >= 0 ? SPEICHERTYP_DB_WERTE[ausText] : text;
        }

        /// <summary>Uebernimmt einen Textwert, wenn Spalte und Wert vorhanden sind.</summary>
        private static void SetzeText(Control ziel, DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return;
            object v = row[spalte];
            if (v == DBNull.Value) return;
            ziel.Text = v.ToString();
        }

        /// <summary>Uebernimmt einen Zahlenwert mit zwei Nachkommastellen (wie bisher).</summary>
        private static void SetzeZahl(Control ziel, DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return;
            object v = row[spalte];
            if (v == DBNull.Value) return;
            try { ziel.Text = Convert.ToDouble(v).ToString("F2"); }
            catch { /* unerwarteter Typ - Vorbelegung stehen lassen */ }
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            // Vor dem Namensdialog pruefen, damit der Anwender nicht erst einen Namen
            // vergibt und danach die Meldung zum Volumen bekommt.
            int volumen;
            if (!VolumenPruefen(out volumen)) return;

            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(frmLabel.m_szName))
                {
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG);
                    return;
                }

                try
                {
                    PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
                    if (ctrl.Exists(frmLabel.m_szName)) { MessageBox.Show(MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT); return; }

                    textBox_Name.Text = frmLabel.m_szName;
                    m_szPufferSp = frmLabel.m_szName;

                    PufferSpModel m = InitDatensatzUpdate(volumen);
                    m.Name = frmLabel.m_szName;

                    if (ctrl.InsertFrom(m))
                    {
                        this.DialogResult = DialogResult.OK;
                        MessageBox.Show(MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT);
                    }
                    else
                    {
                        this.DialogResult = DialogResult.Cancel;
                        MessageBox.Show(MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER);
                    }
                    Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler bei Speichern Unter: " + ex.Message);
                    MessageBox.Show(string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message));
                }
            }
        }

        /// <summary>
        /// Knopf-Pruefung des Volumenfeldes (Folgepaket zu ab5bf32). Leer gilt wie
        /// bisher als 0; bei ungueltiger Eingabe meldet der Helfer, setzt den Fokus
        /// und der Aufrufer kehrt zurueck - der Dialog bleibt offen.
        /// </summary>
        private bool VolumenPruefen(out int volumen)
        {
            return Program.GanzzahlPruefen(textBox_Volumen, "Gesamtvolumen", out volumen, leerErlaubt: true);
        }

        /// <summary>
        /// Baut den Datensatz aus den Feldern. Das Gesamtvolumen kommt als bereits
        /// gepruefter Wert von aussen (Folgepaket zu ab5bf32) - geprueft wird am
        /// jeweiligen Aktionsknopf, siehe VolumenPruefen.
        /// </summary>
        PufferSpModel InitDatensatzUpdate(int volumen)
        {
            PufferSpModel model = new PufferSpModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Hersteller.Text;
            // Befund L0-1: NICHT der angezeigte Text, sondern der DB-Wert der Auswahl.
            model.Speichertyp = SpeichertypDbWert();

            model.Gesamtvolumen = volumen;

            double verluste;
            model.Betriebsbereitschaftverlust = double.TryParse(textBox_Verluste.Text, out verluste) ? verluste : 0.0;

            double kosten;
            model.Investitionskosten = double.TryParse(textBox_Investitionskosten.Text, out kosten) ? kosten : 0.0;

            return model;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            int volumen;
            if (!VolumenPruefen(out volumen)) return;

            try
            {
                PufferSpModel m = InitDatensatzUpdate(volumen);
                PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
                if (ctrl.Exists(m.Name)) { MessageBox.Show(MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT); return; }

                if (ctrl.InsertFrom(m))
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT);
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER);
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern: " + ex.Message);
                MessageBox.Show(string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message));
            }
        }

        /// <summary>
        /// Nur noch Faerbung (Folgepaket zu ab5bf32): das frueher hier eingesetzte
        /// Undo() nahm die Eingabe zurueck, loeste TextChanged erneut aus und liess
        /// die Meldung wiederkehren. Geprueft wird jetzt an den Speicherknoepfen.
        /// </summary>
        private void textBox_Volumen_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            int volumen;
            if (!VolumenPruefen(out volumen)) return;

            try
            {
                PufferSpModel m = InitDatensatzUpdate(volumen);
                PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
                if (ctrl.UpdateFrom(m))
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT);
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Überschreiben: " + ex.Message);
                MessageBox.Show(string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message));
            }
        }
    }
}