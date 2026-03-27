using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public static class TextBoxExtensions
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        private const int EM_SETCUEBANNER = 0x1501;

        /// <summary>
        /// Setzt einen Placeholder-Text (Cue Banner) für eine TextBox via Windows-API.
        /// </summary>
        public static void SetPlaceholder(this TextBox textBox, string placeholder)
        {
            if (textBox.IsHandleCreated)
            {
                SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
            }
            else
            {
                // Falls das Handle noch nicht da ist (sehr früh im Code), 
                // warten wir kurz, bis es erstellt wurde.
                textBox.HandleCreated += (s, e) =>
                    SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
            }
        }

        public static GraphicsPath GetRoundedRect(this Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // Oben Links
            path.AddArc(arc, 180, 90);

            // Oben Rechts
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Unten Rechts
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Unten Links
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Rundet die Ecken eines Controls physikalisch ab.
        /// </summary>
        public static void MakeRounded(this Control control, int radius)
        {
            // Wir abonnieren das Resize-Event, damit die Rundung 
            // mitwächst, wenn der Button seine Größe ändert.
            control.Resize += (s, e) => {
                Rectangle rect = new Rectangle(0, 0, control.Width, control.Height);
                using (GraphicsPath path = rect.GetRoundedRect(radius))
                {
                    control.Region = new Region(path);
                }
            };

            // Initial einmal ausführen
            Rectangle initialRect = new Rectangle(0, 0, control.Width, control.Height);
            using (GraphicsPath path = initialRect.GetRoundedRect(radius))
            {
                control.Region = new Region(path);
            }
        }

        public static void ApplySmoothChildRounding(this Button btn, int radius, Color parentBackColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;

            // Wir deaktivieren die Standard-Zeichnung fast komplett
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // 1. Hintergrund des Eltern-Containers zeichnen (um die Ecken zu "löschen")
                using (SolidBrush parentBrush = new SolidBrush(parentBackColor))
                {
                    e.Graphics.FillRectangle(parentBrush, btn.ClientRectangle);
                }

                // 2. Den abgerundeten Button zeichnen
                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                using (System.Drawing.Drawing2D.GraphicsPath path = rect.GetRoundedRect(radius))
                {
                    using (SolidBrush btnBrush = new SolidBrush(btn.BackColor))
                    {
                        e.Graphics.FillPath(btnBrush, path);
                    }
                }

                // 3. Text manuell zeichnen (da wir den Hintergrund übermalt haben)
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        public static void MakeSmoothButton(this Button btn, int radius)
        {
            // 1. Alles abschalten, was Windows automatisch macht
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255);
            btn.UseVisualStyleBackColor = false;

            // 2. Den Fokus-Rahmen (die gestrichelte Linie) unterdrücken
            // Das ist oft das "Rechteck", das man sieht
            btn.TabStop = false;

            btn.Paint += (s, e) =>
            {
                // Wir holen uns das Grafik-Objekt
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Hintergrundfarbe des Panels ermitteln
                Color parentColor = btn.Parent?.BackColor ?? Color.White;

                // SCHRITT A: Den kompletten Button-Bereich "auslöschen"
                // Clear ist radikaler als FillRectangle und löscht alles Vorherige
                g.Clear(parentColor);

                // SCHRITT B: Den Button-Körper vorbereiten
                // Wir nehmen -1 Pixel an jeder Seite, um Platz für das Anti-Aliasing zu lassen
                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);

                using (var path = rect.GetRoundedRect(radius))
                {
                    // Hover-Logik: Falls Maus drauf, Farbe leicht ändern
                    bool isHovered = btn.ClientRectangle.Contains(btn.PointToClient(Control.MousePosition));
                    Color drawColor = isHovered ? ControlPaint.Light(btn.BackColor, 0.2f) : btn.BackColor;

                    using (SolidBrush sb = new SolidBrush(drawColor))
                    {
                        g.FillPath(sb, path);
                    }
                }

                // SCHRITT C: Text ohne Standard-Fokus-Effekte zeichnen
                TextRenderer.DrawText(g, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // WICHTIG: Wenn die Maus sich bewegt, muss der Button wissen, 
            // dass er sich neu zeichnen soll (für den Hover-Effekt)
            btn.MouseMove += (s, e) => btn.Invalidate();
            btn.MouseLeave += (s, e) => btn.Invalidate();
        }
    }
}