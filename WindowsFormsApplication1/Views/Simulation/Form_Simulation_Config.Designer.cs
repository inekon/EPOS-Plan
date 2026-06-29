namespace WindowsFormsApplication1
{
    partial class Form_Simulation_Config
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Simulation_Config));
            label7 = new System.Windows.Forms.Label();
            btn_Loeschen = new System.Windows.Forms.Button();
            btn_Hinzu = new System.Windows.Forms.Button();
            listView1 = new System.Windows.Forms.ListView();
            label21 = new System.Windows.Forms.Label();
            btn_OK = new System.Windows.Forms.Button();
            btn_Speichern = new System.Windows.Forms.Button();
            label12 = new System.Windows.Forms.Label();
            label11 = new System.Windows.Forms.Label();
            checkBox6 = new System.Windows.Forms.CheckBox();
            checkBox5 = new System.Windows.Forms.CheckBox();
            checkBox4 = new System.Windows.Forms.CheckBox();
            checkBox3 = new System.Windows.Forms.CheckBox();
            checkBox2 = new System.Windows.Forms.CheckBox();
            checkBox1 = new System.Windows.Forms.CheckBox();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            comboBox6 = new System.Windows.Forms.ComboBox();
            comboBox5 = new System.Windows.Forms.ComboBox();
            comboBox4 = new System.Windows.Forms.ComboBox();
            comboBox3 = new System.Windows.Forms.ComboBox();
            comboBox2 = new System.Windows.Forms.ComboBox();
            comboBox1 = new System.Windows.Forms.ComboBox();
            groupBox_Tools = new System.Windows.Forms.GroupBox();
            groupBox_PufferSp = new System.Windows.Forms.GroupBox();
            checkBox_PufferSp = new System.Windows.Forms.CheckBox();
            lblStatus = new System.Windows.Forms.Label();
            groupBox_Tools.SuspendLayout();
            groupBox_PufferSp.SuspendLayout();
            SuspendLayout();
            // 
            // label7
            // 
            resources.ApplyResources(label7, "label7");
            label7.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
            label7.ForeColor = System.Drawing.Color.Black;
            label7.Name = "label7";
            // 
            // btn_Loeschen
            // 
            resources.ApplyResources(btn_Loeschen, "btn_Loeschen");
            btn_Loeschen.Name = "btn_Loeschen";
            btn_Loeschen.UseVisualStyleBackColor = true;
            btn_Loeschen.Click += btn_Loeschen_Click;
            // 
            // btn_Hinzu
            // 
            resources.ApplyResources(btn_Hinzu, "btn_Hinzu");
            btn_Hinzu.Name = "btn_Hinzu";
            btn_Hinzu.UseVisualStyleBackColor = true;
            btn_Hinzu.Click += btn_Hinzu_Click;
            // 
            // listView1
            // 
            resources.ApplyResources(listView1, "listView1");
            listView1.Name = "listView1";
            listView1.OwnerDraw = true;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.DrawColumnHeader += listView1_DrawColumnHeader;
            listView1.DrawItem += listView1_DrawItem;
            listView1.DrawSubItem += listView1_DrawSubItem;
            // 
            // label21
            // 
            resources.ApplyResources(label21, "label21");
            label21.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
            label21.ForeColor = System.Drawing.Color.Black;
            label21.Name = "label21";
            // 
            // btn_OK
            // 
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // btn_Speichern
            // 
            btn_Speichern.Image = Properties.Resources.save_icon_36513;
            resources.ApplyResources(btn_Speichern, "btn_Speichern");
            btn_Speichern.Name = "btn_Speichern";
            btn_Speichern.UseVisualStyleBackColor = true;
            btn_Speichern.Click += btn_Speichern_Click;
            // 
            // label12
            // 
            resources.ApplyResources(label12, "label12");
            label12.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
            label12.ForeColor = System.Drawing.Color.Black;
            label12.Name = "label12";
            // 
            // label11
            // 
            resources.ApplyResources(label11, "label11");
            label11.BackColor = System.Drawing.SystemColors.Control;
            label11.ForeColor = System.Drawing.Color.Black;
            label11.Name = "label11";
            // 
            // checkBox6
            // 
            resources.ApplyResources(checkBox6, "checkBox6");
            checkBox6.Name = "checkBox6";
            checkBox6.UseVisualStyleBackColor = true;
            checkBox6.CheckedChanged += checkBox6_CheckedChanged;
            // 
            // checkBox5
            // 
            resources.ApplyResources(checkBox5, "checkBox5");
            checkBox5.Name = "checkBox5";
            checkBox5.UseVisualStyleBackColor = true;
            checkBox5.CheckedChanged += checkBox5_CheckedChanged;
            // 
            // checkBox4
            // 
            resources.ApplyResources(checkBox4, "checkBox4");
            checkBox4.Name = "checkBox4";
            checkBox4.UseVisualStyleBackColor = true;
            checkBox4.CheckedChanged += checkBox4_CheckedChanged;
            // 
            // checkBox3
            // 
            resources.ApplyResources(checkBox3, "checkBox3");
            checkBox3.Name = "checkBox3";
            checkBox3.UseVisualStyleBackColor = true;
            checkBox3.CheckedChanged += checkBox3_CheckedChanged;
            // 
            // checkBox2
            // 
            resources.ApplyResources(checkBox2, "checkBox2");
            checkBox2.Name = "checkBox2";
            checkBox2.UseVisualStyleBackColor = true;
            checkBox2.CheckedChanged += checkBox2_CheckedChanged;
            // 
            // checkBox1
            // 
            resources.ApplyResources(checkBox1, "checkBox1");
            checkBox1.Name = "checkBox1";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // comboBox6
            // 
            comboBox6.FormattingEnabled = true;
            resources.ApplyResources(comboBox6, "comboBox6");
            comboBox6.Name = "comboBox6";
            comboBox6.SelectedIndexChanged += comboBox5_SelectedIndexChanged;
            // 
            // comboBox5
            // 
            comboBox5.FormattingEnabled = true;
            resources.ApplyResources(comboBox5, "comboBox5");
            comboBox5.Name = "comboBox5";
            // 
            // comboBox4
            // 
            comboBox4.FormattingEnabled = true;
            resources.ApplyResources(comboBox4, "comboBox4");
            comboBox4.Name = "comboBox4";
            // 
            // comboBox3
            // 
            comboBox3.FormattingEnabled = true;
            resources.ApplyResources(comboBox3, "comboBox3");
            comboBox3.Name = "comboBox3";
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            resources.ApplyResources(comboBox2, "comboBox2");
            comboBox2.Name = "comboBox2";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            resources.ApplyResources(comboBox1, "comboBox1");
            comboBox1.Name = "comboBox1";
            // 
            // groupBox_Tools
            // 
            groupBox_Tools.Controls.Add(comboBox5);
            groupBox_Tools.Controls.Add(comboBox1);
            groupBox_Tools.Controls.Add(comboBox2);
            groupBox_Tools.Controls.Add(comboBox3);
            groupBox_Tools.Controls.Add(comboBox4);
            groupBox_Tools.Controls.Add(comboBox6);
            groupBox_Tools.Controls.Add(label1);
            groupBox_Tools.Controls.Add(label2);
            groupBox_Tools.Controls.Add(label3);
            groupBox_Tools.Controls.Add(checkBox1);
            groupBox_Tools.Controls.Add(checkBox2);
            groupBox_Tools.Controls.Add(checkBox3);
            groupBox_Tools.Controls.Add(checkBox4);
            groupBox_Tools.Controls.Add(checkBox5);
            groupBox_Tools.Controls.Add(checkBox6);
            resources.ApplyResources(groupBox_Tools, "groupBox_Tools");
            groupBox_Tools.Name = "groupBox_Tools";
            groupBox_Tools.TabStop = false;
            // 
            // groupBox_PufferSp
            // 
            groupBox_PufferSp.Controls.Add(listView1);
            groupBox_PufferSp.Controls.Add(label7);
            groupBox_PufferSp.Controls.Add(btn_Hinzu);
            groupBox_PufferSp.Controls.Add(btn_Loeschen);
            resources.ApplyResources(groupBox_PufferSp, "groupBox_PufferSp");
            groupBox_PufferSp.Name = "groupBox_PufferSp";
            groupBox_PufferSp.TabStop = false;
            // 
            // checkBox_PufferSp
            // 
            resources.ApplyResources(checkBox_PufferSp, "checkBox_PufferSp");
            checkBox_PufferSp.Name = "checkBox_PufferSp";
            checkBox_PufferSp.UseVisualStyleBackColor = true;
            checkBox_PufferSp.CheckedChanged += checkBox_PufferSp_CheckedChanged;
            // 
            // lblStatus
            // 
            resources.ApplyResources(lblStatus, "lblStatus");
            lblStatus.Name = "lblStatus";
            // 
            // Form_Simulation_Config
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(lblStatus);
            Controls.Add(checkBox_PufferSp);
            Controls.Add(groupBox_PufferSp);
            Controls.Add(groupBox_Tools);
            Controls.Add(label21);
            Controls.Add(btn_OK);
            Controls.Add(btn_Speichern);
            Controls.Add(label12);
            Controls.Add(label11);
            Name = "Form_Simulation_Config";
            groupBox_Tools.ResumeLayout(false);
            groupBox_Tools.PerformLayout();
            groupBox_PufferSp.ResumeLayout(false);
            groupBox_PufferSp.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btn_Loeschen;
        private System.Windows.Forms.Button btn_Hinzu;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox checkBox6;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox6;
        private System.Windows.Forms.ComboBox comboBox5;
        private System.Windows.Forms.ComboBox comboBox4;
        private System.Windows.Forms.ComboBox comboBox3;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.GroupBox groupBox_Tools;
        private System.Windows.Forms.GroupBox groupBox_PufferSp;
        private System.Windows.Forms.CheckBox checkBox_PufferSp;
        private System.Windows.Forms.Label lblStatus;
    }
}