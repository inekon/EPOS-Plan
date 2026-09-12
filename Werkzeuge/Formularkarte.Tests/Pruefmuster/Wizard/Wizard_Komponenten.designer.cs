namespace WindowsFormsApplication1
{
    partial class Wizard_Komponenten
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wizard_Komponenten));
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            karte_Gebaeude = new AktionsKarte();
            karte_WBedarfDaten = new AktionsKarte();
            karte_Prozess = new AktionsKarte();
            karte_Brauchwasser = new AktionsKarte();
            karte_StdStromprofil = new AktionsKarte();
            karte_Stromlastgang = new AktionsKarte();
            karte_WP = new AktionsKarte();
            karte_BHKW = new AktionsKarte();
            karte_Kessel = new AktionsKarte();
            karte_Solar = new AktionsKarte();
            karte_PV = new AktionsKarte();
            karte_StromSp = new AktionsKarte();
            karte_Puffer = new AktionsKarte();
            panel_Textvorlagen = new System.Windows.Forms.Panel();
            label_TextEnthalten = new System.Windows.Forms.Label();
            label_TextOhne = new System.Windows.Forms.Label();
            label_TextNurAnzeige = new System.Windows.Forms.Label();
            label_TextFrage = new System.Windows.Forms.Label();
            label_TextFrageTitel = new System.Windows.Forms.Label();
            label_TextNeuFrage = new System.Windows.Forms.Label();
            label_TextNeuTitel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel_Textvorlagen.SuspendLayout();
            SuspendLayout();
            //
            // label1
            //
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            //
            // label2
            //
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            //
            // label3
            //
            resources.ApplyResources(label3, "label3");
            label3.BackColor = System.Drawing.Color.DimGray;
            label3.ForeColor = System.Drawing.Color.White;
            label3.Name = "label3";
            //
            // pictureBox1
            //
            resources.ApplyResources(pictureBox1, "pictureBox1");
            pictureBox1.Image = Properties.Resources.Logo125_125;
            pictureBox1.Name = "pictureBox1";
            pictureBox1.TabStop = false;
            //
            // karte_Gebaeude
            //
            resources.ApplyResources(karte_Gebaeude, "karte_Gebaeude");
            karte_Gebaeude.Name = "karte_Gebaeude";
            karte_Gebaeude.StatusSichtbar = true;
            karte_Gebaeude.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_Gebaeude.Geklickt += karte_Geklickt;
            //
            // karte_WBedarfDaten
            //
            resources.ApplyResources(karte_WBedarfDaten, "karte_WBedarfDaten");
            karte_WBedarfDaten.Name = "karte_WBedarfDaten";
            karte_WBedarfDaten.StatusSichtbar = true;
            karte_WBedarfDaten.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_WBedarfDaten.Geklickt += karte_Geklickt;
            //
            // karte_Prozess
            //
            resources.ApplyResources(karte_Prozess, "karte_Prozess");
            karte_Prozess.Name = "karte_Prozess";
            karte_Prozess.StatusSichtbar = true;
            karte_Prozess.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_Prozess.Geklickt += karte_Geklickt;
            //
            // karte_Brauchwasser
            //
            resources.ApplyResources(karte_Brauchwasser, "karte_Brauchwasser");
            karte_Brauchwasser.Name = "karte_Brauchwasser";
            karte_Brauchwasser.StatusSichtbar = true;
            karte_Brauchwasser.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_Brauchwasser.Geklickt += karte_Geklickt;
            //
            // karte_StdStromprofil
            //
            resources.ApplyResources(karte_StdStromprofil, "karte_StdStromprofil");
            karte_StdStromprofil.Name = "karte_StdStromprofil";
            karte_StdStromprofil.StatusSichtbar = true;
            karte_StdStromprofil.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_StdStromprofil.Geklickt += karte_Geklickt;
            //
            // karte_Stromlastgang
            //
            resources.ApplyResources(karte_Stromlastgang, "karte_Stromlastgang");
            karte_Stromlastgang.Name = "karte_Stromlastgang";
            karte_Stromlastgang.StatusSichtbar = true;
            karte_Stromlastgang.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_Stromlastgang.Geklickt += karte_Geklickt;
            //
            // karte_WP
            //
            resources.ApplyResources(karte_WP, "karte_WP");
            karte_WP.Name = "karte_WP";
            karte_WP.StatusSichtbar = true;
            karte_WP.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_WP.Geklickt += karte_Geklickt;
            //
            // karte_BHKW
            //
            resources.ApplyResources(karte_BHKW, "karte_BHKW");
            karte_BHKW.Name = "karte_BHKW";
            karte_BHKW.StatusSichtbar = true;
            karte_BHKW.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_BHKW.Geklickt += karte_Geklickt;
            //
            // karte_Kessel
            //
            resources.ApplyResources(karte_Kessel, "karte_Kessel");
            karte_Kessel.Name = "karte_Kessel";
            karte_Kessel.StatusSichtbar = true;
            karte_Kessel.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_Kessel.Geklickt += karte_Geklickt;
            //
            // karte_Solar
            //
            resources.ApplyResources(karte_Solar, "karte_Solar");
            karte_Solar.Name = "karte_Solar";
            karte_Solar.StatusSichtbar = true;
            karte_Solar.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_Solar.Geklickt += karte_Geklickt;
            //
            // karte_PV
            //
            resources.ApplyResources(karte_PV, "karte_PV");
            karte_PV.Name = "karte_PV";
            karte_PV.StatusSichtbar = true;
            karte_PV.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_PV.Geklickt += karte_Geklickt;
            //
            // karte_StromSp
            //
            resources.ApplyResources(karte_StromSp, "karte_StromSp");
            karte_StromSp.Name = "karte_StromSp";
            karte_StromSp.StatusSichtbar = true;
            karte_StromSp.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_StromSp.Geklickt += karte_Geklickt;
            //
            // karte_Puffer
            //
            resources.ApplyResources(karte_Puffer, "karte_Puffer");
            karte_Puffer.Name = "karte_Puffer";
            karte_Puffer.StatusSichtbar = true;
            karte_Puffer.TitelSchrift = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            karte_Puffer.Geklickt += karte_Geklickt;
            //
            // panel_Textvorlagen
            //
            resources.ApplyResources(panel_Textvorlagen, "panel_Textvorlagen");
            panel_Textvorlagen.Controls.Add(label_TextEnthalten);
            panel_Textvorlagen.Controls.Add(label_TextOhne);
            panel_Textvorlagen.Controls.Add(label_TextNurAnzeige);
            panel_Textvorlagen.Controls.Add(label_TextFrage);
            panel_Textvorlagen.Controls.Add(label_TextFrageTitel);
            panel_Textvorlagen.Controls.Add(label_TextNeuFrage);
            panel_Textvorlagen.Controls.Add(label_TextNeuTitel);
            panel_Textvorlagen.Name = "panel_Textvorlagen";
            panel_Textvorlagen.Visible = false;
            //
            // label_TextEnthalten
            //
            resources.ApplyResources(label_TextEnthalten, "label_TextEnthalten");
            label_TextEnthalten.Name = "label_TextEnthalten";
            //
            // label_TextOhne
            //
            resources.ApplyResources(label_TextOhne, "label_TextOhne");
            label_TextOhne.Name = "label_TextOhne";
            //
            // label_TextNurAnzeige
            //
            resources.ApplyResources(label_TextNurAnzeige, "label_TextNurAnzeige");
            label_TextNurAnzeige.Name = "label_TextNurAnzeige";
            //
            // label_TextFrage
            //
            resources.ApplyResources(label_TextFrage, "label_TextFrage");
            label_TextFrage.Name = "label_TextFrage";
            //
            // label_TextFrageTitel
            //
            resources.ApplyResources(label_TextFrageTitel, "label_TextFrageTitel");
            label_TextFrageTitel.Name = "label_TextFrageTitel";
            //
            // label_TextNeuFrage
            //
            resources.ApplyResources(label_TextNeuFrage, "label_TextNeuFrage");
            label_TextNeuFrage.Name = "label_TextNeuFrage";
            //
            // label_TextNeuTitel
            //
            resources.ApplyResources(label_TextNeuTitel, "label_TextNeuTitel");
            label_TextNeuTitel.Name = "label_TextNeuTitel";
            //
            // Wizard_Komponenten
            //
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.White;
            Controls.Add(panel_Textvorlagen);
            Controls.Add(karte_Puffer);
            Controls.Add(karte_StromSp);
            Controls.Add(karte_PV);
            Controls.Add(karte_Solar);
            Controls.Add(karte_Kessel);
            Controls.Add(karte_BHKW);
            Controls.Add(karte_WP);
            Controls.Add(karte_Stromlastgang);
            Controls.Add(karte_StdStromprofil);
            Controls.Add(karte_Brauchwasser);
            Controls.Add(karte_Prozess);
            Controls.Add(karte_WBedarfDaten);
            Controls.Add(karte_Gebaeude);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Name = "Wizard_Komponenten";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel_Textvorlagen.ResumeLayout(false);
            panel_Textvorlagen.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private AktionsKarte karte_Gebaeude;
        private AktionsKarte karte_WBedarfDaten;
        private AktionsKarte karte_Prozess;
        private AktionsKarte karte_Brauchwasser;
        private AktionsKarte karte_StdStromprofil;
        private AktionsKarte karte_Stromlastgang;
        private AktionsKarte karte_WP;
        private AktionsKarte karte_BHKW;
        private AktionsKarte karte_Kessel;
        private AktionsKarte karte_Solar;
        private AktionsKarte karte_PV;
        private AktionsKarte karte_StromSp;
        private AktionsKarte karte_Puffer;
        private System.Windows.Forms.Panel panel_Textvorlagen;
        private System.Windows.Forms.Label label_TextEnthalten;
        private System.Windows.Forms.Label label_TextOhne;
        private System.Windows.Forms.Label label_TextNurAnzeige;
        private System.Windows.Forms.Label label_TextFrage;
        private System.Windows.Forms.Label label_TextFrageTitel;
        private System.Windows.Forms.Label label_TextNeuFrage;
        private System.Windows.Forms.Label label_TextNeuTitel;
    }
}
