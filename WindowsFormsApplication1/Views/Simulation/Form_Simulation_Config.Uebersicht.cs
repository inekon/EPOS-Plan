using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Erzeuger-Übersicht der Simulationskonfiguration (Konzept 4.1) — Layout, Anzeige
    /// und die Dialoge, die per Doppelklick daraus geöffnet werden.
    ///
    /// Aus <c>Form_Simulation_Config.cs</c> herausgelöst (Paket 2): die Hauptdatei hatte
    /// über 2000 Zeilen und mischte Auswahl, Alt-Zuordnung und Übersicht. Hier steht
    /// ausschließlich die Übersicht samt Fußzeile.
    ///
    /// Was NICHT hier steht: die Alt-Zuordnung <c>listView1</c>/<c>_zuordnungen</c> und
    /// ihr Speicherpfad. Sie bleibt in der Hauptdatei, weil sie mit Etappe B (Konzept 4.4)
    /// im Ganzen entfällt.
    /// </summary>
    public partial class Form_Simulation_Config : BaseForm
    {
        // --- Spaltenindizes der Übersicht --------------------------------------------

        // ZWINGENDE VORARBEIT aus Konzept 4.1: Die Indizes standen an drei Stellen
        // doppelt (Columns.Add, Tooltip-switch, Doppelklick-Dispatcher). Mit zwei neuen
        // Spalten hätte das stille Fehlbedienungen ergeben - ein Doppelklick auf
        // "Wärmesenke" hätte den Betriebsmodus geöffnet. Ab jetzt gibt es die Wahrheit
        // genau einmal, hier.
        private const int COL_PRIO = 0;
        private const int COL_ERZEUGER = 1;
        private const int COL_ANLAGE = 2;
        private const int COL_WPPRIO = 3;
        private const int COL_QUELLE = 4;
        private const int COL_SENKE = 5;
        private const int COL_ZWEITSENKE = 6;   // neu (Konzept 4.1/4.2)
        private const int COL_BETRIEBSMODUS = 7;

        // ETAPPE D1 (Konzept_KonfigUI_Hydraulik, Abschnitt 6): Die neunte Spalte
        // „Zuordnung (alt)" ist ENTFALLEN. Sie zeigte den Pufferspeicher aus dem
        // Altmodell Z_ProjektPufferSp - eine zweite, seit Paket 4 nicht mehr gelesene
        // Wahrheit neben der Senkenspalte - und führte per Doppelklick in den
        // Hysterese-Dialog. Der Dialog selbst bleibt (SpeicherregelungBearbeiten); die
        // Schwellen sind längst am Puffer pflegbar (Form_PufferSp_Projekt), und genau
        // dorthin gehören sie. Ebenso entfällt die Zeile „Gesamtsystem", die es nur
        // gab, um eine Zuordnung dieser Spalte anzuzeigen.
        //
        // NICHT betroffen: die Spiegel-Brücke WpSenkeSpiegeln und alle
        // Z_ProjektPufferSp-Schreibwege - sie bleiben bis zur Abnahme unangetastet
        // (Konzeptvorgabe).

        /// <summary>
        /// Spalten, die per Doppelklick einen Dialog öffnen (Konzept 4.1, „Whitelist").
        ///
        /// Der frühere Dispatcher hatte ein <c>else</c>-Fallback <c>int spalte = 4</c>:
        /// jeder Doppelklick, der keine der bekannten Spalten traf, öffnete die
        /// Wärmequelle. Solange nur Wärmepumpen-Zeilen ein <c>Tag</c> trugen, fiel das
        /// nicht auf. Seit ALLE Zeilen ein <c>Tag</c> haben (4.1), öffnete ein
        /// Doppelklick auf die Bezeichnerspalte eines Heizkessels den
        /// Wärmequellen-Dialog. Jetzt gilt: was nicht in dieser Liste steht, tut nichts.
        /// </summary>
        private static readonly int[] SPALTEN_MIT_DIALOG =
        {
            COL_WPPRIO, COL_QUELLE, COL_SENKE, COL_ZWEITSENKE, COL_BETRIEBSMODUS
        };

        // --- Zuordnungs-Rubrik (Konzept 4.4) ------------------------------------------
        //
        // Der Rückwegschalter RUBRIK_SICHTBAR ist mit ETAPPE D1 entfallen. Er hielt seit
        // Paket 2 / Etappe A die Möglichkeit offen, die alte Bedienung wieder
        // einzuschalten; die Rubrik selbst wird jetzt gar nicht mehr angelegt
        // (Form_Simulation_Config.AltRubrikStilllegen), damit hätte der Schalter nichts
        // mehr zu schalten. Der Rückweg ist ab hier die Versionsverwaltung.
        //
        // UNVERÄNDERT bleibt der Datenpfad: _zuordnungen wird weiter aus
        // Z_ProjektPufferSp geladen und beim Speichern zurückgeschrieben, und die
        // Spiegel-Brücke WaermesenkeClass.WpSenkeSpiegeln arbeitet weiter
        // (Konzeptvorgabe: bis zur Abnahme unangetastet).

        /// <summary>Höhe, die unter der Übersicht für die Fußzeile frei bleibt [px].</summary>
        private const int PLATZ_FUSSZEILE = 62;

        /// <summary>
        /// Feste Spaltenbreiten der Übersicht [px], in der Reihenfolge der
        /// <c>COL_*</c>-Konstanten (Konzept 4.1, zweiter Layoutzwang).
        ///
        /// Vorher liefen zwei <c>AutoResizeColumns</c> hintereinander:
        /// <c>ColumnContent</c> und danach <c>HeaderSize</c>. Die zweite überschreibt die
        /// erste vollständig — die Breiten hingen also allein an der LÄNGE DER KOPFTEXTE.
        /// Mit „Wärmeerzeuger", „Anlage(n) im Projekt" … „Zuordnung (Altmodell)" ergab das
        /// rund 910 px in einer 491 px breiten Liste: waagerechter Rollbalken bei jedem
        /// Öffnen, und die inhaltlich wichtigen Spalten waren die schmalsten.
        ///
        /// Jetzt: kompakte Kopftexte, feste Breiten, und das Formular wird einmalig so
        /// weit verbreitert, dass die Summe hineinpasst (<see cref="UebersichtBreiteAnpassen"/>).
        /// Zu lange Zellinhalte (lange Anlagennamen) kürzt die ListView mit „…" — das ist
        /// gewollt; der volle Text steht im Mouseover-Hinweis der Zeile.
        ///
        /// ETAPPE D1: Mit der Spalte „Zuordnung (alt)" fallen 112 px weg. 50 davon gehen
        /// an die beiden SENKEN-Spalten — dort standen die abgeschnittenen Texte
        /// („Puffer Heizung: a…"), weil der Puffername hinter dem Rollenkürzel steht.
        /// Der Rest verschmälert das Fenster: <see cref="UebersichtBreiteAnpassen"/>
        /// rechnet die Spaltensumme und verbreitert nur noch um das, was gebraucht wird.
        /// Bleibt danach trotzdem Platz übrig, bekommt ihn wie bisher die Anlagenspalte
        /// (siehe <see cref="InitErzeugerUebersicht"/>, „rest").
        /// </summary>
        private static readonly int[] SPALTEN_BREITEN =
        {
            40,   // COL_PRIO           "Prio"        1…4
            84,   // COL_ERZEUGER       "Erzeuger"    längster Wert "Solarthermie"
            140,  // COL_ANLAGE         "Anlage"      Herstellerbezeichner, kürzt bei Bedarf
            62,   // COL_WPPRIO         "WP-Prio"     1…9 bzw. "-"
            100,  // COL_QUELLE         "Quelle"      "Erdreich Kollektor 1,5 m" kürzt
            150,  // COL_SENKE          "Senke"       "Puffer Heizung: <Name>"  (D1: +30)
            120,  // COL_ZWEITSENKE     "Zweitsenke"                            (D1: +20)
            92    // COL_BETRIEBSMODUS  "Modus"       "laufzeitoptimiert"
        };

        /// <summary>
        /// Zuschlag der ListView auf die Spaltensumme: 3D-Rahmen, die senkrechte
        /// Bildlaufleiste (erscheint ab der fünften Erzeugerzeile) und eine kleine
        /// Reserve. Die Breite der Bildlaufleiste kommt aus dem System, nicht aus einer
        /// hier festgeschriebenen 17 — sonst rutscht bei abweichenden Systemmaßen genau
        /// die letzte Spalte wieder hinaus.
        /// </summary>
        private static int ListeZuschlag()
        {
            return 2 + SystemInformation.VerticalScrollBarWidth + 6;
        }

        // --- Steuerelemente -----------------------------------------------------------

        // Live-Übersicht der ausgewählten Wärmeerzeuger (rechts oben),
        // wird in InitErzeugerUebersicht programmatisch angelegt
        private GroupBox groupBox_Uebersicht;
        private ListView listView_Uebersicht;

        // Fußzeile: Projekt-Pufferspeicher und Einstieg in die Verwaltung (Konzept 4.1)
        private Label label_PufferListe;
        private Button btn_PufferVerwalten;

        // Fußzeile, rechts: Feature-Flag der zweikanaligen Kaskade (Konzept Kapitel 9)
        private CheckBox checkBox_KaskadeZweikanalig;
        private bool _kaskadeUiUpdate = false;   // verhindert Schreiben beim Vorbelegen

        // Fußzeile, rechts: Einstellung Extrapolation_erlaubt (Paket 8, Konzept 13.4)
        private CheckBox checkBox_Extrapolation;
        private bool _extrapolationUiUpdate = false;

        // Inline-Editor für die Wärmequelle in der Übersicht
        private ComboBox _wqCombo;
        private AnlagenInfo _wqInfo;
        private bool _wqUpdating = false;

        // Außentemperatur der Klimaregion (8760 Stundenwerte) für die Vorschau des
        // Erdreichdialogs. Wird beim ersten Öffnen einmal geladen und gecacht
        // (Konzept 4.5) - nicht bei jeder Parameteränderung.
        private float[] _aussentempCache = null;
        private bool _aussentempGeladen = false;

        // Mouseover-Hinweise in der Übersicht
        private ToolTip _uebersichtTip = new ToolTip();
        private ListViewItem _tipItem = null;
        private int _tipSpalte = -1;

        /// <summary>Eine im Projekt angelegte Anlage (Zeile der Übersicht).</summary>
        private class AnlagenInfo
        {
            public int ID;              // Tab_Energieanlagen.ID
            public int ID_Type;         // 1 WP, 2 Solarthermie, 10 Heizkessel, 11 BHKW
            public string Bezeichner = "";
            public int Prioritaet;      // Einsatzreihenfolge (0 = nicht gesetzt)
            public string WpTyp = "";   // Luft-Wasser / Sole-Wasser / Wasser-Wasser
            public string WQ_Typ = "";  // Wärmequelle (WaermequelleClass.TYP_*)
            public double WQ_Temp;
            public string WS_Typ = "";  // Bedarfsart der Heizkreis-Senke (WaermequelleClass.SENKE_*)
            public string BM_Typ = "";  // Betriebsmodus (WaermequelleClass.MODUS_*)

            /// <summary>Haupt- und Zweitsenke (Konzept 5.3), aus derselben Abfrage gelesen.</summary>
            public WaermesenkeClass.SenkeDaten Senke = new WaermesenkeClass.SenkeDaten();

            public bool IstWaermepumpe
            {
                get { return ID_Type == ProjektPuffer.TYP_WP; }
            }
        }

        // --- Aufbau -------------------------------------------------------------------

        /// <summary>
        /// Legt rechts oben die Übersicht an, die ALLE ausgewählten Wärmeerzeuger mit
        /// Wärmequelle, Wärmesenke und Zweitsenke zeigt (Konzept 4.1). Sie aktualisiert
        /// sich bei jeder Änderung der Auswahl und der Zuordnungen.
        ///
        /// Die Höhe folgt weiterhin <c>groupBox_PufferSp.Top</c>. Mit der ausgeblendeten
        /// Rubrik (Etappe A) steht diese Gruppe am unteren Rand — der freiwerdende
        /// Bereich geht damit an die Übersicht, ohne dass die Formel eine zweite Wahrheit
        /// bekommt (Konzept 4.1, Layoutzwang in Fassung 12).
        /// </summary>
        private void InitErzeugerUebersicht()
        {
            // Erst die Breite (die Gruppe unten misst sich an groupBox_PufferSp), dann bauen
            UebersichtBreiteAnpassen();

            groupBox_Uebersicht = new GroupBox();
            groupBox_Uebersicht.Name = "groupBox_Uebersicht";
            groupBox_Uebersicht.Text = MyResource.Resource.SIM_UEBERSICHT_TITEL;
            groupBox_Uebersicht.Location = new Point(groupBox_PufferSp.Left, 109);
            groupBox_Uebersicht.Size = new Size(groupBox_PufferSp.Width,
                groupBox_PufferSp.Top - 109 - 10);
            this.Controls.Add(groupBox_Uebersicht);
            groupBox_Uebersicht.BringToFront();

            listView_Uebersicht = new ListView();
            listView_Uebersicht.Name = "listView_Uebersicht";
            listView_Uebersicht.View = View.Details;
            listView_Uebersicht.FullRowSelect = true;
            listView_Uebersicht.GridLines = true;
            listView_Uebersicht.MultiSelect = false;
            listView_Uebersicht.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listView_Uebersicht.Font = listView1.Font;
            listView_Uebersicht.Location = new Point(7, 20);
            listView_Uebersicht.Size = new Size(groupBox_Uebersicht.Width - 14,
                groupBox_Uebersicht.Height - 27);
            listView_Uebersicht.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right | AnchorStyles.Bottom;

            // ACHTUNG: Reihenfolge und Anzahl müssen zu den COL_*-Konstanten und zu
            // SPALTEN_BREITEN passen. Kopftexte bewusst kurz (Konzept 4.1, Layoutzwang);
            // was die Spalte bedeutet und dass sie per Doppelklick bearbeitbar ist, steht
            // im Mouseover-Hinweis - das trägt der Kopf nicht mehr mit.
            listView_Uebersicht.Columns.Add(MyResource.Resource.SIM_SPALTE_PRIO, SPALTEN_BREITEN[COL_PRIO], HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add(MyResource.Resource.SIM_ERZEUGERNAME_ALLGEMEIN, SPALTEN_BREITEN[COL_ERZEUGER], HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add(MyResource.Resource.SIM_SPALTE_ANLAGE, SPALTEN_BREITEN[COL_ANLAGE], HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add(MyResource.Resource.SIM_SPALTE_WPPRIO, SPALTEN_BREITEN[COL_WPPRIO], HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add(MyResource.Resource.SIMQ_SPALTE_QUELLE, SPALTEN_BREITEN[COL_QUELLE], HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add(MyResource.Resource.SIM_SPALTE_SENKE, SPALTEN_BREITEN[COL_SENKE], HorizontalAlignment.Left);
            // Spaltenkopf = Beschriftung: SIM_SPALTE_ZWEITSENKE (gross), nicht die klein
            // geschriebene Satzform SIM_ROLLE_ZWEITSENKE.
            listView_Uebersicht.Columns.Add(MyResource.Resource.SIM_SPALTE_ZWEITSENKE, SPALTEN_BREITEN[COL_ZWEITSENKE], HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add(MyResource.Resource.SIM_SPALTE_MODUS, SPALTEN_BREITEN[COL_BETRIEBSMODUS], HorizontalAlignment.Left);

            listView_Uebersicht.MouseDoubleClick += listView_Uebersicht_MouseDoubleClick;

            // Mouseover-Hinweise zu den bearbeitbaren Spalten
            _uebersichtTip.AutoPopDelay = 15000;
            _uebersichtTip.InitialDelay = 400;
            _uebersichtTip.ReshowDelay = 100;
            listView_Uebersicht.MouseMove += listView_Uebersicht_MouseMove;
            listView_Uebersicht.MouseLeave += (s, e) => { _tipItem = null; _tipSpalte = -1; _uebersichtTip.Hide(listView_Uebersicht); };

            groupBox_Uebersicht.Controls.Add(listView_Uebersicht);

            // Bleibt nach der Verbreiterung Platz übrig (der Schirm gab mehr her, als
            // gebraucht wurde), bekommt ihn die Anlagenspalte - dort sind die Texte am
            // längsten. Einmalig; ein Resize-Ereignis wird bewusst nicht abonniert.
            int summe = 0;
            foreach (int b in SPALTEN_BREITEN) summe += b;
            int rest = listView_Uebersicht.Width - ListeZuschlag() - summe;
            if (rest > 0) listView_Uebersicht.Columns[COL_ANLAGE].Width += rest;

            AktualisiereErzeugerUebersicht();
        }

        /// <summary>
        /// Verbreitert Formular und Übersicht so weit, dass die neun Spalten
        /// (<see cref="SPALTEN_BREITEN"/>) ohne waagerechten Rollbalken hineinpassen —
        /// der zweite Layoutzwang aus Konzept 4.1.
        ///
        /// Die Übersicht erbt ihre Breite von <c>groupBox_PufferSp</c> (505 px aus dem
        /// Designer); darin blieben der Liste 491 px für rund 850 px Spalten. Statt die
        /// Spalten unlesbar zu quetschen wächst das Formular: 791 → bis zu 1169 px
        /// Clientbreite.
        ///
        /// GEKAPPT AM SCHIRM: Passt das nicht mehr in den Arbeitsbereich, wird nur so
        /// weit verbreitert wie möglich — dann kommt der Rollbalken für die letzten
        /// Spalten zurück. Das ist der ehrliche Rückfall; das Formular ist in der Größe
        /// veränderbar, der Anwender kann selbst nachhelfen.
        ///
        /// Kein Designer, keine .resx: die Werte dort bleiben unangetastet, verschoben
        /// wird ausschließlich im Code-Behind — wie es <c>AltRubrikStilllegen</c>
        /// mit der Höhe bereits tut.
        /// </summary>
        private void UebersichtBreiteAnpassen()
        {
            int summe = 0;
            foreach (int b in SPALTEN_BREITEN) summe += b;

            // groupBox_Uebersicht = groupBox_PufferSp.Width, ListView = Gruppe - 14
            int gewuenscht = summe + ListeZuschlag() + 14;
            int zusatz = gewuenscht - groupBox_PufferSp.Width;
            if (zusatz <= 0) return;

            // Nicht über den Arbeitsbereich hinaus (DpiUnaware: Pixel sind Pixel).
            Screen schirm = Screen.PrimaryScreen;
            if (schirm != null)
            {
                int rahmen = this.Width - this.ClientSize.Width;
                int moeglich = schirm.WorkingArea.Width - 40 - rahmen - this.ClientSize.Width;
                if (zusatz > moeglich) zusatz = moeglich;
            }
            if (zusatz <= 0) return;

            this.ClientSize = new Size(this.ClientSize.Width + zusatz, this.ClientSize.Height);
            groupBox_PufferSp.Width += zusatz;

            // Die Fußzeile unten rechts mitziehen; sie hat keine Verankerung (Bestand).
            btn_Speichern.Location = new Point(btn_Speichern.Left + zusatz, btn_Speichern.Top);
            btn_OK.Location = new Point(btn_OK.Left + zusatz, btn_OK.Top);
            lblStatus.Location = new Point(lblStatus.Left + zusatz, lblStatus.Top);
        }

        /// <summary>
        /// Fußzeile unter der Übersicht (Mockup 4.1): Aufzählung der Projekt-Puffer und
        /// der Einstieg in die Verwaltung (4.3). Ohne diesen Weg wäre nach dem Entfall
        /// der Rubrik (4.4) kein Pufferspeicher mehr anzulegen.
        /// </summary>
        private void InitPufferFusszeile()
        {
            label_PufferListe = new Label();
            label_PufferListe.Name = "label_PufferListe";
            label_PufferListe.AutoSize = false;
            label_PufferListe.Location = new Point(groupBox_Uebersicht.Left,
                                                   groupBox_Uebersicht.Bottom + 6);
            label_PufferListe.Size = new Size(groupBox_Uebersicht.Width, 20);
            label_PufferListe.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(label_PufferListe);
            label_PufferListe.BringToFront();

            btn_PufferVerwalten = new Button();
            btn_PufferVerwalten.Name = "btn_PufferVerwalten";
            btn_PufferVerwalten.Text = MyResource.Resource.PSP_BTN_PUFFER_VERWALTEN;
            btn_PufferVerwalten.Location = new Point(groupBox_Uebersicht.Left,
                                                    label_PufferListe.Bottom + 4);
            btn_PufferVerwalten.Size = new Size(240, 28);
            btn_PufferVerwalten.Click += btn_PufferVerwalten_Click;
            this.Controls.Add(btn_PufferVerwalten);
            btn_PufferVerwalten.BringToFront();

            InitKaskadeSchalter();
            InitExtrapolationSchalter();

            AktualisierePufferFusszeile();
        }

        /// <summary>
        /// Schalter „Zweikanalige Kaskade (Vorschau)" am rechten Ende der Fußzeile
        /// (Paket 4; Konzept Kapitel 9 „Feature-Flag empfohlen").
        ///
        /// Er schreibt die Projekteinstellung <c>Tab_Einstellungen.Kaskade_Zweikanalig</c>,
        /// und die ist seit Etappe 4b <b>wirksam</b>: <c>SimulationControl</c> verzweigt
        /// darauf in die zweikanalige Kaskade mit herausgelöster Ladephase
        /// (Reihenfolge-Invariante 6.3). Das ändert Ergebnisse — bei Projekten mit
        /// Puffer-Senke deutlich, sonst nur im Rahmen der float-Rundung. Genau das sagt
        /// der Mouseover-Hinweis; der frühere Text („merkt die Entscheidung nur vor")
        /// stammt aus Etappe 4a und wäre jetzt irreführend.
        ///
        /// Kein Designer, keine .resx: wie die übrige Fußzeile rein programmatisch
        /// (Konzept 7, „Layout im Code-Behind"). Der Text ist deutsch — die
        /// durchgängige Lokalisierung des Simulationsbereichs ist Paket 9.
        /// </summary>
        private void InitKaskadeSchalter()
        {
            checkBox_KaskadeZweikanalig = new CheckBox();
            checkBox_KaskadeZweikanalig.Name = "checkBox_KaskadeZweikanalig";
            checkBox_KaskadeZweikanalig.Text = MyResource.Resource.SIM_KASKADE_SCHALTER;
            checkBox_KaskadeZweikanalig.AutoSize = true;
            checkBox_KaskadeZweikanalig.Enabled = false;   // erst mit bekanntem Projekt

            // Rechtsbündig in der Fußzeile, aber nie über dem Verwalten-Knopf.
            int x = groupBox_Uebersicht.Right - 230;
            if (x < btn_PufferVerwalten.Right + 12) x = btn_PufferVerwalten.Right + 12;
            checkBox_KaskadeZweikanalig.Location = new Point(x, btn_PufferVerwalten.Top + 6);
            checkBox_KaskadeZweikanalig.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Zeilenumbrüche der Ressource auf die Plattformform bringen: Die .resx legt
            // sie als LF ab (XML-Normierung), der Bestand hat hier Environment.NewLine
            // gesetzt. Ohne die Umsetzung stünde derselbe Text mit anderen Trennzeichen
            // im Hinweisfenster.
            _uebersichtTip.SetToolTip(checkBox_KaskadeZweikanalig,
                MyResource.Resource.SIM_KASKADE_TOOLTIP.Replace("\n", Environment.NewLine));

            checkBox_KaskadeZweikanalig.CheckedChanged += checkBox_KaskadeZweikanalig_CheckedChanged;
            this.Controls.Add(checkBox_KaskadeZweikanalig);
            checkBox_KaskadeZweikanalig.BringToFront();
        }

        /// <summary>
        /// Belegt den Schalter aus der Datenbank vor. Wird aus <c>SetControls</c>
        /// gerufen, sobald das Projekt bekannt ist.
        /// </summary>
        private void AktualisiereKaskadeSchalter()
        {
            if (checkBox_KaskadeZweikanalig == null) return;

            _kaskadeUiUpdate = true;
            try
            {
                checkBox_KaskadeZweikanalig.Enabled = m_ID_Projekt > 0;
                checkBox_KaskadeZweikanalig.Checked =
                    m_ID_Projekt > 0 && KonfigurationCtrl.KaskadeZweikanaligLesen(m_ID_Projekt);
            }
            finally { _kaskadeUiUpdate = false; }
        }

        private void checkBox_KaskadeZweikanalig_CheckedChanged(object sender, EventArgs e)
        {
            if (_kaskadeUiUpdate || m_ID_Projekt <= 0) return;

            bool wert = checkBox_KaskadeZweikanalig.Checked;

            // Sofort schreiben, nicht erst beim „Speichern": Der Schalter gehört nicht zu
            // dem Einstellungssatz, den btn_Speichern_Click über KonfigurationCtrl.Update
            // wegschreibt (dessen Spaltenliste bleibt unangetastet, siehe
            // KonfigurationCtrl.KaskadeZweikanaligSchreiben).
            if (KonfigurationCtrl.KaskadeZweikanaligSchreiben(m_ID_Projekt, wert))
            {
                ShowStatus(wert
                    ? MyResource.Resource.SIM_STATUS_KASKADE_EIN
                    : MyResource.Resource.SIM_STATUS_KASKADE_AUS,
                    Color.DarkGreen);
                return;
            }

            // Kein Einstellungssatz oder Spalte fehlt (Datenbank nicht auf Schemastand 6):
            // Der Schalter geht zurück, damit die Anzeige nicht mehr behauptet als die
            // Datenbank hergibt.
            _kaskadeUiUpdate = true;
            try { checkBox_KaskadeZweikanalig.Checked = !wert; }
            finally { _kaskadeUiUpdate = false; }

            ShowStatus(MyResource.Resource.SIM_STATUS_EINSTELLUNG_FEHLER, Color.DarkRed);
        }

        /// <summary>
        /// Schalter „Extrapolation der WP-Kennlinie erlauben" in der Fußzeile
        /// (Paket 8; Konzept 13.4).
        ///
        /// Er löst die einzige echte Rückfrage der Engine ab: Bis Paket 8 fragte
        /// <c>SimulationWaermepumpe</c> mitten in der Stundenschleife per MessageBox, ob
        /// unterhalb der niedrigsten Stützstelle der Kennlinie extrapoliert werden darf.
        /// Jeder unbeaufsichtigte Lauf blieb daran hängen.
        ///
        /// <b>Vorbelegung an.</b> Das ist die Antwort, die in jedem dokumentierten Lauf
        /// gegeben wurde — nur damit bleiben die Ergebnisse unverändert. Wer die
        /// Extrapolation ausschließen will, nimmt den Haken heraus; die Simulation bricht
        /// dann mit einer sprechenden Meldung ab, statt still zu rechnen.
        ///
        /// Aufbau exakt wie <see cref="InitKaskadeSchalter"/>: programmatisch, kein
        /// Designer, keine .resx, deutscher Text (die durchgängige Lokalisierung des
        /// Simulationsbereichs ist Paket 9).
        /// </summary>
        private void InitExtrapolationSchalter()
        {
            checkBox_Extrapolation = new CheckBox();
            checkBox_Extrapolation.Name = "checkBox_Extrapolation";
            checkBox_Extrapolation.Text = MyResource.Resource.SIM_EXTRAPOLATION_SCHALTER;
            checkBox_Extrapolation.AutoSize = true;
            checkBox_Extrapolation.Checked = true;         // Vorbelegung wie im Datenmodell
            checkBox_Extrapolation.Enabled = false;        // erst mit bekanntem Projekt
            checkBox_Extrapolation.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _uebersichtTip.SetToolTip(checkBox_Extrapolation,
                MyResource.Resource.SIM_EXTRAPOLATION_TOOLTIP.Replace("\n", Environment.NewLine));

            checkBox_Extrapolation.CheckedChanged += checkBox_Extrapolation_CheckedChanged;
            this.Controls.Add(checkBox_Extrapolation);
            checkBox_Extrapolation.BringToFront();

            // Erst nach dem Hinzufügen platzieren: Ein AutoSize-Steuerelement kennt seine
            // Höhe erst, wenn es zum Formular gehört - und die Höhe wird gebraucht.
            ExtrapolationSchalterPlatzieren();
        }

        /// <summary>
        /// Setzt den Extrapolationsschalter in die Fußzeile — eine Zeile unter den
        /// Kaskadenschalter und KOLLISIONSFREI zur Knopfzeile (Nacharbeit Paket 8,
        /// Befund N13a).
        ///
        /// Zwei Dinge, die die erste Fassung nicht getan hat:
        ///
        ///   1. <b>Null-Schutz.</b> Die Position wurde ungeprüft aus
        ///      <c>checkBox_KaskadeZweikanalig</c> abgeleitet. Fällt der Kaskadenschalter
        ///      künftig weg oder wandert sein Aufbau, wäre das eine
        ///      <c>NullReferenceException</c> im Konstruktor des Formulars — der Dialog
        ///      ließe sich gar nicht mehr öffnen. Ohne ihn gilt dieselbe Rechnung wie in
        ///      <see cref="InitKaskadeSchalter"/>.
        ///
        ///   2. <b>Kollisionsfreiheit.</b> Die Fußzeile ist knapp: Übersicht bis y≈418,
        ///      Pufferzeile, Verwalten-Knopf bis y≈476, Speichern/OK ab y≈490. Eine
        ///      zweite Schalterzeile passt dort nur mit wenigen Pixeln Luft und lag in
        ///      der ersten Fassung auf der Oberkante von <c>btn_Speichern</c>. Statt die
        ///      Zahl fest zu setzen, wird der Bedarf gerechnet und das Formular bei
        ///      Bedarf um genau die fehlenden Pixel höher — dasselbe Vorgehen wie in
        ///      <c>AltRubrikStilllegen</c>, die die Knopfzeile ebenfalls nachzieht
        ///      (die drei Elemente sind ohne Verankerung, Bestand).
        /// </summary>
        private void ExtrapolationSchalterPlatzieren()
        {
            int x, y;
            if (checkBox_KaskadeZweikanalig != null)
            {
                x = checkBox_KaskadeZweikanalig.Left;
                y = checkBox_KaskadeZweikanalig.Bottom + 4;
            }
            else
            {
                // Rückfall: dieselbe Rechnung wie beim Kaskadenschalter, nur eine Zeile
                // tiefer angesetzt (unterhalb des Verwalten-Knopfes).
                x = groupBox_Uebersicht.Right - 230;
                if (x < btn_PufferVerwalten.Right + 12) x = btn_PufferVerwalten.Right + 12;
                y = btn_PufferVerwalten.Bottom + 4;
            }
            checkBox_Extrapolation.Location = new Point(x, y);

            // Abstand zur Knopfzeile herstellen. Ohne diesen Schritt überlappt der
            // Schalter die Oberkante von btn_Speichern.
            const int LUFT = 6;
            int fehlt = (y + checkBox_Extrapolation.Height + LUFT) - btn_Speichern.Top;
            if (fehlt <= 0) return;

            this.ClientSize = new Size(this.ClientSize.Width, this.ClientSize.Height + fehlt);
            btn_Speichern.Location = new Point(btn_Speichern.Left, btn_Speichern.Top + fehlt);
            btn_OK.Location = new Point(btn_OK.Left, btn_OK.Top + fehlt);
            lblStatus.Location = new Point(lblStatus.Left, lblStatus.Top + fehlt);
        }

        /// <summary>Belegt den Schalter aus der Datenbank vor (Gegenstück zu <see cref="AktualisiereKaskadeSchalter"/>).</summary>
        private void AktualisiereExtrapolationSchalter()
        {
            if (checkBox_Extrapolation == null) return;

            _extrapolationUiUpdate = true;
            try
            {
                checkBox_Extrapolation.Enabled = m_ID_Projekt > 0;
                // Ohne Projekt bleibt die Vorbelegung stehen - nicht "aus", denn das wäre
                // die Aussage "Extrapolation verboten", und die trifft nicht zu.
                checkBox_Extrapolation.Checked =
                    m_ID_Projekt <= 0 || KonfigurationCtrl.ExtrapolationErlaubtLesen(m_ID_Projekt);
            }
            finally { _extrapolationUiUpdate = false; }
        }

        private void checkBox_Extrapolation_CheckedChanged(object sender, EventArgs e)
        {
            if (_extrapolationUiUpdate || m_ID_Projekt <= 0) return;

            bool wert = checkBox_Extrapolation.Checked;

            // Sofort schreiben, aus demselben Grund wie beim Kaskadenschalter: Die
            // Einstellung gehört nicht zu dem Satz, den btn_Speichern_Click über
            // KonfigurationCtrl.Update wegschreibt.
            if (KonfigurationCtrl.ExtrapolationErlaubtSchreiben(m_ID_Projekt, wert))
            {
                ShowStatus(wert
                    ? MyResource.Resource.SIM_STATUS_EXTRAPOLATION_EIN
                    : MyResource.Resource.SIM_STATUS_EXTRAPOLATION_AUS,
                    Color.DarkGreen);
                return;
            }

            _extrapolationUiUpdate = true;
            try { checkBox_Extrapolation.Checked = !wert; }
            finally { _extrapolationUiUpdate = false; }

            ShowStatus(MyResource.Resource.SIM_STATUS_EINSTELLUNG_FEHLER, Color.DarkRed);
        }

        /// <summary>Schreibt die Projekt-Puffer in die Fußzeile.</summary>
        private void AktualisierePufferFusszeile()
        {
            if (label_PufferListe == null) return;

            if (m_ID_Projekt <= 0)
            {
                label_PufferListe.Text = MyResource.Resource.PSP_FUSSZEILE_OHNE_PROJEKT;
                return;
            }

            List<WaermesenkeClass.PufferInfo> puffer = WaermesenkeClass.ProjektPufferListe(m_ID_Projekt, null);
            if (puffer.Count == 0)
            {
                label_PufferListe.Text = MyResource.Resource.PSP_FUSSZEILE_KEINER;
                return;
            }

            List<string> teile = new List<string>();
            foreach (WaermesenkeClass.PufferInfo p in puffer)
                // VerwendungAnzeige statt WirksameVerwendung: Der DB-Wert bleibt deutsch,
                // angezeigt wird der übersetzte Text (Paket 9, Befund L0-2).
                teile.Add(p.Bezeichner + " (" +
                          WaermesenkeClass.VerwendungAnzeige(WaermesenkeClass.WirksameVerwendung(p)) + ", " +
                          p.Gesamtvolumen + " l)");

            label_PufferListe.Text = string.Format(MyResource.Resource.PSP_FUSSZEILE_LISTE,
                                                   string.Join(" · ", teile));
        }

        private void btn_PufferVerwalten_Click(object sender, EventArgs e)
        {
            if (m_ID_Projekt <= 0) return;

            Form_PufferSp_Projekt frm = new Form_PufferSp_Projekt();
            frm.ID_Projekt = m_ID_Projekt;
            // Einstieg über die Fußzeile: KEINE Verwendungsvorgabe - hier will der
            // Anwender den Bestand sehen, nicht einen bestimmten Kanal. Der Absprung aus
            // dem Senkendialog setzt die Vorgabe dagegen (Konzept 4.2).
            frm.Verwendung = null;
            frm.SetControls();
            frm.ShowDialog(this);

            // Die Verwaltung schreibt sofort; deshalb unabhängig vom DialogResult neu
            // aufbauen. Ein entfernter Puffer kann außerdem Senken der Anlagen betreffen
            // (ReferenzenLoesen), und die Alt-Zuordnung wird mit ihm gelöscht.
            //
            // ZUERST die Übergangsbrücke, DANN neu laden — sonst schreibt „Speichern" die
            // in der Verwaltung geänderten Betriebstemperaturen still wieder weg:
            // btn_Speichern_Click überträgt die Temperaturen der führenden WP-Zuordnung
            // über PufferSpCtrl.SetTemperaturen an den Puffer (Etappe 4, „führende
            // Ablage"). Stünde in der unsichtbaren Alt-Zuordnung noch der alte Vorlauf,
            // ginge die soeben eingegebene Änderung beim nächsten Speichern verloren.
            // Der UPDATE-Zweig von WpSenkeSpiegeln führt Vorlauf/Rücklauf der Zuordnung
            // dem Puffer nach; erst danach ist _zuordnungen wieder die Wahrheit.
            ZuordnungBrueckeAnwenden();
            ZuordnungenLaden();
            RefreshZuordnungAnzeige();
            AktualisierePufferFusszeile();
        }

        // --- Inhalt der Übersicht -----------------------------------------------------

        /// <summary>
        /// Baut die Erzeuger-Übersicht neu auf: ausgewählte Wärmeerzeuger in
        /// Prioritätsreihenfolge, je Anlage eine Zeile mit Wärmequelle, Wärmesenke,
        /// Zweitsenke und Betriebsmodus.
        ///
        /// Konzept 4.1: Der frühere <c>istWP</c>-Filter entfällt — Senke und Zweitsenke
        /// gibt es für JEDEN Erzeuger, und <c>zeile.Tag</c> trägt deshalb jede Zeile.
        /// WP-spezifisch bleiben nur WP-Priorität, Wärmequelle und Betriebsmodus.
        /// </summary>
        private void AktualisiereErzeugerUebersicht()
        {
            if (listView_Uebersicht == null) return;

            listView_Uebersicht.Items.Clear();

            int prio = 1;
            foreach (string dbWert in listErzeuger)
            {
                if (dbWert == DbWerte.ERZEUGER_GESAMTSYSTEM) continue; // eigener Eintrag weiter unten

                string anzeige = ErzeugerKatalog.Anzeige(dbWert);
                List<AnlagenInfo> anlagen = AnlagenImProjekt(dbWert);

                if (anlagen.Count == 0)
                {
                    listView_Uebersicht.Items.Add(new ListViewItem(new[]
                        { prio.ToString(), anzeige, "-", "", "", "", "", "" }));
                }
                else
                {
                    // Jede im Projekt angelegte Anlage bekommt eine eigene Zeile
                    // (z. B. beide Wärmepumpen); Prio und Erzeuger nur in der ersten
                    // Zeile, damit die Gruppierung erkennbar bleibt.
                    for (int a = 0; a < anlagen.Count; a++)
                    {
                        AnlagenInfo info = anlagen[a];
                        bool istWP = info.IstWaermepumpe;

                        ListViewItem zeile = new ListViewItem(new[]
                        {
                            a == 0 ? prio.ToString() : "",
                            a == 0 ? anzeige : "",
                            info.Bezeichner,
                            istWP ? (info.Prioritaet > 0 ? info.Prioritaet.ToString() : "-") : "",
                            istWP ? WaermequelleAnzeige(info) : "–",
                            WaermesenkeAnzeige(info),
                            ZweitsenkeAnzeige(info),
                            istWP ? BetriebsmodusAnzeige(info) : ""
                        });

                        // Konzept 4.1: Tag für ALLE Erzeugerzeilen, nicht nur für Wärmepumpen.
                        zeile.Tag = info;
                        listView_Uebersicht.Items.Add(zeile);
                    }
                }
                prio++;
            }

            // D1: Die Zeile „Gesamtsystem" ist entfallen. Sie trug ausschließlich die
            // Alt-Zuordnung der weggefallenen Spalte; ohne diese wäre sie eine Zeile
            // ohne jede Aussage gewesen.

            // KEIN AutoResizeColumns mehr: die Breiten stehen fest in SPALTEN_BREITEN
            // (siehe dort). Ein erneutes Autosize würde sie bei jedem Neuaufbau wieder
            // auf die Kopftextlänge zurücksetzen.
        }

        /// <summary>
        /// Liefert alle im Projekt angelegten Anlagen des Erzeuger-Typs aus
        /// Tab_Energieanlagen (inkl. Priorität, WP-Typ, Wärmequelle und den
        /// Senkenfeldern aus Konzept 5.3), sortiert nach Einsatz-Priorität.
        /// </summary>
        private List<AnlagenInfo> AnlagenImProjekt(string dbWert)
        {
            List<AnlagenInfo> anlagen = new List<AnlagenInfo>();

            int typ = 0;
            switch (dbWert)
            {
                case DbWerte.ERZEUGER_WAERMEPUMPE: typ = WizardItemClass.WP_TYP; break;
                case DbWerte.ERZEUGER_HEIZKESSEL: typ = WizardItemClass.KESSEL_TYP; break;
                case DbWerte.ERZEUGER_BHKW: typ = WizardItemClass.BHKW_TYP; break;
                case DbWerte.ERZEUGER_SOLARTHERMIE: typ = WizardItemClass.SOLAR_TYP; break;
            }
            if (typ == 0 || m_ID_Projekt == 0) return anlagen;

            // Konzept 4.1: Die Abfrage führt die neuen WS_*-Spalten mit, damit die
            // Übersicht Senke und Zweitsenke ohne zusätzliche Abfrage je Zeile anzeigt.
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT a.ID, a.Bezeichner, a.Prioritaet, a.WQ_Typ, a.WQ_Temp, a.WS_Typ, a.BM_Typ, " +
                "       a.WS_Ziel, a.WS_ID_Puffer, a.WS_Ladeprio, a.WS_Ladegrenze, a.WS_Ladeprio_PV, " +
                "       a.WS_Ziel2, a.WS_ID_Puffer2, a.WS_Ladeprio2, a.WS_Ladegrenze2, " +
                "       w.Typ AS WPTyp " +
                "FROM Tab_Energieanlagen AS a LEFT JOIN Tab_WP AS w ON a.ID_WP = w.ID " +
                "WHERE a.ID_Projekt=" + m_ID_Projekt + " AND a.ID_Type=" + typ +
                " ORDER BY a.Prioritaet, a.ID");
            if (dt == null) return anlagen;

            foreach (System.Data.DataRow r in dt.Rows)
            {
                AnlagenInfo info = new AnlagenInfo();
                info.ID_Type = typ;
                if (r["ID"] != DBNull.Value) info.ID = Convert.ToInt32(r["ID"]);
                if (r["Bezeichner"] != DBNull.Value) info.Bezeichner = r["Bezeichner"].ToString();
                if (r["Prioritaet"] != DBNull.Value) info.Prioritaet = Convert.ToInt32(r["Prioritaet"]);
                if (r["WPTyp"] != DBNull.Value) info.WpTyp = r["WPTyp"].ToString();
                if (r["WQ_Typ"] != DBNull.Value) info.WQ_Typ = r["WQ_Typ"].ToString();
                if (r["WQ_Temp"] != DBNull.Value) info.WQ_Temp = Convert.ToDouble(r["WQ_Temp"]);
                if (r["WS_Typ"] != DBNull.Value) info.WS_Typ = r["WS_Typ"].ToString();
                if (r["BM_Typ"] != DBNull.Value) info.BM_Typ = r["BM_Typ"].ToString();
                info.Senke = WaermesenkeClass.AusDatenzeile(r);
                if (!string.IsNullOrEmpty(info.Bezeichner)) anlagen.Add(info);
            }

            return anlagen;
        }

        /// <summary>Kompakte Anzeige der Wärmequelle einer Wärmepumpe.</summary>
        private string WaermequelleAnzeige(AnlagenInfo a)
        {
            // Luft-Wasser-WP: Quelle ist immer die Außenluft (Klimadaten)
            if (string.IsNullOrEmpty(a.WpTyp) || a.WpTyp == DbWerte.WP_BAUART_LUFT_WASSER)
                return MyResource.Resource.SIMQ_QUELLE_AUSSENLUFT;

            switch (a.WQ_Typ)
            {
                case WaermequelleClass.TYP_KONSTANT:
                    return string.Format(MyResource.Resource.SIMQ_QUELLE_KONSTANT, a.WQ_Temp.ToString("0.#"));
                case WaermequelleClass.TYP_PUFFER:
                    {
                        // E0: Der Fremdschlüssel ist die führende Identität - erst wenn
                        // er fehlt oder ins Leere zeigt (Altbestand), gilt der Bezeichner.
                        // Dieselbe Rangfolge wie in WaermequelleClass.QuellspeicherZeile.
                        string name = null;
                        object oId = WaermequelleClass.WertLesen(a.ID, "WQ_ID_Puffer");
                        if (oId != null)
                        {
                            WaermesenkeClass.PufferInfo p =
                                WaermesenkeClass.PufferLesen(Convert.ToInt32(oId));
                            if (p != null) name = p.Bezeichner;
                        }
                        if (string.IsNullOrEmpty(name))
                            name = WaermequelleClass.WertLesen(a.ID, "WQ_Puffer") as string;

                        return string.IsNullOrEmpty(name)
                            ? MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER
                            : string.Format(MyResource.Resource.SIMQ_QUELLE_PUFFER_NAME, name);
                    }
                case WaermequelleClass.TYP_PROFIL: return MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL;
                case WaermequelleClass.TYP_CSV: return MyResource.Resource.SIMQ_QUELLE_CSVPROFIL;
                case WaermequelleClass.TYP_ERDREICH: return ErdreichAnzeige(a.ID);
                default: return MyResource.Resource.SIMQ_QUELLE_AUSSENLUFT;
            }
        }

        /// <summary>
        /// Kompakte Anzeige der Wärmequelle Erdreich, z. B.
        /// "Erdreich Kollektor 1,5 m" oder "Erdsonde 2×90 m".
        /// </summary>
        private string ErdreichAnzeige(int idAnlage)
        {
            string quellsystem = WaermequelleClass.WertLesen(idAnlage, "WQ_Quellsystem") as string;
            object oTiefe = WaermequelleClass.WertLesen(idAnlage, "WQ_Tiefe");
            double tiefe = oTiefe != null ? Convert.ToDouble(oTiefe) : 0;

            if (string.Equals(quellsystem, ErdreichTemperatur.QUELLSYSTEM_SONDE,
                              StringComparison.OrdinalIgnoreCase))
            {
                object oAnzahl = WaermequelleClass.WertLesen(idAnlage, "WQ_Anzahl");
                int anzahl = oAnzahl != null ? Convert.ToInt32(oAnzahl) : 0;
                if (anzahl < 1) anzahl = 1;
                return string.Format(MyResource.Resource.SIMQ_ERDSONDE_ANZEIGE,
                                     anzahl, tiefe.ToString("0.#"));
            }

            if (tiefe <= 0) tiefe = ErdreichTemperatur.TIEFE_DEFAULT;
            return string.Format(MyResource.Resource.SIMQ_ERDKOLLEKTOR_ANZEIGE, tiefe.ToString("0.#"));
        }

        /// <summary>
        /// Kompakte Anzeige der Hauptsenke (Konzept 4.1): Heizkreis mit Bedarfsart oder
        /// „Puffer Heizung: &lt;Name&gt;". Ersetzt inhaltlich die frühere, rein
        /// WP-spezifische Senkenspalte, die nur die Bedarfsart zeigte.
        /// </summary>
        private string WaermesenkeAnzeige(AnlagenInfo a)
        {
            return WaermesenkeClass.HauptsenkeAnzeige(a.Senke);
        }

        /// <summary>Kompakte Anzeige der Zweitsenke; „–" ohne Zweitsenke (Konzept 4.1).</summary>
        private string ZweitsenkeAnzeige(AnlagenInfo a)
        {
            return WaermesenkeClass.ZweitsenkeAnzeige(a.Senke);
        }

        // --- Klimadaten für den Erdreichdialog ---------------------------------------

        /// <summary>
        /// Liefert die Außentemperatur der Projekt-Klimaregion (8760 Stundenwerte).
        /// Der Vektor wird einmal je Formularsitzung geladen und gecacht; er ist
        /// derselbe, den die Simulation über SimulationWaermebedarf.Stundentemperatur
        /// verwendet (Tab_Solar.Temperatur der Klimaregion). Liefert null, wenn dem
        /// Projekt keine Klimaregion zugeordnet ist oder keine 8760 Werte vorliegen.
        /// </summary>
        private float[] AussentemperaturLaden()
        {
            if (_aussentempGeladen) return _aussentempCache;
            _aussentempGeladen = true;

            try
            {
                object oRegion = DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = " + m_ID_Projekt);
                if (oRegion == null || oRegion == DBNull.Value) return null;
                int idRegion = Convert.ToInt32(oRegion);
                if (idRegion <= 0) return null;

                System.Data.DataTable dt = DataRepository.GetDataTable(
                    "SELECT Temperatur FROM Tab_Solar WHERE ID_Klimaregion = " + idRegion + " ORDER BY ID");
                if (dt == null || dt.Rows.Count < 8760) return null;

                float[] temp = new float[8760];
                for (int i = 0; i < 8760; i++)
                {
                    object v = dt.Rows[i]["Temperatur"];
                    temp[i] = (v == DBNull.Value) ? 0f : Convert.ToSingle(v);
                }
                _aussentempCache = temp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Außentemperatur konnte nicht geladen werden: " + ex.Message);
            }

            return _aussentempCache;
        }

        /// <summary>DIN-4710-Klimazone der Projekt-Klimaregion; 0 = nicht zugeordnet.</summary>
        private int KlimazoneDesProjekts()
        {
            try
            {
                object oRegion = DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = " + m_ID_Projekt);
                if (oRegion == null || oRegion == DBNull.Value) return 0;
                return KlimaregionCtrl.GetKlimazone(Convert.ToInt32(oRegion));
            }
            catch { return 0; }
        }

        /// <summary>Speichert die DIN-4710-Klimazone an der Projekt-Klimaregion.</summary>
        private void KlimazoneSpeichern(int zone)
        {
            try
            {
                object oRegion = DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = " + m_ID_Projekt);
                if (oRegion == null || oRegion == DBNull.Value) return;
                KlimaregionCtrl.SetKlimazone(Convert.ToInt32(oRegion), zone);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klimazone konnte nicht gespeichert werden: " + ex.Message);
            }
        }

        // --- Betriebsmodus ------------------------------------------------------------

        /// <summary>Kompakte Anzeige des Betriebsmodus einer Wärmepumpe.</summary>
        private string BetriebsmodusAnzeige(AnlagenInfo a)
        {
            switch (a.BM_Typ)
            {
                case WaermequelleClass.MODUS_LEISTUNG: return MyResource.Resource.SIM_MODUS_LEISTUNG;
                case WaermequelleClass.MODUS_PV: return MyResource.Resource.SIM_MODUS_PV;
                default: return MyResource.Resource.SIM_MODUS_LAUFZEIT;
            }
        }

        /// <summary>
        /// Auswahl des Betriebsmodus (Leistungssteuerung) einer Wärmepumpe.
        ///
        /// Konzept 4.1: Seit alle Erzeugerzeilen ein <c>Tag</c> tragen, ist der Dialog
        /// auch aus einer Kessel- oder BHKW-Zeile erreichbar. Seine drei Modi und ihre
        /// Texte sind aber durchgehend WP-spezifisch, und die Engine wertet
        /// <c>BM_Typ</c> ausschließlich in <c>SimulationWaermepumpe</c> aus. Für die
        /// übrigen Erzeuger wird deshalb GESPERRT statt umgetextet — ein Modus, den
        /// niemand liest, wäre eine Zusage ohne Wirkung.
        /// </summary>
        private void BetriebsmodusBearbeiten(AnlagenInfo info)
        {
            if (!info.IstWaermepumpe)
            {
                MessageBox.Show(
                    string.Format(MyResource.Resource.SIM_MSG_MODUS_NUR_WP, info.Bezeichner),
                    MyResource.Resource.SIM_TITEL_BETRIEBSMODUS,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Form frm = new Form();
            frm.Text = string.Format(MyResource.Resource.SIM_BETRIEBSMODUS_FENSTERTITEL, info.Bezeichner);
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(520, 300);

            Label kopf = new Label
            {
                Text = MyResource.Resource.SIM_BETRIEBSMODUS_KOPF,
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(14, 14)
            };

            RadioButton rbLaufzeit = new RadioButton
            {
                Text = MyResource.Resource.SIM_BM_RB_LAUFZEIT,
                AutoSize = true,
                Location = new Point(24, 48)
            };
            Label lLaufzeit = new Label
            {
                Text = MyResource.Resource.SIM_BM_TEXT_LAUFZEIT,
                AutoSize = false,
                Size = new Size(460, 34),
                Location = new Point(46, 70)
            };

            RadioButton rbLeistung = new RadioButton
            {
                Text = MyResource.Resource.SIM_BM_RB_LEISTUNG,
                AutoSize = true,
                Location = new Point(24, 112)
            };
            Label lLeistung = new Label
            {
                Text = MyResource.Resource.SIM_BM_TEXT_LEISTUNG,
                AutoSize = false,
                Size = new Size(460, 34),
                Location = new Point(46, 134)
            };

            RadioButton rbPV = new RadioButton
            {
                Text = MyResource.Resource.SIM_BM_RB_PV,
                AutoSize = true,
                Location = new Point(24, 176)
            };
            Label lPV = new Label
            {
                Text = MyResource.Resource.SIM_BM_TEXT_PV,
                AutoSize = false,
                Size = new Size(460, 48),
                Location = new Point(46, 198)
            };

            switch (info.BM_Typ)
            {
                case WaermequelleClass.MODUS_LEISTUNG: rbLeistung.Checked = true; break;
                case WaermequelleClass.MODUS_PV: rbPV.Checked = true; break;
                default: rbLaufzeit.Checked = true; break;
            }

            Button ok = new Button { Text = MyResource.Resource.SIM_BTN_OK, DialogResult = DialogResult.OK, Location = new Point(332, 258), Width = 85 };
            Button abbruch = new Button { Text = MyResource.Resource.SIM_BTN_ABBRECHEN, DialogResult = DialogResult.Cancel, Location = new Point(423, 258), Width = 85 };

            frm.Controls.Add(kopf);
            frm.Controls.Add(rbLaufzeit); frm.Controls.Add(lLaufzeit);
            frm.Controls.Add(rbLeistung); frm.Controls.Add(lLeistung);
            frm.Controls.Add(rbPV); frm.Controls.Add(lPV);
            frm.Controls.Add(ok);
            frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            if (frm.ShowDialog(this) != DialogResult.OK) return;

            string modus = WaermequelleClass.MODUS_LAUFZEIT;
            if (rbLeistung.Checked) modus = WaermequelleClass.MODUS_LEISTUNG;
            else if (rbPV.Checked) modus = WaermequelleClass.MODUS_PV;

            WaermequelleClass.WertSchreiben(info.ID, "BM_Typ", modus);

            if (modus == WaermequelleClass.MODUS_PV && (comboBox5.SelectedIndex < 0 || !checkBox5.Checked))
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_PV_AUSWAHL,
                    MyResource.Resource.SIM_TITEL_BETRIEBSMODUS_PV,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            AktualisiereErzeugerUebersicht();
        }

        // --- Mouseover und Doppelklick ------------------------------------------------

        /// <summary>
        /// Mouseover-Hinweise: erklärt die per Doppelklick bearbeitbaren Spalten
        /// der Übersicht (WP-Priorität, Wärmequelle, Wärmesenke, Zweitsenke,
        /// Betriebsmodus).
        /// </summary>
        private void listView_Uebersicht_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = listView_Uebersicht.HitTest(e.Location);
            if (hit.Item == null || !(hit.Item.Tag is AnlagenInfo info))
            {
                if (_tipItem != null) { _tipItem = null; _tipSpalte = -1; _uebersichtTip.Hide(listView_Uebersicht); }
                return;
            }

            int spalte = hit.SubItem != null ? hit.Item.SubItems.IndexOf(hit.SubItem) : -1;

            // Nur bei Wechsel neu anzeigen (sonst flackert der Hinweis)
            if (_tipItem == hit.Item && _tipSpalte == spalte) return;
            _tipItem = hit.Item;
            _tipSpalte = spalte;

            string text;
            switch (spalte)
            {
                case COL_WPPRIO:
                    text = info.IstWaermepumpe
                        ? MyResource.Resource.SIM_TIP_WPPRIO
                        : MyResource.Resource.SIM_TIP_WPPRIO_NICHT_WP;
                    break;

                case COL_QUELLE:
                    text = info.IstWaermepumpe
                        ? MyResource.Resource.SIMQ_TIP_QUELLE
                        : MyResource.Resource.SIMQ_TIP_QUELLE_NICHT_WP;
                    break;

                case COL_SENKE:
                    text = MyResource.Resource.SIM_TIP_SENKE;
                    break;

                case COL_ZWEITSENKE:
                    text = MyResource.Resource.SIM_TIP_ZWEITSENKE;
                    break;

                case COL_BETRIEBSMODUS:
                    text = info.IstWaermepumpe
                        ? MyResource.Resource.SIM_TIP_BETRIEBSMODUS
                        : MyResource.Resource.SIM_TIP_BETRIEBSMODUS_NICHT_WP;
                    break;

                default:
                    text = string.Format(MyResource.Resource.SIM_TIP_UEBERSICHT_STANDARD, info.Bezeichner);
                    break;
            }

            _uebersichtTip.Show(text, listView_Uebersicht, e.X + 16, e.Y + 18, 15000);
        }

        /// <summary>
        /// Doppelklick in der Übersicht. Geöffnet wird ausschließlich, was in
        /// <see cref="SPALTEN_MIT_DIALOG"/> steht — jede andere Spalte tut nichts
        /// (Konzept 4.1, Ersatz für das frühere <c>else</c>-Fallback auf Spalte 4).
        /// </summary>
        private void listView_Uebersicht_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = listView_Uebersicht.HitTest(e.Location);
            if (hit.Item == null) return;
            if (!(hit.Item.Tag is AnlagenInfo info)) return; // Zeilen ohne Anlage (z. B. Gesamtsystem)

            if (hit.SubItem == null) return;
            int spalte = hit.Item.SubItems.IndexOf(hit.SubItem);
            if (Array.IndexOf(SPALTEN_MIT_DIALOG, spalte) < 0) return;

            switch (spalte)
            {
                case COL_WPPRIO:
                    WpPrioritaetBearbeiten(info);
                    break;

                case COL_SENKE:
                case COL_ZWEITSENKE:
                    // Beide Spalten führen in denselben Dialog - Haupt- und Zweitsenke
                    // gehören fachlich zusammen (Konzept 4.2).
                    WaermesenkeBearbeiten(info);
                    break;

                case COL_BETRIEBSMODUS:
                    BetriebsmodusBearbeiten(info);
                    break;

                case COL_QUELLE:
                    WaermequelleBearbeiten(info, hit.SubItem.Bounds);
                    break;
            }
        }

        /// <summary>Einsatz-Reihenfolge einer Wärmepumpe (nur dort sinnvoll).</summary>
        private void WpPrioritaetBearbeiten(AnlagenInfo info)
        {
            if (!info.IstWaermepumpe)
            {
                MessageBox.Show(
                    string.Format(MyResource.Resource.SIM_MSG_WPPRIO_NUR_WP, info.Bezeichner),
                    MyResource.Resource.SIM_TITEL_WPPRIO,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string eingabe = EingabeDialog(MyResource.Resource.SIM_WPPRIO_DIALOG_TITEL,
                string.Format(MyResource.Resource.SIM_WPPRIO_DIALOG_TEXT, info.Bezeichner),
                info.Prioritaet > 0 ? info.Prioritaet.ToString() : "1");

            int prioNeu;
            if (eingabe != null && Int32.TryParse(eingabe, out prioNeu) && prioNeu > 0)
            {
                WaermequelleClass.WertSchreiben(info.ID, "Prioritaet", prioNeu);
                AktualisiereErzeugerUebersicht();
            }
        }

        /// <summary>Wärmequelle: nur Wärmepumpen, und dort nur Sole-/Wasser-Wasser.</summary>
        private void WaermequelleBearbeiten(AnlagenInfo info, Rectangle zelle)
        {
            if (!info.IstWaermepumpe)
            {
                MessageBox.Show(
                    MyResource.Resource.SIMQ_MSG_QUELLE_NUR_WP,
                    MyResource.Resource.SIMQ_TITEL_WAERMEQUELLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(info.WpTyp) || info.WpTyp == DbWerte.WP_BAUART_LUFT_WASSER)
            {
                // Der WP-Typ ist ein Persistenzwert und bleibt als solcher stehen; nur
                // der Ersatztext für "nicht gepflegt" ist Anzeige.
                MessageBox.Show(
                    string.Format(MyResource.Resource.SIMQ_MSG_LUFT_WASSER,
                                  string.IsNullOrEmpty(info.WpTyp)
                                      ? MyResource.Resource.SIMQ_WPTYP_NICHT_GEPFLEGT
                                      : info.WpTyp),
                    MyResource.Resource.SIMQ_TITEL_WAERMEQUELLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            WaermequelleAuswahlAnzeigen(info, zelle);
        }

        // --- Senkendialog (Konzept 4.2) -----------------------------------------------

        /// <summary>
        /// Öffnet den Senkendialog <see cref="Form_Waermesenke"/> und schreibt das
        /// Ergebnis über <see cref="WaermesenkeClass.Schreiben"/> an die Anlage.
        ///
        /// Anschließend läuft die ÜBERGANGSBRÜCKE (Konzept 4.4, Etappe A): Die Engine
        /// liest den Wärmepumpen-Pufferspeicher bis Paket 4 aus <c>Z_ProjektPufferSp</c>.
        /// <see cref="WaermesenkeClass.WpSenkeSpiegeln"/> hält diese Alt-Zuordnung mit
        /// dem neuen Modell im Gleichstand; danach wird <c>_zuordnungen</c> neu geladen,
        /// damit der Delete/Insert-Zyklus beim nächsten „Speichern" den gerade erzeugten
        /// Stand nicht wieder wegschreibt. Mit Paket 4 entfallen beide Schritte.
        /// </summary>
        private void WaermesenkeBearbeiten(AnlagenInfo info)
        {
            Form_Waermesenke frm = new Form_Waermesenke();
            frm.ID_Projekt = m_ID_Projekt;
            frm.ID_Anlage = info.ID;
            frm.ID_Type = info.ID_Type;
            frm.AnlagenName = info.Bezeichner;
            frm.BM_Typ = info.BM_Typ;
            frm.Daten = WaermesenkeClass.Lesen(info.ID);
            frm.SetControls();

            DialogResult ergebnis = frm.ShowDialog(this);

            if (ergebnis == DialogResult.OK)
            {
                if (!WaermesenkeClass.Schreiben(info.ID, frm.Daten))
                    ShowStatus(MyResource.Resource.SIM_STATUS_SENKE_FEHLER, Color.Firebrick);
                else
                    ShowStatus(string.Format(MyResource.Resource.SIM_STATUS_SENKE_GESPEICHERT,
                                             WaermesenkeClass.HauptsenkeAnzeige(frm.Daten)),
                               Color.ForestGreen);

                ZuordnungBrueckeAnwenden();
            }

            // Auch nach Abbruch neu aufbauen: der Dialog kann über
            // "Pufferspeicher anlegen..." einen neuen Projekt-Puffer erzeugt haben.
            ZuordnungenLaden();
            RefreshZuordnungAnzeige();
            AktualisierePufferFusszeile();
        }

        /// <summary>
        /// ÜBERGANGSBRÜCKE auf das Altmodell (Konzept 4.4, Etappe A) — ENTFÄLLT MIT
        /// PAKET 4.
        ///
        /// Solange <c>SimulationControl.Do_Simulation</c> den Wärmepumpen-Speicher aus
        /// <c>Z_ProjektPufferSp</c> holt, muss eine im Senkendialog gesetzte Puffer-Senke
        /// dort ankommen — sonst bliebe die Eingabe bis Paket 4 wirkungslos, und der
        /// Anwender sähe eine Einstellung ohne Ergebnis.
        ///
        /// Die Regel steht in <see cref="WaermesenkeClass.WpSenkeSpiegeln"/>:
        /// Hauptsenke <c>PufferHeizung</c> einer WP ⇒ genau eine Zuordnungszeile
        /// <c>Erzeuger = 'Wärmepumpe'</c> auf diesen Puffer; Senke <c>Heizkreis</c> ⇒
        /// Zuordnungszeile entfernen.
        /// </summary>
        private void ZuordnungBrueckeAnwenden()
        {
            if (m_ID_Projekt <= 0) return;
            WaermesenkeClass.WpSenkeSpiegeln(m_ID_Projekt);
        }

        // --- Wärmequellen-Auswahl (Bestand) -------------------------------------------

        /// <summary>
        /// Zeigt das Wärmequellen-Dropdown (Sole-/Wasser-Wasser-WP) direkt in der
        /// Übersicht an - analog zur Zellbearbeitung in der Zuordnungstabelle.
        /// </summary>
        private void WaermequelleAuswahlAnzeigen(AnlagenInfo info, Rectangle zellBounds)
        {
            if (_wqCombo == null)
            {
                _wqCombo = new ComboBox { Visible = false, DropDownStyle = ComboBoxStyle.DropDownList };
                _wqCombo.SelectedIndexChanged += WqCombo_SelectedIndexChanged;
                _wqCombo.LostFocus += (s, ev) => _wqCombo.Visible = false;
            }
            if (!this.Controls.Contains(_wqCombo)) this.Controls.Add(_wqCombo);

            _wqInfo = info;

            _wqUpdating = true;
            _wqCombo.Items.Clear();
            _wqCombo.Items.AddRange(WaermequelleClass.TypAnzeige);
            int aktuell = Array.IndexOf(WaermequelleClass.TypWerte,
                string.IsNullOrEmpty(info.WQ_Typ) ? WaermequelleClass.TYP_AUSSENLUFT : info.WQ_Typ);
            _wqCombo.SelectedIndex = aktuell >= 0 ? aktuell : 0;
            _wqUpdating = false;

            Point screenPoint = listView_Uebersicht.PointToScreen(zellBounds.Location);
            Point formPoint = this.PointToClient(screenPoint);
            _wqCombo.Bounds = new Rectangle(formPoint, new Size(Math.Max(zellBounds.Width, 190), zellBounds.Height));
            _wqCombo.Visible = true;
            _wqCombo.BringToFront();
            _wqCombo.Focus();
            _wqCombo.DroppedDown = true;
        }

        private void WqCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_wqUpdating || _wqInfo == null || _wqCombo.SelectedIndex < 0) return;

            string typNeu = WaermequelleClass.TypWerte[_wqCombo.SelectedIndex];
            AnlagenInfo info = _wqInfo;
            _wqCombo.Visible = false;

            switch (typNeu)
            {
                case WaermequelleClass.TYP_AUSSENLUFT:
                    WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                    break;

                case WaermequelleClass.TYP_KONSTANT:
                    {
                        string eingabe = EingabeDialog(
                            MyResource.Resource.SIMQ_KONSTANT_DIALOG_TITEL,
                            string.Format(MyResource.Resource.SIMQ_KONSTANT_DIALOG_TEXT, info.Bezeichner),
                            info.WQ_Temp != 0 ? info.WQ_Temp.ToString("0.#") : "10");
                        float temp;
                        if (eingabe == null || !WaermequelleClass.ZahlParsen(eingabe, out temp)) return;
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Temp", (double)temp);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }

                case WaermequelleClass.TYP_PUFFER:
                    {
                        // Auswahl des Pufferspeichers, der als Wärmequelle dient.
                        // E0: Der Dialog arbeitet mit den PROJEKT-Puffern und liefert die
                        // ID; der Bezeichner wird nur noch mitgeführt.
                        Form_QuellePufferspeicher frmQuelle = new Form_QuellePufferspeicher();
                        frmQuelle.WPName = info.Bezeichner;
                        frmQuelle.ID_Projekt = m_ID_Projekt;
                        object oIdPuffer = WaermequelleClass.WertLesen(info.ID, "WQ_ID_Puffer");
                        if (oIdPuffer != null) frmQuelle.ID_Puffer = Convert.ToInt32(oIdPuffer);
                        frmQuelle.Pufferspeicher = WaermequelleClass.WertLesen(info.ID, "WQ_Puffer") as string;

                        object oTemp = WaermequelleClass.WertLesen(info.ID, "WQ_Temp");
                        if (oTemp != null) frmQuelle.Quelltemperatur = Convert.ToDouble(oTemp);
                        object oSpreiz = WaermequelleClass.WertLesen(info.ID, "WQ_Spreizung");
                        if (oSpreiz != null && Convert.ToDouble(oSpreiz) > 0) frmQuelle.Spreizung = Convert.ToDouble(oSpreiz);
                        object oReg = WaermequelleClass.WertLesen(info.ID, "WQ_Regeneration");
                        if (oReg != null) frmQuelle.Regeneration = Convert.ToDouble(oReg);
                        object oUnb = WaermequelleClass.WertLesen(info.ID, "WQ_Unbegrenzt");
                        if (oUnb != null) frmQuelle.Unbegrenzt = Convert.ToBoolean(oUnb);

                        frmQuelle.SetControls();
                        if (frmQuelle.ShowDialog(this) != DialogResult.OK) return;

                        // E0: FÜHREND ist der Fremdschlüssel. Er geht über die
                        // Überladung mit ausdrücklichem OleDbType weg — 0 ist keine
                        // gültige Puffer-ID, und die erzwungene Beziehung aus Schritt 4
                        // der SchemaMigration würde sie abweisen (dieselbe Regel wie in
                        // WaermesenkeClass.Schreiben).
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_ID_Puffer",
                            System.Data.OleDb.OleDbType.Integer,
                            frmQuelle.ID_Puffer > 0 ? (object)frmQuelle.ID_Puffer : DBNull.Value);
                        // Der Bezeichner wird MITGESCHRIEBEN: Anzeigen und die
                        // Rückfallkette der Engine (Stufe 2/3) lesen ihn weiter.
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Puffer", frmQuelle.Pufferspeicher);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Temp", frmQuelle.Quelltemperatur);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Spreizung", frmQuelle.Spreizung);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Regeneration", frmQuelle.Regeneration);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Unbegrenzt", frmQuelle.Unbegrenzt);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }

                case WaermequelleClass.TYP_PROFIL:
                    {
                        // Quellprofil über Monats- und Wochenwerte
                        // (analog "Brauchwassertypen Stundenverteilung")
                        Form_Quellprofil frmProfil = new Form_Quellprofil();
                        frmProfil.WPName = info.Bezeichner;
                        frmProfil.Monatswerte = WaermequelleClass.WertLesen(info.ID, "WQ_Monatswerte") as string;
                        frmProfil.Wochenwerte = WaermequelleClass.WertLesen(info.ID, "WQ_Wochenwerte") as string;
                        frmProfil.SetControls();

                        if (frmProfil.ShowDialog(this) != DialogResult.OK) return;

                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Monatswerte", frmProfil.Monatswerte);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Wochenwerte", frmProfil.Wochenwerte);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }

                case WaermequelleClass.TYP_CSV:
                    {
                        if (MessageBox.Show(
                            string.Format(MyResource.Resource.SIMQ_CSV_FRAGE_DATEI,
                                          WaermequelleClass.CSV_FORMAT_HINWEIS),
                            MyResource.Resource.SIMQ_CSV_TITEL, MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Information) != DialogResult.OK) return;

                        OpenFileDialog dlg = new OpenFileDialog();
                        dlg.Title = MyResource.Resource.SIMQ_CSV_DATEIDIALOG_TITEL;
                        dlg.Filter = MyResource.Resource.SIMQ_CSV_DATEIFILTER;
                        if (dlg.ShowDialog() != DialogResult.OK) return;

                        if (WaermequelleClass.ProfilAusCsv(dlg.FileName) == null)
                        {
                            MessageBox.Show(
                                string.Format(MyResource.Resource.SIMQ_CSV_FEHLER,
                                              WaermequelleClass.CSV_FORMAT_HINWEIS),
                                MyResource.Resource.SIMQ_CSV_FEHLER_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_CSV", dlg.FileName);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }

                case WaermequelleClass.TYP_ERDREICH:
                    {
                        // Erdreich nach VDI 4640 (Konzept 4.5): Kollektor oder Sonde.
                        Form_QuelleErdreich frmErde = new Form_QuelleErdreich();
                        frmErde.WPName = info.Bezeichner;

                        string quellsystem = WaermequelleClass.WertLesen(info.ID, "WQ_Quellsystem") as string;
                        if (!string.IsNullOrEmpty(quellsystem)) frmErde.Quellsystem = quellsystem;

                        object oTiefe = WaermequelleClass.WertLesen(info.ID, "WQ_Tiefe");
                        if (oTiefe != null && Convert.ToDouble(oTiefe) > 0) frmErde.Tiefe = Convert.ToDouble(oTiefe);
                        object oFlaeche = WaermequelleClass.WertLesen(info.ID, "WQ_Flaeche");
                        if (oFlaeche != null) frmErde.Flaeche = Convert.ToDouble(oFlaeche);
                        object oAnzahl = WaermequelleClass.WertLesen(info.ID, "WQ_Anzahl");
                        if (oAnzahl != null && Convert.ToInt32(oAnzahl) > 0) frmErde.Anzahl = Convert.ToInt32(oAnzahl);
                        string bodentyp = WaermequelleClass.WertLesen(info.ID, "WQ_Bodentyp") as string;
                        if (!string.IsNullOrEmpty(bodentyp)) frmErde.Bodentyp = bodentyp;
                        // Nutzbare Spreizung (Konzept 13.1) - dieselbe Spalte wie beim
                        // Pufferspeicher-Quellendialog, jetzt auch hier pflegbar.
                        object oSpreizErde = WaermequelleClass.WertLesen(info.ID, "WQ_Spreizung");
                        if (oSpreizErde != null && Convert.ToDouble(oSpreizErde) > 0)
                            frmErde.Spreizung = Convert.ToDouble(oSpreizErde);

                        // Klimazone aus der Region vorbelegen (0 = nicht zugeordnet),
                        // Außentemperaturvektor einmalig laden und gecacht übergeben.
                        int zoneVorher = KlimazoneDesProjekts();
                        frmErde.Klimazone = zoneVorher;
                        frmErde.Aussentemperatur = AussentemperaturLaden();

                        // Ergebnisanbindung der Auslegungsprüfung (Paket 7): Liegt für
                        // diese Anlage ein Simulationslauf der Sitzung vor, bekommt der
                        // Dialog die echten Werte statt "(noch kein Simulationslauf)".
                        ErdreichAuswertung.AnlageErgebnis erdErg =
                            ErdreichAuswertung.FuerAnlage(m_ID_Projekt, info.ID);
                        if (erdErg != null)
                        {
                            frmErde.ErgebnisseVorhanden = erdErg.MaxEntzugBelastbar;
                            frmErde.MaxEntzugW = erdErg.MaxEntzugW;
                            frmErde.JahresentzugKWh = erdErg.JahresentzugKWh;
                            frmErde.VolllastStunden = erdErg.VolllastStunden;
                            if (erdErg.Unwirksam)
                                // Luft-Wasser: die Konfiguration wird gar nicht gerechnet.
                                // Das muss im Dialog stehen, sonst pflegt der Anwender
                                // Bodentyp und Sondenlänge ins Leere (Konzept 4.5).
                                // Umbrüche VOR dem Einsetzen umstellen (die Ressource legt
                                // sie als LF ab, der Bestand nutzte hier \r\n).
                                frmErde.HinweisErgebnis = string.Format(
                                    MyResource.Resource.SIMQ_ERDREICH_WIRKUNGSLOS
                                        .Replace("\n", Environment.NewLine), erdErg.Grenze);
                            else if (!erdErg.MaxEntzugBelastbar)
                                frmErde.HinweisErgebnis = string.Format(
                                    MyResource.Resource.SIMQ_ERDREICH_KEINE_PRUEFUNG
                                        .Replace("\n", Environment.NewLine), erdErg.Grenze);
                            else
                            {
                                if (erdErg.MaxEntzugGeschaetzt)
                                    frmErde.HinweisVorbehalt = erdErg.Grenze;
                                if (erdErg.InklSpeicherladung)
                                    frmErde.HinweisVorbehalt = (frmErde.HinweisVorbehalt.Length > 0
                                        ? frmErde.HinweisVorbehalt + " "
                                        : "") +
                                        MyResource.Resource.SIMQ_ERDREICH_SPEICHERLADUNG;
                                if (erdErg.FrostWarnung)
                                    frmErde.HinweisFrost = erdErg.Frosttext();
                            }
                        }

                        frmErde.SetControls();
                        if (frmErde.ShowDialog(this) != DialogResult.OK) return;

                        // Die Klimazone ist eine Eigenschaft der Region, nicht der Anlage
                        // (Konzept 13.1) - eine Änderung im Dialog geht deshalb an die Region.
                        if (frmErde.Klimazone != zoneVorher) KlimazoneSpeichern(frmErde.Klimazone);

                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Quellsystem", frmErde.Quellsystem);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Tiefe", frmErde.Tiefe);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Flaeche", frmErde.Flaeche);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Anzahl", frmErde.Anzahl);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Bodentyp", frmErde.Bodentyp);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Spreizung", frmErde.Spreizung);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }
            }

            AktualisiereErzeugerUebersicht();
        }

        /// <summary>
        /// Kleiner modaler Eingabedialog (Titel, Beschriftung, Vorgabewert).
        /// Liefert den eingegebenen Text oder null bei Abbruch.
        /// </summary>
        private string EingabeDialog(string titel, string beschriftung, string vorgabe)
        {
            Form frm = new Form();
            frm.Text = titel;
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(340, 140);

            Label lbl = new Label { Text = beschriftung, AutoSize = true, Location = new Point(12, 12) };
            TextBox txt = new TextBox { Location = new Point(12, 75), Width = 316, Text = vorgabe ?? "" };
            Button ok = new Button { Text = MyResource.Resource.SIM_BTN_OK, DialogResult = DialogResult.OK, Location = new Point(172, 105), Width = 75 };
            Button abbruch = new Button { Text = MyResource.Resource.SIM_BTN_ABBRECHEN, DialogResult = DialogResult.Cancel, Location = new Point(253, 105), Width = 75 };

            frm.Controls.Add(lbl);
            frm.Controls.Add(txt);
            frm.Controls.Add(ok);
            frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            return frm.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
        }
    }
}
