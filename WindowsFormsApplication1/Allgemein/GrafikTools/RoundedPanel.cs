using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 20; // Etwas größerer Default für Sichtbarkeit

        public RoundedPanel()
        {
            this.DoubleBuffered = true; // Verhindert Flackern
        }

        // Region nur aktualisieren, wenn sich die Größe ändert, nicht beim Zeichnen!
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (this.Width > 0 && this.Height > 0)
            {
                using (GraphicsPath path = GetRoundedPath(this.ClientRectangle, CornerRadius))
                {
                    this.Region = new Region(path);
                }
            }
        }

        private GraphicsPath GetRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r2 = radius / 2f;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // base.OnPaint(e); // Optional

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Sicherer Zugriff auf Parent-Farbe für den Designer
            Color backColor = (this.Parent != null) ? this.Parent.BackColor : Color.White;

            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.Clear(backColor); // Den Hintergrund säubern
            }

            using (GraphicsPath path = GetRoundedPath(new RectangleF(0.5f, 0.5f, this.Width - 1.5f, this.Height - 1.5f), CornerRadius))
            {
                // Inneren Körper füllen
                using (SolidBrush innerBrush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillPath(innerBrush, path);
                }

                // Rahmen zeichnen
                using (Pen pen = new Pen(Color.FromArgb(0, 120, 215), 3f))
                {
                    pen.Alignment = PenAlignment.Inset;
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }
}