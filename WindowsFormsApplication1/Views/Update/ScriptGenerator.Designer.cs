namespace WindowsFormsApplication1
{
    partial class Form_ScriptGenerator
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.cmbAktion = new System.Windows.Forms.ComboBox();
            this.txtTabelle = new System.Windows.Forms.TextBox();
            this.txtFeldAlt = new System.Windows.Forms.TextBox();
            this.txtFeldNeu = new System.Windows.Forms.TextBox();
            this.txtDatentyp = new System.Windows.Forms.TextBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.rtbOutput = new System.Windows.Forms.RichTextBox();
            this.lblTabelle = new System.Windows.Forms.Label();
            this.lblFeldAlt = new System.Windows.Forms.Label();
            this.lblFeldNeu = new System.Windows.Forms.Label();
            this.lblTyp = new System.Windows.Forms.Label();
            this.btnCopy = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cmbAktion
            // 
            this.cmbAktion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAktion.FormattingEnabled = true;
            this.cmbAktion.Location = new System.Drawing.Point(46, 12);
            this.cmbAktion.Name = "cmbAktion";
            this.cmbAktion.Size = new System.Drawing.Size(260, 21);
            this.cmbAktion.TabIndex = 0;
            // 
            // txtTabelle
            // 
            this.txtTabelle.Location = new System.Drawing.Point(46, 75);
            this.txtTabelle.Name = "txtTabelle";
            this.txtTabelle.Size = new System.Drawing.Size(125, 20);
            this.txtTabelle.TabIndex = 1;
            // 
            // txtFeldAlt
            // 
            this.txtFeldAlt.Location = new System.Drawing.Point(177, 75);
            this.txtFeldAlt.Name = "txtFeldAlt";
            this.txtFeldAlt.Size = new System.Drawing.Size(125, 20);
            this.txtFeldAlt.TabIndex = 2;
            // 
            // txtFeldNeu
            // 
            this.txtFeldNeu.Location = new System.Drawing.Point(46, 142);
            this.txtFeldNeu.Name = "txtFeldNeu";
            this.txtFeldNeu.Size = new System.Drawing.Size(125, 20);
            this.txtFeldNeu.TabIndex = 3;
            // 
            // txtDatentyp
            // 
            this.txtDatentyp.Location = new System.Drawing.Point(177, 142);
            this.txtDatentyp.Multiline = true;
            this.txtDatentyp.Name = "txtDatentyp";
            this.txtDatentyp.Size = new System.Drawing.Size(129, 136);
            this.txtDatentyp.TabIndex = 4;
            this.txtDatentyp.Text = "TEXT(255)";
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(46, 284);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(260, 30);
            this.btnGenerate.TabIndex = 5;
            this.btnGenerate.Text = "Script generieren";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // rtbOutput
            // 
            this.rtbOutput.Location = new System.Drawing.Point(42, 320);
            this.rtbOutput.Name = "rtbOutput";
            this.rtbOutput.Size = new System.Drawing.Size(260, 150);
            this.rtbOutput.TabIndex = 6;
            this.rtbOutput.Text = "";
            // 
            // lblTabelle
            // 
            this.lblTabelle.Location = new System.Drawing.Point(46, 49);
            this.lblTabelle.Name = "lblTabelle";
            this.lblTabelle.Size = new System.Drawing.Size(100, 23);
            this.lblTabelle.TabIndex = 11;
            this.lblTabelle.Text = "Tabelle:";
            // 
            // lblFeldAlt
            // 
            this.lblFeldAlt.Location = new System.Drawing.Point(177, 49);
            this.lblFeldAlt.Name = "lblFeldAlt";
            this.lblFeldAlt.Size = new System.Drawing.Size(100, 23);
            this.lblFeldAlt.TabIndex = 10;
            this.lblFeldAlt.Text = "Feld Alt:";
            // 
            // lblFeldNeu
            // 
            this.lblFeldNeu.Location = new System.Drawing.Point(46, 116);
            this.lblFeldNeu.Name = "lblFeldNeu";
            this.lblFeldNeu.Size = new System.Drawing.Size(100, 23);
            this.lblFeldNeu.TabIndex = 9;
            this.lblFeldNeu.Text = "Feld Neu:";
            // 
            // lblTyp
            // 
            this.lblTyp.Location = new System.Drawing.Point(174, 116);
            this.lblTyp.Name = "lblTyp";
            this.lblTyp.Size = new System.Drawing.Size(100, 23);
            this.lblTyp.TabIndex = 8;
            this.lblTyp.Text = "Datentyp:";
            // 
            // btnCopy
            // 
            this.btnCopy.Location = new System.Drawing.Point(42, 476);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(260, 23);
            this.btnCopy.TabIndex = 7;
            this.btnCopy.Text = "In Zwischenablage kopieren";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // Form_ScriptGenerator
            // 
            this.ClientSize = new System.Drawing.Size(336, 511);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.lblTyp);
            this.Controls.Add(this.lblFeldNeu);
            this.Controls.Add(this.lblFeldAlt);
            this.Controls.Add(this.lblTabelle);
            this.Controls.Add(this.rtbOutput);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.txtDatentyp);
            this.Controls.Add(this.txtFeldNeu);
            this.Controls.Add(this.txtFeldAlt);
            this.Controls.Add(this.txtTabelle);
            this.Controls.Add(this.cmbAktion);
            this.Name = "Form_ScriptGenerator";
            this.Text = "Admin Script Generator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.ComboBox cmbAktion;
        private System.Windows.Forms.TextBox txtTabelle;
        private System.Windows.Forms.TextBox txtFeldAlt;
        private System.Windows.Forms.TextBox txtFeldNeu;
        private System.Windows.Forms.TextBox txtDatentyp;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.RichTextBox rtbOutput;
        private System.Windows.Forms.Label lblTabelle;
        private System.Windows.Forms.Label lblFeldAlt;
        private System.Windows.Forms.Label lblFeldNeu;
        private System.Windows.Forms.Label lblTyp;
        private System.Windows.Forms.Button btnCopy;
    }
}