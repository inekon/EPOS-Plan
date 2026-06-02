using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Threading;
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
        public static int nLanguage = 0; // 0=de, 1=en  

        // Der globale Katalog, auf den alle Formulare zugreifen können
        public static WordPressHelpCatalog HelpCatalog { get; private set; }
        private static Process _webServerProcess;

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {

            var key = Registry.CurrentUser.OpenSubKey(@"Software\\wp-plan", true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"Software\\wp-plan");
            }

            nLanguage = (int)key.GetValue("Language", 0);
            if (nLanguage == 0)
            {
                var culture_de = new CultureInfo("de-DE");
                Thread.CurrentThread.CurrentUICulture = culture_de;
            }
            else
            {
                var culture_en = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = culture_en;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            menuectrl = new MenueCtrl();
            wizardctrl = new WizardCtrl();
            DbClass db = new DbClass();

            try
            {
                DBConnection = db.openDB("DSN=TEST");
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                MessageBox.Show("Datenbank kann nicht geöffnet werden!\nDSN=TEST überprüfen", "Fehler");
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                Application.Exit();
                return;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                MessageBox.Show("Datenbank kann nicht geöffnet werden!\nDSN=TEST überprüfen", "Fehler");
                Application.Exit();
                return;
            }

            ApplicationPath_Common = Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            ApplicationPath_Common = Path.Combine(ApplicationPath_Common, "WP-Plan");
            ApplicationPath_User = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ApplicationPath_User = Path.Combine(ApplicationPath_User, "WP-Plan");

            // wenn die UdateDB.ini Datei und die DB existiert, dann Update starten   
            if (db.GetIniFilePath() != "" && db.GetDBFilePath() != "")
            {
                Form_Update formUpdate = new Form_Update();
                formUpdate.ShowDialog();
            }

            // Katalog-Objekt einmalig erstellen
            // HelpCatalog = new WordPressHelpCatalog("https://wordpress.org/news");
            // Visuelle Hilfe lokal testen:
            // Der Hilfe Server kann lokal simuliert werden durch einen einfachen HTTP-Server,
            // 
            // dotnet tool install --global dotnet-serve
            // starten mit : dotnet serve --directory "C:\Pfad\zu\deinem\Hilfeordner" --port 8080
            HelpCatalog = new WordPressHelpCatalog(Properties.Settings.Default.WordPressUrl); // Lokaler Testserver mit Testartik

            // nur zum Testen, Testserver wird in dieser Funktion beim Starten des Programms automatisch aufgerufen,
            // kein separates CMD Fensetr mit Aufruf nötig
            StartLocalWebServer();

            mdifrm = new MDIMainForm();
            Application.Run(mdifrm);
            
            StopLocalWebServer();
            
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

        public static class UICharacters
        {

            // Benutzung: btnParse.Text = $"{UICharacters.Search} Vorschau";
            // --- Datei-Operationen ---
            public const string OpenFile = "📂"; // \U0001F4C2
            public const string Save = "💾"; // \U0001F4BE
            public const string Settings = "⚙";  // \u2699
            public const string Trash = "🗑";  // \U0001F5D1
            public const string Refresh = "🔄"; // \U0001F504
            public const string Export = "📤"; // \U0001F4E4

            // --- PV-Technik & Details ---
            public const string Energy = "⚡";  // \u26A1
            public const string Sun = "☀️";  // \u2600
            public const string Temp = "🌡️";  // \U0001F321
            public const string Chart = "📊";  // \U0001F4CA
            public const string Geometry = "📐";  // \U0001F4D0
            public const string Eco = "🌿";  // \U0001F33F
            public const string Bifacial = "💎";  // \U0001F48E (Oft für hochwertige/bifaziale Zellen genutzt)

            // --- Status & Navigation ---
            public const string Search = "🔍";  // \U0001F50D
            public const string Success = "✅";  // \u2705
            public const string Cancel = "❌";  // \u274C
            public const string Info = "ℹ";   // \u2139
            public const string Warning = "⚠️";  // \u26A0
            public const string Link = "🔗";  // \U0001F517
            public const string Web = "🌐";  // \U0001F310

            // --- Listen-Steuerung ---
            public const string MoveUp = "⬆";   // \u2B06
            public const string MoveDown = "⬇";   // \u2B07
            public const string Add = "➕";   // \u2795
            public const string Remove = "➖";   // \u2796
        }

        private static void StartLocalWebServer()
        {
            try
            {
                _webServerProcess = new Process();

                // Da 'dotnet' ein globaler Systembefehl ist, können wir ihn direkt beim Namen nennen
                _webServerProcess.StartInfo.FileName = "dotnet";

                // Hier sagen wir dotnet-serve, welchen Ordner es auf welchem Port öffnen soll:
                // "serve" = Tool aufrufen
                // "-d C:\WPFake" = Dieses Verzeichnis ausliefern
                // "-p 8080" = Port 8080 nutzen
                _webServerProcess.StartInfo.Arguments = @"serve -d C:\WPFake -p 8080";

                // WICHTIG: Macht das CMD-Fenster für den Benutzer unsichtbar
                _webServerProcess.StartInfo.CreateNoWindow = true;
                _webServerProcess.StartInfo.UseShellExecute = false;

                _webServerProcess.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Starten von dotnet-serve: " + ex.Message);
            }
        }

        private static void StopLocalWebServer()
        {
            if (_webServerProcess != null && !_webServerProcess.HasExited)
            {
                _webServerProcess.Kill(); // Schließt dotnet-serve im Hintergrund wieder
                _webServerProcess.Dispose();
            }
        }


    }

}