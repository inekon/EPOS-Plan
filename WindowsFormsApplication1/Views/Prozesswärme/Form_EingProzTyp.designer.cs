namespace WindowsFormsApplication1
{
    partial class Form_EingProzTyp
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend3 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.Label1 = new System.Windows.Forms.Label();
            this.listBox_Typname = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.btn_SpeichernUnter = new System.Windows.Forms.Button();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.btn_Schliessen = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Label3 = new System.Windows.Forms.Label();
            this.st1 = new System.Windows.Forms.TextBox();
            this.Label4 = new System.Windows.Forms.Label();
            this.st2 = new System.Windows.Forms.TextBox();
            this.Label5 = new System.Windows.Forms.Label();
            this.st3 = new System.Windows.Forms.TextBox();
            this.Label6 = new System.Windows.Forms.Label();
            this.st4 = new System.Windows.Forms.TextBox();
            this.Label7 = new System.Windows.Forms.Label();
            this.st5 = new System.Windows.Forms.TextBox();
            this.Label8 = new System.Windows.Forms.Label();
            this.st6 = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.st7 = new System.Windows.Forms.TextBox();
            this.Label10 = new System.Windows.Forms.Label();
            this.st8 = new System.Windows.Forms.TextBox();
            this.Label11 = new System.Windows.Forms.Label();
            this.st9 = new System.Windows.Forms.TextBox();
            this.Label12 = new System.Windows.Forms.Label();
            this.st10 = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.st11 = new System.Windows.Forms.TextBox();
            this.Label14 = new System.Windows.Forms.Label();
            this.st12 = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.st13 = new System.Windows.Forms.TextBox();
            this.Label16 = new System.Windows.Forms.Label();
            this.st14 = new System.Windows.Forms.TextBox();
            this.Label17 = new System.Windows.Forms.Label();
            this.st15 = new System.Windows.Forms.TextBox();
            this.Label18 = new System.Windows.Forms.Label();
            this.st16 = new System.Windows.Forms.TextBox();
            this.Label19 = new System.Windows.Forms.Label();
            this.st17 = new System.Windows.Forms.TextBox();
            this.Label20 = new System.Windows.Forms.Label();
            this.st18 = new System.Windows.Forms.TextBox();
            this.Label21 = new System.Windows.Forms.Label();
            this.st19 = new System.Windows.Forms.TextBox();
            this.Label22 = new System.Windows.Forms.Label();
            this.st20 = new System.Windows.Forms.TextBox();
            this.Label23 = new System.Windows.Forms.Label();
            this.st21 = new System.Windows.Forms.TextBox();
            this.Label24 = new System.Windows.Forms.Label();
            this.st22 = new System.Windows.Forms.TextBox();
            this.Label25 = new System.Windows.Forms.Label();
            this.st23 = new System.Windows.Forms.TextBox();
            this.Label26 = new System.Windows.Forms.Label();
            this.st24 = new System.Windows.Forms.TextBox();
            this.btn_WocheUebernehmen = new System.Windows.Forms.Button();
            this.Label27 = new System.Windows.Forms.Label();
            this.btn_Tagkopieren = new System.Windows.Forms.Button();
            this.btn_Tageinfuegen = new System.Windows.Forms.Button();
            this.listBox_Tag = new System.Windows.Forms.ListBox();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label1.Location = new System.Drawing.Point(14, 24);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(200, 17);
            this.Label1.TabIndex = 0;
            this.Label1.Text = "Liste der Prozesstypen in der DB:";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listBox_Typname
            // 
            this.listBox_Typname.ItemHeight = 17;
            this.listBox_Typname.Location = new System.Drawing.Point(17, 45);
            this.listBox_Typname.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Typname.Name = "listBox_Typname";
            this.listBox_Typname.Size = new System.Drawing.Size(197, 157);
            this.listBox_Typname.TabIndex = 2;
            this.listBox_Typname.SelectedIndexChanged += new System.EventHandler(this.listBox_Typname_SelectedIndexChanged);
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label2.Location = new System.Drawing.Point(245, 24);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(270, 17);
            this.Label2.TabIndex = 3;
            this.Label2.Text = "Beschreibung des ausgewählten Prozesstyps:";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(248, 46);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(436, 73);
            this.textBox_Beschreibung.TabIndex = 4;
            // 
            // btn_Neu
            // 
            this.btn_Neu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Neu.Location = new System.Drawing.Point(479, 480);
            this.btn_Neu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.Size = new System.Drawing.Size(102, 31);
            this.btn_Neu.TabIndex = 5;
            this.btn_Neu.Text = "Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Loeschen.Location = new System.Drawing.Point(377, 480);
            this.btn_Loeschen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(87, 31);
            this.btn_Loeschen.TabIndex = 6;
            this.btn_Loeschen.Text = "Löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // btn_SpeichernUnter
            // 
            this.btn_SpeichernUnter.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_SpeichernUnter.Location = new System.Drawing.Point(115, 480);
            this.btn_SpeichernUnter.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_SpeichernUnter.Name = "btn_SpeichernUnter";
            this.btn_SpeichernUnter.Size = new System.Drawing.Size(119, 31);
            this.btn_SpeichernUnter.TabIndex = 7;
            this.btn_SpeichernUnter.Text = "Speichern unter";
            this.btn_SpeichernUnter.UseVisualStyleBackColor = true;
            this.btn_SpeichernUnter.Click += new System.EventHandler(this.btn_SpeichernUnter_Click);
            // 
            // btn_Speichern
            // 
            this.btn_Speichern.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Speichern.Location = new System.Drawing.Point(250, 480);
            this.btn_Speichern.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.Size = new System.Drawing.Size(113, 31);
            this.btn_Speichern.TabIndex = 9;
            this.btn_Speichern.Text = "Speichern in DB";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // btn_Schliessen
            // 
            this.btn_Schliessen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Schliessen.Location = new System.Drawing.Point(617, 480);
            this.btn_Schliessen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Schliessen.Name = "btn_Schliessen";
            this.btn_Schliessen.Size = new System.Drawing.Size(84, 31);
            this.btn_Schliessen.TabIndex = 10;
            this.btn_Schliessen.Text = "Schließen";
            this.btn_Schliessen.UseVisualStyleBackColor = true;
            this.btn_Schliessen.Click += new System.EventHandler(this.btn_Schliessen_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Label3);
            this.groupBox1.Controls.Add(this.st1);
            this.groupBox1.Controls.Add(this.Label4);
            this.groupBox1.Controls.Add(this.st2);
            this.groupBox1.Controls.Add(this.Label5);
            this.groupBox1.Controls.Add(this.st3);
            this.groupBox1.Controls.Add(this.Label6);
            this.groupBox1.Controls.Add(this.st4);
            this.groupBox1.Controls.Add(this.Label7);
            this.groupBox1.Controls.Add(this.st5);
            this.groupBox1.Controls.Add(this.Label8);
            this.groupBox1.Controls.Add(this.st6);
            this.groupBox1.Controls.Add(this.Label9);
            this.groupBox1.Controls.Add(this.st7);
            this.groupBox1.Controls.Add(this.Label10);
            this.groupBox1.Controls.Add(this.st8);
            this.groupBox1.Controls.Add(this.Label11);
            this.groupBox1.Controls.Add(this.st9);
            this.groupBox1.Controls.Add(this.Label12);
            this.groupBox1.Controls.Add(this.st10);
            this.groupBox1.Controls.Add(this.Label13);
            this.groupBox1.Controls.Add(this.st11);
            this.groupBox1.Controls.Add(this.Label14);
            this.groupBox1.Controls.Add(this.st12);
            this.groupBox1.Controls.Add(this.Label15);
            this.groupBox1.Controls.Add(this.st13);
            this.groupBox1.Controls.Add(this.Label16);
            this.groupBox1.Controls.Add(this.st14);
            this.groupBox1.Controls.Add(this.Label17);
            this.groupBox1.Controls.Add(this.st15);
            this.groupBox1.Controls.Add(this.Label18);
            this.groupBox1.Controls.Add(this.st16);
            this.groupBox1.Controls.Add(this.Label19);
            this.groupBox1.Controls.Add(this.st17);
            this.groupBox1.Controls.Add(this.Label20);
            this.groupBox1.Controls.Add(this.st18);
            this.groupBox1.Controls.Add(this.Label21);
            this.groupBox1.Controls.Add(this.st19);
            this.groupBox1.Controls.Add(this.Label22);
            this.groupBox1.Controls.Add(this.st20);
            this.groupBox1.Controls.Add(this.Label23);
            this.groupBox1.Controls.Add(this.st21);
            this.groupBox1.Controls.Add(this.Label24);
            this.groupBox1.Controls.Add(this.st22);
            this.groupBox1.Controls.Add(this.Label25);
            this.groupBox1.Controls.Add(this.st23);
            this.groupBox1.Controls.Add(this.Label26);
            this.groupBox1.Controls.Add(this.st24);
            this.groupBox1.Controls.Add(this.btn_WocheUebernehmen);
            this.groupBox1.Controls.Add(this.Label27);
            this.groupBox1.Controls.Add(this.btn_Tagkopieren);
            this.groupBox1.Controls.Add(this.btn_Tageinfuegen);
            this.groupBox1.Controls.Add(this.listBox_Tag);
            this.groupBox1.Location = new System.Drawing.Point(228, 127);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(473, 262);
            this.groupBox1.TabIndex = 65;
            this.groupBox1.TabStop = false;
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label3.Location = new System.Drawing.Point(18, 28);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(15, 17);
            this.Label3.TabIndex = 65;
            this.Label3.Text = "1";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st1
            // 
            this.st1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st1.Location = new System.Drawing.Point(39, 28);
            this.st1.Margin = new System.Windows.Forms.Padding(5);
            this.st1.Name = "st1";
            this.st1.Size = new System.Drawing.Size(70, 22);
            this.st1.TabIndex = 66;
            // 
            // Label4
            // 
            this.Label4.AutoSize = true;
            this.Label4.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label4.Location = new System.Drawing.Point(18, 52);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(15, 17);
            this.Label4.TabIndex = 67;
            this.Label4.Text = "2";
            this.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st2
            // 
            this.st2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st2.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st2.Location = new System.Drawing.Point(39, 52);
            this.st2.Margin = new System.Windows.Forms.Padding(5);
            this.st2.Name = "st2";
            this.st2.Size = new System.Drawing.Size(70, 22);
            this.st2.TabIndex = 68;
            // 
            // Label5
            // 
            this.Label5.AutoSize = true;
            this.Label5.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label5.Location = new System.Drawing.Point(18, 75);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(15, 17);
            this.Label5.TabIndex = 69;
            this.Label5.Text = "3";
            this.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st3
            // 
            this.st3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st3.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st3.Location = new System.Drawing.Point(39, 75);
            this.st3.Margin = new System.Windows.Forms.Padding(5);
            this.st3.Name = "st3";
            this.st3.Size = new System.Drawing.Size(70, 22);
            this.st3.TabIndex = 70;
            // 
            // Label6
            // 
            this.Label6.AutoSize = true;
            this.Label6.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label6.Location = new System.Drawing.Point(18, 99);
            this.Label6.Name = "Label6";
            this.Label6.Size = new System.Drawing.Size(15, 17);
            this.Label6.TabIndex = 71;
            this.Label6.Text = "4";
            this.Label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st4
            // 
            this.st4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st4.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st4.Location = new System.Drawing.Point(39, 99);
            this.st4.Margin = new System.Windows.Forms.Padding(5);
            this.st4.Name = "st4";
            this.st4.Size = new System.Drawing.Size(70, 22);
            this.st4.TabIndex = 72;
            // 
            // Label7
            // 
            this.Label7.AutoSize = true;
            this.Label7.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label7.Location = new System.Drawing.Point(18, 122);
            this.Label7.Name = "Label7";
            this.Label7.Size = new System.Drawing.Size(15, 17);
            this.Label7.TabIndex = 73;
            this.Label7.Text = "5";
            this.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st5
            // 
            this.st5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st5.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st5.Location = new System.Drawing.Point(39, 122);
            this.st5.Margin = new System.Windows.Forms.Padding(5);
            this.st5.Name = "st5";
            this.st5.Size = new System.Drawing.Size(70, 22);
            this.st5.TabIndex = 74;
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label8.Location = new System.Drawing.Point(18, 146);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(15, 17);
            this.Label8.TabIndex = 75;
            this.Label8.Text = "6";
            this.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st6
            // 
            this.st6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st6.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st6.Location = new System.Drawing.Point(39, 146);
            this.st6.Margin = new System.Windows.Forms.Padding(5);
            this.st6.Name = "st6";
            this.st6.Size = new System.Drawing.Size(70, 22);
            this.st6.TabIndex = 76;
            // 
            // Label9
            // 
            this.Label9.AutoSize = true;
            this.Label9.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label9.Location = new System.Drawing.Point(18, 169);
            this.Label9.Name = "Label9";
            this.Label9.Size = new System.Drawing.Size(15, 17);
            this.Label9.TabIndex = 77;
            this.Label9.Text = "7";
            this.Label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st7
            // 
            this.st7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st7.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st7.Location = new System.Drawing.Point(39, 169);
            this.st7.Margin = new System.Windows.Forms.Padding(5);
            this.st7.Name = "st7";
            this.st7.Size = new System.Drawing.Size(70, 22);
            this.st7.TabIndex = 78;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label10.Location = new System.Drawing.Point(18, 193);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(15, 17);
            this.Label10.TabIndex = 79;
            this.Label10.Text = "8";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st8
            // 
            this.st8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st8.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st8.Location = new System.Drawing.Point(39, 193);
            this.st8.Margin = new System.Windows.Forms.Padding(5);
            this.st8.Name = "st8";
            this.st8.Size = new System.Drawing.Size(70, 22);
            this.st8.TabIndex = 80;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label11.Location = new System.Drawing.Point(119, 28);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(15, 17);
            this.Label11.TabIndex = 81;
            this.Label11.Text = "9";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st9
            // 
            this.st9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st9.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st9.Location = new System.Drawing.Point(149, 28);
            this.st9.Margin = new System.Windows.Forms.Padding(5);
            this.st9.Name = "st9";
            this.st9.Size = new System.Drawing.Size(70, 22);
            this.st9.TabIndex = 82;
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label12.Location = new System.Drawing.Point(119, 52);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(22, 17);
            this.Label12.TabIndex = 83;
            this.Label12.Text = "10";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st10
            // 
            this.st10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st10.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st10.Location = new System.Drawing.Point(149, 52);
            this.st10.Margin = new System.Windows.Forms.Padding(5);
            this.st10.Name = "st10";
            this.st10.Size = new System.Drawing.Size(70, 22);
            this.st10.TabIndex = 84;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label13.Location = new System.Drawing.Point(119, 75);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(22, 17);
            this.Label13.TabIndex = 85;
            this.Label13.Text = "11";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st11
            // 
            this.st11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st11.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st11.Location = new System.Drawing.Point(149, 75);
            this.st11.Margin = new System.Windows.Forms.Padding(5);
            this.st11.Name = "st11";
            this.st11.Size = new System.Drawing.Size(70, 22);
            this.st11.TabIndex = 86;
            // 
            // Label14
            // 
            this.Label14.AutoSize = true;
            this.Label14.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label14.Location = new System.Drawing.Point(119, 99);
            this.Label14.Name = "Label14";
            this.Label14.Size = new System.Drawing.Size(22, 17);
            this.Label14.TabIndex = 87;
            this.Label14.Text = "12";
            this.Label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st12
            // 
            this.st12.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st12.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st12.Location = new System.Drawing.Point(149, 99);
            this.st12.Margin = new System.Windows.Forms.Padding(5);
            this.st12.Name = "st12";
            this.st12.Size = new System.Drawing.Size(70, 22);
            this.st12.TabIndex = 88;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label15.Location = new System.Drawing.Point(119, 122);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(22, 17);
            this.Label15.TabIndex = 89;
            this.Label15.Text = "13";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st13
            // 
            this.st13.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st13.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st13.Location = new System.Drawing.Point(149, 122);
            this.st13.Margin = new System.Windows.Forms.Padding(5);
            this.st13.Name = "st13";
            this.st13.Size = new System.Drawing.Size(70, 22);
            this.st13.TabIndex = 90;
            // 
            // Label16
            // 
            this.Label16.AutoSize = true;
            this.Label16.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label16.Location = new System.Drawing.Point(119, 146);
            this.Label16.Name = "Label16";
            this.Label16.Size = new System.Drawing.Size(22, 17);
            this.Label16.TabIndex = 91;
            this.Label16.Text = "14";
            this.Label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st14
            // 
            this.st14.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st14.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st14.Location = new System.Drawing.Point(149, 146);
            this.st14.Margin = new System.Windows.Forms.Padding(5);
            this.st14.Name = "st14";
            this.st14.Size = new System.Drawing.Size(70, 22);
            this.st14.TabIndex = 92;
            // 
            // Label17
            // 
            this.Label17.AutoSize = true;
            this.Label17.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label17.Location = new System.Drawing.Point(119, 169);
            this.Label17.Name = "Label17";
            this.Label17.Size = new System.Drawing.Size(22, 17);
            this.Label17.TabIndex = 93;
            this.Label17.Text = "15";
            this.Label17.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st15
            // 
            this.st15.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st15.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st15.Location = new System.Drawing.Point(149, 169);
            this.st15.Margin = new System.Windows.Forms.Padding(5);
            this.st15.Name = "st15";
            this.st15.Size = new System.Drawing.Size(70, 22);
            this.st15.TabIndex = 94;
            // 
            // Label18
            // 
            this.Label18.AutoSize = true;
            this.Label18.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label18.Location = new System.Drawing.Point(119, 193);
            this.Label18.Name = "Label18";
            this.Label18.Size = new System.Drawing.Size(22, 17);
            this.Label18.TabIndex = 95;
            this.Label18.Text = "16";
            this.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st16
            // 
            this.st16.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st16.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st16.Location = new System.Drawing.Point(149, 193);
            this.st16.Margin = new System.Windows.Forms.Padding(5);
            this.st16.Name = "st16";
            this.st16.Size = new System.Drawing.Size(70, 22);
            this.st16.TabIndex = 96;
            // 
            // Label19
            // 
            this.Label19.AutoSize = true;
            this.Label19.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label19.Location = new System.Drawing.Point(230, 27);
            this.Label19.Name = "Label19";
            this.Label19.Size = new System.Drawing.Size(22, 17);
            this.Label19.TabIndex = 97;
            this.Label19.Text = "17";
            this.Label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st17
            // 
            this.st17.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st17.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st17.Location = new System.Drawing.Point(260, 27);
            this.st17.Margin = new System.Windows.Forms.Padding(5);
            this.st17.Name = "st17";
            this.st17.Size = new System.Drawing.Size(70, 22);
            this.st17.TabIndex = 98;
            // 
            // Label20
            // 
            this.Label20.AutoSize = true;
            this.Label20.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label20.Location = new System.Drawing.Point(230, 51);
            this.Label20.Name = "Label20";
            this.Label20.Size = new System.Drawing.Size(22, 17);
            this.Label20.TabIndex = 99;
            this.Label20.Text = "18";
            this.Label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st18
            // 
            this.st18.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st18.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st18.Location = new System.Drawing.Point(260, 51);
            this.st18.Margin = new System.Windows.Forms.Padding(5);
            this.st18.Name = "st18";
            this.st18.Size = new System.Drawing.Size(70, 22);
            this.st18.TabIndex = 100;
            // 
            // Label21
            // 
            this.Label21.AutoSize = true;
            this.Label21.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label21.Location = new System.Drawing.Point(230, 74);
            this.Label21.Name = "Label21";
            this.Label21.Size = new System.Drawing.Size(22, 17);
            this.Label21.TabIndex = 101;
            this.Label21.Text = "19";
            this.Label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st19
            // 
            this.st19.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st19.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st19.Location = new System.Drawing.Point(260, 74);
            this.st19.Margin = new System.Windows.Forms.Padding(5);
            this.st19.Name = "st19";
            this.st19.Size = new System.Drawing.Size(70, 22);
            this.st19.TabIndex = 102;
            // 
            // Label22
            // 
            this.Label22.AutoSize = true;
            this.Label22.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label22.Location = new System.Drawing.Point(230, 98);
            this.Label22.Name = "Label22";
            this.Label22.Size = new System.Drawing.Size(22, 17);
            this.Label22.TabIndex = 103;
            this.Label22.Text = "20";
            this.Label22.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st20
            // 
            this.st20.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st20.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st20.Location = new System.Drawing.Point(260, 98);
            this.st20.Margin = new System.Windows.Forms.Padding(5);
            this.st20.Name = "st20";
            this.st20.Size = new System.Drawing.Size(70, 22);
            this.st20.TabIndex = 104;
            // 
            // Label23
            // 
            this.Label23.AutoSize = true;
            this.Label23.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label23.Location = new System.Drawing.Point(230, 121);
            this.Label23.Name = "Label23";
            this.Label23.Size = new System.Drawing.Size(22, 17);
            this.Label23.TabIndex = 105;
            this.Label23.Text = "21";
            this.Label23.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st21
            // 
            this.st21.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st21.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st21.Location = new System.Drawing.Point(260, 121);
            this.st21.Margin = new System.Windows.Forms.Padding(5);
            this.st21.Name = "st21";
            this.st21.Size = new System.Drawing.Size(70, 22);
            this.st21.TabIndex = 106;
            // 
            // Label24
            // 
            this.Label24.AutoSize = true;
            this.Label24.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label24.Location = new System.Drawing.Point(230, 145);
            this.Label24.Name = "Label24";
            this.Label24.Size = new System.Drawing.Size(22, 17);
            this.Label24.TabIndex = 107;
            this.Label24.Text = "22";
            this.Label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st22
            // 
            this.st22.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st22.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st22.Location = new System.Drawing.Point(260, 145);
            this.st22.Margin = new System.Windows.Forms.Padding(5);
            this.st22.Name = "st22";
            this.st22.Size = new System.Drawing.Size(70, 22);
            this.st22.TabIndex = 108;
            // 
            // Label25
            // 
            this.Label25.AutoSize = true;
            this.Label25.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label25.Location = new System.Drawing.Point(230, 168);
            this.Label25.Name = "Label25";
            this.Label25.Size = new System.Drawing.Size(22, 17);
            this.Label25.TabIndex = 109;
            this.Label25.Text = "23";
            this.Label25.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st23
            // 
            this.st23.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st23.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st23.Location = new System.Drawing.Point(260, 168);
            this.st23.Margin = new System.Windows.Forms.Padding(5);
            this.st23.Name = "st23";
            this.st23.Size = new System.Drawing.Size(70, 22);
            this.st23.TabIndex = 110;
            // 
            // Label26
            // 
            this.Label26.AutoSize = true;
            this.Label26.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Label26.Location = new System.Drawing.Point(230, 192);
            this.Label26.Name = "Label26";
            this.Label26.Size = new System.Drawing.Size(22, 17);
            this.Label26.TabIndex = 111;
            this.Label26.Text = "24";
            this.Label26.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // st24
            // 
            this.st24.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.st24.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.st24.Location = new System.Drawing.Point(260, 192);
            this.st24.Margin = new System.Windows.Forms.Padding(5);
            this.st24.Name = "st24";
            this.st24.Size = new System.Drawing.Size(70, 22);
            this.st24.TabIndex = 112;
            // 
            // btn_WocheUebernehmen
            // 
            this.btn_WocheUebernehmen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_WocheUebernehmen.Location = new System.Drawing.Point(39, 224);
            this.btn_WocheUebernehmen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_WocheUebernehmen.Name = "btn_WocheUebernehmen";
            this.btn_WocheUebernehmen.Size = new System.Drawing.Size(291, 27);
            this.btn_WocheUebernehmen.TabIndex = 113;
            this.btn_WocheUebernehmen.Text = "Änderungen Übernehmen";
            this.btn_WocheUebernehmen.UseVisualStyleBackColor = true;
            this.btn_WocheUebernehmen.Click += new System.EventHandler(this.btn_WocheUebernehmen_Click);
            // 
            // Label27
            // 
            this.Label27.AutoSize = true;
            this.Label27.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label27.Location = new System.Drawing.Point(340, 20);
            this.Label27.Name = "Label27";
            this.Label27.Size = new System.Drawing.Size(125, 17);
            this.Label27.TabIndex = 114;
            this.Label27.Text = "Auswahl Wochentag";
            this.Label27.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_Tagkopieren
            // 
            this.btn_Tagkopieren.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Tagkopieren.Location = new System.Drawing.Point(347, 179);
            this.btn_Tagkopieren.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Tagkopieren.Name = "btn_Tagkopieren";
            this.btn_Tagkopieren.Size = new System.Drawing.Size(109, 26);
            this.btn_Tagkopieren.TabIndex = 115;
            this.btn_Tagkopieren.Text = "Tag kopieren";
            this.btn_Tagkopieren.UseVisualStyleBackColor = true;
            // 
            // btn_Tageinfuegen
            // 
            this.btn_Tageinfuegen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Tageinfuegen.Location = new System.Drawing.Point(347, 211);
            this.btn_Tageinfuegen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Tageinfuegen.Name = "btn_Tageinfuegen";
            this.btn_Tageinfuegen.Size = new System.Drawing.Size(109, 26);
            this.btn_Tageinfuegen.TabIndex = 116;
            this.btn_Tageinfuegen.Text = "Tag einfügen";
            this.btn_Tageinfuegen.UseVisualStyleBackColor = true;
            // 
            // listBox_Tag
            // 
            this.listBox_Tag.ItemHeight = 17;
            this.listBox_Tag.Items.AddRange(new object[] {
            "Montag",
            "Dienstag",
            "Mittwoch",
            "Donnerstag",
            "Freitag",
            "Samstag",
            "Sonntag"});
            this.listBox_Tag.Location = new System.Drawing.Point(347, 46);
            this.listBox_Tag.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Tag.Name = "listBox_Tag";
            this.listBox_Tag.Size = new System.Drawing.Size(109, 123);
            this.listBox_Tag.TabIndex = 117;
            this.listBox_Tag.SelectedIndexChanged += new System.EventHandler(this.listBox_Tag_SelectedIndexChanged);
            // 
            // chart1
            // 
            chartArea3.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea3.AxisX.Title = "Wochenstunden";
            chartArea3.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea3.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea3.AxisY.Minimum = 0D;
            chartArea3.AxisY.Title = "Stundenwerte";
            chartArea3.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea3.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea3.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea3);
            legend3.Name = "Legend1";
            this.chart1.Legends.Add(legend3);
            this.chart1.Location = new System.Drawing.Point(17, 232);
            this.chart1.Name = "chart1";
            series3.ChartArea = "ChartArea1";
            series3.IsVisibleInLegend = false;
            series3.Legend = "Legend1";
            series3.Name = "Series1";
            this.chart1.Series.Add(series3);
            this.chart1.Size = new System.Drawing.Size(197, 157);
            this.chart1.TabIndex = 66;
            this.chart1.Text = "Grafik Stundenwerte";
            // 
            // Form_EingProzTyp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(713, 520);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_Typname);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.btn_Neu);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_SpeichernUnter);
            this.Controls.Add(this.btn_Speichern);
            this.Controls.Add(this.btn_Schliessen);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_EingProzTyp";
            this.Text = "Prozesstypen Stundenverteilung";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label1;
