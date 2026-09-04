using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Rahmenfenster des <b>Projektassistenten</b> (bis P4 „Projekt Wizard" —
    /// Entscheidung E5: In der Anwenderdokumentation heißt er durchgängig
    /// „Assistent"; Klassen- und Dateinamen bleiben unverändert, um keinen
    /// Umbenennungssturm auszulösen).
    /// </summary>
    public partial class WizardParent : Form, IAssistentRahmen
    {
        public const int WIZARD_MODE_NEU = 0;
        public const int WIZARD_MODE_BEARBEITEN = 1;

        // --------------------------------------------------------------------
        //  Typisierte Rahmen-Erkennung (P4)
        // --------------------------------------------------------------------

        private static WizardParent _aktiver;
        private WizardParent _vorheriger;

        /// <summary>
        /// Der zurzeit laufende Assistenten-Rahmen; <c>null</c>, wenn keiner offen ist.
        ///
        /// <para>
        /// Ersetzt die Namenssuche <c>Application.OpenForms</c> → <c>form.Name ==
        /// "WizardParent"</c>, die in elf Fachformularen wortgleich stand. Der Rahmen
        /// trägt sich im Konstruktor ein und beim Schließen wieder aus; ein zuvor
        /// eingetragener Rahmen wird dabei wiederhergestellt, damit auch ein
        /// verschachtelter Lauf keinen verwaisten Eintrag hinterlässt.
        /// </para>
        /// </summary>
        public static IAssistentRahmen Aktiver
        {
            get { return _aktiver; }
        }

        /// <summary>Die Seiten des Assistenten (<see cref="IAssistentRahmen"/>).</summary>
        public List<WizardSeite> Seiten
        {
            get { return listPages; }
        }

        /// <summary>Betriebsart des Assistenten (<see cref="IAssistentRahmen"/>).</summary>
        public int Betriebsart
        {
            get { return wizardmode; }
        }

        /// <summary>Projekt-ID, an der der Assistent arbeitet (<see cref="IAssistentRahmen"/>).</summary>
        public int ProjektID
        {
            get { return projektID; }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Abmelden();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Der Rahmen tritt ab. Wird sowohl beim Schließen als auch beim Verwerfen
        /// gerufen: Ein Rahmen, der gebaut, aber nie angezeigt wurde (Prüfstände,
        /// abgebrochene Einstiege), hinterließe sonst einen Eintrag in
        /// <see cref="Aktiver"/>, der auf ein totes Fenster zeigt.
        /// </summary>
        private void Abmelden()
        {
            if (ReferenceEquals(_aktiver, this)) _aktiver = _vorheriger;
            _vorheriger = null;
        }

        private void RahmenVerworfen(object sender, EventArgs e)
        {
            Abmelden();
        }

        public List<WizardSeite> listPages = new List<WizardSeite>();

        /// <summary>
        /// Die DATENSEITE des Assistentenlaufs (iU9-W16a.4, K3): die sieben Listen,
        /// die sechs Ladewege, die Seitenschaltung und der Speicherlauf stehen seit
        /// dieser Teilwelle in <see cref="AssistentCtrl"/> im Kern. Der Rahmen ist
        /// damit reine Oberflaeche und traegt keine einzige SQL-Anweisung mehr.
        /// </summary>
        private readonly AssistentCtrl m_ctrl = new AssistentCtrl();

        public List<WErzeugerModel> list_werzmodel { get { return m_ctrl.Erzeuger; } }
        public List<Z_ProjektProzesswaermeModel> list_prozmodel { get { return m_ctrl.Prozess; } }
        public List<Z_ProjektStromganglinieModel> list_stromlastmodel { get { return m_ctrl.Stromganglinie; } }
        public List<Z_ProjGebModel> list_gebmodel { get { return m_ctrl.Gebaeude; } }
        public List<Z_ProjektStromverbraucherModel> list_stromverbrauchermodel { get { return m_ctrl.Stromverbraucher; } }
        public List<Z_ProjWaermebedarfModel> list_wbmodel { get { return m_ctrl.Waermebedarf; } }

        /// <summary>
        /// Die GETEILTE Liste der ersten Assistentenseite - sie traegt genau EIN
        /// Element (iU9-W15a.6, Weg (a) der Vermessung § 13.5).
        ///
        /// <para><c>Wizard_Projekt</c> war die einzige Seite, die dieser Rahmen an sechs
        /// Stellen mit hartem Typumbruch auslas (<c>((Wizard_Projekt)page).Get*()</c>,
        /// Befund W15a-B42). Die Razor-Fassung bearbeitet stattdessen diese Liste AN
        /// ORT UND STELLE - dieselbe Mechanik wie die vier Bedarfsseiten aus iU9-W9.0a,
        /// nur mit einer einelementigen Liste und ohne einen neuen Vertrag.</para>
        /// </summary>
        private List<ProjektKopfDaten> m_ProjektKopf { get { return m_ctrl.Kopf; } }

        /// <summary>
        /// Die GETEILTE Liste des Komponentenschritts — dreizehn Kacheln
        /// (iU9-W16a.3, dieselbe Mechanik wie <see cref="m_ProjektKopf"/>).
        ///
        /// <para>Die Hülle baut sie bei jedem Bestücken aus
        /// <see cref="KomponentenBestandCtrl"/> neu auf; die Seite schaltet darin um
        /// und meldet jede Umschaltung als Seitenindex zurück. Der Rahmen liest sie
        /// nicht — er wird über den Rückruf geschaltet.</para>
        /// </summary>
        private readonly List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile> m_Komponenten =
            new List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile>();

        /// <remarks>
        /// Die beiden Attribute halten den WinForms-Analysator (WFO1000) still: Eine
        /// oeffentliche Eigenschaft auf einem Control gilt ihm sonst als
        /// Designer-Eigenschaft, die ihre Serialisierung erklaeren muss. Diese hier
        /// ist eine reine Laufzeitgabe des Assistenten und gehoert in keinen Designer.
        /// </remarks>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int wizardmode
        {
            get { return m_ctrl.Betriebsart; }
            set { m_ctrl.Betriebsart = value; }
        }

        public bool gespeichert { get { return m_ctrl.Gespeichert; } }

        private int top = WizardItemClass.KOMPONENTEN_ITEM;
        private int pagecount;

        /// <remarks>
        /// Die beiden Attribute halten den WinForms-Analysator (WFO1000) still: Eine
        /// oeffentliche Eigenschaft auf einem Control gilt ihm sonst als
        /// Designer-Eigenschaft, die ihre Serialisierung erklaeren muss. Diese hier
        /// ist eine reine Laufzeitgabe des Assistenten und gehoert in keinen Designer.
        /// </remarks>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int projektID
        {
            get { return m_ctrl.ProjektId; }
            set { m_ctrl.ProjektId = value; }
        }

        /// <remarks>
        /// Die beiden Attribute halten den WinForms-Analysator (WFO1000) still: Eine
        /// oeffentliche Eigenschaft auf einem Control gilt ihm sonst als
        /// Designer-Eigenschaft, die ihre Serialisierung erklaeren muss. Diese hier
        /// ist eine reine Laufzeitgabe des Assistenten und gehoert in keinen Designer.
        /// </remarks>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool bBereitsGeladen
        {
            get { return m_ctrl.BereitsGeladen; }
            set { m_ctrl.BereitsGeladen = value; }
        }

        public WizardParent()
        {
            // Die Listen sind im AssistentCtrl schon leer; der Konstruktor des
            // Vorlaeufers leerte sie ausdruecklich, weil sie Felder waren.
            wizardmode = WIZARD_MODE_NEU;
            projektID = 0;
            pagecount = 0;
        }

        public WizardParent(List<WizardSeite> WizardPages)
        {
            InitializeComponent();

            // Typisierte Rahmen-Erkennung: ab hier finden die Fachformulare den Rahmen
            // ueber WizardParent.Aktiver. Die Anmeldung steht im Konstruktor, weil
            // schon WizardParent_Load ueber Next() die erste Fachseite bestueckt.
            _vorheriger = _aktiver;
            _aktiver = this;
            Disposed += RahmenVerworfen;

            //wizardmode = WIZARD_MODE_NEU;
            projektID = 0;
            pagecount = 0;

            // iU9-W16a.4: Der Schaltzustand der dreizehn Seiten steht im
            // AssistentCtrl; sein Konstruktor setzt genau dieselben zwei Seiten auf
            // "aktiv" (Komponentenschritt und Projektkopf) und alle uebrigen auf
            // "aus" - die dreizehn Zeilen hier waren nichts anderes.
            listPages = WizardPages;

            pagecount = listPages.Count();

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        private void WizardParent_Load(object sender, EventArgs e)
        {
            // Linke Spalte ab P4: das UserControl ProjektAuswahl (Liste, Suche,
            // Sortierung) statt der schlichten ListBox - dieselbe Komponente, die auch
            // hinter Menue "Projekt -> Oeffnen..." und der Kachel "Zuletzt geoeffnet"
            // steht. Der Bestand wird EINMAL gelesen; eingeblendet wird die Spalte nur
            // im Bearbeiten-Modus auf der Komponentenseite (siehe Next/Back).
            ucProjektAuswahl.Laden();
            ucProjektAuswahl.Visible = false;
            button_ProjektOeffnen.Visible = false;
            top = -1;
            Next();
            btnBack.Enabled = false;
            btnCancel.Enabled = true;
        }

        private void LoadNewForm()
        {
            Form page = listPages.ElementAt(top).wizardform;
            page.FormBorderStyle = FormBorderStyle.None;
            page.TopLevel = false;
            page.AutoScroll = true;

            // 1. Dock auf None, um die echte ungestauchte Wunschgröße der Unterseite zu ermitteln
            page.Dock = DockStyle.None;

            this.pnlContent.Controls.Clear();
            this.pnlContent.Controls.Add(page);
            page.Show();

            // 2. Berechnen, wie viel Platz die Unterseite im Panel *tatsächlich* benötigt
            int extraBreite = page.PreferredSize.Width - this.pnlContent.Width;
            int extraHoehe = page.PreferredSize.Height - this.pnlContent.Height;

            // 3. Hauptfenster vergrößern, falls die Unterseite mehr Platz beansprucht als vorhanden ist
            if (extraBreite > 0) this.Width += extraBreite;
            if (extraHoehe > 0) this.Height += extraHoehe;

            // 4. Sicherheitscheck: Wenn das Fenster nun größer als der physische Bildschirm wird,
            // begrenzen wir es auf die Bildschirmarbeitsfläche, damit die Buttons nicht unter die Taskleiste rutschen.
            Rectangle bildschirm = Screen.FromControl(this).WorkingArea;

            if (this.Width > bildschirm.Width) this.Width = bildschirm.Width;
            if (this.Height > bildschirm.Height) this.Height = bildschirm.Height;

            // 5. Erst JETZT die Unterseite auf Fill setzen
            page.Dock = DockStyle.Fill;
        }
        /*
        private void LoadNewForm()
        {
            Form page = listPages.ElementAt(top).wizardform;
            page.FormBorderStyle = FormBorderStyle.None;
            page.TopLevel = false;
            page.AutoScroll = true;
            page.Dock = DockStyle.Fill;
            this.pnlContent.Controls.Clear();
            this.pnlContent.Controls.Add(page);
            page.Show();
        }*/

        private void Back()
        {
            top = GetNextDownIndex();

            LoadNewForm();

            if (wizardmode == WIZARD_MODE_BEARBEITEN)
            {
                if (top == WizardItemClass.KOMPONENTEN_ITEM)
                {
                    ucProjektAuswahl.Visible = true;
                    label_Projekt.Visible = true;
                    button_ProjektOeffnen.Visible = true;
                }
            }

            btnBack.Enabled = true;
            btnCancel.Enabled = true;
            btnNext.Enabled = true;

            if (top <= WizardItemClass.KOMPONENTEN_ITEM)
            {
                btnBack.Enabled = false;
            }

            if (top >= pagecount)
            {
                btnCancel.Enabled = false;
            }

            KnopfTexteAnwenden();
        }

        private void Next()
        {
            Form page;

            // Bevor die nächste Seite geladen wird...
            if (wizardmode == WIZARD_MODE_BEARBEITEN && ucProjektAuswahl.GewaehlteID <= 0)
                btnNext.Enabled = false;
            else btnNext.Enabled = true;


            // Projekt Eingaben in Projektmodel speichern
            if (top == WizardItemClass.PROJEKT_ITEM)
            {
                page = listPages.ElementAt(top).wizardform;
                ProjektkopfUebernehmen();

                if (!bBereitsGeladen) m_ctrl.Laden(m_ctrl.Projekt.m_szProjektname);
            }

            top = GetNextUpIndex(); //nächste mögliche Seite...

            // nächste Seite laden...
            LoadNewForm();

            // nachdem die nächste Seite geladen wurde...

            page = listPages.ElementAt(top).wizardform;
            ucProjektAuswahl.Visible = false;
            label_Projekt.Visible = false;
            button_ProjektOeffnen.Visible = false;

            if (wizardmode == WIZARD_MODE_BEARBEITEN)
            {
                // vorhandenes Projekt...
                if (top == WizardItemClass.KOMPONENTEN_ITEM)
                {
                    ucProjektAuswahl.Visible = true;
                    label_Projekt.Visible = true;
                    button_ProjektOeffnen.Visible = true;
                }
                else if (top == WizardItemClass.PROJEKT_ITEM)
                {
                    // Bearbeiten: der Projektname ist gesetzt und NICHT aenderbar.
                    ProjektkopfSeiteBestuecken(page, ucProjektAuswahl.GewaehlterName, aenderbar: false);
                }
            }
            else
            {
                // neues Projekt...
                ProjektCtrl prjctrl = new ProjektCtrl();
                projektID = prjctrl.GetMaxID() + 1;
                if (top == WizardItemClass.PROJEKT_ITEM)
                {
                    // Neu: leerer Name, und er DARF geaendert werden.
                    ProjektkopfSeiteBestuecken(page, "", aenderbar: true);
                }
            }

            // iU9-W6.0e: Die Erzeugerseiten, die schon Razor-Komponenten sind, hängen
            // sich über EINE Schnittstelle ein statt über je zwei Zeilen mit hartem
            // Typumbruch — für eine BlazorAssistentSeite<T> träfe der ohnehin nicht mehr.
            // Der Zweig steht VOR der Kette; die noch nicht umgestellten Seiten fallen
            // wie bisher durch sie hindurch.
            // iU9-W16a.3: Der Komponentenschritt ist eine Razor-Komponente und baut
            // seine WebView erst beim Bestuecken (BlazorAssistentSeite). Er wird auf
            // diesem Weg genau EINMAL erreicht - beim Oeffnen des Assistenten; danach
            // stellt ihn ucProjektAuswahl_MarkierungGeaendert neu. Der Zweig steht
            // ZUERST, weil er eine eigene Liste und einen eigenen Aufbau hat.
            if (top == WizardItemClass.KOMPONENTEN_ITEM)
            {
                KomponentenSeiteBestuecken(page);
            }
            else if (page is IAssistentErzeugerSeite erzeugerSeite)
            {
                erzeugerSeite.Modelle = list_werzmodel;
                erzeugerSeite.Bestuecken(projektID, ucProjektAuswahl.GewaehlterName);
            }
            // iU9-W9.0a: dieselbe Schnittstelle mit VIER anderen Listentypen - die
            // Bedarfsseiten (Gebaeude, Waermebedarf extern, Prozesswaerme,
            // Stromverbraucher). Die vier Zweige stehen einzeln da, weil jeder eine
            // andere Liste meint; der harte Typumbruch auf die Form ist weg.
            else if (page is IAssistentListenSeite<Z_ProjGebModel> gebaeudeSeite)
            {
                gebaeudeSeite.Modelle = list_gebmodel;
                gebaeudeSeite.Bestuecken(projektID, ucProjektAuswahl.GewaehlterName);
            }
            else if (page is IAssistentListenSeite<Z_ProjWaermebedarfModel> waermebedarfSeite)
            {
                waermebedarfSeite.Modelle = list_wbmodel;
                waermebedarfSeite.Bestuecken(projektID, ucProjektAuswahl.GewaehlterName);
            }
            else if (page is IAssistentListenSeite<Z_ProjektProzesswaermeModel> prozessSeite)
            {
                prozessSeite.Modelle = list_prozmodel;
                prozessSeite.Bestuecken(projektID, ucProjektAuswahl.GewaehlterName);
            }
            else if (page is IAssistentListenSeite<Z_ProjektStromverbraucherModel> stromSeite)
            {
                stromSeite.Modelle = list_stromverbrauchermodel;
                stromSeite.Bestuecken(projektID, ucProjektAuswahl.GewaehlterName);
            }
            // iU9-W16a.1: Der LETZTE harte Typumbruch der Kette ist entfallen
            // (Befund W16-B15). Die Stromlastgangseite ist seither eine
            // BlazorAssistentSeite<StromganglinieDialog, Z_ProjektStromganglinieModel>
            // und faellt in den Zweig darunter - die Kette ist damit typfrei.
            else if (page is IAssistentListenSeite<Z_ProjektStromganglinieModel> ganglinieSeite)
            {
                ganglinieSeite.Modelle = list_stromlastmodel;
                ganglinieSeite.Bestuecken(projektID, ucProjektAuswahl.GewaehlterName);
            }

            btnBack.Enabled = true;
            btnCancel.Enabled = true;

            // Auf der letzten aktiven Seite wird der Weiter-Knopf zum
            // Speichern-Knopf (Fusion, Nutzerwunsch 30.08.2026); sein
            // Enabled-Zustand kommt von der Bearbeiten-Sperre oben.
            KnopfTexteAnwenden();

            // bei 1. Seite kein zurück möglich
            if (top <= WizardItemClass.KOMPONENTEN_ITEM)
            {
                btnBack.Enabled = false;
            }
        }

        private bool lastIndex()
        {
            return m_ctrl.LetzteAktive(top);
        }

        /// <summary>
        /// Der fruehere Speichern-Knopf ist entfallen (Nutzerwunsch 30.08.2026):
        /// Auf der letzten aktiven Seite traegt dieser Knopf den Speichern-Text
        /// und schliesst den Durchlauf ab.
        /// </summary>
        private void btnNext_Click(object sender, EventArgs e)
        {
            if (lastIndex()) { SpeichernAusfuehren(); return; }
            Next();
        }

        /// <summary>
        /// Setzt die Beschriftung des Weiter-Knopfs: auf der letzten aktiven
        /// Seite "Speichern" (MyResource WIZ_BTN_SPEICHERN), sonst der
        /// Weiter-Text aus der Formular-resx (beim ersten Aufruf gemerkt).
        /// </summary>
        private string _textWeiter;
        private void KnopfTexteAnwenden()
        {
            if (_textWeiter == null) _textWeiter = btnNext.Text;

            btnNext.Text = lastIndex()
                ? (MyResource.Resource.ResourceManager.GetString("WIZ_BTN_SPEICHERN", MyResource.Resource.Culture) ?? "Speichern")
                : _textWeiter;
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Back();
        }

        private int GetNextUpIndex()
        {
            return m_ctrl.NaechsteAktive(top, +1);
        }

        private int GetNextDownIndex()
        {
            return m_ctrl.NaechsteAktive(top, -1);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Program.wizardctrl.speichern = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Übernimmt den Komponentenbestand des gewählten Projekts auf die Kacheln des
        /// Komponentenschritts (Paket P5).
        ///
        /// <para>
        /// <b>Vorher</b> hieß die Methode <c>SetKompCheckBoxes</c> und setzte elf
        /// Häkchen: erst alle auf <c>false</c>, dann je Fundstelle wieder auf
        /// <c>true</c> — mit eigenen Kriterien, die von der Bitmaske der Startmaske
        /// abwichen (<c>ID_WP &gt; 0</c> &amp; Co.) und ohne Brauchwasser und
        /// Pufferspeicher. <b>Jetzt</b> liest <see cref="KomponentenBestandCtrl"/> den
        /// Bestand nach den Kriterien der Startmaske, und die Seite zeigt ihn als
        /// Kachelfeld — eine Quelle, eine Optik, eine Wahrheit.
        /// </para>
        /// </summary>
        private void KomponentenAusBestandSetzen()
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            projctrl.ReadSingle(ucProjektAuswahl.GewaehlterName);
            projektID = projctrl.m_ID;

            KomponentenSeiteBestuecken(listPages.ElementAt(top).wizardform);
        }

        /// <summary>
        /// Reicht dem Komponentenschritt seine geteilte Liste herein und baut ihn auf
        /// (iU9-W16a.3). Das LESEN des Bestands und das Schalten der Seiten liegt
        /// seither in <c>KomponentenauswahlHuelle.Gaben</c> — es lief bis hierher in
        /// <c>Wizard_Komponenten.BestandAnzeigen</c>.
        /// </summary>
        private void KomponentenSeiteBestuecken(Form page)
        {
            if (!(page is IAssistentListenSeite<EPOS.UI.Dialoge.Bedarf.KomponentenZeile> seite)) return;

            seite.Modelle = m_Komponenten;
            seite.Bestuecken(projektID, ucProjektAuswahl.GewaehlterName ?? "");
        }

        /// <summary>
        /// Ein anderes Projekt wurde in der linken Spalte markiert: Komponentenkacheln
        /// neu aus dem Bestand füllen. Tritt an die Stelle von
        /// <c>listBox_Projekte_SelectedIndexChanged</c>.
        /// </summary>
        private void ucProjektAuswahl_MarkierungGeaendert(int id, string name)
        {
            // Die Spalte steht nur im Bearbeiten-Modus auf der Komponentenseite; ein
            // Ereignis von anderswo (z. B. beim Fuellen der Liste) darf nichts anfassen.
            if (top != WizardItemClass.KOMPONENTEN_ITEM) return;

            if (id <= 0)
            {
                if (wizardmode == WIZARD_MODE_BEARBEITEN) btnNext.Enabled = false;
                return;
            }

            KomponentenAusBestandSetzen();
            btnNext.Enabled = true;
            bBereitsGeladen = false;
        }

        public void SetWizardMode(int mode)
        {
            wizardmode = mode;
        }

        /// <summary>
        /// Uebernimmt die sieben Kopffelder der ersten Assistentenseite in
        /// <see cref="m_Projektmodel"/> (iU9-W15a.6).
        ///
        /// <para>Der Vorlaeufer las sie ueber sieben <c>((Wizard_Projekt)page).Get*()</c>
        /// aus, und zwei davon logen: <c>GetDatum()</c> lieferte <c>DateTime.Now</c>
        /// statt des Feldes (Befund W15a-B39), <c>GetErstellDatum()</c> parste den
        /// ANGEZEIGTEN Text ohne Kultur (B40). Hier steht das Aenderungsdatum
        /// ausdruecklich auf JETZT - das war die Absicht hinter <c>GetDatum</c> und
        /// bleibt so.</para>
        /// </summary>
        private void ProjektkopfUebernehmen()
        {
            m_ctrl.ProjektkopfUebernehmen();
        }

        /// <summary>
        /// Reicht der ersten Assistentenseite die geteilte Liste herein und baut sie auf
        /// (iU9-W15a.6). <see cref="ProjektKopfDaten.NameAenderbar"/> wird VOR
        /// <c>Bestuecken</c> gesetzt - es ist der Ersatz fuer
        /// <c>SetEditProjektName(bool)</c>.
        /// </summary>
        private void ProjektkopfSeiteBestuecken(Form page, string projektName, bool aenderbar)
        {
            if (!(page is IAssistentListenSeite<ProjektKopfDaten> seite)) return;

            m_ProjektKopf[0].NameAenderbar = aenderbar;
            seite.Modelle = m_ProjektKopf;
            seite.Bestuecken(projektID, projektName ?? "");
        }

        /// <summary>
        /// Speichert den Assistentenlauf (iU9-W16a.4).
        ///
        /// <para><b>Der Ablauf steht im Kern</b> (<see cref="AssistentCtrl.Speichern"/>,
        /// K3): die beiden Pflichtpruefungen, die beiden Zweige mit ihren bitgleich
        /// uebernommenen Schreibschritten und die zwei Filter. Hier bleibt, was
        /// Oberflaeche ist - die Meldung und das Schliessen des Fensters.</para>
        ///
        /// <para><b>EINE Meldung statt siebzehn stiller <c>return</c>s</b> (Befund
        /// W16-B16, Entscheid E-4). Der Vorlaeufer brach bei jedem Fehlschlag
        /// kommentarlos ab; wer danach kein Projekt vorfand, erfuhr nicht, woran es
        /// lag. Die vier deutschen Literale der Pflichtpruefungen (Befund W16-B17)
        /// sind vier Ressourcenschluessel geworden.</para>
        /// </summary>
        private void SpeichernAusfuehren()
        {
            AssistentErgebnis ergebnis = m_ctrl.Speichern();

            if (!ergebnis.Erfolg)
            {
                Meldung.Warnung(AssistentCtrl.Meldungstext(ergebnis),
                                AssistentCtrl.Meldungstitel(ergebnis));
                return;
            }

            if (wizardmode == WIZARD_MODE_NEU) this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // E4 (Nutzerentscheid 29.08.2026, Konzept 4/E4 Option a): Der Klick auf das
        // Logo im linken Band oeffnete einen Dateidialog und schrieb das gewaehlte Bild
        // DAUERHAFT als Anwendungs-Icon nach Tab_Applikation - ohne jeden Hinweis in der
        // Oberflaeche und ohne Rueckfrage. Handler und Verdrahtung sind ersatzlos
        // entfernt. Auf Nutzerwunsch vom 30.08.2026 ist auch das Logo selbst aus dem
        // linken Band entfernt - damit entfiel zugleich der letzte Leseweg des
        // Anwendungs-Icons (ApplikationCtrl.m_icon -> SetImageFromFile).

        /// <summary>
        /// „Projekt öffnen" (Nutzerwunsch 30.08.2026): macht das in der Liste
        /// markierte Projekt zum aktiven Projekt und schließt den Assistenten
        /// danach — <b>ohne</b> das Detailformular „Konfiguration Projekt".
        ///
        /// <para>
        /// Ersetzt den früheren Knopf „Neues Projekt…" (Umschalten in den
        /// Neu-Modus samt Rückfrage <c>HatVerwerfbareEingaben</c>): Das Anlegen
        /// hat mit der Startmasken-Kachel „Neues Projekt" einen eigenen,
        /// eindeutigen Einstieg — ein zweiter Weg mitten im Bearbeiten-Lauf
        /// stiftete nur Verwechslungsgefahr.
        /// </para>
        /// </summary>
        private void button_ProjektOeffnen_Click(object sender, EventArgs e)
        {
            ProjektOeffnenUndSchliessen(ucProjektAuswahl.GewaehlteID, ucProjektAuswahl.GewaehlterName);
        }

        /// <summary>
        /// Doppelklick in der Projektliste wirkt wie „Projekt öffnen"
        /// (Nutzerwunsch 30.08.2026).
        /// </summary>
        private void ucProjektAuswahl_ProjektGewaehlt(int id, string name)
        {
            if (top != WizardItemClass.KOMPONENTEN_ITEM) return;
            ProjektOeffnenUndSchliessen(id, name);
        }

        /// <summary>
        /// Setzt das gewählte Projekt aktiv, schließt den Assistenten und meldet den
        /// Wechsel kurz an der Startmaske.
        ///
        /// <para>
        /// <b>Kein Detailformular mehr (Nutzerwunsch 30.08.2026).</b> Bis hierher rief
        /// dieser Weg <c>MenueCtrl.ProjektInFormMainLaden</c> und zeigte damit
        /// „Konfiguration Projekt" als Dialog — der Anwender wollte an dieser Stelle
        /// aber nur wechseln, nicht bearbeiten. Jetzt läuft er über
        /// <c>MenueCtrl.ProjektAktivSetzen</c>: Startmaske und „zuletzt geöffnet"
        /// ziehen nach, es geht kein Fenster auf. Das Detailformular bleibt hinter
        /// Menü „Projekt → Öffnen…" und der Kachel „Projekt Details" unverändert
        /// erreichbar.
        /// </para>
        /// </summary>
        private void ProjektOeffnenUndSchliessen(int id, string name)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name)) return;

            if (!Program.menuectrl.ProjektAktivSetzen(name, id)) return;

            // Der Assistent wird von zwei Stellen aus gestartet, die nach seinem
            // Schliessen den Projektkontext aus Program.wizardctrl.Projektname
            // nachziehen (Form_Start.pBox_ProjektOeffnen_Click,
            // MDIMainForm.MenuItem_ProjektBearbeiten_Click). Das Feld haelt den
            // zuletzt GESPEICHERTEN Namen und wird beim Start des Assistenten nicht
            // geleert - ohne diese Zeile holte der Nachzug ein frueher gespeichertes
            // Projekt zurueck und machte das gerade geoeffnete wieder unwirksam.
            if (Program.wizardctrl != null) Program.wizardctrl.Projektname = name;

            // Close() blendet den modalen Rahmen nur aus; ShowDialog kehrt erst nach
            // diesem Handler zurueck. Der Hinweis liegt deshalb ueber der Startmaske
            // und nicht unter dem Assistenten.
            Close();
            if (Program.startfrm != null) Program.startfrm.HinweisProjektGeoeffnet();
        }
    }
}
