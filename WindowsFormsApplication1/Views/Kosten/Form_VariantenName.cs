using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Namensabfrage für Vorlagen-Varianten („Neu…" / „Speichern unter…", Etappe KD2,
    /// Konzept Kostendialoge Rev. 1.2, § 5.1). Designer-fähig (Ä6); der Aufrufer
    /// liest <see cref="Ergebnis"/> nach <c>ShowDialog</c> und schreibt selbst.
    /// FK9: Der Aufrufer belegt das Feld mit dem Namensschema
    /// „‹Name› — Variante ‹n›" vor.
    /// </summary>
    public partial class Form_VariantenName : Form
    {
        public Form_VariantenName()
        {
            InitializeComponent();
        }

        /// <summary>Titel, Frage und Vorbelegung vor <c>ShowDialog</c> setzen.</summary>
        public void SetControls(string titel, string frage, string vorbelegung)
        {
            if (!string.IsNullOrEmpty(titel)) { Text = titel; lblKopfTitel.Text = titel; }
            if (!string.IsNullOrEmpty(frage)) lblFrage.Text = frage;
            txtName.Text = vorbelegung ?? "";
            txtName.SelectAll();
        }

        /// <summary>Der eingegebene Name (getrimmt); leer, wenn abgebrochen.</summary>
        public string Ergebnis { get; private set; }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (name.Length == 0) { txtName.Focus(); return; }
            Ergebnis = name;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
