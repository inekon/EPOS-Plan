using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class BaseForm : Form
    {
        public BaseForm()
        {
            // Globale Standard-Einstellungen für JEDES Fenster
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScroll = true;
            this.MaximumSize = new Size(0, 0); // Unbegrenztes Wachstum erlauben
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || this.DesignMode)
            {
                return;
            }

            // Gilt nur für echte, eigenständige Fenster (nicht für eingebettete Unterseiten)
            if (this.TopLevel && this.FormBorderStyle != FormBorderStyle.None)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable; // Ermöglicht das Ziehen mit der Maus
                this.AutoSize = false;                          // Verhindert das Einfrieren/Zusammenstauchen
                this.AutoScaleMode = AutoScaleMode.Font;         // Skalierung nach Schriftart (Notebook-Standard)
                this.AutoScroll = true;                         // Blendet Scrollbalken ein, wenn der Monitor zu klein ist
                this.MaximumSize = new Size(0, 0);              // Unbegrenztes Wachstum erlauben


                // --- AUTOMATISCHE MINDESTGRÖSSE ---
                // Wir nehmen die Größe, die du im Designer gezeichnet hast, 
                // und setzen sie als absolute Untergrenze fest!
                if (this.MinimumSize.Width == 0 && this.MinimumSize.Height == 0)
                {
                    this.MinimumSize = new Size(this.Width, this.Height);
                }

                // 1. Wunschgröße des Inhalts messen (DPI-skaliert durch das Notebook)
                Size wunschGroesse = this.PreferredSize;

                // 2. Fenster vergrößern, falls der Inhalt wegen Notebook-Skalierung mehr Platz braucht
                if (wunschGroesse.Width > this.Width) this.Width = wunschGroesse.Width;
                if (wunschGroesse.Height > this.Height) this.Height = wunschGroesse.Height;

                // 3. Notebook-Schutz: Begrenzen auf die echte Monitor-Arbeitsfläche (ohne Taskleiste)
                Rectangle bildschirm = Screen.FromControl(this).WorkingArea;

                if (this.Width > bildschirm.Width) this.Width = bildschirm.Width;
                if (this.Height > bildschirm.Height) this.Height = bildschirm.Height;
            }
        }
    }
}