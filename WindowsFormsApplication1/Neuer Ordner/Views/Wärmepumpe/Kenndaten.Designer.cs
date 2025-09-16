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
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(227, 24);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(423, 224);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.checkValue);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(12, 43);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(182, 173);
            this.listBox1.TabIndex = 1;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // btn_ItemNeu
            // 
            this.btn_ItemNeu.Location = new System.Drawing.Point(212, 27);
            this.btn_ItemNeu.Name = "btn_ItemNeu";
            this.btn_ItemNeu.Size = new System.Drawing.Size(165, 34);
            this.btn_ItemNeu.TabIndex = 2;
            this.btn_ItemNeu.Text = "Daten übernehmen";
            this.btn_ItemNeu.UseVisualStyleBackColor = true;
            this.btn_ItemNeu.Click += new System.EventHandler(this.btn_ItemNeu_Click);
            // 
            // textBox_NeuVorlauf
            // 
            this.textBox_NeuVorlauf.Location = new System.Drawing.Point(152, 222);
            this.textBox_NeuVorlauf.Name = "textBox_NeuVorlauf";
            this.textBox_NeuVorlauf.Size = new System.Drawing.Size(42, 20);
            this.textBox_NeuVorlauf.TabIndex = 8;
            // 
            // btn_NeuVorlauf
            // 
            this.btn_NeuVorlauf.Location = new System.Drawing.Point(13, 222);
            this.btn_NeuVorlauf.Name = "btn_NeuVorlauf";
            this.btn_NeuVorlauf.Size = new System.Drawing.Size(133, 23);
            this.btn_NeuVorlauf.TabIndex = 7;
            this.btn_NeuVorlauf.Text = "Neue Vorlauftemperatur";
            this.btn_NeuVorlauf.UseVisualStyleBackColor = true;
            this.btn_NeuVorlauf.Click += new System.EventHandler(this.btn_NeuVorlauf_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(577, 382);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(73, 27);
            this.btn_OK.TabIndex = 12;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Abbruch
            // 
            this.btn_Abbruch.Location = new System.Drawing.Point(489, 382);
            this.btn_Abbruch.Name = "btn_Abbruch";
            this.btn_Abbruch.Size = new System.Drawing.Size(73, 27);
            this.btn_Abbruch.TabIndex = 13;
            this.btn_Abbruch.Text = "Abbruch";
            this.btn_Abbruch.UseVisualStyleBackColor = true;
            this.btn_Abbruch.Click += new System.EventHandler(this.btn_Abbruch_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 24);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(110, 13);
            this.label4.TabIndex = 14;
            this.label4.Text = "Vorlauftemperatur [°C]";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.textBox_Ptherm);
            this.groupBox1.Controls.Add(this.btn_ItemNeu);
            this.groupBox1.Controls.Add(this.textBox_COP);
            this.groupBox1.Controls.Add(this.textBox_Temperatur);
            this.groupBox1.Location = new System.Drawing.Point(227, 254);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(391, 109);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Neue Stützstelle";
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Black;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(165, 29);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(33, 19);
            this.label5.TabIndex = 91;
            this.label5.Text = "°C";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.Black;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(165, 79);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(33, 19);
            this.label13.TabIndex = 90;
            this.label13.Text = "kW";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 13);
            this.label3.TabIndex = 89;
            this.label3.Text = "Ptherm";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 88;
            this.label2.Text = "COP";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 87;
            this.label1.Text = "Temperatur";
            // 
            // textBox_Ptherm
            // 
            this.textBox_Ptherm.Location = new System.Drawing.Point(84, 79);
            this.textBox_Ptherm.Name = "textBox_Ptherm";
            this.textBox_Ptherm.Size = new System.Drawing.Size(75, 20);
            this.textBox_Ptherm.TabIndex = 86;
            // 
            // textBox_COP
            // 
            this.textBox_COP.Location = new System.Drawing.Point(84, 53);
            this.textBox_COP.Name = "textBox_COP";
            this.textBox_COP.Size = new System.Drawing.Size(75, 20);
            this.textBox_COP.TabIndex = 85;
            // 
            // textBox_Temperatur
            // 
            this.textBox_Temperatur.Location = new System.Drawing.Point(84, 27);
            this.textBox_Temperatur.Name = "textBox_Temperatur";
            this.textBox_Temperatur.Size = new System.Drawing.Size(75, 20);
            this.textBox_Temperatur.TabIndex = 84;
            // 
            // Kenndaten
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 421);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btn_Abbruch);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.textBox_NeuVorlauf);
            this.Controls.Add(this.btn_NeuVorlauf);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "Kenndaten";
            this.Text = "Kenndaten";
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