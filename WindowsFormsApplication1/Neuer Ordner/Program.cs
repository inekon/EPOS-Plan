using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.Odbc;
using System.IO;

namespace WindowsFormsApplication1
{
    static class Program
    {
        public static MDIMainForm mdifrm = null;
        public static FormMain mainfrm = null;
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

    }
}
