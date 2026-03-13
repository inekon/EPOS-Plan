using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public class Form_ChartZoom : Form
    {
        public Chart ZoomChart { get; private set; }
        private ChartManager _manager;

        public Form_ChartZoom(string title)
        {
            this.Text = "Detailansicht: " + title;
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;

            // Chart initialisieren
            ZoomChart = new Chart { Dock = DockStyle.Fill };
            ChartArea ca = new ChartArea("MainArea");
            ZoomChart.ChartAreas.Add(ca);

            this.Controls.Add(ZoomChart);

            // ESC-Taste zum Schließen
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };
        }
    }
}