using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class SpKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemLoeschen;

        // AP9: Variantenverwaltung (Fachkonzept 7.3). Sie haengt bewusst an DIESEM
        // Kontextmenue - eine Speichervariante IST eine Zeile der Uebersichtsliste, und
        // das Aktualisierungsmuster nach jeder Aenderung ist dasselbe wie bei den beiden
        // Bestandseintraegen (Aenderungsdatum + SetSPControl, Fachkonzept 5.5).
        private ToolStripMenuItem ContextMenuItemVarianteNeu;
        private ToolStripMenuItem ContextMenuItemVarianteDuplizieren;
        private ToolStripMenuItem ContextMenuItemVarianteAktiv;
        private ToolStripMenuItem ContextMenuItemVarianteLoeschen;
        private ToolStripMenuItem ContextMenuItemVarianteVergleich;

        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();

        ListView listView_SP;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_SP = ctrl;
            m_ID_Projekt = ID_Projekt;
            m_szProjektname = szProjektname;

            // Menüelemente hinzufügen
            ContextMenuItemNeu = new ToolStripMenuItem();
            ContextMenuItemNeu.Text = "Hinzufügen/Bearbeiten";
            ContextMenuItemNeu.Click += new EventHandler(ContextMenuItemNeu_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemNeu);

            ContextMenuItemLoeschen = new ToolStripMenuItem();
            ContextMenuItemLoeschen.Text = "Löschen";
            ContextMenuItemLoeschen.Click += new EventHandler(ContextMenuItemLoeschen_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemLoeschen);

            // AP9 haengt HINTEN an. Der Grund ist keine Geschmacksfrage: FormMain loest
            // Doppelklick und Ziehen-und-Ablegen ueber
            // contextMenuStrip1.Items[0].PerformClick() aus (FormMain.cs:660, :698) -
            // "Hinzufuegen/Bearbeiten" muss der erste Eintrag bleiben.
            contextMenuStrip1.Items.Add(new ToolStripSeparator());

            ContextMenuItemVarianteNeu = new ToolStripMenuItem();
            ContextMenuItemVarianteNeu.Text = MyResource.Resource.VAR_MENU_NEU;
            ContextMenuItemVarianteNeu.Click += new EventHandler(ContextMenuItemVarianteNeu_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemVarianteNeu);

            ContextMenuItemVarianteDuplizieren = new ToolStripMenuItem();
            ContextMenuItemVarianteDuplizieren.Text = MyResource.Resource.VAR_MENU_DUPLIZIEREN;
            ContextMenuItemVarianteDuplizieren.Click += new EventHandler(ContextMenuItemVarianteDuplizieren_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemVarianteDuplizieren);

            ContextMenuItemVarianteAktiv = new ToolStripMenuItem();
            ContextMenuItemVarianteAktiv.Text = MyResource.Resource.VAR_MENU_AKTIV;
            ContextMenuItemVarianteAktiv.Click += new EventHandler(ContextMenuItemVarianteAktiv_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemVarianteAktiv);

            ContextMenuItemVarianteLoeschen = new ToolStripMenuItem();
            ContextMenuItemVarianteLoeschen.Text = MyResource.Resource.VAR_MENU_LOESCHEN;
            ContextMenuItemVarianteLoeschen.Click += new EventHandler(ContextMenuItemVarianteLoeschen_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemVarianteLoeschen);

            // Der Vergleich braucht ein GERECHNETES Simulationsobjekt (Fachkonzept 7.3:
            // ein Lauf je Variante ueber StromspeicherSimCtrl.RechneVariante). Das gibt
            // es hier nicht - und ein zweiter Rechenpfad ohne Simulationskontext waere
            // genau die Doppelung, die das Konzept ausschliesst. Der Eintrag steht
            // deshalb dauerhaft gesperrt als Wegweiser.
            contextMenuStrip1.Items.Add(new ToolStripSeparator());
            ContextMenuItemVarianteVergleich = new ToolStripMenuItem();
            ContextMenuItemVarianteVergleich.Text = MyResource.Resource.VAR_MENU_VERGLEICH;
            ContextMenuItemVarianteVergleich.Enabled = false;
            contextMenuStrip1.Items.Add(ContextMenuItemVarianteVergleich);

             // Kontextmenü dem ListView zuweisen
            listView_SP.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_SP.MouseDown += new MouseEventHandler(listView_SP_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        /// <summary>
        /// Anlagenzeile (Tab_Energieanlagen.ID) eines Listeneintrags.
        ///
        /// Sie steht als LETZTES Unterelement und hat deshalb keine eigene Spalte -
        /// so bleibt sie unsichtbar. Bis AP3b war das der feste Index 6; seit die
        /// Uebersicht Ertrag und Amortisation fuehrt (FormMain.SetSPControl), waere
        /// jede weitere Spalte an dieser Stelle stillschweigend als ID gelesen worden.
        /// </summary>
        private static int AnlagenId(ListViewItem item)
        {
            return Int32.Parse(item.SubItems[item.SubItems.Count - 1].Text);
        }

        private void listView_SP_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_SP.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    if (listView_SP.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                        item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_SP, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            // Statt ueber Items[0]/Items[1]: Seit AP9 haengen weitere Eintraege am
            // selben Menue, und Positionsindizes waeren beim naechsten Einschub still
            // falsch geworden.
            bool auswahl = listView_SP.SelectedItems.Count > 0;

            ContextMenuItemNeu.Enabled = true;
            ContextMenuItemLoeschen.Enabled = auswahl;

            // Die Referenzliste (REF_SP_TYP) bleibt unangetastet: Sie fuehrt den
            // Vergleichsfall des Projekts, nicht dessen Planvarianten (Fachkonzept 7.3).
            bool varianten = auswahl && !IstReferenzliste();
            ContextMenuItemVarianteNeu.Enabled = varianten;
            ContextMenuItemVarianteDuplizieren.Enabled = varianten;
            ContextMenuItemVarianteAktiv.Enabled = varianten;
            ContextMenuItemVarianteLoeschen.Enabled = varianten;
        }

        /// <summary>
        /// Ob dieses Kontextmenue an der Referenzliste haengt. Dieselbe Unterscheidung
        /// wie in <see cref="ContextMenuItemNeu_Click"/> - der Name des Steuerelements
        /// ist im Bestand das Kriterium.
        /// </summary>
        private bool IstReferenzliste()
        {
            return listView_SP != null && listView_SP.Name == "listView_SP_REF";
        }

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_SP.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            if (indexes.Count > 0)
            {
                ListViewItem item = listView_SP.Items[indexes[0]];
                listView_SP.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_ID_Waermeerzeuger(m_ID_Projekt, AnlagenId(item));

                // Nachtrag aus AP9b: Traf das Loeschen die AKTIVE Variante, fuehrt das
                // Projekt danach keine mehr - Uebersichtsanzeige und Gesamtsimulation
                // fielen still auf die Aggregation zurueck (Fachkonzept 7.3). Derselbe
                // Nachzug wie beim Loeschen ueber "Variante loeschen".
                AktiveVarianteSicherstellen(m_ID_Projekt);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetSPControl(m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            Form_Stromspeicher frm = new Form_Stromspeicher();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_werzmodel.Clear();
            if (listView_SP.Name == "listView_SP_REF")
            {
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.REF_SP_TYP);
                id_type = WizardItemClass.REF_SP_TYP;
            }
            else
            {
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);
                id_type = WizardItemClass.SP_TYP;
            }
            
            // Vollstaendig gelesene Modelle durchreichen - wie im Karten-Weg
            // (Form_Start.pBox_Stromspeicher_Click). Die Teilkopie aus
            // ID/ID_SP/ID_Type/Bezeichner hat beim Speichern alle uebrigen Anlagenfelder
            // verloren, weil WizardCtrl unten die Anlagen des Typs loescht und ueber
            // Add_WP_Waermeerzeuger komplett neu schreibt - genullt wurden dabei ID_Carrier,
            // Vorlauf/Ruecklauf, Grenzleistung, Betriebsart, Sperrung/Sperrzeiten,
            // Bivalenter_Betrieb, Abschaltpunkt und Nutzungszeit.
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_werzmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_szProjektname);
            frm.m_nType = id_type;
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                // Datenbank aktualisieren
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                Program.mainfrm.SetSPControl(m_szProjektname);
            }
        }

        private void ContextMenuItemBearbeiten_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_SP.SelectedIndices;

            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            Form_AdminStromspeicher frm = new Form_AdminStromspeicher();

            if (indexes.Count <= 0) return;

            frm.list_spmodel.Clear();
            ListViewItem item = listView_SP.Items[indexes[0]];
            int idAnlage = AnlagenId(item);
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID=" + idAnlage);
            if (werzctrl.rows <= 0) return;

            // Vollstaendig gelesenes Modell durchreichen - dieselbe Ursache wie im
            // Hinzufuegen/Bearbeiten-Zweig oben: Add_WP_Waermeerzeuger schreibt alle Felder
            // des Modells zurueck, eine Teilkopie nullt deshalb den Rest der Anlagenzeile.
            WErzeugerModel model = werzctrl.items[0];
            frm.list_spmodel.Add(model);
            frm.m_bItemBearbeiten = true;
            int id_type = model.ID_Type;

            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                // Datenbank aktualisieren
                BearbeiteteAnlageZurueckschreiben(idAnlage, id_type, frm.list_spmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                Program.mainfrm.SetSPControl(m_szProjektname);
            }
        }

        /// <summary>
        /// Schreibt die bearbeitete Anlagenzeile ueber den Bestandsweg zurueck, ohne die
        /// uebrigen Zeilen des Typs zu verlieren: alle Zeilen des Typs laden, die
        /// bearbeitete darin ersetzen und die VOLLSTAENDIGE Liste neu schreiben -
        /// dasselbe Muster wie <c>WPKontextMenuCtrl.ContextMenuItemBearbeiten_Click</c>.
        /// </summary>
        /// <remarks>
        /// Der fruehere Weg (<c>Del_Projekt_Waermeerzeuger(Projekt, Typ)</c> und dann
        /// <c>Add_WP_Waermeerzeuger</c> mit genau EINER Zeile) loeschte ALLE Anlagenzeilen
        /// des Typs und legte nur die bearbeitete wieder an. Seit AP9 ist jede weitere
        /// Speichervariante genau so eine Zeile (Kommentarblock unten) - die uebrigen
        /// Varianten des Projekts waren damit weg, mitsamt ihrer
        /// <c>Tab_StromspeicherVariante</c>-Saetze (Loeschweitergabe). Die AP9b-Rettung
        /// aendert daran nichts: Sie stellt Betriebsparameter nur auf Anlagenzeilen
        /// zurueck, die die Add-Liste erneut enthaelt.
        /// </remarks>
        private void BearbeiteteAnlageZurueckschreiben(int idAnlage, int id_type,
                                                       List<WErzeugerModel> bearbeitet)
        {
            // Ohne bearbeitete Zeile gibt es nichts zu schreiben - Del + Add mit leerer
            // Liste waere genau der Rundumschlag, den diese Methode ausschliesst.
            if (bearbeitet == null || bearbeitet.Count == 0) return;

            WErzeugerCtrl alleCtrl = new WErzeugerCtrl();
            alleCtrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + id_type);

            List<WErzeugerModel> liste = new List<WErzeugerModel>();
            bool ersetzt = false;
            for (int i = 0; i < alleCtrl.rows; i++)
            {
                if (alleCtrl.items[i].ID == idAnlage)
                {
                    liste.Add(bearbeitet[0]);
                    ersetzt = true;
                }
                else
                {
                    liste.Add(alleCtrl.items[i]);
                }
            }

            // Die Zeile ist zwischenzeitlich nicht mehr da (etwa parallel geloescht):
            // Die bearbeitete kommt wie bisher (wieder) hinein, statt still zu verfallen.
            if (!ersetzt) liste.Add(bearbeitet[0]);

            WizardCtrl wizctrl = new WizardCtrl();
            wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
            wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, liste);
        }

        // =====================================================================
        //  AP9 - Variantenverwaltung (Fachkonzept Stromspeicher 7.3)
        // =====================================================================
        //
        // EINE VARIANTE IST EINE ANLAGENZEILE. Das Fachkonzept legt den Ablauf je
        // Variante in drei Schritten fest, und genau diese drei Schritte stehen unten:
        //
        //   1. Geraete-Projektkopie in Tab_Stromspeicher (StromspeicherCtrl.CopyFromStamm,
        //      idempotent). Varianten DESSELBEN Geraets teilen sich diese eine Kopie -
        //      eine zweite waere ein zweites Geraet, kein zweiter Betriebsplan.
        //   2. Eine Zeile in Tab_Energieanlagen mit ID_Type = SP_TYP, ID_SP = Projekt-ID
        //      aus Schritt 1 und Bezeichner = Variantenname.
        //   3. Eine Zeile in Tab_StromspeicherVariante (StromspeicherVarianteCtrl.Insert)
        //      mit den Vorgabewerten beziehungsweise den Parametern der Vorlage.
        //
        // WARUM NICHT Del_Projekt_Waermeerzeuger + Add_WP_Waermeerzeuger wie in den
        // beiden Bestandseintraegen. Dieser Speicherweg loescht ALLE Anlagenzeilen des
        // Typs und schreibt sie neu. Seit Migrationsschritt 11b haengt an jeder
        // SP-Anlagenzeile eine Variantenzeile mit Loeschweitergabe, und Tab_Energieanlagen
        // vergibt die ID als AutoWert - der Rundumschlag raeumt also erst einmal
        // saemtliche Betriebsparameter des Projekts ab. Angelegt wird deshalb ZIELGENAU
        // mit derselben Anweisung und denselben Parametern (WizardCtrl.SQL_ANLAGE_INSERT /
        // AnlagenParameter), die auch Add_WP_Waermeerzeuger und WErzeugerCtrl.Insert
        // benutzen - eine Wahrheit, nur ohne das Loeschen davor.
        //
        // Der Bestandsweg selbst ist mit AP9b abgesichert: WizardCtrl sichert die
        // Variantenzeilen vor dem Loeschen und schreibt sie danach je Anlagen-Bezeichner
        // zurueck (WizardCtrl.SpVariantenSichern / SpVariantenWiederherstellen). Das ist
        // eine Rettung, kein Ersatz fuer den zielgenauen Weg hier: Sie kann nur
        // wiederherstellen, was der Dialog namentlich unveraendert zurueckgibt, und einen
        // umbenannten Variantennamen erkennt sie nicht wieder.
        //
        // AKTIV wird ausschliesslich ueber StromspeicherVarianteCtrl.SetzeAktiv gesetzt
        // (dort steht die Begruendung); die Zusage "hoechstens eine aktive Variante je
        // Projekt" bleibt damit auch ueber die neuen Wege eine einzige Schreibstelle.

        private void ContextMenuItemVarianteNeu_Click(object sender, EventArgs e)
        {
            VarianteAnlegen(false);
        }

        private void ContextMenuItemVarianteDuplizieren_Click(object sender, EventArgs e)
        {
            VarianteAnlegen(true);
        }

        /// <summary>
        /// Legt eine weitere Speichervariante zur markierten Zeile an.
        /// </summary>
        /// <param name="mitParametern">
        /// <c>false</c> = Vorgabewerte des Modells (Menueeintrag "Neue Variante
        /// anlegen"), <c>true</c> = Betriebsparameter der markierten Variante
        /// uebernehmen (Menueeintrag "Variante duplizieren"). Der Ablauf ist sonst
        /// derselbe - zwei getrennte Methoden waeren zwei Stellen, an denen die
        /// Dreischritt-Folge aus 7.3 gepflegt werden muesste.
        /// </param>
        private void VarianteAnlegen(bool mitParametern)
        {
            ListViewItem quellzeile = MarkierteZeile();
            if (quellzeile == null) return;

            int idQuelle = AnlagenId(quellzeile);
            WErzeugerModel anlage = AnlageLesen(idQuelle);
            if (anlage == null) { Melden(MyResource.Resource.VAR_MSG_ANLAGE_FEHLT); return; }

            // Schritt 1: Die Ausgangsvariante hat ihre Geraete-Projektkopie bereits -
            // sie wird weiterverwendet. Nur wenn die Zeile keine fuehrt (Altdatensatz
            // mit ID_SP = 0, Befund 1.2 i), wird sie ueber den Katalognamen nachgezogen;
            // das ist derselbe Aufruf, den Add_WP_Waermeerzeuger:381-385 macht.
            int idGeraet = anlage.ID_SP;
            if (idGeraet <= 0)
                idGeraet = new StromspeicherCtrl().CopyFromStamm(anlage.Bezeichner, m_ID_Projekt);
            if (idGeraet <= 0) { Melden(MyResource.Resource.VAR_MSG_GERAET_FEHLT); return; }

            // Namensabfrage nach dem Form_Sp_ItemNeu-Muster (Hausdialog fuer
            // Bezeichnungen; er weist eine leere Eingabe selbst zurueck).
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();
            frm.m_szName = Namensvorschlag(anlage.Bezeichner);
            frm.SetControl();
            frm.ShowDialog();
            if (frm.DialogResult != DialogResult.OK) return;

            string name = (frm.m_szName ?? "").Trim();
            if (name.Length == 0) return;
            if (NameVergeben(name))
            {
                Melden(string.Format(MyResource.Resource.VAR_MSG_NAME_VERGEBEN, name));
                return;
            }

            // Schritt 2: die neue Anlagenzeile. ExecuteInsertAndGetId liefert den
            // AutoWert auf DERSELBEN Verbindung zurueck (SELECT @@IDENTITY) - ein
            // MAX(ID)+1 danach koennte bei parallelem Betrieb eine fremde Zeile treffen.
            anlage.ID_Type = WizardItemClass.SP_TYP;
            anlage.ID_SP = idGeraet;
            anlage.Bezeichner = name;

            int idNeu = DataRepository.ExecuteInsertAndGetId(
                WizardCtrl.SQL_ANLAGE_INSERT,
                WizardCtrl.AnlagenParameter(m_ID_Projekt, anlage));

            if (idNeu <= 0) { Melden(MyResource.Resource.VAR_MSG_ANLEGEN_FEHLER); return; }

            // Schritt 3: die Betriebsfuehrung der Variante.
            StromspeicherVarianteCtrl variantenCtrl = new StromspeicherVarianteCtrl();
            StromspeicherVarianteModel neu = new StromspeicherVarianteModel();

            if (mitParametern)
            {
                StromspeicherVarianteModel vorlage = variantenCtrl.ReadByEnergieanlage(idQuelle);
                if (vorlage != null) neu = ParameterUebernehmen(vorlage);
            }

            neu.ID_Energieanlage = idNeu;
            neu.Aktiv = false;                 // SetzeAktiv ist die einzige Schreibstelle

            if (variantenCtrl.Insert(neu) <= 0)
                Melden(MyResource.Resource.VAR_MSG_VARIANTENZEILE_FEHLER);

            // Ohne aktive Variante haetten Uebersichtsanzeige und Gesamtsimulation
            // keinen Bezug (Fachkonzept 5.5). Fuehrt das Projekt noch keine, wird es
            // die neue - ueber SetzeAktiv, nicht ueber ein zweites UPDATE.
            AktiveVarianteSicherstellen(m_ID_Projekt);

            Auffrischen();
        }

        private void ContextMenuItemVarianteAktiv_Click(object sender, EventArgs e)
        {
            ListViewItem zeile = MarkierteZeile();
            if (zeile == null) return;

            int idAnlage = AnlagenId(zeile);

            if (MessageBox.Show(string.Format(MyResource.Resource.VAR_MSG_AKTIV_FRAGE, zeile.Text),
                                MyResource.Resource.VAR_TITEL,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            StromspeicherVarianteCtrl variantenCtrl = new StromspeicherVarianteCtrl();
            StromspeicherVarianteModel variante = VarianteSicherstellen(variantenCtrl, idAnlage);

            if (variante == null || !variantenCtrl.SetzeAktiv(m_ID_Projekt, variante.ID))
            {
                Melden(MyResource.Resource.VAR_MSG_AKTIV_FEHLER);
                return;
            }

            Auffrischen();
        }

        /// <summary>
        /// Loescht eine Speichervariante samt Betriebsparametern.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Geloescht wird die ANLAGENZEILE ueber den Bestandsweg
        /// <c>WizardCtrl.Del_Projekt_ID_Waermeerzeuger</c>; die Loeschweitergabe der
        /// Beziehung <c>FK_SpVariante_Anlage</c> raeumt die Variantenzeile ab. Der
        /// ausdrueckliche zweite Loeschbefehl kostet eine Anweisung und schliesst den
        /// Fall aus, dass die Beziehung auf einer Datenbank nicht angelegt werden
        /// konnte (Migrationsprotokoll) - die Waise zeigte wegen der
        /// MAX(ID)+1-Vergabe spaeter auf eine FREMDE Anlage.
        /// </para>
        /// <para>
        /// Die Geraete-Projektkopie faellt nur mit, wenn KEINE andere Anlagenzeile sie
        /// mehr fuehrt: Varianten desselben Speichers teilen sie sich.
        /// </para>
        /// </remarks>
        private void ContextMenuItemVarianteLoeschen_Click(object sender, EventArgs e)
        {
            ListViewItem zeile = MarkierteZeile();
            if (zeile == null) return;

            int idAnlage = AnlagenId(zeile);

            if (MessageBox.Show(string.Format(MyResource.Resource.VAR_MSG_LOESCHEN_FRAGE, zeile.Text),
                                MyResource.Resource.VAR_TITEL,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            // Die Geraetekopie MERKEN, solange die Anlagenzeile sie noch nennt.
            WErzeugerModel anlage = AnlageLesen(idAnlage);
            int idGeraet = anlage != null ? anlage.ID_SP : 0;

            if (!new WizardCtrl().Del_Projekt_ID_Waermeerzeuger(m_ID_Projekt, idAnlage))
            {
                Melden(MyResource.Resource.VAR_MSG_LOESCHEN_FEHLER);
                return;
            }

            new StromspeicherVarianteCtrl().DeleteByEnergieanlage(idAnlage);
            GeraetekopieAufraeumen(idGeraet, m_ID_Projekt);
            AktiveVarianteSicherstellen(m_ID_Projekt);

            Auffrischen();
        }

        // ---------------------------------------------------------------------
        //  Helfer der Variantenverwaltung
        // ---------------------------------------------------------------------

        private ListViewItem MarkierteZeile()
        {
            ListView.SelectedIndexCollection indexe = listView_SP.SelectedIndices;
            return indexe.Count > 0 ? listView_SP.Items[indexe[0]] : null;
        }

        /// <summary>Die vollstaendig gelesene Anlagenzeile des Projekts, oder <c>null</c>.</summary>
        private WErzeugerModel AnlageLesen(int idAnlage)
        {
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            ctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID=" + idAnlage);
            return ctrl.rows > 0 ? ctrl.items[0] : null;
        }

        /// <summary>
        /// Betriebsparameter einer Vorlage in ein frisches Modell - ohne
        /// <c>ID</c>, <c>ID_Energieanlage</c> und <c>Aktiv</c>. Die drei sind
        /// Eigenschaften der ZEILE, nicht der Betriebsfuehrung, und eine mitkopierte
        /// Aktiv-Marke ergaebe zwei aktive Varianten.
        /// </summary>
        private static StromspeicherVarianteModel ParameterUebernehmen(StromspeicherVarianteModel vorlage)
        {
            return new StromspeicherVarianteModel
            {
                Betriebsart = vorlage.Betriebsart,
                PV_Zulaessig = vorlage.PV_Zulaessig,
                BHKW_Ueberschuss_Zulaessig = vorlage.BHKW_Ueberschuss_Zulaessig,
                BHKW_Stromgefuehrt = vorlage.BHKW_Stromgefuehrt,
                Netzentladung = vorlage.Netzentladung,
                SoC_Min_Prozent = vorlage.SoC_Min_Prozent,
                SoC_Max_Prozent = vorlage.SoC_Max_Prozent,
                Berechnungsart = vorlage.Berechnungsart,
                Preisquelle = vorlage.Preisquelle,
                ID_Preisreihe = vorlage.ID_Preisreihe,
                ID_Kostenprofil = vorlage.ID_Kostenprofil,
                Aufschlag_Anwenden = vorlage.Aufschlag_Anwenden,
                Kompatibilitaetsmodus = vorlage.Kompatibilitaetsmodus,
                Kapitalzins = vorlage.Kapitalzins,
                Nutzungsdauer = vorlage.Nutzungsdauer,
                L_P = vorlage.L_P,
                A_Netzlade = vorlage.A_Netzlade,
                Ladeschwellwert = vorlage.Ladeschwellwert
            };
        }

        /// <summary>
        /// Die Variantenzeile einer Anlage - notfalls neu angelegt. Anlagenzeilen aus
        /// der Zeit vor Migrationsschritt 11b fuehren noch keine; ohne sie gaebe es
        /// nichts, was "aktiv" heissen koennte.
        /// </summary>
        private static StromspeicherVarianteModel VarianteSicherstellen(StromspeicherVarianteCtrl ctrl,
                                                                        int idAnlage)
        {
            StromspeicherVarianteModel variante = ctrl.ReadByEnergieanlage(idAnlage);
            if (variante != null) return variante;

            variante = new StromspeicherVarianteModel { ID_Energieanlage = idAnlage };
            return ctrl.Insert(variante) > 0 ? variante : null;
        }

        /// <summary>
        /// Sorgt dafuer, dass ein Projekt mit Speichervarianten genau eine aktive
        /// fuehrt: Fehlt sie (erste Variante, oder die aktive wurde geloescht),
        /// uebernimmt die erste in Anlagenreihenfolge - dieselbe Wahl wie
        /// Migrationsschritt 11d.
        /// </summary>
        private static void AktiveVarianteSicherstellen(int idProjekt)
        {
            try
            {
                StromspeicherVarianteCtrl ctrl = new StromspeicherVarianteCtrl();
                if (ctrl.ReadAktiveVariante(idProjekt) != null) return;

                List<StromspeicherVarianteModel> alle = ctrl.ReadAllByProjekt(idProjekt);
                if (alle.Count > 0) ctrl.SetzeAktiv(idProjekt, alle[0].ID);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die aktive Speichervariante konnte nicht nachgezogen werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Loescht die Geraete-Projektkopie, wenn keine Anlagenzeile mehr auf sie zeigt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Gezaehlt wird ueber ALLE Anlagenzeilen, nicht nur die dieses Projekts:
        /// Die Produktivdaten fuehren Zeilen, deren <c>ID_SP</c> auf die Geraetekopie
        /// eines ANDEREN Projekts zeigt (Spuren einer Projektkopie). Wer sie noch
        /// braucht, entscheidet also nicht die Projektgrenze.
        /// </para>
        /// <para>
        /// Geloescht wird umgekehrt NUR innerhalb des eigenen Projekts. Eine fremde
        /// Geraetekopie aufzuraeumen ist nicht Sache dieses Loeschbefehls, auch wenn sie
        /// gerade verwaist ist - und eine Zeile ohne Projektbezug war nie eine
        /// Projektkopie.
        /// </para>
        /// </remarks>
        private static void GeraetekopieAufraeumen(int idGeraet, int idProjekt)
        {
            if (idGeraet <= 0 || idProjekt <= 0) return;

            try
            {
                object anzahl = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_SP = ?",
                    new DbParam("@sp", idGeraet));

                if (anzahl == null || anzahl == DBNull.Value || Convert.ToInt32(anzahl) > 0) return;

                DataRepository.ExecuteSQL(
                    "DELETE FROM Tab_Stromspeicher WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@id", idGeraet),
                    new DbParam("@proj", idProjekt));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Geraetekopie des Stromspeichers konnte nicht abgeraeumt werden: " + ex.Message);
            }
        }

        /// <summary>Ist der Name im Projekt schon an eine Anlagenzeile vergeben?</summary>
        private bool NameVergeben(string name)
        {
            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@proj", m_ID_Projekt),
                new DbParam("@bez", name ?? ""));

            return anzahl != null && anzahl != DBNull.Value && Convert.ToInt32(anzahl) > 0;
        }

        /// <summary>
        /// Erster freier Namensvorschlag zur Vorlage ("Speicher (Variante 2)", …). Die
        /// Obergrenze ist eine Notbremse, kein Fachwert - sie verhindert nur, dass ein
        /// unerwarteter Datenbankzustand die Schleife festhaelt.
        /// </summary>
        private string Namensvorschlag(string basis)
        {
            string stamm = string.IsNullOrEmpty(basis) ? MyResource.Resource.VAR_TITEL : basis;

            for (int n = 2; n <= 99; n++)
            {
                string vorschlag = string.Format(MyResource.Resource.VAR_NAME_VORSCHLAG, stamm, n);
                if (!NameVergeben(vorschlag)) return vorschlag;
            }

            return "";
        }

        /// <summary>
        /// Das Aktualisierungsmuster nach jeder Aenderung (Fachkonzept 5.5): Zuordnungen
        /// stehen, also nur noch Aenderungsdatum fortschreiben und die Uebersicht neu
        /// aufbauen.
        /// </summary>
        private void Auffrischen()
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            projctrl.ReadSingle(m_szProjektname);
            projctrl.m_Aenderungsdatum = DateTime.Now;
            projctrl.Update();

            Program.mainfrm.SetSPControl(m_szProjektname);
        }

        private void Melden(string text)
        {
            MessageBox.Show(text, MyResource.Resource.VAR_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
