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
            this.panel4 = new System.Windows.Forms.Panel();
            this.listBox_Betriebskosten = new System.Windows.Forms.ListBox();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.btn_Hinzu_Betriebskosten = new System.Windows.Forms.Button();
            this.flpContainer_Betriebskosten = new System.Windows.Forms.FlowLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.tabEnergie = new System.Windows.Forms.TabPage();
            this.panel8 = new System.Windows.Forms.Panel();
            this.btn_Delete = new System.Windows.Forms.Button();
            this.btn_Carrier = new System.Windows.Forms.Button();
            this.listBox_Energieträger = new System.Windows.Forms.ListBox();
            this.panel9 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.panel7 = new System.Windows.Forms.Panel();
            this.flpContainer_Energiekosten = new System.Windows.Forms.FlowLayoutPanel();
            this.label6 = new System.Windows.Forms.Label();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.label_Gesamt = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.label_ErzeugerGesamt = new System.Windows.Forms.Label();
            this.panel1_space = new System.Windows.Forms.Panel();
            this.tabMain.SuspendLayout();
            this.tabInvest.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tabWartung.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel6.SuspendLayout();
            this.tabEnergie.SuspendLayout();
            this.panel8.SuspendLayout();
            this.panel9.SuspendLayout();
            this.panel7.SuspendLayout();
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
            this.tabMain.Location = new System.Drawing.Point(0, 67);
            this.tabMain.Margin = new System.Windows.Forms.Padding(10);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(1015, 732);
            this.tabMain.TabIndex = 0;
            this.tabMain.SelectedIndexChanged += new System.EventHandler(this.tabMain_SelectedIndexChanged);
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
            this.tabInvest.Size = new System.Drawing.Size(1007, 698);
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
            this.label5.Size = new System.Drawing.Size(88, 17);
            this.label5.TabIndex = 35;
            this.label5.Text = "Energieträger";
            // 
            // flpContainer
            // 
            this.flpContainer.AutoScroll = true;
            this.flpContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpContainer.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpContainer.Location = new System.Drawing.Point(393, 26);
            this.flpContainer.Name = "flpContainer";
            this.flpContainer.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.flpContainer.Size = new System.Drawing.Size(594, 607);
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
            this.panel1.Size = new System.Drawing.Size(606, 665);
            this.panel1.TabIndex = 39;
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Enabled = false;
            this.btn_Hinzu.Location = new System.Drawing.Point(6, 621);
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
            this.tabWartung.Controls.Add(this.panel4);
            this.tabWartung.Controls.Add(this.panel6);
            this.tabWartung.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.tabWartung.Location = new System.Drawing.Point(4, 30);
            this.tabWartung.Name = "tabWartung";
            this.tabWartung.Size = new System.Drawing.Size(1007, 698);
            this.tabWartung.TabIndex = 1;
            this.tabWartung.Text = "Betriebskosten";
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.LightGray;
            this.panel4.Controls.Add(this.listBox_Betriebskosten);
            this.panel4.Controls.Add(this.panel5);
            this.panel4.Location = new System.Drawing.Point(17, 18);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(355, 200);
            this.panel4.TabIndex = 40;
            // 
            // listBox_Betriebskosten
            // 
            this.listBox_Betriebskosten.FormattingEnabled = true;
            this.listBox_Betriebskosten.ItemHeight = 17;
            this.listBox_Betriebskosten.Location = new System.Drawing.Point(6, 37);
            this.listBox_Betriebskosten.Name = "listBox_Betriebskosten";
            this.listBox_Betriebskosten.Size = new System.Drawing.Size(342, 157);
            this.listBox_Betriebskosten.TabIndex = 37;
            this.listBox_Betriebskosten.SelectedIndexChanged += new System.EventHandler(this.listBox_Betriebskosten_SelectedIndexChanged);
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.panel5.Controls.Add(this.label1);
            this.panel5.ForeColor = System.Drawing.Color.White;
            this.panel5.Location = new System.Drawing.Point(6, 7);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(343, 25);
            this.panel5.TabIndex = 38;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 17);
            this.label1.TabIndex = 35;
            this.label1.Text = "Energieträger";
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.LightGray;
            this.panel6.Controls.Add(this.btn_Hinzu_Betriebskosten);
            this.panel6.Controls.Add(this.flpContainer_Betriebskosten);
            this.panel6.Controls.Add(this.label2);
            this.panel6.Location = new System.Drawing.Point(387, 18);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(606, 665);
            this.panel6.TabIndex = 41;
            // 
            // btn_Hinzu_Betriebskosten
            // 
            this.btn_Hinzu_Betriebskosten.Enabled = false;
            this.btn_Hinzu_Betriebskosten.Location = new System.Drawing.Point(6, 621);
            this.btn_Hinzu_Betriebskosten.Name = "btn_Hinzu_Betriebskosten";
            this.btn_Hinzu_Betriebskosten.Size = new System.Drawing.Size(163, 33);
            this.btn_Hinzu_Betriebskosten.TabIndex = 4;
            this.btn_Hinzu_Betriebskosten.Text = "➕ Position Hinzufügen";
            this.btn_Hinzu_Betriebskosten.UseVisualStyleBackColor = true;
            this.btn_Hinzu_Betriebskosten.Click += new System.EventHandler(this.btn_Hinzu_Betriebskosten_Click);
            // 
            // flpContainer_Betriebskosten
            // 
            this.flpContainer_Betriebskosten.AutoScroll = true;
            this.flpContainer_Betriebskosten.BackColor = System.Drawing.SystemColors.Control;
            this.flpContainer_Betriebskosten.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpContainer_Betriebskosten.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpContainer_Betriebskosten.Location = new System.Drawing.Point(6, 8);
            this.flpContainer_Betriebskosten.Name = "flpContainer_Betriebskosten";
            this.flpContainer_Betriebskosten.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.flpContainer_Betriebskosten.Size = new System.Drawing.Size(594, 607);
            this.flpContainer_Betriebskosten.TabIndex = 37;
            this.flpContainer_Betriebskosten.Visible = false;
            this.flpContainer_Betriebskosten.WrapContents = false;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(209, 246);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(205, 74);
            this.label2.TabIndex = 0;
            this.label2.Text = "Energieerzeuger auswählen";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabEnergie
            // 
            this.tabEnergie.Controls.Add(this.panel8);
            this.tabEnergie.Controls.Add(this.panel7);
            this.tabEnergie.Location = new System.Drawing.Point(4, 30);
            this.tabEnergie.Name = "tabEnergie";
            this.tabEnergie.Size = new System.Drawing.Size(1007, 698);
            this.tabEnergie.TabIndex = 2;
            this.tabEnergie.Text = "Energiekosten";
            this.tabEnergie.UseVisualStyleBackColor = true;
            // 
            // panel8
            // 
            this.panel8.BackColor = System.Drawing.Color.LightGray;
            this.panel8.Controls.Add(this.btn_Delete);
            this.panel8.Controls.Add(this.btn_Carrier);
            this.panel8.Controls.Add(this.listBox_Energieträger);
            this.panel8.Controls.Add(this.panel9);
            this.panel8.Location = new System.Drawing.Point(17, 18);
            this.panel8.Name = "panel8";
            this.panel8.Size = new System.Drawing.Size(355, 601);
            this.panel8.TabIndex = 43;
            // 
            // btn_Delete
            // 
            this.btn_Delete.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Delete.Location = new System.Drawing.Point(220, 557);
            this.btn_Delete.Name = "btn_Delete";
            this.btn_Delete.Size = new System.Drawing.Size(128, 33);
            this.btn_Delete.TabIndex = 40;
            this.btn_Delete.Text = "🗑️ Löschen";
            this.btn_Delete.UseVisualStyleBackColor = true;
            this.btn_Delete.Click += new System.EventHandler(this.btn_Delete_Click);
            // 
            // btn_Carrier
            // 
            this.btn_Carrier.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Carrier.Location = new System.Drawing.Point(6, 557);
            this.btn_Carrier.Name = "btn_Carrier";
            this.btn_Carrier.Size = new System.Drawing.Size(128, 33);
            this.btn_Carrier.TabIndex = 39;
            this.btn_Carrier.Text = "➕ Hinzufügen...";
            this.btn_Carrier.UseVisualStyleBackColor = true;
            this.btn_Carrier.Click += new System.EventHandler(this.btn_Carrier_Click);
            // 
            // listBox_Energieträger
            // 
            this.listBox_Energieträger.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.listBox_Energieträger.FormattingEnabled = true;
            this.listBox_Energieträger.ItemHeight = 17;
            this.listBox_Energieträger.Location = new System.Drawing.Point(6, 37);
            this.listBox_Energieträger.Name = "listBox_Energieträger";
            this.listBox_Energieträger.Size = new System.Drawing.Size(342, 514);
            this.listBox_Energieträger.TabIndex = 37;
            this.listBox_Energieträger.SelectedIndexChanged += new System.EventHandler(this.listBox_Energieträger_SelectedIndexChanged);
            // 
            // panel9
            // 
            this.panel9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.panel9.Controls.Add(this.label4);
            this.panel9.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.panel9.ForeColor = System.Drawing.Color.White;
            this.panel9.Location = new System.Drawing.Point(6, 7);
            this.panel9.Name = "panel9";
            this.panel9.Size = new System.Drawing.Size(343, 25);
            this.panel9.TabIndex = 38;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 17);
            this.label4.TabIndex = 35;
            this.label4.Text = "Energieträger";
            // 
            // panel7
            // 
            this.panel7.BackColor = System.Drawing.Color.LightGray;
            this.panel7.Controls.Add(this.flpContainer_Energiekosten);
            this.panel7.Controls.Add(this.label6);
            this.panel7.Location = new System.Drawing.Point(387, 18);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(606, 665);
            this.panel7.TabIndex = 42;
            // 
            // flpContainer_Energiekosten
            // 
            this.flpContainer_Energiekosten.AutoScroll = true;
            this.flpContainer_Energiekosten.BackColor = System.Drawing.SystemColors.Control;
            this.flpContainer_Energiekosten.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpContainer_Energiekosten.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpContainer_Energiekosten.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.flpContainer_Energiekosten.Location = new System.Drawing.Point(6, 8);
            this.flpContainer_Energiekosten.Name = "flpContainer_Energiekosten";
            this.flpContainer_Energiekosten.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.flpContainer_Energiekosten.Size = new System.Drawing.Size(596, 651);
            this.flpContainer_Energiekosten.TabIndex = 37;
            this.flpContainer_Energiekosten.WrapContents = false;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(194, 258);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(205, 74);
            this.label6.TabIndex = 1;
            this.label6.Text = "Energieerzeuger auswählen";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlFooter.Controls.Add(this.label_Gesamt);
            this.pnlFooter.Controls.Add(this.btn_OK);
            this.pnlFooter.Controls.Add(this.label_ErzeugerGesamt);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 799);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(1015, 40);
            this.pnlFooter.TabIndex = 3;
            // 
            // label_Gesamt
            // 
            this.label_Gesamt.AutoSize = true;
            this.label_Gesamt.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Gesamt.ForeColor = System.Drawing.Color.White;
            this.label_Gesamt.Location = new System.Drawing.Point(393, 8);
            this.label_Gesamt.Name = "label_Gesamt";
            this.label_Gesamt.Size = new System.Drawing.Size(19, 21);
            this.label_Gesamt.TabIndex = 5;
            this.label_Gesamt.Text = "0";
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(915, 4);
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
            this.label_ErzeugerGesamt.Location = new System.Drawing.Point(18, 8);
            this.label_ErzeugerGesamt.Name = "label_ErzeugerGesamt";
            this.label_ErzeugerGesamt.Size = new System.Drawing.Size(19, 21);
            this.label_ErzeugerGesamt.TabIndex = 1;
            this.label_ErzeugerGesamt.Text = "0";
            // 
            // panel1_space
            // 
            this.panel1_space.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1_space.Location = new System.Drawing.Point(0, 52);
            this.panel1_space.Name = "panel1_space";
            this.panel1_space.Size = new System.Drawing.Size(1015, 15);
            this.panel1_space.TabIndex = 1;
            // 
            // Form_Kosten
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1015, 839);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.panel1_space);
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
            this.tabWartung.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.panel6.ResumeLayout(false);
            this.tabEnergie.ResumeLayout(false);
            this.panel8.ResumeLayout(false);
            this.panel9.ResumeLayout(false);
            this.panel9.PerformLayout();
            this.panel7.ResumeLayout(false);
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
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.ListBox listBox_Betriebskosten;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.FlowLayoutPanel flpContainer_Betriebskosten;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btn_Hinzu_Betriebskosten;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.FlowLayoutPanel flpContainer_Energiekosten;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.ListBox listBox_Energieträger;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btn_Carrier;
        private System.Windows.Forms.Button btn_Delete;
        private System.Windows.Forms.Label label6;
    }
}