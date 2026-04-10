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
        private const int CB_SETCUEBANNER = 0x1703; // Speziell für ComboBox

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

        public static void SetPlaceholder(this ComboBox comboBox, string placeholder)
        {
            if (comboBox.IsHandleCreated)
            {
                SendMessage(comboBox.Handle, CB_SETCUEBANNER, 0, placeholder);
            }
            else
            {
                // Falls das Handle noch nicht da ist (sehr früh im Code), 
                // warten wir kurz, bis es erstellt wurde.
                comboBox.HandleCreated += (s, e) =>
                    SendMessage(comboBox.Handle, CB_SETCUEBANNER, 0, placeholder);
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
        public static void MakeSmoothRounded(this Control control, int radius, Color borderColor, float borderWidth)
        {
            // Wir deaktivieren die Region, da sie kein Antialiasing erlaubt
            control.Region = null;

            control.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                Rectangle rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);

                using (GraphicsPath path = rect.GetRoundedRect(radius))
                {
                    // 1. Hintergrund füllen
                    using (SolidBrush brush = new SolidBrush(control.BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    // 2. Rahmen zeichnen (das erzeugt die glatte Kante)
                    if (borderWidth > 0)
                    {
                        using (Pen pen = new Pen(borderColor, borderWidth))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                }
            };

            // Wichtig: Parent muss das Control neu zeichnen, wenn sich was ändert
            control.Invalidate();
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
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.UseVisualStyleBackColor = false;

            // Diese Funktion setzt die physikalische Form des Buttons auf "Rund"
            void UpdateRegion()
            {
                Rectangle rect = new Rectangle(0, 0, btn.Width, btn.Height);
                using (var path = rect.GetRoundedRect(radius))
                {
                    btn.Region = new Region(path); // Das macht ihn physikalisch rund
                }
            }

            btn.Resize += (s, e) => UpdateRegion();
            UpdateRegion();

            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                // WICHTIG für weiche Kanten:
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                // Wir holen die Hintergrundfarbe des Parents
                Color parentColor = btn.Parent?.BackColor ?? Color.White;

                // SCHRITT 1: Hintergrund des Buttons mit Parent-Farbe füllen (übermalt die harten Region-Ecken)
                using (SolidBrush parentBrush = new SolidBrush(parentColor))
                {
                    g.FillRectangle(parentBrush, btn.ClientRectangle);
                }

                // SCHRITT 2: Den Button-Körper zeichnen. 
                // Trick: Wir machen das Rechteck 1 Pixel kleiner als das Control (Width-1, Height-1),
                // damit der weiche Rand des Anti-Aliasings nicht von der Region abgeschnitten wird!
                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);

                using (var path = rect.GetRoundedRect(radius))
                {
                    bool isHovered = btn.ClientRectangle.Contains(btn.PointToClient(Control.MousePosition));
                    Color drawColor = isHovered ? ControlPaint.Light(btn.BackColor, 0.2f) : btn.BackColor;

                    // Füllen des runden Körpers
                    using (SolidBrush sb = new SolidBrush(drawColor))
                    {
                        g.FillPath(sb, path);
                    }

                    // SCHRITT 3: Einen dünnen Rahmen in der GLEICHEN Farbe zeichnen.
                    // Das erzwingt das Antialiasing an den Außenkanten.
                    using (Pen pen = new Pen(drawColor, 1.5f)) // 1.5f sorgt für eine weichere Kante
                    {
                        g.DrawPath(pen, path);
                    }
                }

                // Text zeichnen
                TextRenderer.DrawText(g, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };


            btn.MouseMove += (s, e) => btn.Invalidate();
            btn.MouseLeave += (s, e) => btn.Invalidate();
        }

    }
}