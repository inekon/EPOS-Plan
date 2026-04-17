namespace WindowsFormsApplication1
{  
    partial class Form_KostenfaktorItem
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
            this.label_Item = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_Wert = new System.Windows.Forms.TextBox();
            this.textBox_Nutzungsdauer = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.textBox_Einheit = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_Gruppe = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label_Item
            // 
            this.label_Item.AutoSize = true;
            this.label_Item.Location = new System.Drawing.Point(16, 32);
            this.label_Item.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_Item.Name = "label_Item";
            this.label_Item.Size = new System.Drawing.Size(82, 17);
            this.label_Item.TabIndex = 3;
            this.label_Item.Text = "Kostenfaktor";
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(264, 215);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(88, 30);
            this.btn_OK.TabIndex = 4;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Location = new System.Drawing.Point(25, 215);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(88, 30);
            this.btn_Abbrechen.TabIndex = 5;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 163);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 17);
            this.label2.TabIndex = 9;
            this.label2.Text = "Wert";
            // 
            // textBox_Wert
            // 
            this.textBox_Wert.Location = new System.Drawing.Point(114, 160);
            this.textBox_Wert.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Wert.Name = "textBox_Wert";
            this.textBox_Wert.Size = new System.Drawing.Size(91, 25);
            this.textBox_Wert.TabIndex = 8;
            this.textBox_Wert.Text = "0";
            // 
            // textBox_Nutzungsdauer
            // 
            this.textBox_Nutzungsdauer.Location = new System.Drawing.Point(114, 96);
            this.textBox_Nutzungsdauer.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Nutzungsdauer.Name = "textBox_Nutzungsdauer";
            this.textBox_Nutzungsdauer.Size = new System.Drawing.Size(91, 25);
            this.textBox_Nutzungsdauer.TabIndex = 10;
            this.textBox_Nutzungsdauer.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 96);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 17);
            this.label3.TabIndex = 11;
            this.label3.Text = "Nutzungsdauer";
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(114, 32);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(238, 25);
            this.comboBox1.TabIndex = 12;
            // 
            // textBox_Einheit
            // 
            this.textBox_Einheit.Location = new System.Drawing.Point(114, 127);
            this.textBox_Einheit.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Einheit.Name = "textBox_Einheit";
            this.textBox_Einheit.Size = new System.Drawing.Size(91, 25);
            this.textBox_Einheit.TabIndex = 13;
            this.textBox_Einheit.Text = "€";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 130);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 17);
            this.label1.TabIndex = 14;
            this.label1.Text = "Einheit";
            // 
            // comboBox_Gruppe
            // 
            this.comboBox_Gruppe.FormattingEnabled = true;
            this.comboBox_Gruppe.Location = new System.Drawing.Point(114, 63);
            this.comboBox_Gruppe.Name = "comboBox_Gruppe";
            this.comboBox_Gruppe.Size = new System.Drawing.Size(238, 25);
            this.comboBox_Gruppe.TabIndex = 16;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 63);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 17);
            this.label4.TabIndex = 15;
            this.label4.Text = "Gruppe";
            // 
            // Form_KostenfaktorItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(370, 255);
            this.Controls.Add(this.comboBox_Gruppe);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Einheit);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Nutzungsdauer);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_Wert);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label_Item);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_KostenfaktorItem";
            this.Text = "Kostenfaktor ";
            this.Load += new System.EventHandler(this.Form_KostenfaktorItem_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Item;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_Wert;
        private System.Windows.Forms.TextBox textBox_Nutzungsdauer;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox textBox_Einheit;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_Gruppe;
        private System.Windows.Forms.Label label4;
    }
}