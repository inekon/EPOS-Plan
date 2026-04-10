namespace WindowsFormsApplication1
{
    partial class Form_StromTest
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
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_Stromspeicher = new System.Windows.Forms.ComboBox();
            this.comboBox_Lastgang = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.comboBox_Stromprofil = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btn_Simulation_Lastgang = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox_Kuehlung = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Stromspeicher";
            // 
            // comboBox_Stromspeicher
            // 
            this.comboBox_Stromspeicher.FormattingEnabled = true;
            this.comboBox_Stromspeicher.Location = new System.Drawing.Point(89, 15);
            this.comboBox_Stromspeicher.Name = "comboBox_Stromspeicher";
            this.comboBox_Stromspeicher.Size = new System.Drawing.Size(121, 21);
            this.comboBox_Stromspeicher.TabIndex = 1;
            // 
            // comboBox_Lastgang
            // 
            this.comboBox_Lastgang.FormattingEnabled = true;
            this.comboBox_Lastgang.Location = new System.Drawing.Point(346, 45);
            this.comboBox_Lastgang.Name = "comboBox_Lastgang";
            this.comboBox_Lastgang.Size = new System.Drawing.Size(121, 21);
            this.comboBox_Lastgang.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(266, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "oder Lastgang";
            // 
            // btn_Simulation
            // 
            this.btn_Simulation.Location = new System.Drawing.Point(32, 48);
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.Size = new System.Drawing.Size(178, 29);
            this.btn_Simulation.TabIndex = 4;
            this.btn_Simulation.Text = "Simulation mit Profil";
            this.btn_Simulation.UseVisualStyleBackColor = true;
            this.btn_Simulation.Click += new System.EventHandler(this.btn_Simulation_Click);
            // 
            // comboBox_Stromprofil
            // 
            this.comboBox_Stromprofil.FormattingEnabled = true;
            this.comboBox_Stromprofil.Location = new System.Drawing.Point(346, 18);
            this.comboBox_Stromprofil.Name = "comboBox_Stromprofil";
            this.comboBox_Stromprofil.Size = new System.Drawing.Size(121, 21);
            this.comboBox_Stromprofil.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(250, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Strombedarf Profil";
            // 
            // btn_Simulation_Lastgang
            // 
            this.btn_Simulation_Lastgang.Location = new System.Drawing.Point(32, 78);
            this.btn_Simulation_Lastgang.Name = "btn_Simulation_Lastgang";
            this.btn_Simulation_Lastgang.Size = new System.Drawing.Size(178, 29);
            this.btn_Simulation_Lastgang.TabIndex = 7;
            this.btn_Simulation_Lastgang.Text = "Simulation mit Lastgang";
            this.btn_Simulation_Lastgang.UseVisualStyleBackColor = true;
            this.btn_Simulation_Lastgang.Click += new System.EventHandler(this.btn_Simulation_Lastgang_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(32, 149);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(178, 31);
            this.button1.TabIndex = 8;
            this.button1.Text = "Kühlkenndaten";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox_Kuehlung
            // 
            this.textBox_Kuehlung.Location = new System.Drawing.Point(32, 123);
            this.textBox_Kuehlung.Name = "textBox_Kuehlung";
            this.textBox_Kuehlung.Size = new System.Drawing.Size(330, 20);
            this.textBox_Kuehlung.TabIndex = 9;
            this.textBox_Kuehlung.Text = "WP CHA-Monoblock 20/24 (400V) mit E-Heizelement";
            // 
            // Form_StromTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(521, 190);
            this.Controls.Add(this.textBox_Kuehlung);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btn_Simulation_Lastgang);
            this.Controls.Add(this.comboBox_Stromprofil);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.comboBox_Lastgang);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_Stromspeicher);
            this.Controls.Add(this.label1);
            this.Name = "Form_StromTest";
            this.Text = "Form_StromTest";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_Stromspeicher;
        private System.Windows.Forms.ComboBox comboBox_Lastgang;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btn_Simulation;
        private System.Windows.Forms.ComboBox comboBox_Stromprofil;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btn_Simulation_Lastgang;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox_Kuehlung;
    }
}