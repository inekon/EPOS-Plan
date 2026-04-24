using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_CaseEingabe : Form
    {
        private KostenPosition _daten;

        public Form_CaseEingabe()
        {
            InitializeComponent();
        }

        public Form_CaseEingabe(KostenPosition daten)
        {
            InitializeComponent();
            _daten = daten;

            // Werte beim Laden anzeigen
            numBestCase.Value = _daten.BestCase;
            numWorstCase.Value = _daten.WorstCase;
        }
        
        private void btn_OK_Click(object sender, EventArgs e)
        {
            _daten.BestCase = numBestCase.Value;
            _daten.WorstCase = numWorstCase.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
