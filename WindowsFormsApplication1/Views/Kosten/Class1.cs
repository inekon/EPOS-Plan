using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class InvestCategoryPanel : Panel
{
    private TableLayoutPanel grid;
    private Label lblSum;

    public InvestCategoryPanel(string title, string icon, List<CostItemConfig> items)
    {
        this.Dock = DockStyle.Top;
        this.AutoSize = true;
        this.BackColor = Color.White;
        this.Margin = new Padding(0, 0, 0, 20);
        this.Padding = new Padding(1); // Rahmen-Effekt

        // 1. Header (Dunkelblau)
        Panel header = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(15, 31, 61) };

        Label lblIcon = new Label { Text = icon, ForeColor = Color.FromArgb(255, 193, 7), Dock = DockStyle.Left, Width = 40, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 12) };
        Label lblTitle = new Label { Text = title, ForeColor = Color.White, Dock = DockStyle.Left, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Padding = new Padding(0, 10, 0, 0) };

        lblSum = new Label { Text = "0 €", ForeColor = Color.FromArgb(147, 197, 253), Dock = DockStyle.Right, Width = 120, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 10, FontStyle.Bold), Padding = new Padding(0, 0, 10, 0) };

        header.Controls.Add(lblTitle);
        header.Controls.Add(lblIcon);
        header.Controls.Add(lblSum);

        // 2. Tabellen-Header (Graue Zeile)
        Panel tableHeader = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(240, 244, 250) };
        // Hier könntest du Labels für "KOMPONENTE", "KOSTEN", etc. einfügen...

        // 3. Inhalts-Grid
        grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, Padding = new Padding(10) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f)); // Name
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f)); // Kosten
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f)); // Dauer
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f)); // Zins

        // Items hinzufügen
        foreach (var item in items)
        {
            AddRow(item);
        }

        // 4. Footer der Karte (Summenzeile)
        Panel footer = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(235, 240, 250) };
        // ... Summen-Label hier rein

        this.Controls.Add(grid);
        this.Controls.Add(tableHeader);
        this.Controls.Add(header);
    }

    private void AddRow(CostItemConfig config)
    {
        // Spalte 0: Labels
        Panel pnlLabels = new Panel { Dock = DockStyle.Fill, Height = 45 };
        pnlLabels.Controls.Add(new Label { Text = config.SubLabel, Font = new Font("Segoe UI", 7), ForeColor = Color.Gray, Dock = DockStyle.Bottom });
        pnlLabels.Controls.Add(new Label { Text = config.Label, Font = new Font("Segoe UI", 9), Dock = DockStyle.Top, AutoSize = true });

        // Spalte 1-3: NumericUpDowns oder Textboxen

        // RICHTIG (Zuerst Grenzen definieren, dann Wert setzen):
        var txtCost = new NumericUpDown();
        txtCost.Maximum = 1000000; // Erst den "Platz" schaffen
        txtCost.Minimum = 0;       // Optional, falls du negative Werte verhindern willst
        txtCost.Value = (decimal)config.DefaultCost; // Jetzt passt der Wert rein!
        txtCost.Width = 100;

        var txtLife = new NumericUpDown { Value = (decimal)config.DefaultLife, Width = 60 };
        var txtInt = new NumericUpDown { Value = (decimal)config.DefaultInterest, Width = 60 };

        grid.Controls.Add(pnlLabels);
        grid.Controls.Add(txtCost);
        grid.Controls.Add(txtLife);
        grid.Controls.Add(txtInt);
    }

    // Diese Klasse definiert, welche Daten ein einzelner Kostenpunkt hat
    public class CostItemConfig
    {
        public string Label { get; set; }      // z.B. "BHKW-Module"
        public string SubLabel { get; set; }   // z.B. "Wärmeerzeuger"
        public double DefaultCost { get; set; } // 17150
        public double DefaultLife { get; set; } // 13.3
        public double DefaultInterest { get; set; } // 1.15
    }
}