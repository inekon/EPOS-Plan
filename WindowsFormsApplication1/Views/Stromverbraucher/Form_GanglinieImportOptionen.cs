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
    /// Die Oberflaeche steht in <c>Form_GanglinieImportOptionen.Designer.cs</c>,
    /// weiterhin ohne eigene <c>.resx</c> (Projekt-CLAUDE.md: resx-Dateien nicht von
    /// Hand editieren). Alle Beschriftungen kommen aus <c>MyResource</c>
    /// (<c>IMPORT_*</c>, der Fussknopf "OK" aus dem generischen <c>SIM_BTN_OK</c>),
    /// sind damit zweisprachig und werden in
    /// <see cref="TexteSetzen"/> gesetzt; im Designer steht der deutsche Text als
    /// Vorschau.
    /// </para>
    /// <para>
    /// <b>Steuerwerte sind Indizes, keine Anzeigetexte</b> (Drei-Schichten-Regel):
    /// die Auswahllisten fuehren feste Wertefelder
    /// (<see cref="Trennzeichenwerte"/> u. a.), die Beschriftung steht daneben.
    /// </para>
    /// </remarks>
    public partial class Form_GanglinieImportOptionen : Form
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

            // Der Designer setzt AutoScaleMode bewusst auf None und laesst
            // AutoScaleDimensions weg: Die Anwendung laeuft DpiUnaware (app.manifest,
            // Program.SetHighDpiMode). Der bisherige Aufbau setzte zwar
            // AutoScaleMode.Font, aber nie AutoScaleDimensions - der Skalierungsfaktor
            // blieb damit immer 1:1, es fand also faktisch keine Skalierung statt.
            // None haelt genau dieses Verhalten fest.
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteSetzen();

            // Das Tabellenblatt gibt es nur bei Excel-Quellen; die Sichtbarkeit haengt
            // damit am Konstruktorparameter und kann nicht im Designer stehen.
            bool istExcel = vorschau != null && vorschau.IstExcel;
            lbl_Blatt.Visible = istExcel;
            cbo_Blatt.Visible = istExcel;

            ListenFuellen(vorschau);
            OptionenInDialog();
            VorschauFuellen(vorschau);
            m_bAufbau = false;

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Laeuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter. Die
        /// beiden zusammengesetzten Texte - Dateiname im Kopf, Zeilenzahl in der
        /// Vorschauueberschrift - stehen ebenfalls hier; <c>m_szPfad</c> ist zu
        /// diesem Zeitpunkt bereits gesetzt.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.IMPORT_TITEL_OPTIONEN;

            lbl_Datei.Text = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.IMPORT_LBL_DATEI, Path.GetFileName(m_szPfad));

            grp_Format.Text = MyResource.Resource.IMPORT_GRP_OPTIONEN;
            lbl_Trennzeichen.Text = MyResource.Resource.IMPORT_LBL_TRENNZEICHEN;
            lbl_Dezimal.Text = MyResource.Resource.IMPORT_LBL_DEZIMALTRENNER;
            lbl_Wertspalte.Text = MyResource.Resource.IMPORT_LBL_WERTSPALTE;
            lbl_Zeitspalte.Text = MyResource.Resource.IMPORT_LBL_ZEITSPALTE;
            lbl_Einheit.Text = MyResource.Resource.IMPORT_LBL_EINHEIT;
            lbl_Raster.Text = MyResource.Resource.IMPORT_LBL_RASTER;
            lbl_Konvention.Text = MyResource.Resource.IMPORT_LBL_KONVENTION;
            lbl_Blatt.Text = MyResource.Resource.IMPORT_LBL_BLATT;
            chk_Kopfzeile.Text = MyResource.Resource.IMPORT_LBL_KOPFZEILE;
            btn_Aktualisieren.Text = MyResource.Resource.IMPORT_BTN_AKTUALISIEREN;

            grp_Vorschau.Text = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.IMPORT_GRP_VORSCHAU, GanglinienDatei.VorschauZeilen);

            lbl_Hinweis.Text = MyResource.Resource.IMPORT_HINWEIS_OPTIONEN;

            // Fusszeilen-Standard (Design-Politur 21.08.2026): Der Abschlussknopf eines
            // Eingabedialogs heisst "OK", nicht "Einlesen" - der Fachknopf der Maske ist
            // btn_Aktualisieren. SIM_BTN_OK ist der im Haus bereits vorhandene generische
            // Schluessel (de "OK", en "OK"), es kommt also kein neuer Schluessel dazu.
            // IMPORT_BTN_OK ("Einlesen"/"Import") bleibt im Katalog unangetastet.
            btn_OK.Text = MyResource.Resource.SIM_BTN_OK;
            btn_Abbrechen.Text = MyResource.Resource.IMPORT_BTN_ABBRECHEN;
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

        // ==================================================================
        // Oberflaeche - Begruendungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen in Form_GanglinieImportOptionen.Designer.cs.
        // Designer-Code traegt keine Kommentare; die Pixelentscheidungen stehen
        // deshalb hier.
        //
        // --- Design-Politur 21.08.2026 -----------------------------------
        //
        // * Echttexte statt Feldnamen. Im Designer standen als Platzhalter die
        //   Feldnamen ("lbl_Trennzeichen" usw.). Jetzt steht dort der deutsche Text
        //   aus MyResource - als reine VORSCHAU, damit im VS-Designer zu sehen ist,
        //   ob die Beschriftungen in ihre Felder passen. Gesetzt werden sie
        //   weiterhin ausschliesslich in TexteSetzen(); die Maske bleibt
        //   zweisprachig. Die beiden zusammengesetzten Texte stehen woertlich mit
        //   ihrem Platzhalter im Designer ("Datei: {0}", "Vorschau (erste {0}
        //   Zeilen)") - sie werden zur Laufzeit ueber string.Format gefuellt.
        //
        // * Das Beschriftungsraster der Formatgruppe war zu eng. Alle Labels waren
        //   132 px breit, die Auswahllisten begannen links bei x = 150 und rechts
        //   bei x = 566. Mit den Echttexten geht das links nicht auf:
        //     - "Zeitstempel bezeichnet:" (lbl_Konvention) misst 132 px
        //       (TextRenderer.MeasureText, Segoe UI 9 pt) und wurde im 132 px
        //       breiten Feld abgeschnitten - ein Label braucht rund 6 px
        //       Innenrand. Nach der Faustformel des Hauses (7 px je Zeichen + 8)
        //       waeren es sogar 169 px.
        //     - Der Abstand Label/Liste betrug links nur 4 px (Label endet bei 146,
        //       Liste beginnt bei 150) und lag damit unter dem Mindestmass von 6 px.
        //   Neu: linke Spalte Label x = 14 mit 170 px Breite, Liste ab x = 192;
        //   rechte Spalte Label x = 412 mit 140 px Breite, Liste ab x = 560. Beide
        //   Breiten decken die Faustformel ab (links 169, rechts 134) und damit
        //   auch das Englische, wo "Unit of the values:" und "Decimal separator:"
        //   die laengsten sind. Der Abstand Label/Liste betraegt jeweils 8 px,
        //   zwischen linker Liste (Ende 392) und rechtem Label 20 px. Die
        //   Listenbreite bleibt 200 px, die rechte Liste endet bei 760 und damit
        //   36 px vor dem Gruppenrand.
        //
        // * btn_Aktualisieren auf 200 x 30 (vorher 200 x 26) - einheitliche
        //   Knopfhoehe 30 im ganzen Dialog - und buendig zur rechten Listenspalte
        //   auf x = 560. Damit die 4 px Mehrhoehe nicht an den Gruppenrand stossen,
        //   waechst grp_Format von 178 auf 182 px Hoehe (Knopf endet bei y = 174,
        //   8 px Rand). chk_Kopfzeile rutscht von y = 146 auf 148, damit oben 10 px
        //   Abstand zur letzten Listenzeile bleiben (deren Unterkante liegt bei 138).
        //
        // * grp_Vorschau folgt auf y = 220 (vorher 218, 6 px Abstand zur nun
        //   tieferen Formatgruppe) und wird um dieselben 2 px auf 256 gekuerzt -
        //   die Unterkante bleibt exakt bei y = 476, die Liste darin unveraendert.
        //
        // * Fussknoepfe auf einheitliche 110 x 30 (vorher btn_OK 90 x 26,
        //   btn_Abbrechen 94 x 26). Die Unterkante bleibt bei y = 548 (Rand 12),
        //   dafuer wandert der Fuss von y = 522 auf 518. Die rechte Kante bleibt bei
        //   x = 808 (Rand 12): btn_Abbrechen ab 698, btn_OK ab 576; der Abstand
        //   zwischen beiden waechst von 8 auf 12 px.
        //
        // * lbl_Hinweis endete mit seinen 560 px Breite bei x = 572 und stiess damit
        //   auf 4 px an den verbreiterten OK-Knopf - unter dem Mindestmass von 6 px.
        //   Neu 556 px breit ab x = 12 (Ende 568, 8 px Luft) und auf y = 482 gehoben
        //   (Unterkante 516, 6 px zur Vorschaugruppe, 2 px ueber dem Knopffuss).
        //   Label und Knoepfe sind beide Bottom-verankert und das Label zusaetzlich
        //   Left|Right - der 8-px-Abstand bleibt deshalb in jeder Fensterbreite
        //   erhalten. Die Hoehe bleibt bei 34 px: Der Text misst 509 px und steht in
        //   der Grundgroesse einzeilig, bei MinimumSize schrumpft das Feld auf 392 px
        //   und der Text bricht auf zwei Zeilen (30 px) um.
        //
        // * ClientSize bleibt 820 x 560, MinimumSize 660 x 460 - die Politur kommt
        //   ohne Mehrflaeche aus.
    }
}
