using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Options- und Vorschauzone des erweiterten Lastgangimports (AP5,
    /// Fachkonzept 3.2). Zeigt nach der Dateiwahl das erkannte Format, laesst
    /// jede Vorbelegung uebersteuern und blendet die ersten Zeilen der Datei als
    /// Vorschau ein.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Der Dialog ist vollstaendig im Quelltext aufgebaut - keine
    /// <c>.Designer.cs</c>, keine <c>.resx</c> (Projekt-CLAUDE.md: Designer- und
    /// resx-Dateien nicht von Hand editieren). Alle Beschriftungen kommen aus
    /// <c>MyResource</c> (<c>IMPORT_*</c>) und sind damit zweisprachig.
    /// </para>
    /// <para>
    /// <b>Steuerwerte sind Indizes, keine Anzeigetexte</b> (Drei-Schichten-Regel):
    /// die Auswahllisten fuehren feste Wertefelder
    /// (<see cref="Trennzeichenwerte"/> u. a.), die Beschriftung steht daneben.
    /// </para>
    /// </remarks>
    public class Form_GanglinieImportOptionen : Form
    {
        /// <summary>Steuerwerte der Trennzeichenliste, gleiche Reihenfolge wie die Beschriftungen.</summary>
        private static readonly char[] Trennzeichenwerte = { ';', ',', '\t', '|', '\0' };

        /// <summary>Steuerwerte der Dezimaltrennerliste.</summary>
        private static readonly char[] Dezimalwerte = { ',', '.' };

        /// <summary>Steuerwerte der Einheitenliste.</summary>
        private static readonly GanglinienEinheit[] Einheitswerte =
        {
            GanglinienEinheit.Kilowatt,
            GanglinienEinheit.KilowattstundeJeIntervall
        };

        /// <summary>Steuerwerte der Rasterliste.</summary>
        private static readonly GanglinienRaster[] Rasterwerte =
        {
            GanglinienRaster.Unbekannt,
            GanglinienRaster.Stunde,
            GanglinienRaster.Viertelstunde,
            GanglinienRaster.Minute
        };

        /// <summary>Steuerwerte der Konventionsliste.</summary>
        private static readonly IntervallKonvention[] Konventionswerte =
        {
            IntervallKonvention.Automatisch,
            IntervallKonvention.Anfang,
            IntervallKonvention.Ende
        };

        private readonly string m_szPfad;

        private ComboBox cbo_Trennzeichen, cbo_Dezimal, cbo_Wertspalte, cbo_Zeitspalte;
        private ComboBox cbo_Einheit, cbo_Raster, cbo_Konvention, cbo_Blatt;
        private CheckBox chk_Kopfzeile;
        private ListView listView_Vorschau;
        private Label lbl_Datei, lbl_Blatt, lbl_Hinweis;
        private Button btn_Aktualisieren, btn_OK, btn_Abbrechen;
        private GroupBox grp_Format, grp_Vorschau;
        private bool m_bAufbau = true;

        /// <summary>Die vom Anwender bestaetigten Leseoptionen.</summary>
        public GanglinienImportOptionen Optionen { get; private set; }

        /// <summary>
        /// Baut den Dialog aus einer bereits erstellten Formaterkennung auf.
        /// </summary>
        /// <param name="pfad">Quelldatei (nur fuer Anzeige und Neuerkennung).</param>
        /// <param name="vorschau">Ergebnis von <see cref="GanglinienDatei.Erkenne"/>.</param>
        public Form_GanglinieImportOptionen(string pfad, GanglinienVorschau vorschau)
        {
            m_szPfad = pfad ?? "";
            Optionen = (vorschau != null ? vorschau.Vorschlag : new GanglinienImportOptionen()).Kopie();

            AufbauSteuerelemente(vorschau != null && vorschau.IstExcel);
            ListenFuellen(vorschau);
            OptionenInDialog();
            VorschauFuellen(vorschau);
            m_bAufbau = false;
        }

        // ==================================================================
        // Aufbau
        // ==================================================================

        private void AufbauSteuerelemente(bool istExcel)
        {
            Text = MyResource.Resource.IMPORT_TITEL_OPTIONEN;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(820, 560);
            MinimumSize = new Size(660, 460);

            lbl_Datei = new Label();
            lbl_Datei.SetBounds(12, 10, 796, 18);
            lbl_Datei.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lbl_Datei.AutoEllipsis = true;
            lbl_Datei.Text = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.IMPORT_LBL_DATEI, Path.GetFileName(m_szPfad));
            Controls.Add(lbl_Datei);

            grp_Format = new GroupBox();
            grp_Format.SetBounds(12, 32, 796, 178);
            grp_Format.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grp_Format.Text = MyResource.Resource.IMPORT_GRP_OPTIONEN;
            Controls.Add(grp_Format);

            const int sp1 = 14, sp1e = 150, sp2 = 420, sp2e = 566;
            int y = 26;

            Beschriftung(grp_Format, MyResource.Resource.IMPORT_LBL_TRENNZEICHEN, sp1, y);
            cbo_Trennzeichen = Auswahl(grp_Format, sp1e, y, 200);
            Beschriftung(grp_Format, MyResource.Resource.IMPORT_LBL_DEZIMALTRENNER, sp2, y);
            cbo_Dezimal = Auswahl(grp_Format, sp2e, y, 200);

            y += 30;
            Beschriftung(grp_Format, MyResource.Resource.IMPORT_LBL_WERTSPALTE, sp1, y);
            cbo_Wertspalte = Auswahl(grp_Format, sp1e, y, 200);
            Beschriftung(grp_Format, MyResource.Resource.IMPORT_LBL_ZEITSPALTE, sp2, y);
            cbo_Zeitspalte = Auswahl(grp_Format, sp2e, y, 200);

            y += 30;
            Beschriftung(grp_Format, MyResource.Resource.IMPORT_LBL_EINHEIT, sp1, y);
            cbo_Einheit = Auswahl(grp_Format, sp1e, y, 200);
            Beschriftung(grp_Format, MyResource.Resource.IMPORT_LBL_RASTER, sp2, y);
            cbo_Raster = Auswahl(grp_Format, sp2e, y, 200);

            y += 30;
            Beschriftung(grp_Format, MyResource.Resource.IMPORT_LBL_KONVENTION, sp1, y);
            cbo_Konvention = Auswahl(grp_Format, sp1e, y, 200);
            lbl_Blatt = Beschriftung(grp_Format, MyResource.Resource.IMPORT_LBL_BLATT, sp2, y);
            cbo_Blatt = Auswahl(grp_Format, sp2e, y, 200);
            lbl_Blatt.Visible = istExcel;
            cbo_Blatt.Visible = istExcel;

            y += 30;
            chk_Kopfzeile = new CheckBox();
            chk_Kopfzeile.SetBounds(sp1, y, 340, 22);
            chk_Kopfzeile.Text = MyResource.Resource.IMPORT_LBL_KOPFZEILE;
            grp_Format.Controls.Add(chk_Kopfzeile);

            btn_Aktualisieren = new Button();
            btn_Aktualisieren.SetBounds(sp2e, y - 2, 200, 26);
            btn_Aktualisieren.Text = MyResource.Resource.IMPORT_BTN_AKTUALISIEREN;
            btn_Aktualisieren.Click += Aktualisieren_Click;
            grp_Format.Controls.Add(btn_Aktualisieren);

            grp_Vorschau = new GroupBox();
            grp_Vorschau.SetBounds(12, 218, 796, 258);
            grp_Vorschau.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grp_Vorschau.Text = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.IMPORT_GRP_VORSCHAU, GanglinienDatei.VorschauZeilen);
            Controls.Add(grp_Vorschau);

            listView_Vorschau = new ListView();
            listView_Vorschau.SetBounds(12, 20, 772, 228);
            listView_Vorschau.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listView_Vorschau.View = View.Details;
            listView_Vorschau.FullRowSelect = true;
            listView_Vorschau.GridLines = true;
            listView_Vorschau.MultiSelect = false;
            grp_Vorschau.Controls.Add(listView_Vorschau);

            lbl_Hinweis = new Label();
            lbl_Hinweis.SetBounds(12, 486, 560, 34);
            lbl_Hinweis.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbl_Hinweis.Text = MyResource.Resource.IMPORT_HINWEIS_OPTIONEN;
            Controls.Add(lbl_Hinweis);

            btn_OK = new Button();
            btn_OK.SetBounds(616, 522, 90, 26);
            btn_OK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_OK.Text = MyResource.Resource.IMPORT_BTN_OK;
            btn_OK.DialogResult = DialogResult.OK;
            btn_OK.Click += OK_Click;
            Controls.Add(btn_OK);

            btn_Abbrechen = new Button();
            btn_Abbrechen.SetBounds(714, 522, 94, 26);
            btn_Abbrechen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_Abbrechen.Text = MyResource.Resource.IMPORT_BTN_ABBRECHEN;
            btn_Abbrechen.DialogResult = DialogResult.Cancel;
            Controls.Add(btn_Abbrechen);

            AcceptButton = btn_OK;
            CancelButton = btn_Abbrechen;
        }

        private static Label Beschriftung(Control eltern, string text, int x, int y)
        {
            Label l = new Label();
            l.SetBounds(x, y + 4, 132, 18);
            l.Text = text;
            eltern.Controls.Add(l);
            return l;
        }

        private static ComboBox Auswahl(Control eltern, int x, int y, int breite)
        {
            ComboBox c = new ComboBox();
            c.SetBounds(x, y, breite, 22);
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            eltern.Controls.Add(c);
            return c;
        }

        // ==================================================================
        // Listen und Zustand
        // ==================================================================

        private void ListenFuellen(GanglinienVorschau vorschau)
        {
            cbo_Trennzeichen.Items.Clear();
            cbo_Trennzeichen.Items.Add(MyResource.Resource.IMPORT_TRENN_SEMIKOLON);
            cbo_Trennzeichen.Items.Add(MyResource.Resource.IMPORT_TRENN_KOMMA);
            cbo_Trennzeichen.Items.Add(MyResource.Resource.IMPORT_TRENN_TABULATOR);
            cbo_Trennzeichen.Items.Add(MyResource.Resource.IMPORT_TRENN_PIPE);
            cbo_Trennzeichen.Items.Add(MyResource.Resource.IMPORT_TRENN_KEINES);

            cbo_Dezimal.Items.Clear();
            cbo_Dezimal.Items.Add(MyResource.Resource.IMPORT_DEZ_KOMMA);
            cbo_Dezimal.Items.Add(MyResource.Resource.IMPORT_DEZ_PUNKT);

            cbo_Einheit.Items.Clear();
            cbo_Einheit.Items.Add(MyResource.Resource.IMPORT_EINHEIT_KW);
            cbo_Einheit.Items.Add(MyResource.Resource.IMPORT_EINHEIT_KWH);

            cbo_Raster.Items.Clear();
            cbo_Raster.Items.Add(MyResource.Resource.IMPORT_RASTER_AUTO);
            cbo_Raster.Items.Add(MyResource.Resource.IMPORT_RASTER_STUNDE);
            cbo_Raster.Items.Add(MyResource.Resource.IMPORT_RASTER_VIERTEL);
            cbo_Raster.Items.Add(MyResource.Resource.IMPORT_RASTER_MINUTE);

            cbo_Konvention.Items.Clear();
            cbo_Konvention.Items.Add(MyResource.Resource.IMPORT_KONV_AUTO);
            cbo_Konvention.Items.Add(MyResource.Resource.IMPORT_KONV_ANFANG);
            cbo_Konvention.Items.Add(MyResource.Resource.IMPORT_KONV_ENDE);

            cbo_Blatt.Items.Clear();
            if (vorschau != null)
                foreach (string b in vorschau.Blaetter) cbo_Blatt.Items.Add(b);

            int spalten = vorschau != null ? Math.Max(vorschau.Spaltenzahl, 1) : 1;
            cbo_Wertspalte.Items.Clear();
            cbo_Zeitspalte.Items.Clear();
            cbo_Zeitspalte.Items.Add(MyResource.Resource.IMPORT_SPALTE_KEINE);
            for (int i = 1; i <= spalten; i++)
            {
                string t = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_SPALTE_N, i);
                cbo_Wertspalte.Items.Add(t);
                cbo_Zeitspalte.Items.Add(t);
            }
        }

        private void OptionenInDialog()
        {
            m_bAufbau = true;
            cbo_Trennzeichen.SelectedIndex = Index(Trennzeichenwerte, Optionen.Trennzeichen, 4);
            cbo_Dezimal.SelectedIndex = Index(Dezimalwerte, Optionen.Dezimaltrenner, 1);
            cbo_Einheit.SelectedIndex = Index(Einheitswerte, Optionen.Einheit, 0);
            cbo_Raster.SelectedIndex = Index(Rasterwerte, Optionen.Raster, 0);
            cbo_Konvention.SelectedIndex = Index(Konventionswerte, Optionen.Konvention, 0);
            chk_Kopfzeile.Checked = Optionen.Kopfzeile;

            cbo_Wertspalte.SelectedIndex = Grenzen(Optionen.WertSpalte, cbo_Wertspalte.Items.Count);
            cbo_Zeitspalte.SelectedIndex = Grenzen(Optionen.ZeitSpalte + 1, cbo_Zeitspalte.Items.Count);

            if (cbo_Blatt.Items.Count > 0)
            {
                int i = cbo_Blatt.Items.IndexOf(Optionen.Blattname ?? "");
                cbo_Blatt.SelectedIndex = i >= 0 ? i : 0;
            }
            m_bAufbau = false;
        }

        private void DialogInOptionen()
        {
            Optionen.Trennzeichen = Wert(Trennzeichenwerte, cbo_Trennzeichen.SelectedIndex, '\0');
            Optionen.Dezimaltrenner = Wert(Dezimalwerte, cbo_Dezimal.SelectedIndex, '.');
            Optionen.Einheit = Wert(Einheitswerte, cbo_Einheit.SelectedIndex, GanglinienEinheit.Kilowatt);
            Optionen.Raster = Wert(Rasterwerte, cbo_Raster.SelectedIndex, GanglinienRaster.Unbekannt);
            Optionen.Konvention = Wert(Konventionswerte, cbo_Konvention.SelectedIndex, IntervallKonvention.Automatisch);
            Optionen.Kopfzeile = chk_Kopfzeile.Checked;
            Optionen.WertSpalte = Math.Max(0, cbo_Wertspalte.SelectedIndex);
            Optionen.ZeitSpalte = cbo_Zeitspalte.SelectedIndex - 1;
            Optionen.Blattname = cbo_Blatt.SelectedItem != null ? cbo_Blatt.SelectedItem.ToString() : "";
        }

        private static int Index<T>(T[] werte, T gesucht, int vorgabe)
        {
            for (int i = 0; i < werte.Length; i++)
                if (Equals(werte[i], gesucht)) return i;
            return vorgabe;
        }

        private static T Wert<T>(T[] werte, int index, T vorgabe)
        {
            return index >= 0 && index < werte.Length ? werte[index] : vorgabe;
        }

        private static int Grenzen(int index, int anzahl)
        {
            if (anzahl <= 0) return -1;
            if (index < 0) return 0;
            return index < anzahl ? index : anzahl - 1;
        }

        // ==================================================================
        // Vorschau
        // ==================================================================

        private void VorschauFuellen(GanglinienVorschau vorschau)
        {
            listView_Vorschau.BeginUpdate();
            try
            {
                listView_Vorschau.Items.Clear();
                listView_Vorschau.Columns.Clear();
                if (vorschau == null || vorschau.Zeilen.Count == 0) return;

                int spalten = Math.Max(vorschau.Spaltenzahl, 1);
                listView_Vorschau.Columns.Add(MyResource.Resource.IMPORT_SPALTE_ZEILE, 60);
                for (int s = 1; s <= spalten; s++)
                    listView_Vorschau.Columns.Add(
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_SPALTE_N, s), 140);

                for (int z = 0; z < vorschau.Zeilen.Count; z++)
                {
                    string[] felder = vorschau.Zeilen[z];
                    ListViewItem item = new ListViewItem((z + 1).ToString(CultureInfo.CurrentCulture));
                    for (int s = 0; s < spalten; s++)
                        item.SubItems.Add(s < felder.Length ? felder[s] : "");
                    if (z == 0 && Optionen.Kopfzeile) item.ForeColor = SystemColors.GrayText;
                    listView_Vorschau.Items.Add(item);
                }
            }
            finally { listView_Vorschau.EndUpdate(); }
        }

        private void Aktualisieren_Click(object sender, EventArgs e)
        {
            if (m_bAufbau) return;
            DialogInOptionen();

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                // Die Vorschau folgt den Optionen des Anwenders, nicht der Erkennung:
                // GanglinienDatei.Vorschau raet nichts, sondern zerlegt mit dem
                // gewaehlten Trennzeichen und dem gewaehlten Tabellenblatt.
                GanglinienVorschau neu = GanglinienDatei.Vorschau(m_szPfad, Optionen);
                if (neu != null && neu.Lesbar)
                {
                    GanglinienImportOptionen behalten = Optionen;
                    ListenFuellen(neu);
                    Optionen = behalten;
                    OptionenInDialog();
                    VorschauFuellen(neu);
                }
            }
            finally { Cursor.Current = Cursors.Default; }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            DialogInOptionen();
        }
    }
}
