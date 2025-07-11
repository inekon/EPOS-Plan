using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    static class Program
    {
        public static MDIMainForm mdifrm = null;
        public static Form_Main mainfrm = null;
        public static MenueCtrl menuectrl = null;
        public static OdbcConnection DBConnection = null;
        

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            menuectrl = new MenueCtrl(); 
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
            mdifrm = new MDIMainForm();
            Application.Run(mdifrm);
            db.closeDB();
            Application.Exit(); 

        }
    }
}
