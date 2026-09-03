using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPEN D2 und D3 (Konzept_KonfigUI_Hydraulik, Abschnitt 3, 3a und 6) —
    /// Kartenlayout der Simulationskonfiguration.
    ///
    /// <b>Was hier steht.</b> Der Aufbau der beiden Kartenspalten, das Befüllen der
    /// Karten aus den Projektdaten und die Kaskaden-Umsortierung über ▲▼. Die Karten
    /// selbst sind <see cref="ErzeugerKarte"/> und <see cref="SpeicherKarte"/> — reine
    /// Anzeigeflächen ohne Datenbankzugriff.
    ///
    /// <b>Was NICHT hier steht.</b> Die Editoren. Sie bleiben unverändert in
    /// <c>Form_Simulation_Config.Uebersicht.cs</c> (Senken-, Quellen-, Modus- und
    /// Prioritätsdialog) und werden von hier nur AUFGERUFEN. Konzept Abschnitt 3:
    /// „Die neue Seite ist Lesefläche, keine Parallel-Editierwelt."
    ///
    /// <b>Was ersetzt wurde.</b>
    /// <list type="bullet">
    ///   <item><description><c>listView_Uebersicht</c> — acht Spalten mit fest
    ///     verdrahteten Breiten (<c>SPALTEN_BREITEN</c>), deren Summe das Formular auf
    ///     1113 px aufblies, und ein Doppelklick-Dispatcher über Spaltenindizes
    ///     (<c>SPALTEN_MIT_DIALOG</c>). Beides ist mit den Karten gegenstandslos: Ein
    ///     Chip trägt sein Editorziel selbst (<see cref="ErzeugerKarte.ChipZiel"/>), und
    ///     die Breite regelt das <see cref="TableLayoutPanel"/>.</description></item>
    ///   <item><description>Die Rubrik „Erzeuger &amp;&amp; Speicher" mit vier
    ///     ComboBoxen und vier Checkboxen. Die Steuerelemente bleiben als
    ///     PERSISTENZMODELL bestehen (siehe <see cref="KaskadeLesen"/>) — sichtbar ist
    ///     nur noch die Kartenreihenfolge.</description></item>
    ///   <item><description><c>label_PufferListe</c>, die einzeilige Aufzählung der
    ///     Projekt-Puffer in der Fußzeile. Die Speicherkarten sagen dasselbe und
    ///     mehr (Konzept 3a).</description></item>
    /// </list>
    /// </summary>
    public partial class Form_Simulation_Config : BaseForm
    {
        // --- Maße des Kartenlayouts ---------------------------------------------------
        //
        // Nur noch DREI Zahlen statt der Pixel-Arithmetik aus Paket 2/8: Rand, Oberkante
        // und Höhe der Fußzeile. Alles Übrige rechnet das TableLayoutPanel bzw. die
        // Verankerung aus (Konzeptvorgabe D2/D3: „beendet die Pixel-Arithmetik für diese
        // Bereiche").

        /// <summary>Seitenrand des Kartenbereichs [px] — wie label11 und groupBox_Tools.</summary>
        private const int KARTEN_RAND = 19;

        /// <summary>
        /// RECHTER Rand des Kartenbereichs und des Ansichtsumschalters [px].
        ///
        /// <para><b>D3 (28.08.2026), offener Punkt aus D2.</b> Links richtet sich alles an
        /// den Entwurfselementen aus (<c>label11</c> 18, <c>groupBox_Tools</c> 19) — das
        /// bleibt so, sonst stünde die Kartenspalte nicht mehr unter ihrer Überschrift.
        /// RECHTS gab es dagegen zwei Maße nebeneinander: der Kartenbereich 19 px, die
        /// Fußzeile seit der Norm 12 px. Der 19er stammt NICHT aus dem Platz für den
        /// Rollbalken — den zieht <see cref="KartenBreiteAnpassen"/> innerhalb der Spalte
        /// ab —, sondern war schlicht dieselbe Zahl wie links. Rechts gilt jetzt das
        /// Randmaß der Norm, damit Umschalter, Kartenfläche und Knopfreihe EINE Flucht
        /// bilden.</para>
        /// </summary>
        private const int KARTEN_RAND_RECHTS = FusszeilenNorm.RAND;

        /// <summary>Oberkante des Kartenbereichs [px] — unter der Überschrift label11.</summary>
        private const int KARTEN_OBEN = 44;

        /// <summary>Höhe der Fußzeile [px]: Schalterzeile, Knopfzeile und Ränder.</summary>
        private const int FUSS_HOEHE = 82;

        /// <summary>Wunschgröße des Dialogs [px], gedeckelt an der Arbeitsfläche.</summary>
        private const int WUNSCH_BREITE = 1120;
        private const int WUNSCH_HOEHE = 620;

        // --- Steuerelemente -----------------------------------------------------------

        private TableLayoutPanel tableLayout_Karten;
        private Label label_KopfErzeuger;
        private Label label_KopfSpeicher;
        private FlowLayoutPanel flow_Erzeuger;
        private FlowLayoutPanel flow_Speicher;

        /// <summary>Einstieg in die Puffer-Verwaltung; steht seit D3 IN der Speicherspalte.</summary>
        private Button btn_PufferVerwalten;

        /// <summary>
        /// Sperre gegen Rückkopplung beim programmatischen Umsortieren der Kaskade:
        /// <see cref="KaskadeSchreiben"/> setzt <c>SelectedValue</c> und <c>Checked</c>
        /// der vier Auswahlfelder, und deren Ereignisse riefen sonst mitten im Umbau
        /// <c>AddErzeuger</c> und damit den Kartenaufbau auf.
        /// </summary>
        private bool _kaskadeSetzen;

        /// <summary>
        /// Aufgeklappte Speicherkarte (<c>Tab_Pufferspeicher.ID</c>); 0 = keine.
        /// Konzept 3a: „es ist immer höchstens eine Karte aufgeklappt". Die Karte selbst
        /// kennt ihre Nachbarn nicht — die Regel gilt hier.
        /// </summary>
        private int _offenerSpeicher;

        /// <summary>
        /// ABNAHMEBEFUND 3 — aufgeklappte Karte der Strom-/Speicherseite als
        /// <c>Tab_Energieanlagen.ID_Type</c> (<c>PV_TYP</c> bzw. <c>SP_TYP</c>);
        /// 0 = keine. Dieselbe Regel wie bei den Wärmespeichern: Es ist immer höchstens
        /// EINE Karte offen, und die Karte selbst kennt ihre Nachbarn nicht.
        /// </summary>
        private int _offeneStromgruppe;

        /// <summary>
        /// ABNAHMEBEFUND 1 — stehen die NICHT gewählten Komponenten in der Spalte?
        ///
        /// Vorbelegung <c>false</c>: Die Spalte zeigt, was gerechnet wird. Die
        /// gestrichelten Platzhalter („BHKW — nicht in der Simulation · keine Anlage im
        /// Projekt") sind seit D2 aber der EINZIGE Weg, eine Komponente überhaupt
        /// zuzuschalten — die vier Erzeuger-Combos samt Haken und die beiden
        /// Strom-Auswahlfelder sind unsichtbar (<see cref="AltSteuerelementeStilllegen"/>)
        /// und werden nur noch programmatisch bedient. Ausgeblendet heißt deshalb
        /// „eingeklappt", nicht „weg": Der Textschalter am Spaltenende
        /// (<see cref="VerfuegbarSchalterAnfuegen"/>) holt sie zurück.
        ///
        /// Sitzungszustand, bewusst nicht persistiert: Die Ansicht legt bisher keinen
        /// einzigen Anzeigezustand in der Datenbank ab (auch <c>_offenerSpeicher</c> und
        /// <c>_offeneStromgruppe</c> nicht) — eine Spalte in <c>Tab_Einstellungen</c> für
        /// eine Sichtvorliebe wäre der erste Bruch mit diesem Muster.
        /// </summary>
        private bool _verfuegbareZeigen;

        /// <summary>
        /// Wie viele Platzhalterkarten der laufende Spaltenaufbau unterdrückt hat — die
        /// Zahl im Einblendeschalter.
        /// </summary>
        private int _verfuegbarVersteckt;

        /// <summary>
        /// Quellpuffer-ID → Anlagen, die ihn als Wärmequelle nutzen („Quelle für",
        /// Konzept 3a). Wird je Auffrischung EINMAL gefüllt (<see cref="QuellnutzerSammeln"/>).
        /// </summary>
        private Dictionary<int, List<string>> _quellnutzer = new Dictionary<int, List<string>>();

        /// <summary>
        /// Puffer-IDs, die überhaupt von einer Anlage geladen werden — der Filter vor
        /// <see cref="Ladeordnung.Ladereihenfolge"/> (siehe <see cref="GeladenePufferSammeln"/>).
        /// </summary>
        private HashSet<int> _geladenePuffer = new HashSet<int>();

        /// <summary>
        /// Systemvorgabe des Projekts (kleinster Vorlauf, größter Rücklauf über die
        /// Wärmeerzeuger) — die dritte Stufe der Temperatur-Vorrangkette. Sie hängt nur
        /// am Projekt, wird aber je Speicherkarte gebraucht; deshalb einmal je
        /// Auffrischung geholt statt 79-mal (siehe <see cref="TemperaturHerkunft"/>).
        /// </summary>
        private int? _systemVorlauf;
        private int? _systemRuecklauf;

        /// <summary>
        /// PAKET P1 — Puffer-ID → Schichtenzahl, aber NUR für Speicher mit <c>N &gt; 1</c>
        /// (<c>PufferSpCtrl.SchichtenJeProjekt</c>). Ein leeres Verzeichnis heißt „kein
        /// geschichteter Speicher im Projekt" und ist zugleich der Zustand ohne
        /// Migrationsschritt 53.
        /// </summary>
        private Dictionary<int, int> _schichtenJePuffer = new Dictionary<int, int>();

        /// <summary>
        /// PAKET P1 — Puffer-ID → <c>T_oben_Mittel</c> [°C] aus dem JÜNGSTEN Ergebnis des
        /// Projekts (siehe <see cref="TObenSammeln"/>). Leer, solange kein Lauf gerechnet
        /// wurde oder die Spalte keinen Wert trägt.
        /// </summary>
        private Dictionary<int, double> _tObenJePuffer = new Dictionary<int, double>();

        // --- Aufbau -------------------------------------------------------------------

        /// <summary>
        /// ETAPPE D2/D3 — Nachfolger von <c>AltRubrikStilllegen</c>,
        /// <c>InitErzeugerUebersicht</c>, <c>UebersichtBreiteAnpassen</c> und
        /// <c>InitPufferFusszeile</c>.
        ///
        /// <b>Was entfallen ist.</b> Die vier Höhen- und Breitenkorrekturen, mit denen
        /// sich der Dialog seit Paket 2 selbst zurechtgeschoben hat: +105 px für die
        /// Alt-Rubrik, +322 px für die Spaltensumme der Übersicht, +16 px für den
        /// Extrapolationsschalter und das Nachziehen der drei unverankerten
        /// Fußzeilenelemente bei jedem dieser Schritte. Jede Zahl war für sich richtig
        /// und in Summe nicht mehr nachvollziehbar.
        ///
        /// <b>Was an ihre Stelle tritt.</b> Eine Wunschgröße, ein
        /// <see cref="TableLayoutPanel"/> mit zwei Spalten und eine ordentliche
        /// Verankerung der Fußzeile. Damit ist der Dialog erstmals sauber
        /// größenveränderbar: Bisher standen <c>btn_Speichern</c> und <c>btn_OK</c> ohne
        /// Anker (also oben-links verankert) und blieben beim Vergrößern in der Mitte des
        /// Fensters stehen, während die untenverankerte Statuszeile mitwanderte.
        ///
        /// <b>Alt-Steuerelemente.</b> <c>groupBox_Tools</c> (vier Erzeuger-ComboBoxen,
        /// Stromerzeuger, Energiespeicher) wird UNSICHTBAR, nicht entfernt: Sie ist das
        /// Persistenzmodell von <c>Tab_Einstellungen.Tool_1..6</c>, aus dem
        /// <c>btn_Speichern_Click</c> unverändert liest (siehe
        /// <see cref="KaskadeLesen"/>). <c>groupBox_PufferSp</c> und
        /// <c>checkBox_PufferSp</c> sind seit D1 unsichtbar und bleiben es; ihre Rolle
        /// als Höhenanker der Übersicht ist mit dieser Methode weggefallen.
        /// </summary>
        private void KartenLayoutAufbauen()
        {
            AltSteuerelementeStilllegen();
            FenstergroesseSetzen();
            KartenbereichAufbauen();

            // D4: Umschalter Liste/Schema und die Schemafläche. NACH dem Kartenbereich -
            // die Schemafläche übernimmt dessen Rechteck und Verankerung.
            SchemaAufbauen();

            // Der Schalter der Fußzeile entsteht wie bisher programmatisch (Paket 8);
            // platziert wird er zusammen mit der übrigen Fußzeile und nicht mehr aus sich
            // selbst heraus. PAKET A1: Der zweite Schalter „Zweikanalige Kaskade" ist
            // entfallen (Begründung in Form_Simulation_Config.Uebersicht).
            InitExtrapolationSchalter();
            // PAKET B2: der zweite Fußzeilenschalter - der Lesepunkt der
            // Booster-Quelltemperatur. Er bleibt unsichtbar, bis das Projekt einen
            // gekoppelten Booster führt (AktualisiereBoosterLesepunktSchalter).
            InitBoosterLesepunktSchalter();
            FusszeilePlatzieren();
        }

        private void AltSteuerelementeStilllegen()
        {
            // D2: Die linke Auswahlmechanik verschwindet aus der Oberfläche. Die
            // Steuerelemente selbst bleiben - sie tragen Tool_1..6 (siehe Klassenkopf).
            groupBox_Tools.Visible = false;

            // Beschriftungen, die nur die entfallene Mechanik erklären.
            label12.Visible = false;   // "Erzeuger in der Reihenfolge auswählen ..."
            label21.Visible = false;   // "Priorität absteigend"

            // D1-Bestand: Alt-Rubrik und ihr Einblendeschalter.
            checkBox_PufferSp.Visible = false;
            checkBox_PufferSp.Checked = true;   // hält evtl. abfragende Logik konsistent
            groupBox_PufferSp.Visible = false;
        }

        /// <summary>
        /// Setzt die Wunschgröße des Dialogs, gedeckelt auf die Arbeitsfläche.
        ///
        /// Zwei Kartenspalten nebeneinander brauchen mehr als die 791 px des Entwurfs;
        /// die Übersicht hatte sich denselben Platz vorher über die Spaltensumme geholt
        /// (1113 px). Es wird nur VERGRÖSSERT — auf einem kleinen Bildschirm bleibt der
        /// Dialog bei dem, was hineinpasst, und <see cref="BaseForm"/> deckelt in ihrem
        /// <c>OnLoad</c> noch einmal auf die echte Arbeitsfläche.
        /// </summary>
        private void FenstergroesseSetzen()
        {
            int breite = WUNSCH_BREITE;
            int hoehe = WUNSCH_HOEHE;

            Screen schirm = Screen.PrimaryScreen;
            if (schirm != null)
            {
                // DpiUnaware (Program.cs): Pixel sind Pixel, kein Skalierungsfaktor.
                int rahmenBreite = Width - ClientSize.Width;
                int rahmenHoehe = Height - ClientSize.Height;
                breite = Math.Min(breite, schirm.WorkingArea.Width - 40 - rahmenBreite);
                hoehe = Math.Min(hoehe, schirm.WorkingArea.Height - 40 - rahmenHoehe);
            }

            ClientSize = new Size(Math.Max(ClientSize.Width, breite),
                                  Math.Max(ClientSize.Height, hoehe));
        }

        private void KartenbereichAufbauen()
        {
            tableLayout_Karten = new TableLayoutPanel();
            tableLayout_Karten.Name = "tableLayout_Karten";
            tableLayout_Karten.ColumnCount = 2;
            tableLayout_Karten.RowCount = 2;
            tableLayout_Karten.BackColor = Color.Transparent;

            // Verhältnis wie im Mockup (1,55fr : 1fr): Die Erzeugerkarten tragen mehr
            // Chips, die Speicherkarten sind zugeklappt einzeilig.
            tableLayout_Karten.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61.5f));
            tableLayout_Karten.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38.5f));
            tableLayout_Karten.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayout_Karten.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tableLayout_Karten.Location = new Point(KARTEN_RAND, KARTEN_OBEN);
            tableLayout_Karten.Size = new Size(
                ClientSize.Width - KARTEN_RAND - KARTEN_RAND_RECHTS,
                ClientSize.Height - FUSS_HOEHE - KARTEN_OBEN);
            tableLayout_Karten.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                                        AnchorStyles.Right | AnchorStyles.Bottom;

            label_KopfErzeuger = SpaltenKopf(MyResource.Resource.SIM_KARTEN_KOPF_ERZEUGER);
            label_KopfSpeicher = SpaltenKopf(MyResource.Resource.PSP_KARTEN_KOPF_SPEICHER);

            flow_Erzeuger = Kartenspalte("flow_Erzeuger");
            flow_Speicher = Kartenspalte("flow_Speicher");

            // D3: Die 8 px rechts sind der ZWISCHENraum der beiden Spalten. In der
            // rechten Spalte wären sie ein zusätzlicher Außenrand — die graue Fläche
            // endete damit 8 px vor der Flucht, die KARTEN_RAND_RECHTS vorgibt.
            label_KopfSpeicher.Margin = new Padding(0, 0, 0, 2);
            flow_Speicher.Margin = new Padding(0, 0, 0, 0);

            tableLayout_Karten.Controls.Add(label_KopfErzeuger, 0, 0);
            tableLayout_Karten.Controls.Add(label_KopfSpeicher, 1, 0);
            tableLayout_Karten.Controls.Add(flow_Erzeuger, 0, 1);
            tableLayout_Karten.Controls.Add(flow_Speicher, 1, 1);

            Controls.Add(tableLayout_Karten);
            tableLayout_Karten.BringToFront();

            // Einstieg in die Puffer-Verwaltung: KEIN Fußzeilenknopf mehr, sondern die
            // letzte Zeile der Speicherspalte (Konzept 3a / Mockup Abschnitt 4).
            btn_PufferVerwalten = new Button();
            btn_PufferVerwalten.Name = "btn_PufferVerwalten";
            btn_PufferVerwalten.Text = MyResource.Resource.PSP_BTN_PUFFER_VERWALTEN;
            btn_PufferVerwalten.Height = 28;
            btn_PufferVerwalten.Margin = new Padding(0, 4, 0, 4);
            btn_PufferVerwalten.Click += btn_PufferVerwalten_Click;
        }

        private Label SpaltenKopf(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = false;
            l.Height = 34;
            l.Dock = DockStyle.Fill;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.ForeColor = KartenStil.TEXT_LEISE;
            l.Margin = new Padding(0, 0, 8, 2);
            return l;
        }

        private FlowLayoutPanel Kartenspalte(string name)
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.Name = name;
            f.Dock = DockStyle.Fill;
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            f.AutoScroll = true;
            f.BackColor = KartenStil.FLAECHE;
            f.Padding = new Padding(8);
            f.Margin = new Padding(0, 0, 8, 0);
            f.ClientSizeChanged += delegate { KartenBreiteAnpassen(f); };
            return f;
        }

        /// <summary>
        /// Zieht die Karten auf die Breite ihrer Spalte.
        ///
        /// Ein <see cref="FlowLayoutPanel"/> streckt seine Kinder nicht — die Breite muss
        /// von Hand nachgeführt werden. Der Platz der senkrechten Bildlaufleiste wird
        /// IMMER abgezogen, auch wenn sie gerade nicht sichtbar ist: Sonst pendelt das
        /// Layout (Karte breiter → Leiste verschwindet → Karte noch breiter → Leiste
        /// wieder da), weil die Kartenhöhe über den Chip-Umbruch an der Breite hängt.
        /// </summary>
        private static void KartenBreiteAnpassen(FlowLayoutPanel flow)
        {
            if (flow == null) return;

            int breite = flow.Width - flow.Padding.Horizontal -
                         SystemInformation.VerticalScrollBarWidth - 2;
            if (breite < 140) breite = 140;

            foreach (Control c in flow.Controls)
            {
                int w = breite - c.Margin.Horizontal;
                if (c.Width != w) c.Width = w;
            }
        }

        /// <summary>
        /// Setzt die Fußzeile: Schalterzeile links oben, Knopfzeile unten rechts,
        /// Statuszeile unten links — und verankert alles, was mitwandern muss.
        ///
        /// Die Fußzeilenhöhe ist mit <see cref="FUSS_HOEHE"/> festgeschrieben statt aus
        /// den Elementen hochgerechnet: Die alte Rechnung ergab je nach Schriftgröße
        /// Kollisionen, die dann mit einem nachträglichen „Formular um die fehlenden
        /// Pixel höher" repariert wurden (Befund N13a, Paket 8). Sichergestellt wird die
        /// Kollisionsfreiheit jetzt durch die Aufteilung: Schalter über den Knöpfen,
        /// Statuszeile neben ihnen.
        /// </summary>
        private void FusszeilePlatzieren()
        {
            int fussOben = ClientSize.Height - FUSS_HOEHE;

            // PAKET A1: Die Schalterzeile trägt nur noch die Extrapolation - der Schalter
            // „Zweikanalige Kaskade" ist mit dem einkanaligen Altpfad entfallen. Er stand
            // links, die Extrapolation daneben; jetzt rückt sie an den linken Rand.
            checkBox_Extrapolation.Location = new Point(KARTEN_RAND, fussOben + 6);
            checkBox_Extrapolation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            // PAKET B2: der Booster-Lesepunkt NEBEN der Extrapolation, in derselben
            // Schalterzeile. Die x-Position wird gemessen statt geraten - die
            // Extrapolations-Beschriftung ist auf Englisch länger als auf Deutsch, und
            // eine feste Zahl wäre nur für eine Sprache richtig. AutoSize hat die Breite
            // bereits ermittelt, weil beide Kästchen zu diesem Zeitpunkt Kinder des
            // Formulars sind.
            checkBox_BoosterLesepunkt.Location =
                new Point(checkBox_Extrapolation.Right + 24, fussOben + 6);
            checkBox_BoosterLesepunkt.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            // D2 (28.08.2026): Die hier ausgerechnete Knopfzeile war die Vorlage für die
            // Fußzeilen-Norm — Reihenfolge, Verankerung und der 10-px-Abstand stammen aus
            // dieser Methode. Sie ruft die Norm jetzt selbst auf, damit es genau EINE
            // Stelle gibt, an der Knopfgröße und Randabstand stehen; der Dialog wechselt
            // damit von 103×30 / Rand 19 auf die Norm 110×30 / Rand 12.
            FusszeilenNorm.Anwenden(this, btn_OK, btn_Speichern);

            lblStatus.Location = new Point(KARTEN_RAND, lblStatus.Top);
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        }

        // --- Kaskadenreihenfolge (Persistenz unverändert) -----------------------------

        /// <summary>
        /// Liest die Kaskade so, wie <c>btn_Speichern_Click</c> sie schreibt: vier
        /// Plätze, je Platz der DB-Wert des angehakten Auswahlfelds, sonst leer.
        ///
        /// <b>Warum über die unsichtbaren Steuerelemente und nicht über eine eigene
        /// Liste.</b> Die Zuordnung „Platz → <c>Tab_Einstellungen.Tool_n</c>" existiert
        /// genau einmal, nämlich in <c>btn_Speichern_Click</c>. Eine zweite Liste
        /// daneben wäre eine zweite Wahrheit über die Kaskadenposition — und die liest
        /// <see cref="Ladeordnung.Kaskadenpositionen"/> als Sortierkriterium der
        /// Ladereihenfolge (Konzept 3.4). Die Karten sind deshalb eine ANSICHT auf die
        /// vier Auswahlfelder, kein Ersatz für sie.
        /// </summary>
        private List<string> KaskadeLesen()
        {
            ComboBox[] felder = { comboBox1, comboBox2, comboBox3, comboBox4 };
            CheckBox[] haken = { checkBox1, checkBox2, checkBox3, checkBox4 };

            List<string> plaetze = new List<string>();
            for (int i = 0; i < felder.Length; i++)
                plaetze.Add(haken[i].Checked ? GetDbValue(felder[i]) : "");

            return plaetze;
        }

        /// <summary>Schreibt die vier Plätze zurück und baut die Anzeige neu auf.</summary>
        private void KaskadeSchreiben(List<string> plaetze)
        {
            ComboBox[] felder = { comboBox1, comboBox2, comboBox3, comboBox4 };
            CheckBox[] haken = { checkBox1, checkBox2, checkBox3, checkBox4 };

            _kaskadeSetzen = true;
            try
            {
                for (int i = 0; i < felder.Length; i++)
                {
                    if (string.IsNullOrEmpty(plaetze[i]))
                    {
                        felder[i].SelectedIndex = -1;
                        haken[i].Checked = false;
                    }
                    else
                    {
                        felder[i].SelectedValue = plaetze[i];
                        haken[i].Checked = true;
                    }
                }
            }
            finally { _kaskadeSetzen = false; }

            // Einmal am Ende statt bei jedem Zwischenschritt.
            AddErzeuger();
        }

        /// <summary>
        /// Verschiebt einen Erzeuger in der Kaskade um einen Rang
        /// (<paramref name="richtung"/> −1 = nach vorn, +1 = nach hinten).
        ///
        /// <b>Getauscht werden PLATZINHALTE, verdichtet wird nicht.</b>
        /// <see cref="Ladeordnung.Kaskadenpositionen"/> liest die SPALTENNUMMER
        /// <c>Tool_1..4</c> als Kaskadenposition und benutzt sie als zweites
        /// Sortierkriterium der Ladereihenfolge (Konzept 3.4). Würde beim Verschieben
        /// eine Lücke geschlossen — etwa Tool_1 leer, Tool_2 belegt —, änderten sich
        /// Positionen, die niemand angefasst hat. Der Tausch lässt jeden unbeteiligten
        /// Platz stehen und erzeugt damit genau die Belegung, die auch die alte
        /// ComboBox-Bedienung erzeugt hätte.
        /// </summary>
        private void KaskadeVerschieben(string dbWert, int richtung)
        {
            if (string.IsNullOrEmpty(dbWert)) return;

            List<string> plaetze = KaskadeLesen();

            List<int> belegt = new List<int>();
            for (int i = 0; i < plaetze.Count; i++)
                if (!string.IsNullOrEmpty(plaetze[i])) belegt.Add(i);

            int rang = -1;
            for (int i = 0; i < belegt.Count; i++)
                if (string.Equals(plaetze[belegt[i]], dbWert, StringComparison.Ordinal))
                {
                    rang = i;
                    break;
                }

            int ziel = rang + richtung;
            if (rang < 0 || ziel < 0 || ziel >= belegt.Count) return;

            string merker = plaetze[belegt[rang]];
            plaetze[belegt[rang]] = plaetze[belegt[ziel]];
            plaetze[belegt[ziel]] = merker;

            KaskadeSchreiben(plaetze);
        }

        /// <summary>Die belegten Kaskadenplätze in ihrer Reihenfolge (DB-Werte).</summary>
        private List<string> KaskadeBelegt()
        {
            List<string> belegt = new List<string>();
            foreach (string wert in KaskadeLesen())
                if (!string.IsNullOrEmpty(wert) && !belegt.Contains(wert)) belegt.Add(wert);
            return belegt;
        }

        /// <summary>
        /// Nimmt einen Wärmeerzeuger in die Simulation auf — das „+ aufnehmen" der
        /// verfügbaren Karte.
        ///
        /// Entspricht im Bestand: einen freien Auswahlplatz auf diesen Erzeuger stellen
        /// und seine Checkbox anhaken. Genommen wird der erste freie Platz HINTER dem
        /// letzten belegten; damit erscheint die Karte am Ende der Kaskade, so wie es die
        /// Bedienung erwarten lässt. Erst wenn dort keiner frei ist, wird eine Lücke
        /// weiter vorn gefüllt — vier Plätze für vier Erzeugertypen, es bleibt also immer
        /// einer übrig.
        /// </summary>
        private void KaskadeAufnehmen(string dbWert)
        {
            if (string.IsNullOrEmpty(dbWert)) return;

            List<string> plaetze = KaskadeLesen();
            if (plaetze.Contains(dbWert)) return;   // schon aufgenommen

            int letzterBelegt = -1;
            for (int i = 0; i < plaetze.Count; i++)
                if (!string.IsNullOrEmpty(plaetze[i])) letzterBelegt = i;

            int ziel = -1;
            for (int i = letzterBelegt + 1; i < plaetze.Count; i++)
                if (string.IsNullOrEmpty(plaetze[i])) { ziel = i; break; }

            if (ziel < 0)
                for (int i = 0; i < plaetze.Count; i++)
                    if (string.IsNullOrEmpty(plaetze[i])) { ziel = i; break; }

            if (ziel < 0) return;   // alle vier Plätze belegt

            plaetze[ziel] = dbWert;
            KaskadeSchreiben(plaetze);
        }

        /// <summary>
        /// Nimmt einen Wärmeerzeuger aus der Simulation — das „×" der aufgenommenen
        /// Karte. Entspricht im Bestand dem Abhaken der Checkbox: der Platz wird leer,
        /// alle übrigen bleiben, wo sie sind (keine Verdichtung, Begründung in
        /// <see cref="KaskadeVerschieben"/>).
        /// </summary>
        private void KaskadeEntfernen(string dbWert)
        {
            if (string.IsNullOrEmpty(dbWert)) return;

            List<string> plaetze = KaskadeLesen();
            bool getroffen = false;
            for (int i = 0; i < plaetze.Count; i++)
                if (string.Equals(plaetze[i], dbWert, StringComparison.Ordinal))
                {
                    plaetze[i] = "";
                    getroffen = true;
                }

            if (getroffen) KaskadeSchreiben(plaetze);
        }

        /// <summary>
        /// Setzt den Auswahlplatz der Strom- bzw. Speicherseite (<c>Tool_5</c>,
        /// <c>Tool_6</c>). Leerer <paramref name="dbWert"/> = nicht aufnehmen.
        ///
        /// Bedient dieselben zwei Steuerelemente wie der Bestand und in derselben
        /// Reihenfolge (erst das Auswahlfeld, dann der Haken), damit
        /// <c>btn_Speichern_Click</c> unverändert dasselbe liest.
        /// </summary>
        private void StromAuswahlSetzen(ComboBox feld, CheckBox haken, string dbWert)
        {
            _kaskadeSetzen = true;
            try
            {
                if (string.IsNullOrEmpty(dbWert))
                {
                    feld.SelectedIndex = -1;
                    feld.Text = "";
                    haken.Checked = false;
                }
                else
                {
                    feld.SelectedValue = dbWert;
                    haken.Checked = true;
                }
            }
            finally { _kaskadeSetzen = false; }

            AktualisiereErzeugerUebersicht();
        }

        // --- Aufbau der Erzeugerspalte ------------------------------------------------

        /// <summary>
        /// Baut beide Kartenspalten neu auf.
        ///
        /// Der Name ist der der abgelösten ListView-Methode geblieben: Er steht an neun
        /// Aufrufstellen in <c>Form_Simulation_Config.cs</c>, <c>…Karten.cs</c> und
        /// <c>…Uebersicht.cs</c> (Befund W10‑B40 — der Kommentar sprach von acht;
        /// gezählt sind neun), und jede davon meint dasselbe — „die Anzeige stimmt
        /// nicht mehr, bau sie neu". Die Speicherkarten hängen an denselben Daten
        /// (Ladereihenfolge, Senken) und müssen deshalb mitlaufen.
        /// </summary>
        private void AktualisiereErzeugerUebersicht()
        {
            AktualisiereErzeugerKarten();
            AktualisiereSpeicherKarten();

            // D4: dieselben Daten, zweite Ansicht. Das Schema rechnet nur, wenn es auch
            // sichtbar ist (siehe AktualisiereSchema); die Hervorhebung dagegen ist nach
            // jedem Neuaufbau der Karten nachzuziehen - die alten Karten sind entsorgt.
            AktualisiereSchema();
            AuswahlInKartenZeigen();
        }

        /// <summary>
        /// Baut die Erzeugerspalte neu auf: drei Gruppen wie in der abgelösten Rubrik
        /// „Erzeuger &amp;&amp; Speicher" — Wärmeerzeuger, Stromerzeuger, Energiespeicher.
        ///
        /// <b>Die Karten sind hier NICHT nur Anzeige.</b> Die vier Wärmeerzeuger-Combos
        /// mit ihren Checkboxen und die beiden Strom-Auswahlfelder trafen zwei
        /// Entscheidungen zugleich: WELCHE der im Projekt vorhandenen Technologien
        /// mitgerechnet wird (<c>Tab_Einstellungen.Tool_1..6</c>) und in welcher
        /// REIHENFOLGE (Platz 1…4). Beides bildet die Kartenansicht ab:
        ///
        /// <list type="bullet">
        ///   <item><description><b>Aufgenommen</b> — die Komponente steht in Tool_1..6.
        ///     Bei den Wärmeerzeugern trägt sie ihren Kaskadenrang und ▲▼; × nimmt sie
        ///     wieder heraus.</description></item>
        ///   <item><description><b>Verfügbar</b> — die Komponente steht im Katalog der
        ///     Auswahlfelder, ist aber nicht aufgenommen (der leere Auswahlplatz des
        ///     Bestands). Sie erscheint gestrichelt und ausgegraut mit „+ aufnehmen".
        ///     </description></item>
        /// </list>
        ///
        /// Angeboten werden GENAU die Einträge, die die Auswahlfelder boten
        /// (<see cref="ErzeugerKatalog.WAERMEERZEUGER"/>, <c>STROMERZEUGER</c>,
        /// <c>ENERGIESPEICHER</c>) — auch dann, wenn im Projekt keine passende Anlage
        /// liegt. Das war im Bestand ebenso möglich; die Karte sagt es dann im Chip
        /// „keine Anlage im Projekt", statt die Wahl stillschweigend zu verstecken.
        /// </summary>
        private void AktualisiereErzeugerKarten()
        {
            if (flow_Erzeuger == null) return;

            flow_Erzeuger.SuspendLayout();
            try
            {
                SpalteLeeren(flow_Erzeuger, null);
                _verfuegbarVersteckt = 0;

                // PAKET S2: die Warnbefunde des Projekts EINMAL je Auffrischung — nicht
                // je Karte. Der Katalog liest Anlagen, Senkenlisten und Speicherzeilen;
                // ein Aufruf je Karte wäre bei fünf Erzeugern fünfmal dieselbe Auskunft.
                WarnbefundeSammeln();

                // PAKET B1 (F9): dieselbe Bauart für die Booster-Anzeigeregel.
                BoosterAnlagenSammeln();

                WaermeerzeugerGruppe();
                StromGruppe(label2.Text, comboBox5, checkBox5,
                            ErzeugerKatalog.STROMERZEUGER, WizardItemClass.PV_TYP);
                StromGruppe(label3.Text, comboBox6, checkBox6,
                            ErzeugerKatalog.ENERGIESPEICHER, WizardItemClass.SP_TYP);

                // ABNAHMEBEFUND 1: der Weg zurück zu den ausgeblendeten Platzhaltern.
                VerfuegbarSchalterAnfuegen();

                // „+ Anlage hinzufügen …": Ein Einstieg ohne Wizard-Kontext gibt es
                // nicht — Anlagen entstehen im Assistenten bzw. über die Projektseite
                // (WizardParent). Statt eines Knopfes, der nichts Sinnvolles öffnen
                // kann, steht hier der Weg als Text. Das AUFNEHMEN in die Simulation
                // passiert dagegen auf dieser Seite (siehe Klassenkopf).
                flow_Erzeuger.Controls.Add(Hinweiszeile(MyResource.Resource.SIM_KARTE_ANLAGE_HINZU));
            }
            finally
            {
                flow_Erzeuger.ResumeLayout();
                KartenBreiteAnpassen(flow_Erzeuger);
            }
        }

        /// <summary>
        /// Gruppe „Wärmeerzeuger": erst die aufgenommenen in Kaskadenreihenfolge, dann
        /// die verfügbaren.
        ///
        /// Je AUFGENOMMENEM Erzeuger entsteht eine Karte pro Anlage im Projekt (zwei
        /// Wärmepumpen = zwei Karten mit demselben Rang). ▲▼ und × stehen nur auf der
        /// ERSTEN Karte des Erzeugers: Reihenfolge und Teilnahme gelten dem Erzeugertyp,
        /// nicht der einzelnen Anlage — genau so, wie ein Auswahlfeld des Bestands für
        /// alle Anlagen seines Typs zugleich entschied.
        ///
        /// <b>ANWENDERENTSCHEID F2 (30.08.2026) — „Modul n von m".</b> Genau diese
        /// Absicht war an der Karte nicht ablesbar: Zwei BHKW trugen beide „1", ohne dass
        /// irgendetwas den Rang als Typrang auswies — ein Anwender hielt sein zweites
        /// BHKW daraufhin für gar nicht angezeigt. Der Rang bleibt, wie er ist (er IST
        /// die Kaskadenstufe des Typs); dazu bekommt jede Karte bei MEHREREN Anlagen
        /// desselben Typs den Ausweis <see cref="ModulChip"/> — <c>n</c> in der
        /// Anzeigereihenfolge dieser Gruppe, <c>m</c> = Anlagen des Typs. Bei m = 1
        /// entsteht kein Ausweis; dort bleibt das Bestandsbild unverändert.
        /// </summary>
        private void WaermeerzeugerGruppe()
        {
            flow_Erzeuger.Controls.Add(Gruppenkopf(label1.Text));

            List<string> kaskade = KaskadeBelegt();

            for (int i = 0; i < kaskade.Count; i++)
            {
                string dbWert = kaskade[i];
                string erzeuger = ErzeugerKatalog.Anzeige(dbWert);
                List<AnlagenInfo> anlagen = AnlagenImProjekt(dbWert);
                string rang = (i + 1).ToString();

                if (anlagen.Count == 0)
                {
                    // Aufgenommen, aber im Projekt gibt es keine Anlage dazu. Die alte
                    // Übersicht zeigte dafür eine Zeile mit "-" in der Anlagenspalte;
                    // die Karte sagt es im Klartext.
                    ErzeugerKarte leer = ErzeugerKarteAnlegen(dbWert, null);
                    leer.Setzen(new ErzeugerKarte.Aufbau
                    {
                        Rang = rang,
                        Titel = erzeuger,
                        Chips = new List<ErzeugerKarte.ChipDaten>
                        {
                            new ErzeugerKarte.ChipDaten
                            {
                                Text = MyResource.Resource.SIM_KARTE_OHNE_ANLAGE,
                                Stil = ErzeugerKarte.ChipStil.Flaeche
                            }
                        },
                        Reihenfolge = true,
                        AufMoeglich = i > 0,
                        AbMoeglich = i < kaskade.Count - 1,
                        Umschaltbar = true
                    });
                    continue;
                }

                for (int a = 0; a < anlagen.Count; a++)
                {
                    AnlagenInfo info = anlagen[a];
                    ErzeugerKarte karte = ErzeugerKarteAnlegen(dbWert, info);
                    karte.Setzen(new ErzeugerKarte.Aufbau
                    {
                        Rang = rang,
                        Titel = string.Format(MyResource.Resource.SIM_KARTE_TITEL,
                                              erzeuger, info.Bezeichner),
                        // F2: n = Platz in der Anzeigereihenfolge dieser Gruppe,
                        // m = Anlagen des Typs. Beides steht hier bereits fest.
                        Chips = ErzeugerChips(info, a + 1, anlagen.Count),
                        Reihenfolge = a == 0,
                        AufMoeglich = i > 0,
                        AbMoeglich = i < kaskade.Count - 1,
                        Umschaltbar = a == 0,
                        Editierbar = true
                    });
                }
            }

            foreach (string dbWert in ErzeugerKatalog.WAERMEERZEUGER)
            {
                if (kaskade.Contains(dbWert)) continue;
                VerfuegbarKarte(dbWert, TypZuAnlagentyp(dbWert),
                                delegate { KaskadeAufnehmen(dbWert); });
            }

            // ABNAHMEBEFUND 1: Seit die Platzhalter ausgeblendet sind, kann diese Gruppe
            // LEER sein — vorher standen dort immer vier gestrichelte Karten. Eine
            // Überschrift ohne alles darunter sagt nichts; der Satz sagt, dass die Gruppe
            // stimmt und nur nichts gewählt ist. Der Ressourcenschlüssel liegt seit D2
            // ungenutzt bereit und ist für genau diesen Fall geschrieben.
            if (kaskade.Count == 0)
                flow_Erzeuger.Controls.Add(
                    Hinweiszeile(MyResource.Resource.SIM_KARTE_KEINE_ERZEUGER));
        }

        /// <summary>
        /// Gruppe „Stromerzeuger" bzw. „Energiespeicher" — je ein Auswahlplatz
        /// (<c>Tool_5</c> / <c>Tool_6</c>) mit genau einem Katalogeintrag.
        ///
        /// Anders als die Wärmeseite haben sie keine Kaskadenposition (die Kaskade ist
        /// die Wärmeseite) und keinen Senkendialog; ▲▼ und ✎ entfallen deshalb. Die
        /// AUSWAHL gibt es hier genauso — sie war im Bestand die Checkbox neben dem
        /// jeweiligen Auswahlfeld.
        /// </summary>
        private void StromGruppe(string ueberschrift, ComboBox feld, CheckBox haken,
                                 string[] katalog, int idType)
        {
            flow_Erzeuger.Controls.Add(Gruppenkopf(ueberschrift));

            string gewaehlt = haken.Checked ? GetDbValue(feld) : "";

            foreach (string dbWert in katalog)
            {
                if (string.Equals(dbWert, gewaehlt, StringComparison.Ordinal))
                {
                    List<string> namen = AnlagenNamen(idType);
                    List<ErzeugerKarte.ChipDaten> chips = new List<ErzeugerKarte.ChipDaten>();
                    if (namen.Count == 0)
                        chips.Add(new ErzeugerKarte.ChipDaten
                        {
                            Text = MyResource.Resource.SIM_KARTE_OHNE_ANLAGE,
                            Stil = ErzeugerKarte.ChipStil.Flaeche
                        });

                    ErzeugerKarte karte = new ErzeugerKarte();
                    flow_Erzeuger.Controls.Add(karte);

                    ComboBox f = feld;
                    CheckBox h = haken;
                    karte.Entfernen += delegate { StromAuswahlSetzen(f, h, ""); };

                    // ABNAHMEBEFUND 3: Die beiden Stromkarten zeigten bis hierher nur den
                    // Anlagennamen. Die Gerätedaten stehen jetzt in einem Aufklappbereich
                    // - lesend, wie die ganze Seite; gepflegt wird weiter im Katalog bzw.
                    // auf der Parameterseite der Simulation.
                    //
                    // Der ID_Type im Tag ist die Gruppenkennung, an der die
                    // "höchstens eine offen"-Regel die Karten unterscheidet. Die
                    // Wärmekarten tragen dort ihre AnlagenInfo und bleiben deshalb
                    // unberührt.
                    karte.Tag = idType;
                    karte.Umschalten += StromKarte_Umschalten;

                    karte.Setzen(new ErzeugerKarte.Aufbau
                    {
                        Titel = namen.Count > 0
                            ? string.Format(MyResource.Resource.SIM_KARTE_TITEL,
                                            ErzeugerKatalog.Anzeige(dbWert),
                                            string.Join(" · ", namen.ToArray()))
                            : ErzeugerKatalog.Anzeige(dbWert),
                        Chips = chips,
                        Detailchips = StromDetailchips(idType),
                        Aufgeklappt = _offeneStromgruppe == idType,
                        Umschaltbar = true
                    });
                    continue;
                }

                ComboBox f2 = feld;
                CheckBox h2 = haken;
                string wert = dbWert;
                VerfuegbarKarte(dbWert, idType, delegate { StromAuswahlSetzen(f2, h2, wert); });
            }
        }

        // --- Aufklappbare Gerätedaten der Strom- und Speicherkarte (Abnahmebefund 3) ---
        //
        // AUSGANGSLAGE. Die beiden Gruppen „Stromerzeuger" und „Energiespeicher" trugen
        // nur den Anlagennamen. Was die Simulation daraus macht - Kapazität, Leistung,
        // Wirkungsgrad, Betriebsart, SoC-Band bzw. Modul, Anzahl, Ausrichtung - stand
        // nirgends auf dieser Seite, obwohl genau sie die Konfiguration zeigen soll.
        //
        // NUR LESEND, wie die ganze Kartenansicht (Konzept 3: „Lesefläche, keine
        // Parallel-Editierwelt"): Gepflegt werden die Gerätedaten im Speicher- bzw.
        // PV-Katalog, die Betriebsführung auf der Parameterseite der Simulation.
        //
        // DATENZUGRIFF über die vorhandenen Controller (StromspeicherCtrl,
        // StromspeicherVarianteCtrl, PhotovoltaikCtrl, WErzeugerCtrl) - kein RecordSet in
        // neuem Code (CLAUDE.md).

        /// <summary>
        /// Schaltet den Detailbereich einer Stromkarte auf oder zu — Zeile für Zeile
        /// derselbe Ablauf wie <see cref="SpeicherKarte_Umschalten"/>.
        /// </summary>
        /// <remarks>
        /// Umgeschaltet wird an den VORHANDENEN Karten, die Spalte wird NICHT neu
        /// aufgebaut. Das ist nicht nur schneller: Ein Neuaufbau entsorgt genau die
        /// Karte, aus deren Klick-Ereignis dieser Aufruf kommt (siehe
        /// <c>ErzeugerKarte.Melden</c>).
        /// </remarks>
        private void StromKarte_Umschalten(object sender, EventArgs e)
        {
            ErzeugerKarte karte = sender as ErzeugerKarte;
            if (karte == null || !(karte.Tag is int)) return;

            _offeneStromgruppe = karte.Aufgeklappt ? 0 : (int)karte.Tag;

            // Es ist immer höchstens eine Karte offen (Konzept 3a).
            flow_Erzeuger.SuspendLayout();
            try
            {
                foreach (Control c in flow_Erzeuger.Controls)
                {
                    ErzeugerKarte k = c as ErzeugerKarte;
                    if (k != null && k.Tag is int)
                        k.Aufgeklappt = (int)k.Tag == _offeneStromgruppe;
                }
            }
            finally
            {
                flow_Erzeuger.ResumeLayout();
                KartenBreiteAnpassen(flow_Erzeuger);
            }
        }

        /// <summary>
        /// Die Detailchips der Strom- bzw. Speicherkarte. Leere Liste = kein
        /// Aufklappbereich (die Karte sieht dann aus wie bisher).
        /// </summary>
        private List<ErzeugerKarte.ChipDaten> StromDetailchips(int idType)
        {
            if (idType == WizardItemClass.SP_TYP) return SpeicherDetailchips();
            if (idType == WizardItemClass.PV_TYP) return PvDetailchips();
            return new List<ErzeugerKarte.ChipDaten>();
        }

        /// <summary>
        /// Gerätedaten aller Speicheranlagen des Projekts plus die Betriebsführung der
        /// AKTIVEN Variante (Fachkonzept 5.1/7.3) — das ist die Einheit, die die
        /// Gesamtsimulation rechnet.
        /// </summary>
        private List<ErzeugerKarte.ChipDaten> SpeicherDetailchips()
        {
            List<ErzeugerKarte.ChipDaten> chips = new List<ErzeugerKarte.ChipDaten>();
            if (m_ID_Projekt <= 0) return chips;

            System.Globalization.CultureInfo kultur = System.Globalization.CultureInfo.CurrentCulture;

            WErzeugerCtrl anlagen = new WErzeugerCtrl();
            anlagen.ReadAllFilter("ID_Projekt=" + m_ID_Projekt +
                                  " and ID_Type=" + WizardItemClass.SP_TYP);

            List<int> gezeigt = new List<int>();
            for (int i = 0; i < anlagen.rows; i++)
            {
                int idGeraet = anlagen.items[i].ID_SP;
                if (idGeraet <= 0 || gezeigt.Contains(idGeraet)) continue;
                gezeigt.Add(idGeraet);

                StromspeicherCtrl geraet = new StromspeicherCtrl();
                geraet.ReadSingle(idGeraet);
                if (geraet.m_ID <= 0) continue;

                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_KAPAZITAET,
                                          geraet.m_Energie.ToString("N2", kultur)),
                     ErzeugerKarte.ChipStil.Senke);
                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_LEISTUNG,
                                          geraet.m_Leistung.ToString("N2", kultur)));

                double etaRt = geraet.m_WirkungsgradRT > 0.0
                    ? geraet.m_WirkungsgradRT : StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE;
                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_WIRKUNGSGRAD,
                                          etaRt.ToString("N2", kultur)));

                if (!string.IsNullOrEmpty(geraet.m_szTyp))
                    Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_TYP, geraet.m_szTyp),
                         ErzeugerKarte.ChipStil.Flaeche);

                if (geraet.m_ZyklenZugesichert > 0)
                    Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_ZYKLEN,
                                              geraet.m_ZyklenZugesichert.ToString("N0", kultur)));
            }

            if (chips.Count == 0)
            {
                Chip(chips, MyResource.Resource.SIM_KARTE_OHNE_GERAET, ErzeugerKarte.ChipStil.Flaeche);
                return chips;
            }

            SpeicherVariantenchips(chips, kultur);
            return chips;
        }

        /// <summary>
        /// Die Betriebsführung der aktiven Variante als Chips; ohne aktive Variante ein
        /// Hinweis. Genau diese Unterscheidung entscheidet auch im Rechenweg, ob die
        /// Simulation eine Variante rechnet oder auf die Aggregation zurückfällt
        /// (<c>StromspeicherSimCtrl.LeseParameter</c>).
        /// </summary>
        private void SpeicherVariantenchips(List<ErzeugerKarte.ChipDaten> chips,
                                            System.Globalization.CultureInfo kultur)
        {
            StromspeicherVarianteModel variante = null;
            try
            {
                variante = new StromspeicherVarianteCtrl().ReadAktiveVariante(m_ID_Projekt);
            }
            catch (Exception ex)
            {
                // Die Karte ist Beiwerk - sie darf den Dialog nicht kippen, wenn die
                // Variantentabelle (Migrationsschritt 11b) noch fehlt.
                Console.WriteLine("Die aktive Speichervariante konnte nicht gelesen werden: " + ex.Message);
            }

            if (variante == null)
            {
                Chip(chips, MyResource.Resource.SIM_KARTE_SP_OHNE_VARIANTE,
                     ErzeugerKarte.ChipStil.Warnung);
                return;
            }

            Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_VARIANTE,
                                      BetriebsartAnzeige(variante.Betriebsart)),
                 ErzeugerKarte.ChipStil.Quelle);
            Chip(chips, BerechnungsartAnzeige(variante.Berechnungsart),
                 ErzeugerKarte.ChipStil.Quelle);
            Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_BAND,
                                      variante.SoC_Min_Prozent.ToString("N0", kultur),
                                      variante.SoC_Max_Prozent.ToString("N0", kultur)));
            Chip(chips, variante.Netzentladung
                     ? MyResource.Resource.SIM_KARTE_SP_NETZENTLADUNG_AN
                     : MyResource.Resource.SIM_KARTE_SP_NETZENTLADUNG_AUS,
                 ErzeugerKarte.ChipStil.Flaeche);
        }

        /// <summary>
        /// Gerätedaten aller PV-Anlagen des Projekts: Modul, Anzahl, Ausrichtung und die
        /// rechnerische Spitzenleistung.
        /// </summary>
        /// <remarks>
        /// <c>Tab_Energieanlagen.PV_Leistung</c> ist trotz seines Namens die
        /// MODULANZAHL — so liest es <c>SimulationPV.Berechnung</c> (Fläche =
        /// Breite · Länge · PV_Leistung). Die kWp-Angabe entsteht daraus mit der
        /// Modulleistung <c>Tab_PV.Leistung</c> [W] und ist deshalb als „rechnerisch"
        /// gekennzeichnet: Sie steht nirgends gepflegt in der Datenbank.
        /// </remarks>
        private List<ErzeugerKarte.ChipDaten> PvDetailchips()
        {
            List<ErzeugerKarte.ChipDaten> chips = new List<ErzeugerKarte.ChipDaten>();
            if (m_ID_Projekt <= 0) return chips;

            System.Globalization.CultureInfo kultur = System.Globalization.CultureInfo.CurrentCulture;

            WErzeugerCtrl anlagen = new WErzeugerCtrl();
            anlagen.ReadAllFilter("ID_Projekt=" + m_ID_Projekt +
                                  " and ID_Type=" + WizardItemClass.PV_TYP);

            for (int i = 0; i < anlagen.rows; i++)
            {
                WErzeugerModel anlage = anlagen.items[i];

                PhotovoltaikCtrl modul = new PhotovoltaikCtrl();
                if (anlage.ID_PV > 0) modul.ReadSingle(anlage.ID_PV);

                if (!string.IsNullOrEmpty(modul.m_szName))
                    Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_MODUL, modul.m_szName),
                         ErzeugerKarte.ChipStil.Flaeche);

                long anzahl = (long)anlage.PV_Leistung;
                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_ANZAHL,
                                          anzahl.ToString("N0", kultur)),
                     ErzeugerKarte.ChipStil.Quelle);

                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_AUSRICHTUNG,
                                          anlage.m_Neigung.ToString(kultur),
                                          anlage.m_Azimut.ToString(kultur)));

                if (modul.m_Leistung > 0.0 && anzahl > 0)
                    Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_KWP,
                                              (modul.m_Leistung * anzahl / 1000.0).ToString("N2", kultur)));
            }

            if (chips.Count == 0)
                Chip(chips, MyResource.Resource.SIM_KARTE_OHNE_GERAET, ErzeugerKarte.ChipStil.Flaeche);

            return chips;
        }

        /// <summary>Anzeigetext einer Betriebsart (Persistenzwert → Sprachschicht).</summary>
        private static string BetriebsartAnzeige(string dbWert)
        {
            return dbWert == DbWerte.SP_BETRIEBSART_GRAUSTROM
                ? MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRAUSTROM
                : MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRUENSTROM;
        }

        /// <summary>Anzeigetext einer Berechnungsart (Persistenzwert → Sprachschicht).</summary>
        private static string BerechnungsartAnzeige(string dbWert)
        {
            if (dbWert == DbWerte.SP_BERECHNUNG_NACHTNUTZUNG)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG;
            if (dbWert == DbWerte.SP_BERECHNUNG_ARBITRAGE)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE;
            return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG;
        }

        /// <summary>Hängt einen Detailchip an - leere Texte werden übergangen.</summary>
        private static void Chip(List<ErzeugerKarte.ChipDaten> chips, string text,
                                 ErzeugerKarte.ChipStil stil = ErzeugerKarte.ChipStil.Neutral)
        {
            if (string.IsNullOrEmpty(text)) return;
            chips.Add(new ErzeugerKarte.ChipDaten { Text = text, Stil = stil });
        }

        /// <summary>
        /// Eine gestrichelte Karte „im Katalog wählbar, nicht aufgenommen".
        ///
        /// ABNAHMEBEFUND 1: Standardmäßig entsteht sie GAR NICHT — die Spalte zeigt, was
        /// gerechnet wird. Gezählt wird trotzdem, denn die Zahl steht im Einblendeschalter
        /// am Spaltenende (<see cref="_verfuegbareZeigen"/>).
        /// </summary>
        private void VerfuegbarKarte(string dbWert, int idType, EventHandler aufnehmen)
        {
            if (!_verfuegbareZeigen)
            {
                _verfuegbarVersteckt++;
                return;
            }

            List<string> namen = idType > 0 ? AnlagenNamen(idType) : new List<string>();

            List<ErzeugerKarte.ChipDaten> chips = new List<ErzeugerKarte.ChipDaten>();
            chips.Add(new ErzeugerKarte.ChipDaten
            {
                Text = MyResource.Resource.SIM_KARTE_VERFUEGBAR,
                Stil = ErzeugerKarte.ChipStil.Flaeche
            });
            if (namen.Count == 0)
                chips.Add(new ErzeugerKarte.ChipDaten
                {
                    Text = MyResource.Resource.SIM_KARTE_OHNE_ANLAGE,
                    Stil = ErzeugerKarte.ChipStil.Flaeche
                });

            ErzeugerKarte karte = new ErzeugerKarte();
            flow_Erzeuger.Controls.Add(karte);
            karte.Aufnehmen += aufnehmen;

            karte.Setzen(new ErzeugerKarte.Aufbau
            {
                Titel = namen.Count > 0
                    ? string.Format(MyResource.Resource.SIM_KARTE_TITEL,
                                    ErzeugerKatalog.Anzeige(dbWert),
                                    string.Join(" · ", namen.ToArray()))
                    : ErzeugerKatalog.Anzeige(dbWert),
                Chips = chips,
                Zustand = ErzeugerKarte.Kartenzustand.Verfuegbar,
                Umschaltbar = true
            });
        }

        /// <summary>
        /// ABNAHMEBEFUND 1 — Textschalter am Ende der Erzeugerspalte, der die nicht
        /// gewählten Komponenten ein- und wieder ausblendet.
        ///
        /// Er steht am SPALTENENDE und nicht je Gruppe: Die Zahl der Platzhalter ist
        /// klein (höchstens vier Wärmeerzeuger plus zwei Stromplätze), drei Schalter
        /// wären mehr Bedienelement als Inhalt. Sein Stil ist der der übrigen
        /// Spaltenhinweise (<see cref="Hinweiszeile"/>) in der Quellfarbe, mit der auch
        /// das „+ aufnehmen" der Karten geschrieben ist.
        ///
        /// Gibt es nichts zu verstecken und ist nichts versteckt, erscheint er nicht —
        /// eine vollständig aufgenommene Konfiguration bekommt keine leere Zeile.
        /// </summary>
        private void VerfuegbarSchalterAnfuegen()
        {
            if (!_verfuegbareZeigen && _verfuegbarVersteckt == 0) return;

            Label l = Hinweiszeile(_verfuegbareZeigen
                ? MyResource.Resource.SIM_KARTE_VERFUEGBAR_AUSBLENDEN
                : string.Format(MyResource.Resource.SIM_KARTE_VERFUEGBAR_EINBLENDEN,
                                _verfuegbarVersteckt));
            l.ForeColor = KartenStil.QUELLE_TEXT;
            l.Cursor = Cursors.Hand;
            l.Margin = new Padding(0, 8, 0, 4);

            l.Click += delegate
            {
                _verfuegbareZeigen = !_verfuegbareZeigen;

                // VERZÖGERT, aus demselben Grund wie ErzeugerKarte.Melden: Der Neuaufbau
                // entsorgt über SpalteLeeren genau dieses Label, aus dessen Klick-Ereignis
                // der Aufruf kommt.
                //
                // Nur die ERZEUGERSPALTE: Speicherkarten und Schema hängen nicht an dieser
                // Sichtvorliebe, und ihr Neuaufbau kostet bei einem Projekt mit vielen
                // Puffern spürbar (siehe SpeicherKarteDaten). Die Hervorhebung ist danach
                // nachzuziehen — die alten Karten sind entsorgt.
                BeginInvoke((MethodInvoker)delegate
                {
                    AktualisiereErzeugerKarten();
                    AuswahlInKartenZeigen();
                });
            };

            flow_Erzeuger.Controls.Add(l);
        }

        /// <summary>Fette Gruppenüberschrift wie in der abgelösten Rubrik.</summary>
        private Label Gruppenkopf(string text)
        {
            Label l = Hinweiszeile((text ?? "").TrimEnd(' ', ':'));
            KartenStil.Schnitt(l, FontStyle.Bold);
            l.ForeColor = KartenStil.TEXT;
            l.Margin = new Padding(0, flow_Erzeuger.Controls.Count == 0 ? 0 : 10, 0, 4);
            return l;
        }

        /// <summary><c>Tab_Energieanlagen.ID_Type</c> zu einem Wärmeerzeuger-DB-Wert; 0 = unbekannt.</summary>
        private static int TypZuAnlagentyp(string dbWert)
        {
            switch (dbWert)
            {
                case DbWerte.ERZEUGER_WAERMEPUMPE: return WizardItemClass.WP_TYP;
                case DbWerte.ERZEUGER_HEIZKESSEL: return WizardItemClass.KESSEL_TYP;
                case DbWerte.ERZEUGER_BHKW: return WizardItemClass.BHKW_TYP;
                case DbWerte.ERZEUGER_SOLARTHERMIE: return WizardItemClass.SOLAR_TYP;
                default: return 0;
            }
        }

        /// <summary>
        /// Bezeichner aller Projektanlagen eines Typs, OHNE Wiederholungen.
        ///
        /// Entdoppelt wird bewusst: Im Bestand stehen regelmäßig mehrere Zeilen
        /// desselben Moduls (Projekt 1011: vier Batteriezeilen, davon drei namensgleich).
        /// Eine Kopfzeile „Stromspeicher · BYD B-Box HVM 11.0 · BYD B-Box HVM 11.0 ·
        /// BYD B-Box HVM 11.0 · Vaillant 10030745" sagt nichts, was die entdoppelte
        /// Fassung nicht auch sagt.
        /// </summary>
        private List<string> AnlagenNamen(int idType)
        {
            List<string> namen = new List<string>();
            if (m_ID_Projekt <= 0 || idType <= 0) return namen;

            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT Bezeichner FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt=" + m_ID_Projekt + " AND ID_Type=" + idType +
                // Ungepflegte Priorität ans ENDE (ANLAGENPRIO_UNGEPFLEGT), sonst stünde
                // eine frisch angelegte Anlage in der Kopfzeile vor der konfigurierten.
                " ORDER BY " + Ladeordnung.SqlAnlagenprio(null) + ", ID");
            if (dt == null) return namen;

            foreach (System.Data.DataRow r in dt.Rows)
            {
                if (r["Bezeichner"] == DBNull.Value) continue;
                string name = r["Bezeichner"].ToString();
                if (name.Length > 0 && !namen.Contains(name)) namen.Add(name);
            }
            return namen;
        }

        private ErzeugerKarte ErzeugerKarteAnlegen(string dbWert, AnlagenInfo info)
        {
            ErzeugerKarte karte = new ErzeugerKarte();
            karte.Tag = info;

            string wert = dbWert;
            karte.NachOben += delegate { KaskadeVerschieben(wert, -1); };
            karte.NachUnten += delegate { KaskadeVerschieben(wert, +1); };
            karte.Entfernen += delegate { KaskadeEntfernen(wert); };

            if (info != null)
            {
                AnlagenInfo anlage = info;

                // D4: Ein einfacher Klick wählt die Anlage aus - die Auswahl teilen sich
                // Karten- und Schema-Ansicht.
                karte.Ausgewaehlt += delegate { KarteAusgewaehlt(anlage.ID); };

                // Standard-Editor der Karte: der Senkendialog. Er ist der einzige, der
                // für JEDEN Erzeugertyp etwas zu sagen hat (Konzept 4.2) - Quelle, Modus
                // und WP-Priorität sind wärmepumpenspezifisch und hängen an ihren Chips.
                karte.Bearbeiten += delegate { WaermesenkeBearbeiten(anlage); };
                karte.ChipBearbeiten += delegate (ErzeugerKarte.ChipDaten chip)
                {
                    ChipEditorOeffnen(anlage, karte, chip);
                };
            }

            flow_Erzeuger.Controls.Add(karte);
            return karte;
        }

        /// <summary>
        /// Ersatz für den Doppelklick-Dispatcher der alten Übersicht
        /// (<c>listView_Uebersicht_MouseDoubleClick</c>): Statt über Spaltenindizes
        /// entscheidet das Ziel, das der Chip mitbringt. Die aufgerufenen Dialoge und
        /// ihre Vorbedingungen sind unverändert.
        /// </summary>
        private void ChipEditorOeffnen(AnlagenInfo info, ErzeugerKarte karte,
                                       ErzeugerKarte.ChipDaten chip)
        {
            switch (chip.Ziel)
            {
                case ErzeugerKarte.ChipZiel.Senke:
                case ErzeugerKarte.ChipZiel.Zweitsenke:
                    // Beide führen in denselben Dialog - Haupt- und Zweitsenke gehören
                    // fachlich zusammen (Konzept 4.2).
                    WaermesenkeBearbeiten(info);
                    break;

                case ErzeugerKarte.ChipZiel.Modus:
                    BetriebsmodusBearbeiten(info);
                    break;

                case ErzeugerKarte.ChipZiel.Prioritaet:
                    WpPrioritaetBearbeiten(info);
                    break;

                case ErzeugerKarte.ChipZiel.Quelle:
                    // Der Quellen-Inlineeditor braucht ein Rechteck in Bildschirmnähe,
                    // an dem er aufklappt. Früher war das die Zelle der ListView, jetzt
                    // die Karte selbst.
                    WaermequelleBearbeiten(info, KarteAlsZelle(karte));
                    break;
            }
        }

        /// <summary>
        /// Rechteck der Karte in Koordinaten, die
        /// <c>WaermequelleAuswahlAnzeigen</c> erwartet: Der Bestandscode rechnet mit
        /// <c>listView_Uebersicht.PointToScreen</c> auf Bildschirmkoordinaten um. Damit
        /// dieselbe Rechnung für eine Karte stimmt, wird hier der Versatz zwischen Karte
        /// und Liste vorweggenommen — die Liste gibt es nicht mehr, also wird direkt in
        /// Formularkoordinaten geliefert und der Umweg in
        /// <see cref="WaermequelleAuswahlAnzeigen"/> auf den neuen Bezug gestellt.
        /// </summary>
        private Rectangle KarteAlsZelle(ErzeugerKarte karte)
        {
            Point aufDemSchirm = karte.PointToScreen(new Point(0, karte.Height - 4));
            Point imFormular = PointToClient(aufDemSchirm);
            return new Rectangle(imFormular, new Size(Math.Min(karte.Width, 260), 24));
        }

        /// <summary>
        /// Die Chips einer Erzeugerkarte (Konzept Abschnitt 3, Mockup 4).
        ///
        /// <paramref name="modulNr"/> und <paramref name="modulAnzahl"/> sind die Stelle
        /// dieser Anlage unter den Anlagen ihres Erzeugertyps (Anwenderentscheid F2,
        /// siehe <see cref="ModulChip"/>); <c>modulAnzahl &lt; 2</c> heißt „einzige Anlage
        /// des Typs" und erzeugt keinen Ausweis.
        /// </summary>
        private List<ErzeugerKarte.ChipDaten> ErzeugerChips(AnlagenInfo info,
                                                            int modulNr, int modulAnzahl)
        {
            List<ErzeugerKarte.ChipDaten> chips = new List<ErzeugerKarte.ChipDaten>();

            ModulChip(chips, modulNr, modulAnzahl);
            QuellenChip(info, chips);
            BoosterChip(info, chips);
            SenkenChips(info, chips);
            TemperaturChip(info, chips);
            WarnChip(info, chips);

            if (info.IstWaermepumpe)
            {
                chips.Add(new ErzeugerKarte.ChipDaten
                {
                    Text = string.Format(MyResource.Resource.SIM_KARTE_WPPRIO,
                                         info.Prioritaet > 0 ? info.Prioritaet.ToString() : "–"),
                    Hinweis = MyResource.Resource.SIM_TIP_WPPRIO,
                    Ziel = ErzeugerKarte.ChipZiel.Prioritaet
                });

                chips.Add(new ErzeugerKarte.ChipDaten
                {
                    Text = BetriebsmodusAnzeige(info),
                    Hinweis = MyResource.Resource.SIM_TIP_BETRIEBSMODUS,
                    Ziel = ErzeugerKarte.ChipZiel.Modus
                });
            }

            return chips;
        }

        /// <summary>
        /// Der MODUL-AUSWEIS einer Erzeugerkarte (Anwenderentscheid F2, 30.08.2026):
        /// „Modul n von m", sobald das Projekt MEHRERE Anlagen desselben Erzeugertyps
        /// führt.
        ///
        /// <para><b>Was er beantwortet.</b> Die Rangziffer der Kopfzeile ist die
        /// Kaskadenstufe des ERZEUGERTYPS (<see cref="WaermeerzeugerGruppe"/>); alle
        /// Karten eines Typs tragen deshalb dieselbe. Das ist richtig, sah aber aus wie
        /// eine doppelt gezeigte Karte — der gemeldete Fall: zwei BHKW, zweimal „1", der
        /// Anwender vermisste sein zweites BHKW. Der Ausweis nennt die Stelle innerhalb
        /// des Typs und lässt den Rang dabei unangetastet.</para>
        ///
        /// <para><b>Nur bei m &gt; 1.</b> „Modul 1 von 1" sagt nichts, was die Karte
        /// nicht schon sagt; Projekte mit einer Anlage je Typ behalten ihr
        /// Bestandsbild.</para>
        ///
        /// <para><paramref name="nr"/> zählt in der ANZEIGEREIHENFOLGE — der von
        /// <see cref="AnlagenImProjekt"/> gelieferten (gepflegte Priorität zuerst,
        /// ungepflegte dahinter, dann ID; Fix HB1). Damit ist „Modul 2" dieselbe Karte,
        /// die auch als zweite in der Spalte steht.</para>
        ///
        /// <para><b>Stil <c>Flaeche</c></b> — graue Füllung ohne Rahmen, wie beim
        /// Temperaturpaar: ein stiller Ausweis, der den bedeutungstragenden Farben
        /// daneben (Quelle blau, Senke koralle, Warnung amber) nicht ins Gehege kommt.
        /// Kein <c>ChipZiel</c>: An der Modulnummer gibt es nichts zu bearbeiten, ein
        /// Doppelklick öffnet deshalb den Standard-Editor der Karte wie auf jeder
        /// anderen zielfreien Stelle. Er läuft als gewöhnlicher Chip im umbrechenden
        /// Chipbereich mit — die Kartenhöhe folgt (<c>ErzeugerKarte.HoeheNachfuehren</c>),
        /// die Kopfzeile bleibt unberührt.</para>
        /// </summary>
        private void ModulChip(List<ErzeugerKarte.ChipDaten> chips, int nr, int anzahl)
        {
            if (chips == null || anzahl < 2 || nr < 1) return;

            chips.Add(new ErzeugerKarte.ChipDaten
            {
                Text = string.Format(T("SIM_KARTE_MODUL", "Modul {0} von {1}"), nr, anzahl),
                Stil = ErzeugerKarte.ChipStil.Flaeche,
                Hinweis = string.Format(
                    T("SIM_KARTE_TIP_MODUL",
                      "Der Rang gilt dem Erzeugertyp. Dieses Projekt führt {0} Anlagen " +
                      "dieses Typs — jede hat ihre eigene Karte."),
                    anzahl)
            });
        }

        /// <summary>
        /// Anzeigetext über den Ressourcenschlüssel, mit deutschem Rückfall — dasselbe
        /// Muster wie in <c>SteuerGutschriftRechner.T</c>. Es hält neue Texte aus
        /// <c>MyResource/Resource.Designer.cs</c> heraus, die Visual Studio selbst
        /// regeneriert; die Schlüssel werden in den <c>.resx</c> nachgetragen.
        /// </summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }

        /// <summary>
        /// PAKET S2 — die Befunde des Warnkriterienkatalogs (Konzept 6.2) je Anlage,
        /// EINMAL je Auffrischung der Kartenspalte gesammelt.
        ///
        /// <para>Aufgenommen werden nur Befunde MIT Anlagenbezug. Die rein
        /// speicherbezogenen (W2, leeres Klassen-Set) und der projektweite Ring gehören
        /// nicht an eine Erzeugerkarte; sie stehen im Laufprotokoll und — soweit sie den
        /// Speicher betreffen — künftig an der Speicherkarte (Paket P2).</para>
        /// </summary>
        private Dictionary<int, List<string>> _warnbefunde = new Dictionary<int, List<string>>();

        /// <summary>
        /// PAKET B1 (Entscheidung F9) — die Booster-Anlagen des Projekts, EINMAL je
        /// Auffrischung geholt (Muster <see cref="_warnbefunde"/>): Anlage →
        /// geteilter Quellpuffer.
        /// </summary>
        private Dictionary<int, int> _boosterAnlagen = new Dictionary<int, int>();

        private void BoosterAnlagenSammeln()
        {
            _boosterAnlagen = (m_ID_Projekt > 0)
                ? Warnkriterien.BoosterAnlagen(m_ID_Projekt)
                : new Dictionary<int, int>();
        }

        /// <summary>
        /// Das BOOSTER-BADGE einer Erzeugerkarte (Konzept 8.2 Punkt 3, Entscheidung F9).
        ///
        /// <para><b>Anzeigeregel, kein Datenfeld.</b> F9 hat gegen einen eigenen
        /// Anlagentyp und gegen neue Persistenz entschieden; die Marke wird aus der
        /// Konfiguration abgeleitet (<see cref="Warnkriterien.BoosterAnlagen"/>) —
        /// Wärmequelle Pufferspeicher auf einen GETEILTEN Puffer, also einen, den ein
        /// anderer Erzeuger dieses Projekts lädt.</para>
        ///
        /// <para>Sie gilt für Wärmepumpe UND Heizkessel (Konzept 8.4: Gleichbehandlung) —
        /// es ist dieselbe Kopplung, und die Karte darf sie nicht bei einem der beiden
        /// verschweigen.</para>
        ///
        /// <para>Der Tooltip nennt den Quellspeicher: Die Aussage „Booster" ist ohne ihn
        /// nicht nachprüfbar. Stil <c>QuelleKaskade</c> — dasselbe Blau-gestrichelt wie
        /// der Quellen-Chip daneben, denn das Badge erläutert genau diesen; als
        /// Doppelklickziel deshalb ebenfalls die Quelle.</para>
        /// </summary>
        private void BoosterChip(AnlagenInfo info, List<ErzeugerKarte.ChipDaten> chips)
        {
            int idPuffer;
            if (!_boosterAnlagen.TryGetValue(info.ID, out idPuffer) || idPuffer <= 0) return;

            string name = WaermesenkeClass.PufferName(idPuffer);
            if (name.Length == 0) name = MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER;

            chips.Add(new ErzeugerKarte.ChipDaten
            {
                Text = MyResource.Resource.SIM_KARTE_BOOSTER,
                Stil = ErzeugerKarte.ChipStil.QuelleKaskade,
                Hinweis = string.Format(MyResource.Resource.SIM_KARTE_TIP_BOOSTER, name),
                Ziel = ErzeugerKarte.ChipZiel.Quelle
            });
        }

        private void WarnbefundeSammeln()
        {
            _warnbefunde = new Dictionary<int, List<string>>();
            if (m_ID_Projekt <= 0) return;

            foreach (Warnbefund b in Warnkriterien.PruefeProjekt(m_ID_Projekt))
            {
                if (b == null || b.ID_Anlage <= 0 || string.IsNullOrEmpty(b.Text)) continue;

                List<string> texte;
                if (!_warnbefunde.TryGetValue(b.ID_Anlage, out texte))
                {
                    texte = new List<string>();
                    _warnbefunde[b.ID_Anlage] = texte;
                }

                string zeile = Zeilenumbruch.Einzeilig(b.Text);
                if (!texte.Contains(zeile)) texte.Add(zeile);
            }
        }

        /// <summary>
        /// Der WARN-Chip einer Erzeugerkarte (Konzept 6.2): ein dezentes Amber-Chip mit
        /// den Befunden im Mouseover, sonst gar nichts.
        ///
        /// <para><b>Kein Modaldialog beim Öffnen.</b> Der Katalog meldet
        /// Konfigurationen, die zulässig sind — sie zu blockieren oder mit einer
        /// MessageBox zu quittieren, wäre genau die Bevormundung, die Entscheidung F6
        /// abgeschafft hat. Die ausführliche Meldung bekommt der Anwender dort, wo er
        /// die Zuordnung einstellt (Senkendialog) und im Laufprotokoll.</para>
        ///
        /// <para>Der Chip führt in den SENKENDIALOG (<c>ChipZiel.Senke</c>): Alle
        /// anlagenbezogenen Kriterien — W1, W3 und der Kurzschluss — hängen an einer
        /// Senkenzeile, und dort wird sie geändert.</para>
        /// </summary>
        private void WarnChip(AnlagenInfo info, List<ErzeugerKarte.ChipDaten> chips)
        {
            List<string> texte;
            if (!_warnbefunde.TryGetValue(info.ID, out texte) || texte.Count == 0) return;

            chips.Add(new ErzeugerKarte.ChipDaten
            {
                Text = MyResource.Resource.SIMWARN_KARTE_CHIP,
                Stil = ErzeugerKarte.ChipStil.Warnung,
                Hinweis = MyResource.Resource.SIMWARN_KARTE_CHIP_TIP + Environment.NewLine +
                          "• " + string.Join(Environment.NewLine + "• ", texte.ToArray()),
                Ziel = ErzeugerKarte.ChipZiel.Senke
            });
        }

        /// <summary>
        /// Der Quellen-Chip einer Erzeugerkarte.
        ///
        /// <b>ETAPPE D5b — Freischaltung je <c>ID_Type</c></b> (Konzept Abschnitt 4;
        /// D5a-Restpunkt 2 und Review-2-Befund K3-1). Bis D5a war der Chip WÄRMEPUMPEN
        /// vorbehalten: Ein Heizkessel mit Quellpuffer zeigte zwar seit D2/D3 den
        /// Kaskaden-Chip, konnte ihn aber nicht öffnen (<c>ChipZiel.Keines</c>), und ein
        /// Kessel OHNE Quellpuffer bekam gar keinen Chip — die Kaskade war über die
        /// Oberfläche also nicht einzurichten, nur per SQL.
        ///
        /// Jetzt gilt: Wärmepumpe UND Heizkessel tragen einen anklickbaren Quellen-Chip
        /// (<see cref="WaermequelleClass.QuellenwahlMoeglich"/>), Solarthermie und BHKW
        /// keinen. Das ist dieselbe Grenze, die die Engine zieht (Befund E-K2-2): Nur
        /// Wärmepumpe und Kessel werten eine Ebenenmaske aus, jede andere Art bekäme eine
        /// Warnung und einen wirkungslosen Eintrag.
        /// </summary>
        private void QuellenChip(AnlagenInfo info, List<ErzeugerKarte.ChipDaten> chips)
        {
            bool waehlbar = WaermequelleClass.QuellenwahlMoeglich(info.ID_Type);

            // E0: WQ_ID_Puffer ist die führende Identität des Quellpuffers;
            // QuellPufferDerAnlage löst sie mit demselben Vorrang auf wie die Engine
            // (FK vor Bezeichner) und liefert 0, wenn die Quelle kein Puffer ist.
            int idQuellPuffer = WaermesenkeClass.QuellPufferDerAnlage(m_ID_Projekt, info.ID);

            if (idQuellPuffer > 0)
            {
                string name = WaermesenkeClass.PufferName(idQuellPuffer);
                if (name.Length == 0) name = MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER;

                chips.Add(new ErzeugerKarte.ChipDaten
                {
                    Text = string.Format(MyResource.Resource.SIM_KARTE_QUELLE_KASKADE, name),
                    Stil = ErzeugerKarte.ChipStil.QuelleKaskade,
                    Hinweis = MyResource.Resource.SIM_KARTE_TIP_KASKADE,
                    Ziel = waehlbar ? ErzeugerKarte.ChipZiel.Quelle : ErzeugerKarte.ChipZiel.Keines
                });
                return;
            }

            // Solarthermie und BHKW haben keine wählbare Wärmequelle (Konzept 4,
            // Anforderung 5) - für sie entsteht gar kein Chip, statt einen anzubieten und
            // den Klick darauf mit einer Meldung abzuweisen.
            if (!waehlbar) return;

            if (info.IstWaermepumpe)
            {
                // PAKET Q1 (Konzept 8.1 Punkt 1): BAUART-BINDUNG SICHTBAR. Eine
                // Luft-Wasser-Wärmepumpe hat keine Wahl - ihre Quelle IST die Außenluft,
                // und die Engine erzwingt das seit jeher still
                // (WaermequelleClass.Quelltemperatur gibt für diese Bauart immer den
                // Außentemperaturvektor zurück, Quellspeicher immer null). Bis Q1 sah der
                // Chip trotzdem wählbar aus und wies den Klick mit einer Meldung ab; das
                // ist eine Einladung, der eine Absage folgt. Jetzt steht die Quelle als
                // FESTER Eintrag da: Flächenstil statt Quellrahmen, kein Handzeiger
                // (ChipZiel.Keines), und der Mouseover-Hinweis nennt den Grund. Die
                // Abbruchmeldung in WaermequelleBearbeiten bleibt als zweite Sicherung -
                // sie deckt den Weg über Schema und Tastatur ab.
                //
                // Die FEHLENDE Bauart (WpTyp leer) gehört dazu: Engine und Anzeige
                // rechnen sie seit jeher wie Luft-Wasser.
                bool bauartGebunden = string.IsNullOrEmpty(info.WpTyp) ||
                                      info.WpTyp == DbWerte.WP_BAUART_LUFT_WASSER;

                chips.Add(new ErzeugerKarte.ChipDaten
                {
                    Text = string.Format(MyResource.Resource.SIM_KARTE_QUELLE, WaermequelleAnzeige(info)),
                    Stil = bauartGebunden ? ErzeugerKarte.ChipStil.Flaeche
                                          : ErzeugerKarte.ChipStil.Quelle,
                    Hinweis = bauartGebunden
                        ? string.Format(MyResource.Resource.SIMQ_TIP_QUELLE_BAUART,
                                        string.IsNullOrEmpty(info.WpTyp)
                                            ? MyResource.Resource.SIMQ_WPTYP_NICHT_GEPFLEGT
                                            : info.WpTyp)
                        : MyResource.Resource.SIMQ_TIP_QUELLE,
                    Ziel = bauartGebunden ? ErzeugerKarte.ChipZiel.Keines
                                          : ErzeugerKarte.ChipZiel.Quelle
                });
                return;
            }

            // Heizkessel ohne Quellpuffer: Er rechnet mit dem Systemrücklauf als
            // Eintrittstemperatur - das ist der Normalfall und keine Fehlstelle. Der Chip
            // sagt es und ist der Einstieg in die Kaskade.
            chips.Add(new ErzeugerKarte.ChipDaten
            {
                Text = string.Format(MyResource.Resource.SIM_KARTE_QUELLE,
                                     MyResource.Resource.SIMQ_QUELLE_SYSTEMRUECKLAUF),
                Stil = ErzeugerKarte.ChipStil.Quelle,
                Hinweis = MyResource.Resource.SIMQ_TIP_QUELLE_KESSEL,
                Ziel = ErzeugerKarte.ChipZiel.Quelle
            });
        }

        /// <summary>
        /// Die SENKENKETTE einer Erzeugerkarte (Konzept 5.3).
        ///
        /// <b>PAKET S1.</b> Eine Anlage hat nicht mehr zwei Senkenplätze, sondern eine
        /// geordnete Liste (<c>Z_AnlageSenke</c>). Die Karte zeigt sie als Kette:
        /// „Senke: Heizkreis · Zweitsenke: Puffer P1 · → Puffer P2".
        ///
        /// <b>PAKET A1:</b> Alle Chips lesen jetzt DIESELBE Quelle — die Senkenliste aus
        /// <c>info.Senken</c>, zugeteilt aus EINER Projektabfrage. Bis dahin kamen die
        /// ersten beiden aus der auf die Altspalten gespiegelten Sicht und der Rest aus
        /// einem Nachschlag je Karte; die Sonderbehandlung der Prozess-Ziele (die sich in
        /// den Altspalten als „Heizung" spiegelten) ist damit gegenstandslos.
        /// </summary>
        private void SenkenChips(AnlagenInfo info, List<ErzeugerKarte.ChipDaten> chips)
        {
            List<Z_AnlageSenkeModel> kette = info.Senken;

            Z_AnlageSenkeModel rang1 = info.SenkeAufRang(0);
            bool pufferSenke = rang1 != null &&
                               WaermesenkeClass.IstPufferZiel(rang1.Ziel) && rang1.ID_Puffer > 0;

            string text = WaermesenkeAnzeige(info);
            string hinweis = MyResource.Resource.SIM_TIP_SENKE;

            if (pufferSenke)
            {
                // PAKET PARALLELVERBUND (Entscheidung 17.08.2026): Lädt der Erzeuger einen
                // gemeinsamen Vorrat aus mehreren Speichern, muss die Karte das zeigen -
                // sonst stünde dort der Name EINES Behälters, während der Lauf mit der
                // Summe rechnet. Der Zusatz steht VOR der Kreisziffer, damit die
                // Ladeposition wie bisher am Ende des Chips sitzt.
                //
                // Punktueller Griff auf die Verbundzuordnung, nur bei Puffer-Senke: Die
                // Karten bauen ihre Daten aus EINER Projektabfrage (AnlagenInfo.Senken),
                // und eine Anlage ohne Puffer-Senke kann keinen Verbund haben.
                int zusatz = WaermesenkeClass.VerbundLesen(info.ID).Count;
                if (zusatz > 0)
                {
                    text += " " + string.Format(MyResource.Resource.SIM_KARTE_VERBUND_ZUSATZ, zusatz);
                    hinweis = string.Format(MyResource.Resource.SIM_TIP_VERBUND, zusatz + 1) +
                              Environment.NewLine + hinweis;
                }

                List<Ladeordnung.LadeEintrag> ordnung =
                    Ladeordnung.Ladereihenfolge(m_ID_Projekt, rang1.ID_Puffer);
                int position = Ladeordnung.Position(ordnung, info.ID, false);
                if (position > 0)
                {
                    text += " " + KartenStil.Kreisziffer(position);
                    hinweis = string.Format(MyResource.Resource.SIM_POSITION_LAEDT_ALS,
                                            position, ordnung.Count) +
                              Environment.NewLine + hinweis;
                }
            }

            chips.Add(new ErzeugerKarte.ChipDaten
            {
                Text = string.Format(MyResource.Resource.SIM_KARTE_SENKE, text),
                Stil = pufferSenke ? ErzeugerKarte.ChipStil.Senke : ErzeugerKarte.ChipStil.Neutral,
                Hinweis = hinweis,
                Ziel = ErzeugerKarte.ChipZiel.Senke
            });

            Z_AnlageSenkeModel rang2 = info.SenkeAufRang(1);
            string zweit = ZweitsenkeAnzeige(info);
            bool zweitPuffer = rang2 != null &&
                               WaermesenkeClass.IstPufferZiel(rang2.Ziel) && rang2.ID_Puffer > 0;

            if (zweitPuffer)
            {
                List<Ladeordnung.LadeEintrag> ordnung2 =
                    Ladeordnung.Ladereihenfolge(m_ID_Projekt, rang2.ID_Puffer);
                int position2 = Ladeordnung.Position(ordnung2, info.ID, true);
                if (position2 > 0) zweit += " " + KartenStil.Kreisziffer(position2);
            }

            chips.Add(new ErzeugerKarte.ChipDaten
            {
                Text = string.Format(MyResource.Resource.SIM_KARTE_ZWEITSENKE, zweit),
                Stil = zweitPuffer ? ErzeugerKarte.ChipStil.Senke : ErzeugerKarte.ChipStil.Neutral,
                Hinweis = MyResource.Resource.SIM_TIP_ZWEITSENKE,
                Ziel = ErzeugerKarte.ChipZiel.Zweitsenke
            });

            // --- Ränge ab 3 (Paket S1) ------------------------------------------------
            //
            // Sie tragen dasselbe Chipziel wie die Zweitsenke: Der Doppelklick führt in
            // denselben Dialog, weil die Senken einer Anlage fachlich EINE Einstellung
            // sind. Die Ladeposition steht wie bei den beiden ersten Chips dahinter — die
            // Ladeordnung kennt „Zweitsenke" als Boolean, und jeder Rang über 1 ist dort
            // eine Zweitsenke (dieselbe Ableitung wie in der Engine).
            for (int i = 2; i < kette.Count; i++)
            {
                Z_AnlageSenkeModel z = kette[i];
                if (z == null) continue;

                string weiter = WaermesenkeClass.SenkeAnzeige(z);
                bool weiterPuffer = WaermesenkeClass.IstPufferZiel(z.Ziel) && z.ID_Puffer > 0;

                if (weiterPuffer)
                {
                    List<Ladeordnung.LadeEintrag> ordnungN =
                        Ladeordnung.Ladereihenfolge(m_ID_Projekt, z.ID_Puffer);
                    int positionN = Ladeordnung.Position(ordnungN, info.ID, true);
                    if (positionN > 0) weiter += " " + KartenStil.Kreisziffer(positionN);
                }

                chips.Add(new ErzeugerKarte.ChipDaten
                {
                    Text = string.Format(MyResource.Resource.SIM_KARTE_SENKE_WEITER, weiter),
                    Stil = weiterPuffer ? ErzeugerKarte.ChipStil.Senke : ErzeugerKarte.ChipStil.Neutral,
                    Hinweis = MyResource.Resource.SIM_TIP_ZWEITSENKE,
                    Ziel = ErzeugerKarte.ChipZiel.Zweitsenke
                });
            }
        }

        // PAKET A1: Senkenkette(int) und KettenText sind ENTFALLEN. Die erste holte die
        // Senkenliste je Karte einzeln nach, weil die Kartendaten selbst noch aus den
        // Altspalten kamen; die zweite entschied Chip für Chip, ob der Bestandstext oder
        // die Kette gilt (nötig nur für die beiden Prozess-Ziele, die sich in den
        // Altspalten als „Heizung" spiegelten). Die Kette steht jetzt in
        // AnlagenInfo.Senken - eine Abfrage für das ganze Projekt, eine Wahrheit.


        /// <summary>
        /// Temperaturchip mit der WARNREGEL aus Konzept Abschnitt 5:
        /// „Erzeuger-Vorlauf ≥ Puffer-Vorlauf, sonst Warnung (Anzeige amber; keine harte
        /// Sperre — die Engine kappt ohnehin physikalisch)."
        ///
        /// Gezeigt wird das Paar des ZUGEORDNETEN PUFFERS, sobald der Erzeuger einen
        /// lädt — das ist die Temperatur, auf die er arbeiten muss. Ohne Pufferziel (oder
        /// bei einem Puffer ohne gepflegtes Paar) steht das Paar der Anlage selbst da.
        /// </summary>
        private void TemperaturChip(AnlagenInfo info, List<ErzeugerKarte.ChipDaten> chips)
        {
            WaermesenkeClass.PufferInfo puffer = null;
            Z_AnlageSenkeModel rang1 = info.SenkeAufRang(0);
            if (rang1 != null && WaermesenkeClass.IstPufferZiel(rang1.Ziel) && rang1.ID_Puffer > 0)
                puffer = WaermesenkeClass.PufferLesen(rang1.ID_Puffer);

            bool pufferPaar = puffer != null && puffer.Vorlauf > 0 && puffer.Ruecklauf > 0;

            string text;
            if (pufferPaar)
                text = string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                     puffer.Vorlauf, puffer.Ruecklauf);
            else if (info.Vorlauf > 0 && info.Ruecklauf > 0)
                text = string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                     info.Vorlauf, info.Ruecklauf);
            else
                return;   // nichts Gepflegtes - lieber kein Chip als eine erfundene Zahl

            bool warnung = pufferPaar && info.Vorlauf > 0 && info.Vorlauf < puffer.Vorlauf;

            chips.Add(new ErzeugerKarte.ChipDaten
            {
                Text = text,
                Stil = warnung ? ErzeugerKarte.ChipStil.Warnung : ErzeugerKarte.ChipStil.Flaeche,
                Hinweis = warnung
                    ? string.Format(MyResource.Resource.SIM_KARTE_TIP_TEMPERATUR_WARNUNG,
                                    info.Vorlauf, puffer.Bezeichner, puffer.Vorlauf)
                    : null
            });
        }

        // --- Aufbau der Speicherspalte ------------------------------------------------

        private void AktualisiereSpeicherKarten()
        {
            if (flow_Speicher == null) return;

            flow_Speicher.SuspendLayout();
            try
            {
                SpalteLeeren(flow_Speicher, btn_PufferVerwalten);

                List<WaermesenkeClass.PufferInfo> puffer = m_ID_Projekt > 0
                    ? WaermesenkeClass.ProjektPufferListe(m_ID_Projekt, null)
                    : new List<WaermesenkeClass.PufferInfo>();

                // EINMAL je Auffrischung, nicht je Karte (siehe QuellnutzerSammeln
                // bzw. GeladenePufferSammeln).
                _quellnutzer = QuellnutzerSammeln();
                _geladenePuffer = GeladenePufferSammeln();
                _systemVorlauf = PufferSpCtrl.SystemVorlauf(m_ID_Projekt);
                _systemRuecklauf = PufferSpCtrl.SystemRuecklauf(m_ID_Projekt);

                // PAKET P1: Schichtenzahl und Ergebnistemperatur - je EINE Abfrage für
                // ALLE Karten, aus demselben Grund wie die vier Zeilen darüber.
                _schichtenJePuffer = PufferSpCtrl.SchichtenJeProjekt(m_ID_Projekt);
                _tObenJePuffer = TObenSammeln();

                foreach (WaermesenkeClass.PufferInfo p in puffer)
                {
                    SpeicherKarte karte = new SpeicherKarte();
                    flow_Speicher.Controls.Add(karte);
                    karte.Setzen(SpeicherKarteDaten(p));
                    karte.Aufgeklappt = p.ID == _offenerSpeicher;
                    karte.Umschalten += SpeicherKarte_Umschalten;
                    karte.Bearbeiten += SpeicherKarte_Bearbeiten;
                }

                if (puffer.Count == 0)
                    flow_Speicher.Controls.Add(Hinweiszeile(
                        m_ID_Projekt > 0
                            ? MyResource.Resource.PSP_KARTE_KEIN_SPEICHER
                            : MyResource.Resource.PSP_FUSSZEILE_OHNE_PROJEKT));

                // Der Einstieg in die Verwaltung bleibt (Konzept 4.3) - ohne ihn wäre
                // kein Pufferspeicher mehr anzulegen.
                flow_Speicher.Controls.Add(btn_PufferVerwalten);
                btn_PufferVerwalten.Enabled = m_ID_Projekt > 0;
            }
            finally
            {
                flow_Speicher.ResumeLayout();
                KartenBreiteAnpassen(flow_Speicher);
            }
        }

        private void SpeicherKarte_Umschalten(object sender, EventArgs e)
        {
            SpeicherKarte karte = sender as SpeicherKarte;
            if (karte == null) return;

            _offenerSpeicher = karte.Aufgeklappt ? 0 : karte.ID_Puffer;

            // Konzept 3a: höchstens EINE Karte offen.
            flow_Speicher.SuspendLayout();
            try
            {
                foreach (Control c in flow_Speicher.Controls)
                {
                    SpeicherKarte k = c as SpeicherKarte;
                    if (k != null) k.Aufgeklappt = k.ID_Puffer == _offenerSpeicher;
                }
            }
            finally
            {
                flow_Speicher.ResumeLayout();
                KartenBreiteAnpassen(flow_Speicher);
            }

            // D4: Der Klick ist zugleich die Auswahl - unabhängig davon, ob die Karte
            // gerade auf- oder zugeklappt wird.
            SpeicherkarteAusgewaehlt(karte.ID_Puffer);
        }

        private void SpeicherKarte_Bearbeiten(object sender, EventArgs e)
        {
            SpeicherKarte karte = sender as SpeicherKarte;
            if (karte == null || m_ID_Projekt <= 0) return;

            PufferVerwaltungOeffnen(karte.ID_Puffer);
        }

        /// <summary>Alles aus einer Spalte entfernen; <paramref name="behalten"/> überlebt.</summary>
        private static void SpalteLeeren(FlowLayoutPanel flow, Control behalten)
        {
            List<Control> alt = new List<Control>();
            foreach (Control c in flow.Controls) alt.Add(c);

            flow.Controls.Clear();

            foreach (Control c in alt)
                if (!ReferenceEquals(c, behalten)) c.Dispose();
        }

        /// <summary>Eine leise Textzeile zwischen den Karten (Hinweise, Gruppenköpfe).</summary>
        private Label Hinweiszeile(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = false;
            l.Height = 22;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.ForeColor = KartenStil.TEXT_LEISE;
            l.BackColor = Color.Transparent;
            l.Margin = new Padding(0, 2, 0, 6);
            return l;
        }

        /// <summary>Füllt eine Speicherkarte aus den Projektdaten (Konzept 3a).</summary>
        private SpeicherKarte.Daten SpeicherKarteDaten(WaermesenkeClass.PufferInfo p)
        {
            SpeicherKarte.Daten d = new SpeicherKarte.Daten();
            d.ID_Puffer = p.ID;
            d.Bezeichner = p.Bezeichner.Length > 0
                ? p.Bezeichner : MyResource.Resource.PSP_BEZEICHNER_ERSATZ;

            string kanal = WaermesenkeClass.WirksameVerwendung(p);
            d.Verwendung = WaermesenkeClass.VerwendungAnzeige(kanal);

            // PAKET P1: Schicht-Badge „N Schichten" (Konzept 10). Nur bei N > 1 - das
            // Verzeichnis führt Ein-Zonen-Speicher gar nicht erst.
            int schichten;
            if (_schichtenJePuffer.TryGetValue(p.ID, out schichten))
                d.Schichtung = string.Format(MyResource.Resource.PSP_KARTE_SCHICHTEN, schichten);

            if (p.Gesamtvolumen > 0)
                d.Volumen = string.Format(MyResource.Resource.PSP_KARTE_VOLUMEN, p.Gesamtvolumen);

            int vorlauf, ruecklauf;
            string herkunft = TemperaturHerkunft(p, out vorlauf, out ruecklauf);
            if (vorlauf > 0 && ruecklauf > 0)
                d.Temperaturpaar = string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                                 vorlauf, ruecklauf);

            // --- Lader in wirksamer Reihenfolge (Ladeordnung 3.4) --------------------
            //
            // VORFILTER: Ladereihenfolge fragt je Aufruf die Anlagen des Projekts und die
            // Kaskadenplätze neu ab. Für einen Speicher, den niemand lädt, ist das
            // Ergebnis garantiert leer — die Abfrage also verschenkt. Projekt 1023 der
            // Arbeitskopie führt 79 Puffer-Zeilen, von denen genau EINE geladen wird;
            // ohne den Vorfilter kostete der Seitenaufbau dort über 150 Abfragen. Der
            // Filter benutzt dieselbe Bedingung wie Ladeordnung (ID auf einem der beiden
            // Senkenfelder UND ein Puffer-Ziel), das Ergebnis ist damit unverändert.
            List<Ladeordnung.LadeEintrag> lader = _geladenePuffer.Contains(p.ID)
                ? Ladeordnung.Ladereihenfolge(m_ID_Projekt, p.ID)
                : new List<Ladeordnung.LadeEintrag>();
            d.LaderAnzahl = lader.Count;

            if (lader.Count > 0)
            {
                List<string> teile = new List<string>();
                for (int i = 0; i < lader.Count; i++)
                {
                    Ladeordnung.LadeEintrag e = lader[i];
                    string name = e.Bezeichner.Length > 0 ? e.Bezeichner : e.Erzeuger;

                    string zeile = (i + 1) + ". " + name + " (" +
                                   string.Format(MyResource.Resource.SIM_POSITION_BIS,
                                                 e.Obergrenze.ToString("0.#")) + ")";

                    if (e.Zweitsenke)
                        zeile += " " + MyResource.Resource.SIM_ROLLE_ZWEITSENKE;
                    if (e.LadeprioPV > 0)
                        zeile += " " + string.Format(MyResource.Resource.PSP_KARTE_PV_RANG,
                                                     e.LadeprioPV);

                    teile.Add(zeile);
                }
                d.Detailzeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_LADER,
                                                 string.Join(" · ", teile.ToArray())));
            }
            else
            {
                d.Detailzeilen.Add(MyResource.Resource.PSP_KARTE_LADER_KEINE);
            }

            // --- Versorgt: der Kanal, aus dem entladen wird --------------------------
            d.Detailzeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_VERSORGT, d.Verwendung));

            // --- PARALLELVERBUND (Entscheidung 17.08.2026) ---------------------------
            //
            // Ein MITGLIED hat im Lauf keinen eigenen Füllstand: Seine Kapazität steckt im
            // Leitspeicher, und in Tab_ErgebnisPufferspeicher steht keine Zeile für ihn.
            // Ohne diese Zeile suchte der Anwender im Ergebnis nach einem Speicher, den es
            // dort nicht gibt - genau die stille Leerstelle, die das Paket vermeiden soll.
            //
            // EINE Abfrage je Karte, und nur diese eine: Die Zahl der Mitglieder des
            // eigenen Verbunds interessiert an dieser Stelle nicht, sie steht am
            // Erzeuger-Chip (SenkenChips).
            int idLeit = AnlagePufferVerbundCtrl.LeitspeicherFuerMitglied(p.ID);
            if (idLeit > 0 && idLeit != p.ID)
                d.Detailzeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_IM_VERBUND,
                                                 WaermesenkeClass.PufferName(idLeit)));

            // --- Quelle für: NUR Erzeuger (Invariante S-1) ---------------------------
            List<string> quelleFuer = QuelleFuerAnlagen(p);
            if (quelleFuer.Count > 0)
                d.Detailzeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_QUELLE_FUER,
                                                 string.Join(" · ", quelleFuer.ToArray())));

            // Ein Abnehmer ist der eigene Kanal; jede Kaskadenentnahme kommt hinzu.
            d.AbnehmerAnzahl = 1 + quelleFuer.Count;

            // --- Entladeprioritaet ---------------------------------------------------
            bool manuell = p.Entladeprio >= Ladeordnung.PRIO_MIN &&
                           p.Entladeprio <= Ladeordnung.PRIO_MAX;

            // Der Automatikwert ist die BESTE Ladepriorität am Speicher (Konzept 3.6) —
            // also der erste Eintrag der bereits geholten Ladereihenfolge. Der Aufruf
            // von Ladeordnung.EntladeprioAutomatik täte dasselbe, würde die Reihenfolge
            // dafür aber ein ZWEITES Mal aus der Datenbank holen; bei 79 Speicherkarten
            // ist das messbar.
            int automatik = lader.Count > 0 ? lader[0].Ladeprio : Ladeordnung.PRIO_SONSTIGE;

            string prio = manuell
                ? string.Format(MyResource.Resource.PSP_LADEPRIO_MANUELL, p.Entladeprio)
                : string.Format(MyResource.Resource.PSP_PRIO_AUTOMATISCH_WERT, automatik);
            d.Detailzeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_ENTLADEPRIO, prio));

            // --- Temperaturherkunft --------------------------------------------------
            d.Detailzeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_TEMP_HERKUNFT, herkunft));

            // --- PAKET P1: Ergebnistemperatur der obersten Schicht --------------------
            //
            // Die Zeile erscheint NUR, wenn das jüngste Ergebnis des Projekts für diesen
            // Speicher einen Wert trägt (Konzept 10: „T_oben in der Detailansicht"). Ohne
            // gerechneten Lauf, vor Migrationsschritt 52 und bei einem Ein-Zonen-Speicher
            // aus einem Lauf vor P1 fehlt sie ersatzlos - eine Zeile mit „-" wäre eine
            // Aussage über etwas, das niemand gemessen hat.
            double tOben;
            if (_tObenJePuffer.TryGetValue(p.ID, out tOben))
                d.Detailzeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_T_OBEN,
                                                 tOben.ToString("0.#")));

            // --- Schwellenband -------------------------------------------------------
            d.SchwelleEin = p.SchwelleEin;
            d.SchwelleAusNachrang = p.SchwelleAusNachrang;
            d.SchwelleAus = p.SchwelleAus;
            d.Schwellentext = string.Format(MyResource.Resource.PSP_KARTE_SCHWELLEN,
                                            p.SchwelleEin.ToString("0.#"),
                                            p.SchwelleAusNachrang.ToString("0.#"),
                                            p.SchwelleAus.ToString("0.#"));
            return d;
        }

        /// <summary>
        /// Abbildung Quellpuffer → Anlagen, die ihn als WÄRMEQUELLE nutzen — die Kaskade
        /// aus Anforderung 6 des Konzepts, für ALLE Speicher auf einmal.
        ///
        /// <b>Invariante S-1.</b> Gefragt wird ausschließlich
        /// <c>Tab_Energieanlagen</c>; Quell- und Senkenbezüge existieren nur dort, nie an
        /// <c>Tab_Pufferspeicher</c>. Ein Speicher kann in dieser Abbildung damit
        /// strukturell nicht als NUTZER auftauchen — genau das verlangt die Invariante
        /// („ein direkter Pfeil Speicher → Speicher darf nie gezeichnet werden").
        ///
        /// Aufgelöst wird je Anlage über
        /// <see cref="WaermesenkeClass.QuellPufferDerAnlage"/> und nicht über einen
        /// eigenen Vergleich: Das ist dieselbe Rangfolge (FK <c>WQ_ID_Puffer</c> vor
        /// Bezeichner <c>WQ_Puffer</c>), die Engine und Erzeugerkarte benutzen. Ein
        /// zweiter Vergleich hier könnte bei Altbestand eine andere Antwort geben.
        ///
        /// <b>Einmal je Auffrischung, nicht je Karte.</b> Die erste Fassung fragte die
        /// Anlagen für JEDEN Speicher neu und löste die Quellidentität Puffer×Anlage-mal
        /// auf. Bei Projekt 1023 der Arbeitskopie — 79 Puffer-Zeilen, drei Erzeuger —
        /// waren das über 300 zusätzliche Abfragen je Aufbau; der Dialog stand im
        /// Harness minutenlang. Die Abbildung hängt nur am Projekt, also wird sie einmal
        /// gebaut.
        /// </summary>
        private Dictionary<int, List<string>> QuellnutzerSammeln()
        {
            Dictionary<int, List<string>> map = new Dictionary<int, List<string>>();
            if (m_ID_Projekt <= 0) return map;

            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt=" + m_ID_Projekt +
                " AND ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ")" +
                // Ungepflegte Priorität ans ENDE (ANLAGENPRIO_UNGEPFLEGT) - die
                // Reihenfolge steht auf der Speicherkarte als Kaskadenliste.
                " ORDER BY " + Ladeordnung.SqlAnlagenprio(null) + ", ID");
            if (dt == null) return map;

            foreach (System.Data.DataRow r in dt.Rows)
            {
                if (r["ID"] == DBNull.Value) continue;
                int idAnlage = Convert.ToInt32(r["ID"]);

                int idPuffer = WaermesenkeClass.QuellPufferDerAnlage(m_ID_Projekt, idAnlage);
                if (idPuffer <= 0) continue;

                string name = r["Bezeichner"] == DBNull.Value ? "" : r["Bezeichner"].ToString();
                if (name.Length == 0) continue;

                if (!map.ContainsKey(idPuffer)) map[idPuffer] = new List<string>();
                map[idPuffer].Add(name + " " + MyResource.Resource.PSP_KARTE_KASKADE);
            }

            return map;
        }

        /// <summary>
        /// Alle Puffer des Projekts, die überhaupt von einer Anlage GELADEN werden.
        ///
        /// Dieselbe Bedingung wie in <see cref="Ladeordnung.Ladereihenfolge"/>: Die
        /// Senkenzeile muss ein Puffer-Ziel tragen UND einen Speicher benennen. Eine
        /// halbe Konfiguration („Ziel ohne Puffer" oder umgekehrt) zählt nicht — sonst
        /// bekäme ein Speicher hier eine Ladereihenfolge-Abfrage, die leer zurückkommt.
        ///
        /// <para><b>PAKET A1:</b> Gelesen wird die SENKENLISTE, nicht mehr das
        /// Spaltenpaar <c>WS_ID_Puffer</c>/<c>WS_ID_Puffer2</c>. Ein Speicher, den erst
        /// eine Senke ab Rang 3 lädt, fehlte in dieser Menge — seine Karte behauptete
        /// dann „wird von keiner Anlage geladen".</para>
        ///
        /// Einmal je Auffrischung; Zweck ist der Vorfilter in
        /// <see cref="SpeicherKarteDaten"/> (Begründung dort).
        /// </summary>
        private HashSet<int> GeladenePufferSammeln()
        {
            HashSet<int> geladen = new HashSet<int>();
            if (m_ID_Projekt <= 0) return geladen;

            foreach (Senkenliste liste in WaermesenkeClass.SenkenlistenLadenStill(m_ID_Projekt))
            {
                if (liste == null) continue;

                foreach (Senkenzeile z in liste.Zeilen)
                    if (z != null && z.IstPuffersenke && z.IDPuffer > 0)
                        geladen.Add(z.IDPuffer);
            }

            return geladen;
        }

        /// <summary>
        /// PAKET P1 — <c>T_oben_Mittel</c> je Speicher aus dem JÜNGSTEN Ergebnis des
        /// Projekts; nie <c>null</c>.
        ///
        /// <para><b>Zwei Abfragen statt einer Unterabfrage.</b> Erst der Ergebniskopf
        /// (<c>MAX(ID)</c> je Projekt), dann seine Speicherzeilen. Ein Parameter in der
        /// Unterabfrage ist bei ACE eine bekannte Falle (dieselbe Vorsicht wie in
        /// <c>SchemaMigration</c>, Schritt 25c), und die Kopfabfrage ist ohnehin billig.</para>
        ///
        /// <para><b>Spaltentolerant und still.</b> Gelesen wird über
        /// <c>ErgebnisCtrl.PufferZeilenLesenStill</c> — dieselbe dialogfreie Bauart, die
        /// auch der Referenzlauf benutzt; fehlt die Tabelle, kommt <c>null</c> zurück.
        /// Fehlt die Spalte <c>T_oben_Mittel</c> (Lauf vor Schritt 52) oder steht dort
        /// NULL, bleibt der Speicher aus dem Verzeichnis weg und seine Karte zeigt die
        /// Zeile nicht.</para>
        ///
        /// <para>Einmal je Auffrischung, nicht je Karte — dieselbe Begründung wie bei
        /// <see cref="QuellnutzerSammeln"/>.</para>
        /// </summary>
        private Dictionary<int, double> TObenSammeln()
        {
            Dictionary<int, double> werte = new Dictionary<int, double>();
            if (m_ID_Projekt <= 0) return werte;

            int idErgebnis = StilleDb.Zahl(StilleDb.Scalar(
                "SELECT MAX(ID) FROM [" + ErgebnisCtrl.TAB_KOPF + "] WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, m_ID_Projekt)));
            if (idErgebnis <= 0) return werte;

            System.Data.DataTable dt = ErgebnisCtrl.PufferZeilenLesenStill(idErgebnis);
            if (dt == null || !dt.Columns.Contains(SchemaKatalog.SPALTE_PUFFER_T_OBEN_MITTEL))
                return werte;

            foreach (System.Data.DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID_Pufferspeicher"));
                object v = StilleDb.Feld(r, SchemaKatalog.SPALTE_PUFFER_T_OBEN_MITTEL);
                if (id <= 0 || v == null || v == DBNull.Value || werte.ContainsKey(id)) continue;

                try { werte[id] = Convert.ToDouble(v); }
                catch { /* unlesbarer Wert - dann bleibt die Zeile weg */ }
            }

            return werte;
        }

        /// <summary>Die Anlagen, für die dieser Puffer die Quelle ist; nie <c>null</c>.</summary>
        private List<string> QuelleFuerAnlagen(WaermesenkeClass.PufferInfo p)
        {
            List<string> namen;
            if (p != null && _quellnutzer != null && _quellnutzer.TryGetValue(p.ID, out namen))
                return namen;

            return new List<string>();
        }

        /// <summary>
        /// Herkunft der Betriebstemperaturen eines Puffers — die Vorrangkette aus
        /// Paket 1/4 (Konzept 5.1), die die Engine beim Lesen durchläuft:
        ///
        /// <list type="number">
        ///   <item><description>eigene Werte an <c>Tab_Pufferspeicher</c> — seit Etappe 4
        ///     die führende und seit PAKET A1 die EINZIGE Ablage;</description></item>
        ///   <item><description>die Systemvorgabe des Projekts
        ///     (<c>PufferSpCtrl.SystemVorlauf/-Ruecklauf</c>: kleinster Vorlauf, größter
        ///     Rücklauf über die Wärmeerzeuger).</description></item>
        /// </list>
        ///
        /// <para><b>PAKET A1:</b> Die mittlere Stufe — die Zuordnungszeile
        /// <c>Z_ProjektPufferSp</c> — ist entfallen. Migrationsschritt 51 hat ihre
        /// Temperaturen einmalig an <c>Tab_Pufferspeicher</c> übergeben; wo dort ein Paar
        /// steht, greift Stufe 1, und wo keines steht, stand auch in der Zuordnung
        /// keines.</para>
        ///
        /// Greift nichts davon, bleibt es bei „nicht gepflegt" — die Engine fällt dann
        /// auf ihre Vorgabespreizung zurück, und genau das soll die Karte sagen statt
        /// eine Zahl zu zeigen, die nirgends steht.
        /// </summary>
        private string TemperaturHerkunft(WaermesenkeClass.PufferInfo p,
                                          out int vorlauf, out int ruecklauf)
        {
            vorlauf = 0;
            ruecklauf = 0;
            if (p == null) return MyResource.Resource.PSP_KARTE_TEMP_KEINE;

            if (p.Vorlauf > 0 && p.Ruecklauf > 0)
            {
                vorlauf = p.Vorlauf;
                ruecklauf = p.Ruecklauf;
                return MyResource.Resource.PSP_KARTE_TEMP_EIGEN;
            }

            if (ProjektPuffer.IstTemperaturpaar(_systemVorlauf, _systemRuecklauf))
            {
                vorlauf = _systemVorlauf.Value;
                ruecklauf = _systemRuecklauf.Value;
                return MyResource.Resource.PSP_KARTE_TEMP_SYSTEM;
            }

            return MyResource.Resource.PSP_KARTE_TEMP_KEINE;
        }
    }
}
