namespace WindowsFormsApplication1
{
    partial class Form_KonfigPufferspeicher
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_KonfigPufferspeicher));
            this.comboBox_Erzeuger = new System.Windows.Forms.ComboBox();
            this.comboBox_Puffer = new System.Windows.Forms.ComboBox();
            this.textBox_Vorlauf = new System.Windows.Forms.TextBox();
            this.textBox_Ruecklauf = new System.Windows.Forms.TextBox();
            this.btn_Abbruch = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // comboBox_Erzeuger
            // 
            resources.ApplyResources(this.comboBox_Erzeuger, "comboBox_Erzeuger");
            this.comboBox_Erzeuger.FormattingEnabled = true;
            this.comboBox_Erzeuger.Name = "comboBox_Erzeuger";
            // 
            // comboBox_Puffer
            // 
            resources.ApplyResources(this.comboBox_Puffer, "comboBox_Puffer");
            this.comboBox_Puffer.FormattingEnabled = true;
            this.comboBox_Puffer.Name = "comboBox_Puffer";
            this.comboBox_Puffer.SelectedIndexChanged += new System.EventHandler(this.comboBox_Puffer_SelectedIndexChanged);
            // 
            // textBox_Vorlauf
            // 
            resources.ApplyResources(this.textBox_Vorlauf, "textBox_Vorlauf");
            this.textBox_Vorlauf.Name = "textBox_Vorlauf";
            // 
            // textBox_Ruecklauf
            // 
            resources.ApplyResources(this.textBox_Ruecklauf, "textBox_Ruecklauf");
            this.textBox_Ruecklauf.Name = "textBox_Ruecklauf";
            // 
            // btn_Abbruch
            // 
            resources.ApplyResources(this.btn_Abbruch, "btn_Abbruch");
            this.btn_Abbruch.Name = "btn_Abbruch";
            this.btn_Abbruch.UseVisualStyleBackColor = true;
            this.btn_Abbruch.Click += new System.EventHandler(this.btn_Abbruch_Click);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // Form_KonfigPufferspeicher
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Abbruch);
            this.Controls.Add(this.textBox_Ruecklauf);
            this.Controls.Add(this.textBox_Vorlauf);
            this.Controls.Add(this.comboBox_Puffer);
            this.Controls.Add(this.comboBox_Erzeuger);
            this.Name = "Form_KonfigPufferspeicher";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox_Erzeuger;
        private System.Windows.Forms.ComboBox comboBox_Puffer;
        private System.Windows.Forms.TextBox textBox_Vorlauf;
        private System.Windows.Forms.TextBox textBox_Ruecklauf;
        private System.Windows.Forms.Button btn_Abbruch;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
    }
}