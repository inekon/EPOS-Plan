// Prüfmuster für Formularkarte — der Öffner des LETZTEN „unklar"-Zustands des Bestands.
//
// Stand vor iU9-W14a.1 (479336c^), Zeilen 19-45 und 162-211 von
// WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_Admin.cs: die einzige
// Stelle des Bestands, die Form_PufferSp_Bearbeiten geöffnet hat — über zwei Knöpfe,
// die derselbe Load-Handler in einem Zweig dauerhaft SPERRT und nie wieder einschaltet
// (m_bReadOnly, gesetzt vom Sprungziel PufferSpAdminNurLesen).
//
// Genau daran hängt die Regel, die der Erreichbarkeitsgraph prüft: Ein Weg über einen
// dauerhaft gesperrten Knopf heißt „unklar", nicht „ja". Im laufenden Bestand wird
// dieser Zustand nach W14a nie wieder auftreten — beide Masken sind Razor —, und ohne
// dieses Muster wäre die Regel nicht mehr prüfbar.
//
// Gekürzt auf das, was der Graph liest: die beiden Öffnermethoden, der Load-Handler mit
// dem Sperrzweig und die Felder, an denen beides hängt. Der Filter-, Detail- und
// Löschteil der Maske steht nicht hier; er trägt keine Kante.
//
// Ergänzt sind nur Namensraum und Klassenhülle, damit der Auszug für sich allein
// syntaktisch gültiges C# ist — der Aufrufersucher des Werkzeugs zerlegt ihn mit Roslyn.

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_Admin : Form
    {
        private PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
        public int m_ID_Projekt = 0;
        public bool m_bReadOnly = false;

        public Form_PufferSp_Admin()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);
            listBox_PufferSp_DB.Items.Clear();
        }

        private void Form_PufferSp_Admin_Load(object sender, EventArgs e)
        {
            LoadDBPufferSp();

            if (m_bReadOnly)
            {
                btn_Neu.Enabled = false;
                btn_Bearbeiten.Enabled = false;
                btn_Loeschen.Enabled = false;
            }
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_PufferSp_Bearbeiten frm = new Form_PufferSp_Bearbeiten(Form_PufferSp_Bearbeiten.MODE_EDIT);
            if (listBox_PufferSp_DB.Text == "") return;
            frm.SetControls(listBox_PufferSp_DB.Text);
            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK)
            {
                string szKessel = frm.m_szPufferSp;
                LoadDBPufferSp();
                listBox_PufferSp_DB.Text = szKessel;
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_PufferSp_Bearbeiten frm = new Form_PufferSp_Bearbeiten(Form_PufferSp_Bearbeiten.MODE_NEU);
            string szName = NamensDialogHuelle.Bezeichner(this);

            if (szName != null)
            {
                frm.SetControls(szName);

                DialogResult ret = frm.ShowDialog();
                if (ret == DialogResult.OK)
                {
                    string szKessel = frm.m_szPufferSp;
                    LoadDBPufferSp();
                    listBox_PufferSp_DB.Text = szKessel;
                }
            }
        }

        private void LoadDBPufferSp()
        {
            listBox_PufferSp_DB.Items.Clear();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_PufferSp_DB.Items.Add(ctrl.items[i].Name);
            }
        }
    }
}
