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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.checkBox_WP = new System.Windows.Forms.CheckBox();
            this.checkBox_Solar = new System.Windows.Forms.CheckBox();
            this.checkBox_PV = new System.Windows.Forms.CheckBox();
            this.checkBox_StromSp = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.checkBox_Prozess = new System.Windows.Forms.CheckBox();
            this.checkBox_Stromlastgang = new System.Windows.Forms.CheckBox();
            this.checkBox_Kessel = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 106);
            this.label1.Name = "label1";
            this.label1.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label1.Size = new System.Drawing.Size(712, 46);
            this.label1.TabIndex = 1;
            this.label1.Text = "Projektkonfiguration Erstellung";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label2.Location = new System.Drawing.Point(31, 152);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label2.Size = new System.Drawing.Size(609, 88);
            this.label2.TabIndex = 2;
            this.label2.Text = resources.GetString("label2.Text");
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // checkBox_WP
            // 
            this.checkBox_WP.AutoSize = true;
            this.checkBox_WP.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.checkBox_WP.Location = new System.Drawing.Point(128, 282);
            this.checkBox_WP.Name = "checkBox_WP";
            this.checkBox_WP.Size = new System.Drawing.Size(128, 25);
            this.checkBox_WP.TabIndex = 3;
            this.checkBox_WP.Text = "Wärmepumpe";
            this.checkBox_WP.UseVisualStyleBackColor = true;
            this.checkBox_WP.CheckedChanged += new System.EventHandler(this.checkBox_WP_CheckedChanged);
            // 
            // checkBox_Solar
            // 
            this.checkBox_Solar.AutoSize = true;
            this.checkBox_Solar.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.checkBox_Solar.Location = new System.Drawing.Point(128, 305);
            this.checkBox_Solar.Name = "checkBox_Solar";
            this.checkBox_Solar.Size = new System.Drawing.Size(65, 25);
            this.checkBox_Solar.TabIndex = 4;
            this.checkBox_Solar.Text = "Solar";
            this.checkBox_Solar.UseVisualStyleBackColor = true;
            this.checkBox_Solar.CheckedChanged += new System.EventHandler(this.checkBox_Solar_CheckedChanged);
            // 
            // checkBox_PV
            // 
            this.checkBox_PV.AutoSize = true;
            this.checkBox_PV.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.checkBox_PV.Location = new System.Drawing.Point(128, 328);
            this.checkBox_PV.Name = "checkBox_PV";
            this.checkBox_PV.Size = new System.Drawing.Size(116, 25);
            this.checkBox_PV.TabIndex = 5;
            this.checkBox_PV.Text = "Photovoltaik";
            this.checkBox_PV.UseVisualStyleBackColor = true;
            this.checkBox_PV.CheckedChanged += new System.EventHandler(this.checkBox_PV_CheckedChanged);
            // 
            // checkBox_StromSp
            // 
            this.checkBox_StromSp.AutoSize = true;
            this.checkBox_StromSp.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.checkBox_StromSp.Location = new System.Drawing.Point(128, 351);
            this.checkBox_StromSp.Name = "checkBox_StromSp";
            this.checkBox_StromSp.Size = new System.Drawing.Size(129, 25);
            this.checkBox_StromSp.TabIndex = 6;
            this.checkBox_StromSp.Text = "Stromspeicher";
            this.checkBox_StromSp.UseVisualStyleBackColor = true;
            this.checkBox_StromSp.CheckedChanged += new System.EventHandler(this.checkBox_StromSp_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.DimGray;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(31, 244);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(460, 21);
            this.label3.TabIndex = 7;
            this.label3.Text = "Wärmeerzeuger bzw. Energieerzeuger  Komponenten auswählen:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox1.Image = global::WindowsFormsApplication1.Properties.Resources.Logo125_125;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(712, 106);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // checkBox_Prozess
            // 
            this.checkBox_Prozess.AutoSize = true;
            this.checkBox_Prozess.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.checkBox_Prozess.Location = new System.Drawing.Point(128, 374);
            this.checkBox_Prozess.Name = "checkBox_Prozess";
            this.checkBox_Prozess.Size = new System.Drawing.Size(130, 25);
            this.checkBox_Prozess.TabIndex = 8;
            this.checkBox_Prozess.Text = "Prozesswärme";
            this.checkBox_Prozess.UseVisualStyleBackColor = true;
            this.checkBox_Prozess.CheckedChanged += new System.EventHandler(this.checkBox_Prozess_CheckedChanged);
            // 
            // checkBox_Stromlastgang
            // 
            this.checkBox_Stromlastgang.AutoSize = true;
            this.checkBox_Stromlastgang.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.checkBox_Stromlastgang.Location = new System.Drawing.Point(128, 397);
            this.checkBox_Stromlastgang.Name = "checkBox_Stromlastgang";
            this.checkBox_Stromlastgang.Size = new System.Drawing.Size(130, 25);
            this.checkBox_Stromlastgang.TabIndex = 9;
            this.checkBox_Stromlastgang.Text = "Stromlastgang";
            this.checkBox_Stromlastgang.UseVisualStyleBackColor = true;
            this.checkBox_Stromlastgang.CheckedChanged += new System.EventHandler(this.checkBox_Stromlastgang_CheckedChanged);
            // 
            // checkBox_Kessel
            // 
            this.checkBox_Kessel.AutoSize = true;
            this.checkBox_Kessel.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.checkBox_Kessel.Location = new System.Drawing.Point(127, 420);
            this.checkBox_Kessel.Name = "checkBox_Kessel";
            this.checkBox_Kessel.Size = new System.Drawing.Size(122, 25);
            this.checkBox_Kessel.TabIndex = 10;
            this.checkBox_Kessel.Text = "Spitzenkessel";
            this.checkBox_Kessel.UseVisualStyleBackColor = true;
            this.checkBox_Kessel.CheckedChanged += new System.EventHandler(this.checkBox_Kessel_CheckedChanged);
            // 
            // Wizard_Komponenten
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(712, 453);
            this.Controls.Add(this.checkBox_Kessel);
            this.Controls.Add(this.checkBox_Stromlastgang);
            this.Controls.Add(this.checkBox_Prozess);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.checkBox_StromSp);
            this.Controls.Add(this.checkBox_PV);
            this.Controls.Add(this.checkBox_Solar);
            this.Controls.Add(this.checkBox_WP);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Wizard_Komponenten";
            this.Text = "frm1";
            this.Shown += new System.EventHandler(this.Wizard_Komponenten_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.CheckBox checkBox_WP;
        private System.Windows.Forms.CheckBox checkBox_Solar;
        private System.Windows.Forms.CheckBox checkBox_PV;
        private System.Windows.Forms.CheckBox checkBox_StromSp;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBox_Prozess;
        private System.Windows.Forms.CheckBox checkBox_Stromlastgang;
        private System.Windows.Forms.CheckBox checkBox_Kessel;
    }
}