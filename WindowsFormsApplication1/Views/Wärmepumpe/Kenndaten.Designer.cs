namespace WindowsFormsApplication1
{
    partial class Kenndaten
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Kenndaten));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btn_ItemNeu = new System.Windows.Forms.Button();
            this.textBox_NeuVorlauf = new System.Windows.Forms.TextBox();
            this.btn_NeuVorlauf = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Abbruch = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Ptherm = new System.Windows.Forms.TextBox();
            this.textBox_COP = new System.Windows.Forms.TextBox();
            this.textBox_Temperatur = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            resources.ApplyResources(this.dataGridView1, "dataGridView1");
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.checkValue);
            // 
            // listBox1
            // 
            resources.ApplyResources(this.listBox1, "listBox1");
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Name = "listBox1";
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // btn_ItemNeu
            // 
            resources.ApplyResources(this.btn_ItemNeu, "btn_ItemNeu");
            this.btn_ItemNeu.Name = "btn_ItemNeu";
            this.btn_ItemNeu.UseVisualStyleBackColor = true;
            this.btn_ItemNeu.Click += new System.EventHandler(this.btn_ItemNeu_Click);
            // 
            // textBox_NeuVorlauf
            // 
            resources.ApplyResources(this.textBox_NeuVorlauf, "textBox_NeuVorlauf");
            this.textBox_NeuVorlauf.Name = "textBox_NeuVorlauf";
            // 
            // btn_NeuVorlauf
            // 
            resources.ApplyResources(this.btn_NeuVorlauf, "btn_NeuVorlauf");
            this.btn_NeuVorlauf.Name = "btn_NeuVorlauf";
            this.btn_NeuVorlauf.UseVisualStyleBackColor = true;
            this.btn_NeuVorlauf.Click += new System.EventHandler(this.btn_NeuVorlauf_Click);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Abbruch
            // 
            resources.ApplyResources(this.btn_Abbruch, "btn_Abbruch");
            this.btn_Abbruch.Name = "btn_Abbruch";
            this.btn_Abbruch.UseVisualStyleBackColor = true;
            this.btn_Abbruch.Click += new System.EventHandler(this.btn_Abbruch_Click);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.textBox_Ptherm);
            this.groupBox1.Controls.Add(this.btn_ItemNeu);
            this.groupBox1.Controls.Add(this.textBox_COP);
            this.groupBox1.Controls.Add(this.textBox_Temperatur);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.BackColor = System.Drawing.Color.Black;
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Name = "label5";
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.BackColor = System.Drawing.Color.Black;
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Name = "label13";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // textBox_Ptherm
            // 
            resources.ApplyResources(this.textBox_Ptherm, "textBox_Ptherm");
            this.textBox_Ptherm.Name = "textBox_Ptherm";
            // 
            // textBox_COP
            // 
            resources.ApplyResources(this.textBox_COP, "textBox_COP");
            this.textBox_COP.Name = "textBox_COP";
            // 
            // textBox_Temperatur
            // 
            resources.ApplyResources(this.textBox_Temperatur, "textBox_Temperatur");
            this.textBox_Temperatur.Name = "textBox_Temperatur";
            // 
            // Kenndaten
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btn_Abbruch);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.textBox_NeuVorlauf);
            this.Controls.Add(this.btn_NeuVorlauf);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "Kenndaten";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button btn_ItemNeu;
        private System.Windows.Forms.TextBox textBox_NeuVorlauf;
        private System.Windows.Forms.Button btn_NeuVorlauf;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Abbruch;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_Ptherm;
        private System.Windows.Forms.TextBox textBox_COP;
        private System.Windows.Forms.TextBox textBox_Temperatur;
    }
}