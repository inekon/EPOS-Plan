using System;
using System.Drawing;
using System.Windows.Documents;
using System.Windows.Forms;


public partial class ucKostenZeile : UserControl
{
    public KostenPosition Daten { get; private set; }

    public ucKostenZeile(KostenPosition pos)
    {
        InitializeComponent();
        // Fixiert die Höhe des gesamten Zeilen-Controls
        //this.MaximumSize = new Size(0, 45); // 0 bedeutet Breite ist flexibel
        //this.MinimumSize = new Size(0, 45);
        this.Margin = new Padding(5, 5, 5, 5);
        this.Daten = pos;
        lblName.Text = pos.Name;
        lblEinheit.Text = pos.Einheit;

        numBetrag.DecimalPlaces = 2; // Erlaubt zwei Nachkommastellen
        numBetrag.Increment = 1M;  // Schritte beim Klicken
        numDauer.DecimalPlaces = 2; // Erlaubt zwei Nachkommastellen
        numDauer.Increment = 0.5M;  // Schritte beim Klicken
        numBetrag.Value = (decimal)pos.Betrag;
        numDauer.Value = (decimal)pos.Nutzungsdauer;

        // Events abfangen, um Änderungen zurück ins Objekt zu schreiben
        numBetrag.ValueChanged += (s, e) => { pos.Betrag = (decimal)numBetrag.Value; OnValueChanged(); };
        numDauer.ValueChanged += (s, e) => { pos.Nutzungsdauer = (decimal)numDauer.Value; OnValueChanged(); };

        // Abstand: Links=0, Oben=2, Rechts=0, Unten=2
        this.Margin = new Padding(0, 2, 0, 0);

        btn_Delete.Visible = false;
        if (!pos.IsMainComponent)
        {
            btn_Delete.Font = new Font("Segoe MDL2 Assets", 12, FontStyle.Bold);
            btn_Delete.Text = "\u2796";
            // Optional: Rand entfernen für flachen Look
            btn_Delete.FlatStyle = FlatStyle.Flat;
            btn_Delete.FlatAppearance.BorderSize = 0;
            btn_Delete.ForeColor = Color.DarkGray;
            btn_Delete.Size = new Size(25, 25);
            btn_Delete.Visible = true;  
        }
    }

    // Neues Event für das Löschen
    public event EventHandler DeleteRequested;

    // Im Konstruktor oder via Designer das Click-Event des Buttons abonnieren
    private void btnDelete_Click(object sender, EventArgs e)
    {
        // Wir feuern das Event, damit das FlowLayoutPanel weiß: "Ich muss weg!"
        DeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler ValueChanged;
    protected void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);
}

public class KostenPosition
{
    public int ID { get; set; } // Der Primärschlüssel (Autowert) aus Tab_ProjektWerte
    public string Name { get; set; }
    public string Gruppe { get; set; } // z.B. "Infrastruktur"
    public decimal Betrag { get; set; }
    public string Einheit { get; set; }
    public decimal Nutzungsdauer { get; set; }
    public string Gruppenname { get; set; }
    public bool IsMainComponent { get; set; }
    public int StammID { get; set; } // Optional: Verweis auf die Stammdaten, falls benötigt    
    public string Komponente { get; set; }
}
