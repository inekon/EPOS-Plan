namespace WindowsFormsApplication1
{
    partial class Form_Kosten
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
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabInvest = new System.Windows.Forms.TabPage();
            this.panel3 = new System.Windows.Forms.Panel();
            this.listBox_Erzeuger = new System.Windows.Forms.ListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.flpContainer = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.tabWartung = new System.Windows.Forms.TabPage();
            this.tabEnergie = new System.Windows.Forms.TabPage();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlGlobal = new System.Windows.Forms.Panel();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.btn_OK = new System.Windows.Forms.Button();
            this.label_ErzeugerGesamt = new System.Windows.Forms.Label();
            this.panel1_space = new System.Windows.Forms.Panel();
            this.label_Gesamt = new System.Windows.Forms.Label();
            this.tabMain.SuspendLayout();
            this.tabInvest.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabInvest);
            this.tabMain.Controls.Add(this.tabWartung);
            this.tabMain.Controls.Add(this.tabEnergie);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.tabMain.Location = new System.Drawing.Point(0, 110);
            this.tabMain.Margin = new System.Windows.Forms.Padding(10);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(1015, 676);
            this.tabMain.TabIndex = 0;
            // 
            // tabInvest
            // 
            this.tabInvest.AutoScroll = true;
            this.tabInvest.Controls.Add(this.panel3);
            this.tabInvest.Controls.Add(this.flpContainer);
            this.tabInvest.Controls.Add(this.panel1);
            this.tabInvest.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabInvest.Location = new System.Drawing.Point(4, 30);
            this.tabInvest.Name = "tabInvest";
            this.tabInvest.Padding = new System.Windows.Forms.Padding(3);
            this.tabInvest.Size = new System.Drawing.Size(1007, 642);
            this.tabInvest.TabIndex = 0;
            this.tabInvest.Text = "Investitionskosten";
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.LightGray;
            this.panel3.Controls.Add(this.listBox_Erzeuger);
            this.panel3.Controls.Add(this.panel2);
            this.panel3.Location = new System.Drawing.Point(17, 18);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(355, 200);
            this.panel3.TabIndex = 36;
            // 
            // listBox_Erzeuger
            // 
            this.listBox_Erzeuger.FormattingEnabled = true;
            this.listBox_Erzeuger.ItemHeight = 17;
            this.listBox_Erzeuger.Location = new System.Drawing.Point(6, 37);
            this.listBox_Erzeuger.Name = "listBox_Erzeuger";
            this.listBox_Erzeuger.Size = new System.Drawing.Size(342, 157);
            this.listBox_Erzeuger.TabIndex = 37;
            this.listBox_Erzeuger.SelectedIndexChanged += new System.EventHandler(this.listBox_Erzeuger_SelectedIndexChanged);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.panel2.Controls.Add(this.label5);
            this.panel2.ForeColor = System.Drawing.Color.White;
            this.panel2.Location = new System.Drawing.Point(6, 7);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(343, 25);
            this.panel2.TabIndex = 38;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 17);
            this.label5.TabIndex = 35;
            this.label5.Text = "Energieerzeuger";
            // 
            // flpContainer
            // 
            this.flpContainer.AutoScroll = true;
            this.flpContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpContainer.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpContainer.Location = new System.Drawing.Point(393, 26);
            this.flpContainer.Name = "flpContainer";
            this.flpContainer.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.flpContainer.Size = new System.Drawing.Size(594, 560);
            this.flpContainer.TabIndex = 36;
            this.flpContainer.Visible = false;
            this.flpContainer.WrapContents = false;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.LightGray;
            this.panel1.Controls.Add(this.btn_Hinzu);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Location = new System.Drawing.Point(387, 18);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(606, 618);
            this.panel1.TabIndex = 39;
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Enabled = false;
            this.btn_Hinzu.Location = new System.Drawing.Point(6, 574);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(163, 33);
            this.btn_Hinzu.TabIndex = 4;
            this.btn_Hinzu.Text = "➕ Position Hinzufügen";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(209, 246);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(205, 74);
            this.label3.TabIndex = 0;
            this.label3.Text = "Energieerzeuger auswählen";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabWartung
            // 
            this.tabWartung.Location = new System.Drawing.Point(4, 30);
            this.tabWartung.Name = "tabWartung";
            this.tabWartung.Size = new System.Drawing.Size(1007, 642);
            this.tabWartung.TabIndex = 1;
            this.tabWartung.Text = "Betriebskosten";
            // 
            // tabEnergie
            // 
            this.tabEnergie.Location = new System.Drawing.Point(4, 30);
            this.tabEnergie.Name = "tabEnergie";
            this.tabEnergie.Size = new System.Drawing.Size(1007, 642);
            this.tabEnergie.TabIndex = 2;
            this.tabEnergie.Text = "Energiekosten";
            this.tabEnergie.UseVisualStyleBackColor = true;
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(1015, 52);
            this.pnlHeader.TabIndex = 1;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(17, 13);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(175, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Kostenverwaltung";
            // 
            // pnlGlobal
            // 
            this.pnlGlobal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.pnlGlobal.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGlobal.Location = new System.Drawing.Point(0, 52);
            this.pnlGlobal.Name = "pnlGlobal";
            this.pnlGlobal.Size = new System.Drawing.Size(1015, 43);
            this.pnlGlobal.TabIndex = 2;
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlFooter.Controls.Add(this.label_Gesamt);
            this.pnlFooter.Controls.Add(this.btn_OK);
            this.pnlFooter.Controls.Add(this.label_ErzeugerGesamt);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 786);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(1015, 53);
            this.pnlFooter.TabIndex = 3;
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(915, 12);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 33);
            this.btn_OK.TabIndex = 3;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // label_ErzeugerGesamt
            // 
            this.label_ErzeugerGesamt.AutoSize = true;
            this.label_ErzeugerGesamt.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_ErzeugerGesamt.ForeColor = System.Drawing.Color.White;
            this.label_ErzeugerGesamt.Location = new System.Drawing.Point(18, 16);
            this.label_ErzeugerGesamt.Name = "label_ErzeugerGesamt";
            this.label_ErzeugerGesamt.Size = new System.Drawing.Size(19, 21);
            this.label_ErzeugerGesamt.TabIndex = 1;
            this.label_ErzeugerGesamt.Text = "0";
            // 
            // panel1_space
            // 
            this.panel1_space.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1_space.Location = new System.Drawing.Point(0, 95);
            this.panel1_space.Name = "panel1_space";
            this.panel1_space.Size = new System.Drawing.Size(1015, 15);
            this.panel1_space.TabIndex = 1;
            // 
            // label_Gesamt
            // 
            this.label_Gesamt.AutoSize = true;
            this.label_Gesamt.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Gesamt.ForeColor = System.Drawing.Color.White;
            this.label_Gesamt.Location = new System.Drawing.Point(281, 16);
            this.label_Gesamt.Name = "label_Gesamt";
            this.label_Gesamt.Size = new System.Drawing.Size(19, 21);
            this.label_Gesamt.TabIndex = 5;
            this.label_Gesamt.Text = "0";
            // 
            // Form_Kosten
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1015, 839);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.panel1_space);
            this.Controls.Add(this.pnlGlobal);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlFooter);
            this.Name = "Form_Kosten";
            this.Text = "Kosteneditor";
            this.tabMain.ResumeLayout(false);
            this.tabInvest.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabInvest;
        private System.Windows.Forms.TabPage tabWartung;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlGlobal;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.TabPage tabEnergie;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label label_ErzeugerGesamt;
        private System.Windows.Forms.Panel panel1_space;
        private System.Windows.Forms.FlowLayoutPanel flpContainer;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox listBox_Erzeuger;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btn_Hinzu;
        private System.Windows.Forms.Label label_Gesamt;
    }
}