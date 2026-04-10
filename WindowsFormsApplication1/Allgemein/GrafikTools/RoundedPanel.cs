using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 5; // Hier den Radius einstellen

        protected override void OnPaint(PaintEventArgs e)
        {
            // WICHTIG: Kein base.OnPaint(e) aufrufen, wenn wir die Region komplett selbst steuern
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            float radius = CornerRadius;
            float thickness = 3f; // Rahmendicke

            // Wir ziehen 2 Pixel ab, um Puffer für die Kantenglättung zu haben
            float width = this.Width - 1.5f;
            float height = this.Height - 1.5f;

            using (GraphicsPath path = new GraphicsPath())
            {
                // Wir starten bei 0.5, damit der Rahmen oben links nicht klebt
                float offset = 0.5f;

                path.AddArc(offset, offset, radius, radius, 180, 90);
                path.AddArc(width - radius, offset, radius, radius, 270, 90);
                path.AddArc(width - radius, height - radius, radius, radius, 0, 90); // Die kritische Ecke
                path.AddArc(offset, height - radius, radius, radius, 90, 90);
                path.CloseAllFigures();

                // 1. Setze die Region (leicht vergrößert, damit das Antialiasing nicht abgeschnitten wird)
                this.Region = new Region(new Rectangle(0, 0, this.Width, this.Height));

                // 2. Hintergrund zeichnen (sonst hast du hässliche Artefakte)
                using (SolidBrush brush = new SolidBrush(this.Parent.BackColor))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }

                // 3. Den abgerundeten Körper füllen
                using (SolidBrush innerBrush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillPath(innerBrush, path);
                }

                // 4. Den Rahmen zeichnen
                using (Pen pen = new Pen(Color.FromArgb(0, 120, 215), thickness))
                {
                    pen.Alignment = PenAlignment.Inset; // Ganz wichtig!
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }


    }
}
