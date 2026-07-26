using System;
using System.Drawing;
using System.Windows.Forms;

public class SectionPanel : Panel
{
    private Panel pnlHeader;
    private Label lblIcon;
    private Label lblTitle;
    private Label lblTotal;
    public Panel Body { get; private set; }

    public SectionPanel(string title, string icon, string totalValue = "0 €")
    {
        this.Dock = DockStyle.Top;
        this.AutoSize = true;
        this.Margin = new Padding(0, 0, 0, 15);
        this.BackColor = Color.White;

        // Header
        pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(15, 31, 61), Cursor = Cursors.Hand };
        
        lblIcon = new Label { Text = icon, ForeColor = Color.White, AutoSize = false, Size = new Size(30, 40), 
                              TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Left };
        
        lblTitle = new Label { Text = title, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), 
                               TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
        
        lblTotal = new Label { Text = totalValue, ForeColor = Color.FromArgb(147, 197, 253), AutoSize = true, 
                               TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Right, Padding = new Padding(0,0,10,0) };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblIcon);
        pnlHeader.Controls.Add(lblTotal);

        // Body
        Body = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
        
        this.Controls.Add(Body);
        this.Controls.Add(pnlHeader);
    }
}