namespace WindowsFormsApplication1
{
    partial class NavigatorUebersicht
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            this.label_1 = new System.Windows.Forms.Label();
            this.label_2 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.bt_WaermebedarfUebersicht = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // label_1
            // 
            this.label_1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Underline);
            this.label_1.ForeColor = System.Drawing.Color.Black;
            this.label_1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_1.Location = new System.Drawing.Point(352, 32);
            this.label_1.Name = "label_1";
            this.label_1.Size = new System.Drawing.Size(140, 17);
            this.label_1.TabIndex = 288;
            this.label_1.Text = "Strombedarfsdeckung:";
            this.label_1.Visible = false;
            // 
            // label_2
            // 
            this.label_2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Underline);
            this.label_2.ForeColor = System.Drawing.Color.Black;
            this.label_2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_2.Location = new System.Drawing.Point(41, 32);
            this.label_2.Name = "label_2";
            this.label_2.Size = new System.Drawing.Size(154, 17);
            this.label_2.TabIndex = 289;
            this.label_2.Text = "Wärmebedarfsdeckung:";
            this.label_2.Visible = false;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(364, 161);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.Size = new System.Drawing.Size(616, 371);
            this.dataGridView1.TabIndex = 290;
            this.dataGridView1.Visible = false;
            // 
            // bt_WaermebedarfUebersicht
            // 
            this.bt_WaermebedarfUebersicht.AutoSize = true;
            this.bt_WaermebedarfUebersicht.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bt_WaermebedarfUebersicht.Location = new System.Drawing.Point(799, 32);
            this.bt_WaermebedarfUebersicht.Name = "bt_WaermebedarfUebersicht";
            this.bt_WaermebedarfUebersicht.Size = new System.Drawing.Size(166, 39);
            this.bt_WaermebedarfUebersicht.TabIndex = 291;
            this.bt_WaermebedarfUebersicht.Text = "Wärmebedarf Übersicht...";
            this.bt_WaermebedarfUebersicht.UseVisualStyleBackColor = true;
            this.bt_WaermebedarfUebersicht.Click += new System.EventHandler(this.bt_WaermebedarfUebersicht_Click);
            // 
            // NavigatorUebersicht
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.bt_WaermebedarfUebersicht);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.label_2);
            this.Controls.Add(this.label_1);
            this.Name = "NavigatorUebersicht";
            this.Size = new System.Drawing.Size(990, 628);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.NavigatorUebersicht_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label_1;
        private System.Windows.Forms.Label label_2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button bt_WaermebedarfUebersicht;
    }
}
