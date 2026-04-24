using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApplication1;


public partial class ucKostenZeile : UserControl
{
    public KostenPosition Daten { get; private set; }

    public ucKostenZeile(KostenPosition pos)
    {
        InitializeComponent();

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

        btnOpenCases.Height = 18;
        btnOpenCases.Top = numDauer.Top; //(this.ClientSize.Height - btnOpenCases.Height) / 2;
        btnOpenCases.MaximumSize = new Size(33, 18); // Breite, Höhe
        btnOpenCases.MinimumSize = new Size(0, 18);   // Verhindert das Schrumpfen
        btnOpenCases.FlatStyle = FlatStyle.Flat;
        btnOpenCases.UseVisualStyleBackColor = false; // Verhindert, dass das System die Kontrolle übernimmt
        btnOpenCases.FlatAppearance.BorderSize = 1; // Ein Rahmen von 1 Pixel
        btnOpenCases.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215); // Das Windows-Blau
        btnOpenCases.ForeColor = Color.Black;
        btnOpenCases.TabStop = false;
        btnOpenCases.UseCompatibleTextRendering = true; // Hilft manchmal bei Farb-Glitching

        toolTip1.ToolTipTitle = "Preis-Szenarien";
        toolTip1.ToolTipIcon = ToolTipIcon.Info;
        toolTip1.IsBalloon = true; // Macht den Tooltip zu einer Sprechblase (optional)

        UpdateTooltip();
    }

    // Neues Event für das Löschen
    public event EventHandler DeleteRequested;

    // Im Konstruktor oder via Designer das Click-Event des Buttons abonnieren
    private void btnDelete_Click(object sender, EventArgs e)
    {
        // Wir feuern das Event, damit das FlowLayoutPanel weiß: "Ich muss weg!"
        DeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetBerechnetenWert(decimal wert)
    {
        // Control anpassen, das den Betrag anzeigt.
        if (this.numBetrag.InvokeRequired)
        {
            this.Invoke(new Action(() => this.numBetrag.Value = wert));
        }
        else
        {
            this.numBetrag.Value = wert;
        }
    }

    private void btnOpenCases_Click(object sender, EventArgs e)
    {
        // Wir öffnen ein neues kleines Formular und übergeben das Datenobjekt
        using (var frm = new Form_CaseEingabe(this.Daten))
        {
            // Die Position des Buttons auf dem Bildschirm berechnen
            // PointToScreen wandelt die (0,0) Koordinate des Buttons in globale Bildschirm-Pixel um
            Point btnLocation = btnOpenCases.PointToScreen(Point.Empty);

            // StartPosition des Formulars auf "Manual" setzen
            frm.StartPosition = FormStartPosition.Manual;

            // Das Fenster knapp unterhalb des Buttons positionieren
            // X-Position des Buttons 5px nach unten
            frm.Location = new Point(btnLocation.X, btnLocation.Y + btnOpenCases.Height + 5);
     
            if (frm.ShowDialog() == DialogResult.OK)
            {
                // Den Tooltip sofort aktualisieren
                UpdateTooltip();
                // Wenn der User im Formular auf OK drückt, sind die Werte
                // bereits im "this.Daten" Objekt aktualisiert.
                // WICHTIG: Event feuern, damit das Hauptformular weiß: "Hier hat sich was geändert!"
                OnValueChanged();
            }
        }
    }

    public void UpdateTooltip()
    {
        // mehrzeiligen String
        string info = $"📊 Kalkulations-Varianten:\n" +
                      $"--------------------------\n" +
                      $"Best Case:  {Daten.BestCase:N2} €\n" +
                      $"Worst Case: {Daten.WorstCase:N2} €";

        // "btnOpenCases" ist +/- Button
        toolTip1.SetToolTip(btnOpenCases, info);

        // Optional: Färbe den Button ein, wenn Werte hinterlegt sind
        if (Daten.BestCase != 0 || Daten.WorstCase != 0)
        {
            // Schrift bleibt schwarz, aber der Rahmen wird dicker oder die Fläche dezent farbig
            btnOpenCases.FlatAppearance.BorderColor = Color.DeepSkyBlue;
            btnOpenCases.FlatAppearance.BorderSize = 2; // Deutlicher Hinweis
            btnOpenCases.ForeColor = Color.Black; // Sicherstellen, dass sie schwarz bleibt
        }
        else
        {
            btnOpenCases.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
            btnOpenCases.FlatAppearance.BorderSize = 1;
            btnOpenCases.ForeColor = Color.Black;
        }
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
    public decimal WorstCase { get; set; }
    public decimal BestCase { get; set; }
    public string Einheit { get; set; }
    public decimal Nutzungsdauer { get; set; }
    public string Gruppenname { get; set; }
    public bool IsMainComponent { get; set; }
    public int StammID { get; set; } // Optional: Verweis auf die Stammdaten, falls benötigt    
    public string Komponente { get; set; }
}
