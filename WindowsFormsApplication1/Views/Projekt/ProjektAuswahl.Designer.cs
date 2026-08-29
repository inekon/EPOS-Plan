namespace WindowsFormsApplication1
{
    partial class ProjektAuswahl
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

        #region Vom Komponenten-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjektAuswahl));
            label_Suche = new System.Windows.Forms.Label();
            textBox_Suche = new System.Windows.Forms.TextBox();
            listView_Projekte = new System.Windows.Forms.ListView();
            columnHeader_Name = new System.Windows.Forms.ColumnHeader();
            columnHeader_Kunde = new System.Windows.Forms.ColumnHeader();
            columnHeader_Geaendert = new System.Windows.Forms.ColumnHeader();
            label_Anzahl = new System.Windows.Forms.Label();
            SuspendLayout();
            //
            // label_Suche
            //
            resources.ApplyResources(label_Suche, "label_Suche");
            label_Suche.Name = "label_Suche";
            //
            // textBox_Suche
            //
            resources.ApplyResources(textBox_Suche, "textBox_Suche");
            textBox_Suche.Name = "textBox_Suche";
            textBox_Suche.TextChanged += textBox_Suche_TextChanged;
            //
            // listView_Projekte
            //
            listView_Projekte.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnHeader_Name, columnHeader_Kunde, columnHeader_Geaendert });
            resources.ApplyResources(listView_Projekte, "listView_Projekte");
            listView_Projekte.FullRowSelect = true;
            listView_Projekte.GridLines = true;
            listView_Projekte.HideSelection = false;
            listView_Projekte.MultiSelect = false;
            listView_Projekte.Name = "listView_Projekte";
            listView_Projekte.UseCompatibleStateImageBehavior = false;
            listView_Projekte.View = System.Windows.Forms.View.Details;
            listView_Projekte.ColumnClick += listView_Projekte_ColumnClick;
            listView_Projekte.SelectedIndexChanged += listView_Projekte_SelectedIndexChanged;
            listView_Projekte.DoubleClick += listView_Projekte_DoubleClick;
            //
            // columnHeader_Name
            //
            resources.ApplyResources(columnHeader_Name, "columnHeader_Name");
            //
            // columnHeader_Kunde
            //
            resources.ApplyResources(columnHeader_Kunde, "columnHeader_Kunde");
            //
            // columnHeader_Geaendert
            //
            resources.ApplyResources(columnHeader_Geaendert, "columnHeader_Geaendert");
            //
            // label_Anzahl
            //
            resources.ApplyResources(label_Anzahl, "label_Anzahl");
            label_Anzahl.Name = "label_Anzahl";
            //
            // ProjektAuswahl
            //
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label_Anzahl);
            Controls.Add(listView_Projekte);
            Controls.Add(textBox_Suche);
            Controls.Add(label_Suche);
            Name = "ProjektAuswahl";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label_Suche;
        private System.Windows.Forms.TextBox textBox_Suche;
        private System.Windows.Forms.ListView listView_Projekte;
        private System.Windows.Forms.ColumnHeader columnHeader_Name;
        private System.Windows.Forms.ColumnHeader columnHeader_Kunde;
        private System.Windows.Forms.ColumnHeader columnHeader_Geaendert;
        private System.Windows.Forms.Label label_Anzahl;
    }
}
