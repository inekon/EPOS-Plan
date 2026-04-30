using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class ucBrennstoffZeile : UserControl
    {
        public ProjektBrennstoff Daten { get; set; }
        public event EventHandler ValueChanged;

        public ucBrennstoffZeile(ProjektBrennstoff daten)
        {
            InitializeComponent();
            this.Daten = daten;
            FillUI();
            RegisterEvents();
        }

        private void FillUI()
        {
            lblName.Text = Daten.Name;
            lblEinheit.Text = Daten.Einheit;
            lblPreisEinheit.Text = Daten.PreisEinheit;
            lblHi.Text = Daten.Hi.ToString("N2");

            numArbeitspreis.DecimalPlaces = 2; // Erlaubt zwei Nachkommastellen
            numArbeitspreis.Increment = 0.1M;  // Schritte beim Klicken
            numGrundpreis.DecimalPlaces = 2; // Erlaubt zwei Nachkommastellen
            numGrundpreis.Increment = 0.1M;  // Schritte beim Klicken
            numLeistungpreis.DecimalPlaces = 2; // Erlaubt zwei Nachkommastellen
            numLeistungpreis.Increment = 0.1M;  // Schritte beim Klicken

            numArbeitspreis.Value = (decimal)Daten.ArbeitspreisAnzeige;
            numGrundpreis.Value = (decimal)Daten.GrundpreisAnzeige;
            numLeistungpreis.Value = (decimal)Daten.LeistungspreisAnzeige;

            // Farbe anpassen, wenn es ein individueller Projektpreis ist
            numArbeitspreis.ForeColor = Daten.ProjektArbeitspreis > 0 ? Color.Blue : Color.Black;
        }

        private void RegisterEvents()
        {
            // Wir nutzen eine gemeinsame Methode für alle Änderungen
            numGrundpreis.ValueChanged += OnUpdate;
            numArbeitspreis.ValueChanged += OnUpdate;
            numLeistungpreis.ValueChanged += OnUpdate;
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            // Werte von der UI zurück in das Objekt schreiben
            Daten.ProjektArbeitspreis = (double)numArbeitspreis.Value;
            Daten.ProjektGrundpreis = (double)numGrundpreis.Value;
            Daten.ProjektLeistungspreis = (double)numLeistungpreis.Value;

            // Farbe aktualisieren (Feedback für den User)
            numArbeitspreis.ForeColor = Daten.ProjektArbeitspreis > 0 ? Color.Blue : Color.Black;

            // Jetzt das Event nach außen an die Hauptform melden
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ProjektBrennstoff
    {
        // Aus Tab_Brennstoff_Stamm
        public int StammID { get; set; }
        public string Name { get; set; }
        public string Einheit { get; set; }
        public double Hi { get; set; }
        public double Hs { get; set; }
        public string Kategorie { get; set; }
        public double DefaultArbeitspreis { get; set; }
        public double DefaultGrundpreis { get; set; }
        public double DefaultLeistungspreis { get; set; }
        public string PreisEinheit { get; set; }

        // Aus Tab_Projekt_Brennstoffe
        public double ProjektArbeitspreis { get; set; }
        public double ProjektGrundpreis { get; set; }
        public double ProjektLeistungspreis { get; set; }
        public bool Aktiv { get; set; }
        public string Bezug { get; set; } // "Hi" oder "Hs"

        // Helper: Nutzt Projektpreis falls vorhanden, sonst Stamm
        public double ArbeitspreisAnzeige => ProjektArbeitspreis > 0 ? ProjektArbeitspreis : DefaultArbeitspreis;
        public double GrundpreisAnzeige => ProjektGrundpreis > 0 ? ProjektGrundpreis : DefaultGrundpreis;
        public double LeistungspreisAnzeige => ProjektLeistungspreis > 0 ? ProjektLeistungspreis : DefaultLeistungspreis;
    }
}