private System.Windows.Forms.ListBox listBox_Typname;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.Button btn_Neu;
private System.Windows.Forms.Button btn_Loeschen;
private System.Windows.Forms.Button btn_SpeichernUnter;
private System.Windows.Forms.Button btn_Speichern;
private System.Windows.Forms.Button btn_Schliessen;
private System.Windows.Forms.GroupBox groupBox1;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox st1;
private System.Windows.Forms.Label Label4;
private System.Windows.Forms.TextBox st2;
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.TextBox st3;
private System.Windows.Forms.Label Label6;
private System.Windows.Forms.TextBox st4;
private System.Windows.Forms.Label Label7;
private System.Windows.Forms.TextBox st5;
private System.Windows.Forms.Label Label8;
private System.Windows.Forms.TextBox st6;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.TextBox st7;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox st8;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.TextBox st9;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.TextBox st10;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox st11;
private System.Windows.Forms.Label Label14;
private System.Windows.Forms.TextBox st12;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.TextBox st13;
private System.Windows.Forms.Label Label16;
private System.Windows.Forms.TextBox st14;
private System.Windows.Forms.Label Label17;
private System.Windows.Forms.TextBox st15;
private System.Windows.Forms.Label Label18;
private System.Windows.Forms.TextBox st16;
private System.Windows.Forms.Label Label19;
private System.Windows.Forms.TextBox st17;
private System.Windows.Forms.Label Label20;
private System.Windows.Forms.TextBox st18;
private System.Windows.Forms.Label Label21;
private System.Windows.Forms.TextBox st19;
private System.Windows.Forms.Label Label22;
private System.Windows.Forms.TextBox st20;
private System.Windows.Forms.Label Label23;
private System.Windows.Forms.TextBox st21;
private System.Windows.Forms.Label Label24;
private System.Windows.Forms.TextBox st22;
private System.Windows.Forms.Label Label25;
private System.Windows.Forms.TextBox st23;
private System.Windows.Forms.Label Label26;
private System.Windows.Forms.TextBox st24;
private System.Windows.Forms.Button btn_WocheUebernehmen;
private System.Windows.Forms.Label Label27;
private System.Windows.Forms.Button btn_Tagkopieren;
private System.Windows.Forms.Button btn_Tageinfuegen;
private System.Windows.Forms.ListBox listBox_Tag;
private System.Windows.Forms.DataVisualization.Charting.Chart chart1;


 
    }
}