using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Admin-Dublettensuche ueber die Kataloge der <see cref="KatalogRegistry"/>
    /// (Konzept_Dublettenpruefung_Import_EPOS-Plan.md, Abschnitt 5, Paket D3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reine Code-Form ohne Designer und ohne eigene <c>.resx</c> - Muster
    /// <c>Form_Gesetzesparameter</c>. Alle Anzeigetexte kommen aus <c>MyResource</c>,
    /// alle Steuerwerte sind die sprachneutralen Katalog-Schluessel der Registry
    /// (Drei-Schichten-Regel: kein Anzeigetext ist je Steuerwert).
    /// </para>
    /// <para>
    /// Der Scan selbst liegt in <see cref="DublettenPruefung"/> (rein lesend), das
    /// Bereinigen in <see cref="KatalogBereinigung"/> (Leerkopien-Regel aus
    /// Migrationsschritt 24 samt Datenblock-Bedingung). Diese Maske ist nur die
    /// Oberflaeche darum herum: Baum der Namens-/Inhaltsgruppen, Gegenueberstellung,
    /// gefuehrtes Bereinigen, Einzel-Loeschen mit Verwendungspruefung (Konzept 5.3),
    /// Umbenennen mit Namensvalidierung und ein speicherbares Sitzungsprotokoll.
    /// </para>
    /// <para>
    /// Auslieferungssaetze (<c>ReadOnly</c>) werden hier weder geloescht noch
    /// umbenannt - dieselbe Zusage wie in den bestehenden Loeschsperren der
    /// Admin-Masken und in <see cref="KatalogBereinigung"/>.
    /// </para>
    /// </remarks>
    public class Form_KatalogDubletten : Form
    {
        private ComboBox _cbKatalog;
        private Button _btnPruefen;
        private Label _lblStatus;
        private TreeView _tree;
        private TextBox _tbDetails;
        private Button _btnBereinigen;
        private Button _btnLoeschen;
        private Button _btnUmbenennen;
        private Button _btnProtokoll;
        private TextBox _tbProtokoll;

        /// <summary>Scanergebnisse je Katalog-Schluessel (nur gescannte Kataloge).</summary>
        private readonly Dictionary<string, ScanErgebnis> _ergebnisse =
            new Dictionary<string, ScanErgebnis>(StringComparer.Ordinal);

        /// <summary>Sitzungsprotokoll (append-only), Quelle fuer "Protokoll speichern".</summary>
        private readonly List<string> _protokoll = new List<string>();

        public Form_KatalogDubletten()
        {
            BaueOberflaeche();
            TexteSetzen();
            KatalogeFuellen();
        }

        // ==================================================================
        // Oberflaeche
        // ==================================================================

        private void BaueOberflaeche()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.ClientSize = new Size(1000, 640);
            this.MinimumSize = new Size(840, 540);

            _cbKatalog = new ComboBox
            {
                Name = "cbKatalog",
                Location = new Point(12, 12),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(_cbKatalog);

            _btnPruefen = new Button
            {
                Name = "btnPruefen",
                Location = new Point(300, 10),
                Width = 110
            };
            _btnPruefen.Click += btnPruefen_Click;
            this.Controls.Add(_btnPruefen);

            _lblStatus = new Label
            {
                Name = "lblStatus",
                Location = new Point(420, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 90, 160)
            };
            this.Controls.Add(_lblStatus);

            _tree = new TreeView
            {
                Name = "treeErgebnis",
                Location = new Point(12, 44),
                Size = new Size(450, 372),
                HideSelection = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };
            _tree.AfterSelect += tree_AfterSelect;
            this.Controls.Add(_tree);

            _tbDetails = new TextBox
            {
                Name = "tbDetails",
                Location = new Point(470, 44),
                Size = new Size(518, 372),
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                Font = new Font(FontFamily.GenericMonospace, 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(_tbDetails);

            _btnBereinigen = new Button
            {
                Name = "btnBereinigen",
                Location = new Point(12, 428),
                Width = 170,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnBereinigen.Click += btnBereinigen_Click;
            this.Controls.Add(_btnBereinigen);

            _btnLoeschen = new Button
            {
                Name = "btnLoeschen",
                Location = new Point(188, 428),
                Width = 130,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnLoeschen.Click += btnLoeschen_Click;
            this.Controls.Add(_btnLoeschen);

            _btnUmbenennen = new Button
            {
                Name = "btnUmbenennen",
                Location = new Point(324, 428),
                Width = 145,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnUmbenennen.Click += btnUmbenennen_Click;
            this.Controls.Add(_btnUmbenennen);

            _btnProtokoll = new Button
            {
                Name = "btnProtokoll",
                Location = new Point(475, 428),
                Width = 150,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnProtokoll.Click += btnProtokoll_Click;
            this.Controls.Add(_btnProtokoll);

            _tbProtokoll = new TextBox
            {
                Name = "tbProtokoll",
                Location = new Point(12, 462),
                Size = new Size(976, 166),
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(_tbProtokoll);
        }

        /// <summary>Alle Anzeigetexte aus MyResource - nach dem Aufbau gesetzt.</summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.ADM_DUBLETTEN_TITEL;
            _btnPruefen.Text = MyResource.Resource.ADM_DUBLETTEN_PRUEFEN;
            _btnBereinigen.Text = MyResource.Resource.ADM_DUBLETTEN_BTN_BEREINIGEN;
            _btnLoeschen.Text = MyResource.Resource.ADM_DUBLETTEN_BTN_LOESCHEN;
            _btnUmbenennen.Text = MyResource.Resource.ADM_DUBLETTEN_BTN_UMBENENNEN;
            _btnProtokoll.Text = MyResource.Resource.ADM_DUBLETTEN_BTN_PROTOKOLL;
            _lblStatus.Text = "";
        }

        // ==================================================================
        // Katalogauswahl (Anzeige -> Schluessel, kein Anzeigetext als Steuerwert)
        // ==================================================================

        /// <summary>Ein Eintrag der Katalogauswahl: Steuerwert + lokalisierte Anzeige.</summary>
        internal sealed class KatalogItem
        {
            public KatalogItem(string schluessel, string anzeige) { Schluessel = schluessel; Anzeige = anzeige; }
            /// <summary>Registry-Schluessel; leer = alle Kataloge.</summary>
            public string Schluessel { get; private set; }
            public string Anzeige { get; private set; }
            public override string ToString() { return Anzeige; }
        }

        /// <summary>Zuordnung Schluessel -> Anzeigename fuer alle Registry-Kataloge.</summary>
        internal static List<KeyValuePair<string, string>> KatalogAuswahl()
        {
            var liste = new List<KeyValuePair<string, string>>();
            foreach (KatalogDefinition k in KatalogRegistry.Alle)
                liste.Add(new KeyValuePair<string, string>(k.Schluessel, KatalogAnzeige(k.Schluessel)));
            return liste;
        }

        /// <summary>Lokalisierter Anzeigename eines Katalogs; unbekannte Schluessel zeigen sich selbst.</summary>
        internal static string KatalogAnzeige(string schluessel)
        {
            switch (schluessel)
            {
                case "WP": return MyResource.Resource.ADM_KATALOG_WP;
                case "HEIZKESSEL": return MyResource.Resource.ADM_KATALOG_HEIZKESSEL;
                case "PUFFERSPEICHER": return MyResource.Resource.ADM_KATALOG_PUFFERSPEICHER;
                case "SOLARKOLLEKTOREN": return MyResource.Resource.ADM_KATALOG_SOLARKOLLEKTOREN;
                case "PV": return MyResource.Resource.ADM_KATALOG_PV;
                case "BHKW": return MyResource.Resource.ADM_KATALOG_BHKW;
                case "STROMSPEICHER": return MyResource.Resource.ADM_KATALOG_STROMSPEICHER;
                case "GEBAEUDE": return MyResource.Resource.ADM_KATALOG_GEBAEUDE;
                case "KLIMAREGION": return MyResource.Resource.ADM_KATALOG_KLIMAREGION;
                case "BRAUCHWASSER": return MyResource.Resource.ADM_KATALOG_BRAUCHWASSER;
                case "BRAUCHWASSERTYP": return MyResource.Resource.ADM_KATALOG_BRAUCHWASSERTYP;
                case "STROMVERBRAUCHER": return MyResource.Resource.ADM_KATALOG_STROMVERBRAUCHER;
                case "STROMVERBRAUCHERTYP": return MyResource.Resource.ADM_KATALOG_STROMVERBRAUCHERTYP;
                case "PROZESSWAERME": return MyResource.Resource.ADM_KATALOG_PROZESSWAERME;
                case "PROZESSTYP": return MyResource.Resource.ADM_KATALOG_PROZESSTYP;
                case "STROMGANGLINIE": return MyResource.Resource.ADM_KATALOG_STROMGANGLINIE;
                case "SOLARGANGLINIE": return MyResource.Resource.ADM_KATALOG_SOLARGANGLINIE;
                case "WAERMEBEDARF": return MyResource.Resource.ADM_KATALOG_WAERMEBEDARF;
                case "GEBAEUDETYP": return MyResource.Resource.ADM_KATALOG_GEBAEUDETYP;
                default: return schluessel ?? "";
            }
        }

        private void KatalogeFuellen()
        {
            _cbKatalog.Items.Clear();
            _cbKatalog.Items.Add(new KatalogItem("", MyResource.Resource.ADM_DUBLETTEN_ALLE));
            foreach (KeyValuePair<string, string> kv in KatalogAuswahl())
                _cbKatalog.Items.Add(new KatalogItem(kv.Key, kv.Value));
            _cbKatalog.SelectedIndex = 0;
        }

        /// <summary>Der gewaehlte Katalog-Schluessel; leer = alle Kataloge.</summary>
        internal string GewaehlterSchluessel
        {
            get
            {
                KatalogItem i = _cbKatalog.SelectedItem as KatalogItem;
                return i == null ? "" : i.Schluessel;
            }
        }

        // ==================================================================
        // Scan
        // ==================================================================

        private void btnPruefen_Click(object sender, EventArgs e)
        {
            var ziele = new List<KatalogDefinition>();
            string schluessel = GewaehlterSchluessel;
            if (schluessel.Length == 0)
            {
                ziele.AddRange(KatalogRegistry.Alle);
            }
            else
            {
                KatalogDefinition k = KatalogRegistry.Finde(schluessel);
                if (k == null) return;
                ziele.Add(k);
            }

            _btnPruefen.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                foreach (KatalogDefinition k in ziele) Scannen(k);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                _btnPruefen.Enabled = true;
            }

            BaumFuellen();
            StatusNachScan();
        }

        /// <summary>Scannt EINEN Katalog und legt das Ergebnis ab (Fehler ins Protokoll).</summary>
        private void Scannen(KatalogDefinition k)
        {
            _lblStatus.Text = string.Format(MyResource.Resource.ADM_DUBLETTEN_STATUS_PRUEFE,
                                            KatalogAnzeige(k.Schluessel));
            _lblStatus.Refresh();

            ScanErgebnis erg = DublettenPruefung.ScanKatalog(k);
            _ergebnisse[k.Schluessel] = erg;
            if (erg.Fehler != null) Protokoll(k.Tabelle + ": " + erg.Fehler);
        }

        /// <summary>Statuszeile nach dem Scan: "keine Dubletten" oder leer (Baum spricht).</summary>
        private void StatusNachScan()
        {
            int gruppen = 0;
            foreach (ScanErgebnis erg in _ergebnisse.Values)
                if (erg.Fehler == null)
                    gruppen += erg.Namensgruppen.Count + AnzuzeigendeInhaltsgruppen(erg).Count;
            _lblStatus.Text = gruppen == 0 ? MyResource.Resource.ADM_DUBLETTEN_KEINE : "";
        }

        // ==================================================================
        // Ergebnisbaum
        // ==================================================================

        /// <summary>Knoten-Anker: Katalog immer, Gruppe ab Gruppenknoten, Satz nur am Blatt.</summary>
        private sealed class KnotenInfo
        {
            public KatalogDefinition Katalog;
            public DublettenGruppe Gruppe;
            public KatalogSatz Satz;
        }

        /// <summary>
        /// Inhaltsgruppen fuer die Anzeige: Gruppen, deren Saetze alle denselben
        /// normalisierten Namen tragen, stehen bereits als Namensgruppe im Baum und
        /// werden hier NICHT wiederholt.
        /// </summary>
        private static List<DublettenGruppe> AnzuzeigendeInhaltsgruppen(ScanErgebnis erg)
        {
            var liste = new List<DublettenGruppe>();
            foreach (DublettenGruppe g in erg.Inhaltsgruppen)
                if (g.VerschiedeneNamen) liste.Add(g);
            return liste;
        }

        private void BaumFuellen()
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();

            // Reihenfolge der Registry, nur gescannte Kataloge.
            foreach (KatalogDefinition k in KatalogRegistry.Alle)
            {
                ScanErgebnis erg;
                if (!_ergebnisse.TryGetValue(k.Schluessel, out erg)) continue;

                TreeNode wurzel = new TreeNode(string.Format(MyResource.Resource.ADM_DUBLETTEN_WURZEL,
                    KatalogAnzeige(k.Schluessel), erg.Saetze.Count));
                wurzel.Tag = new KnotenInfo { Katalog = k };

                if (erg.Fehler == null)
                {
                    if (erg.Namensgruppen.Count > 0)
                    {
                        TreeNode ast = new TreeNode(string.Format(
                            MyResource.Resource.ADM_DUBLETTEN_AST_NAMEN, erg.Namensgruppen.Count));
                        ast.Tag = new KnotenInfo { Katalog = k };
                        foreach (DublettenGruppe g in erg.Namensgruppen)
                            ast.Nodes.Add(GruppenKnoten(k, g, g.Saetze[0].Name));
                        wurzel.Nodes.Add(ast);
                    }

                    List<DublettenGruppe> inhalt = AnzuzeigendeInhaltsgruppen(erg);
                    if (inhalt.Count > 0)
                    {
                        TreeNode ast = new TreeNode(string.Format(
                            MyResource.Resource.ADM_DUBLETTEN_AST_INHALT, inhalt.Count));
                        ast.Tag = new KnotenInfo { Katalog = k };
                        foreach (DublettenGruppe g in inhalt)
                            ast.Nodes.Add(GruppenKnoten(k, g, string.Format(
                                MyResource.Resource.ADM_DUBLETTEN_GRUPPE_INHALT, NamensListe(g))));
                        wurzel.Nodes.Add(ast);
                    }
                }

                _tree.Nodes.Add(wurzel);
                wurzel.Expand();
                foreach (TreeNode ast in wurzel.Nodes) ast.Expand();
            }

            _tree.EndUpdate();
            tree_AfterSelect(_tree, new TreeViewEventArgs(_tree.SelectedNode));
        }

        private TreeNode GruppenKnoten(KatalogDefinition k, DublettenGruppe g, string text)
        {
            TreeNode knoten = new TreeNode(text)
            {
                Tag = new KnotenInfo { Katalog = k, Gruppe = g }
            };
            foreach (KatalogSatz s in g.Saetze)
            {
                string blatt = "ID " + s.Id.ToString(CultureInfo.CurrentCulture) + " — " + s.Name +
                               (s.ReadOnly ? " " + MyResource.Resource.ADM_DUBLETTEN_AUSLIEFERUNG : "");
                knoten.Nodes.Add(new TreeNode(blatt)
                {
                    Tag = new KnotenInfo { Katalog = k, Gruppe = g, Satz = s }
                });
            }
            return knoten;
        }

        /// <summary>Die verschiedenen Namen einer Inhaltsgruppe, " / "-getrennt.</summary>
        private static string NamensListe(DublettenGruppe g)
        {
            var namen = new List<string>();
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (KatalogSatz s in g.Saetze)
                if (gesehen.Add(s.NameNormalisiert)) namen.Add(s.Name);
            return string.Join(" / ", namen.ToArray());
        }

        // ==================================================================
        // Detailbereich
        // ==================================================================

        private void tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            KnotenInfo info = e != null && e.Node != null ? e.Node.Tag as KnotenInfo : null;
            _btnBereinigen.Enabled = info != null;
            _btnLoeschen.Enabled = info != null && info.Satz != null;
            _btnUmbenennen.Enabled = info != null && info.Satz != null;
            _tbDetails.Text = DetailText(info);
        }

        /// <summary>
        /// Gruppenauswahl: Feld-Gegenueberstellung Satz 1 gegen jeden weiteren
        /// (nur abweichende Spalten). Satzauswahl: Namens- + Vergleichsspalten des Satzes.
        /// </summary>
        private static string DetailText(KnotenInfo info)
        {
            if (info == null) return "";
            StringBuilder sb = new StringBuilder();

            if (info.Satz != null)
            {
                KatalogSatz s = info.Satz;
                if (s.Zeile == null) return "";
                sb.Append(info.Katalog.NamensSpalte).Append(" = ").Append(Zelle(s.Zeile, info.Katalog.NamensSpalte));
                sb.AppendLine();
                foreach (string sp in DublettenPruefung.VergleichsSpalten(info.Katalog, s.Zeile.Table))
                {
                    sb.Append(sp).Append(" = ").Append(Zelle(s.Zeile, sp));
                    sb.AppendLine();
                }
                return sb.ToString();
            }

            if (info.Gruppe != null && info.Gruppe.Saetze.Count > 1)
            {
                KatalogSatz erster = info.Gruppe.Saetze[0];
                for (int i = 1; i < info.Gruppe.Saetze.Count; i++)
                {
                    KatalogSatz zweiter = info.Gruppe.Saetze[i];
                    if (i > 1) sb.AppendLine();
                    sb.Append("ID ").Append(erster.Id).Append(" \"").Append(erster.Name).Append("\"  |  ")
                      .Append("ID ").Append(zweiter.Id).Append(" \"").Append(zweiter.Name).Append("\"");
                    sb.AppendLine();
                    foreach (string sp in DublettenPruefung.AbweichendeSpalten(info.Katalog, erster, zweiter))
                    {
                        sb.Append(sp).Append(": ").Append(Zelle(erster.Zeile, sp))
                          .Append(" | ").Append(Zelle(zweiter.Zeile, sp));
                        sb.AppendLine();
                    }
                }
                return sb.ToString();
            }

            return "";
        }

        /// <summary>Zellwert als Anzeige-Text; NULL und unbekannte Spalten leer.</summary>
        private static string Zelle(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return "";
            object v = r[spalte];
            if (v == null || v is DBNull) return "";
            return Convert.ToString(v, CultureInfo.CurrentCulture);
        }

        // ==================================================================
        // Aktionen
        // ==================================================================

        /// <summary>Der KnotenInfo der Auswahl; ohne Auswahl der einzige gescannte Katalog.</summary>
        private KnotenInfo AktuelleAuswahl()
        {
            if (_tree.SelectedNode != null) return _tree.SelectedNode.Tag as KnotenInfo;
            if (_ergebnisse.Count == 1)
                foreach (ScanErgebnis erg in _ergebnisse.Values)
                    return new KnotenInfo { Katalog = erg.Katalog };
            return null;
        }

        /// <summary>
        /// Leerkopien-Regel auf die gewaehlte Gruppe bzw. (Wurzel-/Astknoten) auf den
        /// ganzen Katalog anwenden - mit Rueckfrage samt Gruppenanzahl. Gefuellte oder
        /// schreibgeschuetzte Dubletten bleiben stehen und landen im Protokoll.
        /// </summary>
        private void btnBereinigen_Click(object sender, EventArgs e)
        {
            KnotenInfo info = AktuelleAuswahl();
            if (info == null || info.Katalog == null) return;
            KatalogDefinition k = info.Katalog;

            int gruppen;
            if (info.Gruppe != null)
            {
                gruppen = 1;
            }
            else
            {
                ScanErgebnis erg;
                gruppen = _ergebnisse.TryGetValue(k.Schluessel, out erg) && erg.Fehler == null
                    ? erg.Namensgruppen.Count : 0;
            }
            if (gruppen == 0)
            {
                MessageBox.Show(MyResource.Resource.ADM_DUBLETTEN_KEINE,
                                MyResource.Resource.ADM_DUBLETTEN_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                    string.Format(MyResource.Resource.ADM_DUBLETTEN_FRAGE_BEREINIGEN, gruppen),
                    MyResource.Resource.ADM_DUBLETTEN_TITEL,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            BereinigungsErgebnis berg;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (info.Gruppe != null)
                {
                    berg = new BereinigungsErgebnis();
                    KatalogBereinigung.GruppeBereinigen(k, info.Gruppe, berg);
                }
                else
                {
                    berg = KatalogBereinigung.LeereKopienBereinigen(k);
                }
            }
            finally { Cursor.Current = Cursors.Default; }

            foreach (string zeile in berg.Protokoll) Protokoll(zeile);
            NeuScannen(k);
        }

        /// <summary>
        /// Einzel-Loeschen eines Blatts: ReadOnly hart gesperrt, davor die
        /// Verwendungspruefung (Konzept 5.3) und die ausdrueckliche Bestaetigung.
        /// Geloescht wird kaskadiert ueber <see cref="KatalogBereinigung.SatzLoeschen"/>.
        /// </summary>
        private void btnLoeschen_Click(object sender, EventArgs e)
        {
            KnotenInfo info = AktuelleAuswahl();
            if (info == null || info.Satz == null) return;
            KatalogDefinition k = info.Katalog;
            KatalogSatz satz = info.Satz;

            // Auslieferungsbestand nie anfassen - dieselbe Zusage wie ueberall.
            if (satz.ReadOnly)
            {
                MessageBox.Show(MyResource.Resource.ADM_DUBLETTEN_READONLY_GESPERRT,
                                MyResource.Resource.ADM_DUBLETTEN_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verwendungspruefung: nachgewiesene Referenzstellen der Registry zaehlen.
            if (k.VerwendungsPruefungen.Length == 0)
            {
                MessageBox.Show(MyResource.Resource.ADM_DUBLETTEN_KEINE_VERWENDUNGSPRUEFUNG,
                                MyResource.Resource.ADM_DUBLETTEN_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var treffer = new List<string>();
                foreach (VerwendungsPruefung vp in k.VerwendungsPruefungen)
                {
                    int anzahl = VerwendungZaehlen(vp, satz);
                    if (anzahl > 0)
                        treffer.Add(vp.Tabelle + " (" + anzahl.ToString(CultureInfo.CurrentCulture) + ")");
                }
                if (treffer.Count > 0 &&
                    MessageBox.Show(
                        string.Format(MyResource.Resource.ADM_DUBLETTEN_VERWENDET,
                                      string.Join(", ", treffer.ToArray())),
                        MyResource.Resource.ADM_DUBLETTEN_TITEL,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    return;
            }

            if (MessageBox.Show(
                    string.Format(MyResource.Resource.ADM_DUBLETTEN_FRAGE_LOESCHEN, satz.Name, satz.Id),
                    MyResource.Resource.ADM_DUBLETTEN_TITEL,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            bool ok = KatalogBereinigung.SatzLoeschen(k, satz.Id);
            Protokoll(k.Tabelle + ", ID " + satz.Id + " \"" + satz.Name + "\": " +
                      (ok ? "geloescht." : "Loeschen fehlgeschlagen - die Zeile bleibt stehen."));
            if (ok) NeuScannen(k);
        }

        /// <summary>SELECT COUNT(*) einer Verwendungs-Pruefabfrage (Name oder Katalog-ID).</summary>
        private static int VerwendungZaehlen(VerwendungsPruefung vp, KatalogSatz satz)
        {
            try
            {
                object anz = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM [" + vp.Tabelle + "] WHERE [" + vp.Spalte + "] = ?",
                    new OleDbParameter("@wert", vp.UeberName ? (object)(satz.Name ?? "") : (object)satz.Id));
                return anz == null || anz is DBNull ? 0 : Convert.ToInt32(anz);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Umbenennen eines Blatts: ReadOnly gesperrt (die Migration faende den Satz
        /// nicht wieder - KONTEXT_Stammdaten_Aenderbarkeit.md 4.3); neuer Name nicht
        /// leer und normalisiert nicht anderweitig vergeben (Konzept 4.3).
        /// </summary>
        private void btnUmbenennen_Click(object sender, EventArgs e)
        {
            KnotenInfo info = AktuelleAuswahl();
            if (info == null || info.Satz == null) return;
            KatalogDefinition k = info.Katalog;
            KatalogSatz satz = info.Satz;

            if (satz.ReadOnly)
            {
                MessageBox.Show(MyResource.Resource.ADM_DUBLETTEN_READONLY_GESPERRT,
                                MyResource.Resource.ADM_DUBLETTEN_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string neu = NameErfragen(k, satz);
            if (neu == null) return;

            bool ok = DataRepository.ExecuteSQL(
                "UPDATE [" + k.Tabelle + "] SET [" + k.NamensSpalte + "] = ? WHERE [" + k.IdSpalte + "] = ?",
                new OleDbParameter("@name", neu),
                new OleDbParameter("@id", satz.Id));

            Protokoll(k.Tabelle + ", ID " + satz.Id + ": \"" + satz.Name + "\" -> \"" + neu + "\"" +
                      (ok ? "" : " (Umbenennen fehlgeschlagen)"));
            if (ok) NeuScannen(k);
        }

        /// <summary>
        /// Kleiner Eingabedialog fuer den neuen Namen. Der eigene (normalisierte) Name
        /// des Satzes bleibt erlaubt, damit sich Schreibweisen (Leerzeichen,
        /// Gross/Klein) korrigieren lassen. Rueckgabe null = Abbruch.
        /// </summary>
        private string NameErfragen(KatalogDefinition k, KatalogSatz satz)
        {
            HashSet<string> vergeben = DublettenPruefung.VergebeneNamen(k);
            string ergebnis = null;

            using (Form dlg = new Form())
            {
                dlg.Text = MyResource.Resource.ADM_DUBLETTEN_BTN_UMBENENNEN;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ClientSize = new Size(460, 108);

                dlg.Controls.Add(new Label
                {
                    Text = MyResource.Resource.ADM_DUBLETTEN_NAME_NEU,
                    Location = new Point(12, 12),
                    AutoSize = true
                });

                TextBox tb = new TextBox
                {
                    Name = "tbNameNeu",
                    Location = new Point(12, 34),
                    Width = 436,
                    Text = satz.Name
                };
                dlg.Controls.Add(tb);

                Button ok = new Button
                {
                    Name = "btnOk",
                    Text = MyResource.Resource.IMP_KONFLIKT_OK,
                    Location = new Point(252, 70),
                    Width = 96
                };
                ok.Click += (s2, e2) =>
                {
                    string neu = (tb.Text ?? "").Trim();
                    string norm = DublettenPruefung.NormalisiereName(neu);
                    if (neu.Length == 0 ||
                        (!string.Equals(norm, satz.NameNormalisiert, StringComparison.Ordinal) &&
                         vergeben.Contains(norm)))
                    {
                        MessageBox.Show(string.Format(MyResource.Resource.IMP_KONFLIKT_NAME_UNGUELTIG, neu),
                                        MyResource.Resource.ADM_DUBLETTEN_TITEL,
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    ergebnis = neu;
                    dlg.DialogResult = DialogResult.OK;
                };
                dlg.Controls.Add(ok);

                Button abbruch = new Button
                {
                    Name = "btnAbbruch",
                    Text = MyResource.Resource.IMP_KONFLIKT_ABBRECHEN,
                    Location = new Point(352, 70),
                    Width = 96,
                    DialogResult = DialogResult.Cancel
                };
                dlg.Controls.Add(abbruch);

                dlg.AcceptButton = ok;
                dlg.CancelButton = abbruch;

                if (dlg.ShowDialog(this) != DialogResult.OK) return null;
            }
            return ergebnis;
        }

        private void btnProtokoll_Click(object sender, EventArgs e)
        {
            if (_protokoll.Count == 0) return;

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "*.txt|*.txt";
                dlg.DefaultExt = "txt";
                dlg.FileName = "KatalogDubletten.txt";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    // CRLF je Zeile, UTF-8 - lesbar in Editor und Excel.
                    File.WriteAllText(dlg.FileName,
                        string.Join("\r\n", _protokoll.ToArray()) + "\r\n", Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, MyResource.Resource.ADM_DUBLETTEN_TITEL,
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // ==================================================================
        // Helfer
        // ==================================================================

        /// <summary>Katalog nach einer Aktion neu scannen und den Baum neu aufbauen.</summary>
        private void NeuScannen(KatalogDefinition k)
        {
            Cursor.Current = Cursors.WaitCursor;
            try { Scannen(k); }
            finally { Cursor.Current = Cursors.Default; }
            BaumFuellen();
            StatusNachScan();
        }

        /// <summary>Eine Zeile ins Sitzungsprotokoll (Liste + Anzeige, append-only).</summary>
        private void Protokoll(string zeile)
        {
            _protokoll.Add(zeile);
            _tbProtokoll.AppendText(zeile + "\r\n");
        }
    }
}
