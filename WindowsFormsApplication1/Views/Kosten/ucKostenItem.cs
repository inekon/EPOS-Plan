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

        // Obergrenze: der Designer stand auf 99.999 €. Das reicht für ein Modul, nicht
        // für eine Anlage — das Beispiel-BHKW „2G 250kw.el Gas" kommt über den
        // spezifischen Preis auf 163.400 € (653,60 €/kWel × 250 kWel), und jedes Setzen
        // eines größeren Wertes hätte eine ArgumentOutOfRangeException geworfen.
        // Programmatisch statt im Designer, damit die generierte Datei unberührt bleibt.
        numBetrag.Maximum = 100000000M;

        // Untergrenze: Der Designer lässt sie auf 0, und damit machte Klemme() aus jedem
        // negativen Betrag stillschweigend eine 0 — Erlöse waren so weder eingebbar noch
        // darstellbar (Etappe E3, Leitentscheidung L5). Für ERLÖSpositionen wird die
        // Grenze gespiegelt: Sie dürfen nur ≤ 0 sein, Kostenpositionen weiterhin nur ≥ 0.
        // Damit kann ein Erlös nirgends als Kosten in eine Summe geraten — dieselbe Regel,
        // die BetriebskostenCtrl.Betrag rechnerisch erzwingt.
        if (pos.IstErloes)
        {
            numBetrag.Minimum = -100000000M;
            numBetrag.Maximum = 0M;
        }

        numDauer.DecimalPlaces = 2; // Erlaubt zwei Nachkommastellen
        numDauer.Increment = 0.5M;  // Schritte beim Klicken
        numBetrag.Value = Klemme(pos.Betrag, numBetrag.Minimum, numBetrag.Maximum);

        // Abgeleitete Positionen: Der Betrag entsteht aus Menge × Einheitpreis und darf
        // hier nicht überschrieben werden — sonst liefen der gespeicherte Betrag und die
        // gespeicherte Herleitung auseinander. Gesperrt und SICHTBAR gekennzeichnet, nicht
        // still geleert (Konzept 4.1; die Altanwendung leerte die Absolutfelder
        // kommentarlos, Befund 6).
        if (pos.Abgeleitet)
        {
            numBetrag.ReadOnly = true;
            numBetrag.Increment = 0M;
            numBetrag.BackColor = SystemColors.Control;
            numBetrag.Cursor = Cursors.No;
            if (!string.IsNullOrEmpty(pos.Herleitung))
                toolTip1.SetToolTip(numBetrag, pos.Herleitung);
        }
        numDauer.Value = Klemme(pos.Nutzungsdauer, numDauer.Minimum, numDauer.Maximum);

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
        decimal w = Klemme(wert, numBetrag.Minimum, numBetrag.Maximum);

        // Control anpassen, das den Betrag anzeigt.
        if (this.numBetrag.InvokeRequired)
        {
            this.Invoke(new Action(() => this.numBetrag.Value = w));
        }
        else
        {
            this.numBetrag.Value = w;
        }
    }

    /// <summary>
    /// Hält einen Wert in den Grenzen des Drehfeldes. Ein Betrag aus der Datenbank kann
    /// außerhalb liegen (Altbestand, importierte Daten); <c>NumericUpDown.Value</c> wirft
    /// dann eine Ausnahme und riss bislang den Aufbau der ganzen Positionsliste ab.
    /// </summary>
    private static decimal Klemme(decimal wert, decimal min, decimal max)
    {
        if (wert < min) return min;
        if (wert > max) return max;
        return wert;
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
                      $"Best Case Kosten:  {Daten.BestCase:N2} €\n" +
                      $"Worst Case Kosten: {Daten.WorstCase:N2} €\n" +
                      $"Best Case Nutzungsdauer:  {Daten.BestCase_Nutzungsdauer:N2} €\n" +
                      $"Worst Case Nutzungsdauer: {Daten.WorstCase_Nutzungsdauer:N2} €";

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
    public decimal WorstCase_Nutzungsdauer { get; set; }
    public decimal BestCase_Nutzungsdauer { get; set; }
    public string Einheit { get; set; }
    public decimal Nutzungsdauer { get; set; }
    public string Gruppenname { get; set; }
    public bool IsMainComponent { get; set; }
    public int StammID { get; set; } // Optional: Verweis auf die Stammdaten, falls benötigt
    public string Komponente { get; set; }

    // ------------------------------------------------- Etappe E3 (Migrationsschritt 19)

    /// <summary>
    /// Erlösposition (<c>Tab_ProjektWerte.IstErloes</c>) — nur dann darf der Betrag
    /// negativ sein. Vorbelegung false, also das Verhalten des gesamten Bestands.
    /// </summary>
    public bool IstErloes { get; set; }

    /// <summary>
    /// Bemessungsart (<c>DbWerte.BEMESSUNG_*</c>). Vorbelegung
    /// <see cref="DbWerte.BEMESSUNG_BETRAG"/> — der fest eingegebene Jahresbetrag und
    /// damit das Verhalten jeder Bestandszeile.
    /// </summary>
    public string Bemessung { get; set; } = DbWerte.BEMESSUNG_BETRAG;

    /// <summary>
    /// true, wenn der Betrag aus Menge × Einheitpreis entsteht und deshalb nicht von
    /// Hand geändert werden darf.
    /// </summary>
    public bool Abgeleitet
    {
        get
        {
            return !string.IsNullOrEmpty(Bemessung) &&
                   !string.Equals(Bemessung, DbWerte.BEMESSUNG_BETRAG, System.StringComparison.Ordinal);
        }
    }

    /// <summary>Klartext der Herleitung für den Hinweis am gesperrten Feld.</summary>
    public string Herleitung { get; set; } = "";
}
