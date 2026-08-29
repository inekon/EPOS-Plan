using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Schlanke Hüllform um <see cref="ProjektAuswahl"/> — der Dialog „Projekt öffnen"
    /// (Konzept „Projektdialoge vereinheitlichen", Paket P3).
    ///
    /// <para>
    /// Sie enthält bewusst nur die Liste und die zwei Knöpfe: Die gesamte Logik
    /// (Lesen, Suchen, Sortieren, Auswahl) steckt im UserControl, damit derselbe
    /// Baustein später auch in der linken Spalte des Projektassistenten stehen kann
    /// (Paket P4), ohne dass irgendetwas doppelt gepflegt werden muss.
    /// </para>
    /// <para>
    /// <b>Ergebnisfelder</b> heißen wie in <see cref="Form_ProjektSpeichernUnter"/>
    /// (<c>m_szProjekt</c>, <c>m_ID_Projekt</c>), damit der Aufrufer in
    /// <see cref="MenueCtrl"/> denselben Ladeweg unverändert weiterverwenden kann.
    /// </para>
    /// </summary>
    public partial class Form_ProjektAuswahl : Form
    {
        /// <summary>Name des gewählten Projekts; leer, wenn abgebrochen wurde.</summary>
        public string m_szProjekt = "";

        /// <summary>Tab_Projekt.ID des gewählten Projekts; 0, wenn abgebrochen wurde.</summary>
        public int m_ID_Projekt = 0;

        private bool _nachAenderungSortieren;
        private string _vorauswahl = "";

        public Form_ProjektAuswahl()
        {
            InitializeComponent();
            AcceptButton = btn_OK;
            CancelButton = btn_Abbrechen;
        }

        /// <summary>
        /// Öffnet den Dialog mit der Sicht „zuletzt geändert zuerst" und stellt
        /// <paramref name="vorauswahl"/> scharf, sofern das Projekt noch existiert.
        /// Genutzt von der Startmasken-Kachel „Zuletzt geöffnet".
        /// </summary>
        public void ZuletztGeaendertZuerst(string vorauswahl)
        {
            _nachAenderungSortieren = true;
            _vorauswahl = vorauswahl ?? "";
        }

        private void Form_ProjektAuswahl_Load(object sender, EventArgs e)
        {
            if (_nachAenderungSortieren)
                ucAuswahl.SortiereNach(ProjektAuswahl.SPALTE_GEAENDERT, true);

            ucAuswahl.Laden();

            if (_vorauswahl.Length > 0) ucAuswahl.Vorauswaehlen(_vorauswahl);
            ucAuswahl.SuchfeldFokussieren();
        }

        private void ucAuswahl_ProjektGewaehlt(int id, string name)
        {
            // Doppelklick in der Liste = OK
            Uebernehmen(id, name);
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            if (ucAuswahl.GewaehlteID <= 0)
            {
                // Text_Select ist der vorhandene Schluessel "Bitte auswaehlen!" /
                // "Please select!" - kein neuer MyResource-Schluessel noetig.
                MessageBox.Show(MyResource.Resource.Text_Select,
                                MyResource.Resource.Text_Hinweis,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Uebernehmen(ucAuswahl.GewaehlteID, ucAuswahl.GewaehlterName);
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            ucAuswahl.Abbrechen();
            m_szProjekt = "";
            m_ID_Projekt = 0;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Uebernehmen(int id, string name)
        {
            m_ID_Projekt = id;
            m_szProjekt = name ?? "";
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
