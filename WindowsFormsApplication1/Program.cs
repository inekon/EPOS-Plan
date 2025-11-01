using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    static class Program
    {
        public static MDIMainForm mdifrm = null;
        public static FormMain mainfrm = null;
        public static Form_Start startfrm = null;
        public static MenueCtrl menuectrl = null;
        public static OdbcConnection DBConnection = null;
        public static WizardCtrl wizardctrl = null;
        public static string ApplicationPath_Common = "";
        public static string ApplicationPath_User = "";

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            menuectrl = new MenueCtrl();
            wizardctrl = new WizardCtrl();
            DbClass db = new DbClass();
            
            try
            {
                DBConnection = db.openDB();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                MessageBox.Show("Datenbaank kann nicht geöffnet werden!\nDSN=TEST überprüfen", "Fehler");
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                Application.Exit();
                return;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                MessageBox.Show("Datenbaank kann nicht geöffnet werden!\nDSN=TEST überprüfen", "Fehler");
                Application.Exit();
                return;
            }

            ApplicationPath_Common = Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            ApplicationPath_Common = Path.Combine(ApplicationPath_Common, "WP-Plan");
            ApplicationPath_User = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ApplicationPath_User = Path.Combine(ApplicationPath_User, "WP-Plan");

            mdifrm = new MDIMainForm();
            Application.Run(mdifrm);

            db.closeDB();
            Application.Exit(); 
        }

        public static bool HasValue(this double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        public static bool checkInt(Control ctrl, string text)
        {
            int number;
            if (!int.TryParse(text, out number))
            {
                ctrl.Focus();
                MessageBox.Show("Eingaben überprüfen!");
                return false;
            }
            return true;
        }

        public static bool checkDouble(Control ctrl, string text)
        {
            double number;
            if (!double.TryParse(text, out number))
            {
                ctrl.Focus();
                MessageBox.Show("Eingaben überprüfen: " + text);
                return false;
            }
            return true;
        }

        public static double convertTxt2Double(string txt)
        {
            if (txt != "")
            {
                double number = Convert.ToDouble(txt, System.Globalization.CultureInfo.InvariantCulture);
                return number;
            }
            return 0;
        }

        public static int convertTxt2Int(string txt)
        {
            if (txt != "")
            {
                int number;
                if (Int32.TryParse(txt, out number))
                {
                    return number;
                }
            }
            return 0;
        }

        public static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
            {
                graphics.FillPath(brush, path);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
