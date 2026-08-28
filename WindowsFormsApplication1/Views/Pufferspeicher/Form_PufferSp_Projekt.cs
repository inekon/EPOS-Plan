using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Pufferspeicher-Verwaltung auf PROJEKTEBENE (Konzept 4.3).
    ///
    /// Ein Speicher, der als Wärmequelle oder -senke dienen soll, muss zuvor als
    /// Projekt-Pufferspeicher angelegt sein (Konzept 3.3). Dieser Dialog ist der
    /// ausdrückliche Weg dorthin: Katalogübernahme aus <c>Tab_Pufferspeicher_STAMM</c>
    /// oder freie Eingabe, Pflichtfeld Verwendung, Betriebsparameter, Schwellen und
    /// Entladepriorität — dazu die beiden Kontrollanzeigen „Ladereihenfolge dieses
    /// Speichers" und „Wird als n. von m … entladen".
    ///
    /// NEUBAU, kein Feldzusatz an <see cref="Form_PufferSp_Bearbeiten"/>: jene Maske
    /// arbeitet ausschließlich gegen die STAMM-Tabelle und liest positionsbasiert
    /// <c>row[2]…row[6]</c> (Konzept 4.3, letzter Absatz). Hier wird durchgehend über
    /// Spaltennamen gelesen.
    ///
    /// Die Oberfläche steht seit der Designer-Umstellung in
    /// <c>Form_PufferSp_Projekt.Designer.cs</c>, weiterhin ohne eigene <c>.resx</c>: Alle
    /// sichtbaren Texte kommen aus <c>MyResource</c> und werden in
    /// <see cref="TexteSetzen"/> gesetzt; im Designer stehen nur Platzhalter. Die
    /// Pixelentscheidungen aus den Abnahmebefunden stehen als Kommentarblock in dieser
    /// Datei — Designer-Code trägt keine Kommentare (Muster
    /// <see cref="Form_QuellePufferspeicher"/>).
    ///
    /// WICHTIG: Anlegen, Ändern und Entfernen wirken SOFORT auf die Datenbank — der
    /// Dialog ist eine Verwaltung, kein Formular mit Abbruch. Deshalb schließt er nur mit
    /// „Schließen" (DialogResult.OK); ein „Abbrechen", das nichts zurücknähme, wäre eine
    /// Zusage, die der Dialog nicht halten kann.
    /// </summary>
    public partial class Form_PufferSp_Projekt : Form
    {
        // --- Übergabe ----------------------------------------------------------------

        /// <summary>Projekt, dessen Pufferspeicher verwaltet werden.</summary>
        public int ID_Projekt;

        /// <summary>Vorbelegung bzw. zuletzt bearbeitete Verwendung (Heizung|Brauchwasser).</summary>
        public string Verwendung = WaermesenkeClass.VERWENDUNG_HEIZUNG;

        /// <summary>ID des zuletzt angelegten oder ausgewählten Puffers; 0 = keiner.</summary>
        public int ID_Puffer;

        // --- Innerer Zustand ----------------------------------------------------------
        //
        // Die Steuerelemente stehen in Form_PufferSp_Projekt.Designer.cs.

        private List<WaermesenkeClass.PufferInfo> _projektPuffer =
            new List<WaermesenkeClass.PufferInfo>();
        private DataTable _katalog;

        /// <summary>0 = Neuanlage, sonst die ID des gerade bearbeiteten Puffers.</summary>
        private int _bearbeiteteId;

        private bool _aktualisiert;

        /// <summary>Eintrag des Entladeprioritäts-Dropdowns (0 = automatisch).</summary>
        private class PrioItem
        {
            public int Wert;
            public string Text = "";
            public override string ToString() { return Text; }
        }

        /// <summary>
        /// Eintrag des Verwendungs-Dropdowns (Behebung Befund L0-2).
        ///
        /// <para>
        /// Vorher standen die DB-Werte <c>„Heizung"</c> und <c>„Brauchwasser"</c>
        /// UNMITTELBAR als ComboBox-Einträge in der Liste, und
        /// <c>SelectedItem.ToString()</c> las sie als Steuerwert zurück. Der angezeigte
        /// Text war damit nicht lokalisierbar, ohne zugleich den Persistenzwert zu
        /// verändern — genau die Verwechslung, die die Drei-Schichten-Regel verbietet.
        /// </para>
        ///
        /// Jetzt trägt der Eintrag beides getrennt: <see cref="DbWert"/> geht in die
        /// Datenbank und in jeden Vergleich, <see cref="ToString"/> liefert den
        /// übersetzten Anzeigetext.
        /// </summary>
        private class VerwendungItem
        {
            public string DbWert = "";
            public string Anzeige = "";
            public override string ToString() { return Anzeige; }
        }

        // --- Klassen-Set (Paket K2, Migrationsschritt 49, Konzept 6.1) ----------------
        //
        // Die drei Häkchen sind FÜHREND; die Verwendungs-ComboBox darüber bleibt als
        // Bestandsanzeige stehen und zeigt den abgeleiteten Altwert. Aufbau und Regeln
        // stehen in KlassenSetAufbauen().

        private Label _lblKlassenSet;
        private CheckBox _chkNutzungHeizung;
        private CheckBox _chkNutzungBrauchwasser;
        private CheckBox _chkNutzungProzess;
        private ToolTip _ttKlassenSet;

        /// <summary>Sperre gegen das gegenseitige Nachziehen von ComboBox und Häkchen.</summary>
        private bool _klassenSetSpiegelt;

        // --- Schichtung (Paket P1, Migrationsschritt 53, Konzept 7.2) -----------------
        //
        // Ebenfalls programmatisch; Aufbau und Regeln stehen in SchichtungAufbauen().

        private GroupBox _gbSchichtung;
        private NumericUpDown _nudSchichten;
        private TextBox _tbLadeleistung;
        private TextBox _tbEntladeleistung;
        private TextBox _tbHoehe;
        private TextBox _tbLambda;
        private TextBox _tbTNutzBW;
        private TextBox _tbEntnahmeHeizung;
        private TextBox _tbEntnahmeBW;
        private TextBox _tbEntnahmeProzess;
        private Label _lblEntnahmeHeizung;
        private Label _lblEntnahmeBW;
        private Label _lblEntnahmeProzess;

        /// <summary>Die Steuerelemente, die nur bei <c>N &gt; 1</c> erscheinen.</summary>
        private readonly List<Control> _schichtNurErweitert = new List<Control>();

        /// <summary>Aktueller Zustand der Gruppe: true = erweiterte Zeilen sichtbar.</summary>
        private bool _schichtErweitert;

        /// <summary>
        /// Client-Höhe der Maske OHNE Bildschirmklemmung — die Rechengröße beim
        /// Auf- und Zuklappen der Schichtgruppe.
        ///
        /// <para><b>Warum nicht einfach <c>ClientSize.Height ± Delta</c>.</b> Auf einem
        /// kleinen Schirm hat <see cref="FensterEinpassung"/> das Fenster bereits auf die
        /// Arbeitsfläche geklemmt. Eine Rechnung auf dem GEKLEMMTEN Maß verlöre beim
        /// Zuklappen 150 px, die gar nicht sichtbar waren — das Fenster würde kleiner als
        /// der Bildschirm hergibt und behielte trotzdem seine Bildlaufleisten. Gerechnet
        /// wird deshalb auf dem Sollmaß; die Klemmung setzt anschließend wieder auf, was
        /// tatsächlich passt.</para>
        /// </summary>
        private int _schichtSollHoehe;

        public Form_PufferSp_Projekt()
        {
            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske ist ein FixedDialog mit fest
            // gerechneten Pixelpositionen, und die Anwendung läuft DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). Vor der Designer-Umstellung
            // wurde AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls
            // keine Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();
            TexteSetzen();
            VerwendungslisteFuellen();

            // PAKET K2: die drei Klassen-Set-Häkchen. Programmatisch, nicht im Designer
            // (Konzept 10) - und VOR FensterEinpassung, weil die Maske dabei um eine
            // Zeile wächst und die Einpassung das Entwurfsmaß beim ersten Aufruf misst.
            KlassenSetAufbauen();

            // PAKET P1: die Gruppe „Schichtung und Leistungsgrenzen" - aus demselben
            // Grund programmatisch und ebenfalls vor der Einpassung.
            SchichtungAufbauen();

            FensterEinpassung.Einhaengen(this);
        }

        // ==================================================================
        // Oberfläche — gerettete Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen seit der Designer-Umstellung in
        // Form_PufferSp_Projekt.Designer.cs. Designer-Code trägt keine Kommentare;
        // die Pixelentscheidungen aus den Abnahmebefunden stehen deshalb hier.
        //
        // * ClientSize 700 x 648 (Paket BHKW-Regulär): 32 px höher als bisher (616). Die
        //   Eigenschaftengruppe hat eine vierte Schwellenzeile für den Mindestfüllstand
        //   bekommen; alles darunter ist um denselben Betrag nachgerückt.
        // * _gbDaten, Größe 676 x 232: +32 für die Mindestfüllstand-Zeile.
        // * Um dieselben 32 px nachgerückt sind _gbLaden (12/374), _lblEntladeprio
        //   (16/538), _cbEntladeprio (180/534), _lblEntladeInfo (400/538), _lblStatus
        //   (14/578) sowie die beiden Fußknöpfe (y = 610).
        // * _lblMindestfuellstand (16/193) und _tbSchwelleReserve (260/190) tragen den
        //   MINDESTFÜLLSTAND/NOTRESERVE [%] als viertes Schwellenfeld (Paket
        //   BHKW-Regulär). Eigene Zeile, weil die Zeile der drei Schaltschwellen über die
        //   ganze Gruppenbreite belegt ist.
        //   Der Parameter wirkt AUSSCHLIESSLICH auf die Entladung im BHKW-Pfad (ein BHKW
        //   braucht einen Anlaufvorrat); alle anderen Erzeuger entladen den Speicher
        //   unverändert bis 0. Das Feld steht trotzdem an JEDEM Puffer - der Anwender
        //   weiß beim Anlegen nicht, welcher Erzeuger den Speicher später bedient, und
        //   eine Sichtbarkeitsregel nach Erzeugerart hätte den Wert bei einer späteren
        //   Zuordnung stillschweigend entwertet.
        // * _btnUebernehmen (400/610) und _btnSchliessen (550/610) standen im Bestand als
        //   ClientSize.Width - 300 bzw. - 150; bei ClientSize.Width = 700 sind das genau
        //   diese beiden Werte.
        //
        // DESIGN-POLITUR 21.08.2026
        // Im Designer stehen jetzt die deutschen ECHTTEXTE statt der Feldnamen (auch die
        // sechs Spaltenköpfe samt „#" und die beiden Formatvorlagen von _lblQmax und
        // _lblEntladeInfo). TexteSetzen() bleibt unverändert und überschreibt sie beim
        // Start; _lblQmax und _lblEntladeInfo füllt AnzeigenAktualisieren(), das über
        // SetControls() an JEDER Aufrufstelle vor ShowDialog läuft. _lblStatus bleibt
        // BEWUSST ohne Entwurfstext: Die Fußzeile meldet nur vollzogene Aktionen und ist
        // beim Öffnen leer.
        // Mit den Echttexten sind folgende Überstände aufgefallen und behoben:
        // * _tbBezeichner 300 -> 200 px breit (rechte Kante 480 -> 380). Bei 300 px lag
        //   _lblGesamtvolumen (x = 380) VOLLSTÄNDIG hinter dem Eingabefeld — das Feld
        //   steht in der Z-Reihenfolge davor, die Beschriftung war unsichtbar.
        // * _lblGesamtvolumen und _lblBereitschaftsverluste auf x = 388, _tbVolumen und
        //   _tbVerluste auf x = 556 (vorher 380 bzw. 540). „Bereitschaftsverl.
        //   [kWh/24h]:" misst 159 px (Segoe UI 9 pt) und stieß bei x = 380 bis 539 vor —
        //   1 px vor dem Eingabefeld. Jetzt 9 px Abstand; die rechte Kante der Felder
        //   liegt bei 666 und damit noch innerhalb der 676 px breiten Gruppe.
        // * _tbSchwelleReserve 260 -> 284. Deutsch reicht 260 (Beschriftung 184 px),
        //   Englisch nicht: „Minimum charge level/emergency reserve [%]:" misst 257 px.
        // * _btnNeu/_btnEntfernen/_btnKatalog auf einheitlich 214 x 30 (vorher x 26) an
        //   y = 22/58/94 — 6 px Abstand. _lbProjekt wächst mit auf 420 x 102, damit Liste
        //   und Knopfleiste gemeinsam bei y = 124 enden; _gbListe 676 x 122 -> 676 x 134
        //   (7 px bis zum Gruppenrahmen).
        // * Um die daraus entstehenden 12 px nachgerückt sind _gbDaten (12/148), _gbLaden
        //   (12/386), _lblEntladeprio (16/550), _cbEntladeprio (180/546), _lblEntladeInfo
        //   (400/550), _lblStatus (14/590) und die beiden Fußknöpfe (y = 622).
        // * Fußknöpfe 130 x 28 -> 130 x 30 (Mindestmaß der Politur; die 130 px Breite
        //   bleibt). Unterkante 652, ClientSize 700 x 648 -> 700 x 662 für die 10 px Luft
        //   darunter. Die x-Werte 400/550 und damit die rechte Kante bleiben unberührt.
        // * _lblStatus 430 -> 380 px breit: Die Fußzeile reichte bis x = 444 und lag
        //   damit über der Knopfspalte (ab x = 400); jetzt endet sie bei 394, also 6 px
        //   davor.
        // Knopf-Semantik NICHT angetastet: Der Dialog bleibt eine Verwaltung mit
        // Sofortwirkung — „Übernehmen" (AcceptButton) und „Schließen" (CancelButton,
        // DialogResult.OK), bewusst ohne „Abbrechen" (siehe Klassenkommentar oben).
        //
        // PAKET K2 und P1 rücken die Maske ein weiteres Mal auseinander, aber NICHT im
        // Designer: KlassenSetAufbauen() schiebt für die Häkchenzeile alles unterhalb der
        // Verwendungszeile um 30 px nach unten, SchichtungAufbauen() setzt die Gruppe
        // „Schichtung und Leistungsgrenzen" unter die Eigenschaften und schiebt alles
        // darunter um deren Höhe. Beide rechnen mit denselben Pixeln wie die Liste oben —
        // die Zahlen stehen als Konstanten bei den jeweiligen Aufbaumethoden. Damit
        // bleibt der Designer-Entwurf unangetastet und ein späterer Blick in den Designer
        // zeigt weiter den Bestand.

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL;

            // --- Bestand --------------------------------------------------------------
            _gbListe.Text = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL;
            _btnNeu.Text = MyResource.Resource.PSP_BTN_NEUER_PUFFERSPEICHER;
            _btnEntfernen.Text = MyResource.Resource.PSP_BTN_ENTFERNEN;
            _btnKatalog.Text = MyResource.Resource.PSP_BTN_KATALOG_ANSEHEN;

            // --- Eigenschaften --------------------------------------------------------
            _gbDaten.Text = MyResource.Resource.PSP_GRUPPE_EIGENSCHAFTEN;
            _lblAusKatalog.Text = MyResource.Resource.PSP_LABEL_AUS_KATALOG;
            _lblBezeichner.Text = MyResource.Resource.PSP_LABEL_BEZEICHNER;
            _lblVerwendung.Text = MyResource.Resource.PSP_LABEL_VERWENDUNG;
            _lblGesamtvolumen.Text = MyResource.Resource.PSP_LABEL_GESAMTVOLUMEN;
            _lblBereitschaftsverluste.Text = MyResource.Resource.PSP_LABEL_BEREITSCHAFTSVERLUSTE;
            _lblVorlauf.Text = MyResource.Resource.PSP_LABEL_VORLAUF;
            _lblRuecklauf.Text = MyResource.Resource.PSP_LABEL_RUECKLAUF;
            _lblEinschaltschwelle.Text = MyResource.Resource.PSP_LABEL_EINSCHALTSCHWELLE;
            _lblAbschaltschwelle.Text = MyResource.Resource.PSP_LABEL_ABSCHALTSCHWELLE;
            _lblSchwelleNachrangig.Text = MyResource.Resource.PSP_LABEL_SCHWELLE_NACHRANGIG;
            _lblMindestfuellstand.Text = MyResource.Resource.PSP_LABEL_MINDESTFUELLSTAND;

            // --- Ladereihenfolge ------------------------------------------------------
            _gbLaden.Text = MyResource.Resource.PSP_GRUPPE_LADEREIHENFOLGE;
            // Die laufende Nummer ist ein Symbol, kein übersetzbarer Satz; sie steht
            // trotzdem hier, damit ALLE Spaltenüberschriften an einer Stelle stehen.
            _colNr.Text = "#";
            _colAnlage.Text = MyResource.Resource.SIM_SPALTE_ANLAGE;
            _colErzeuger.Text = MyResource.Resource.SIM_ERZEUGERNAME_ALLGEMEIN;
            _colSenke.Text = MyResource.Resource.SIM_SPALTE_SENKE;
            _colLadeprio.Text = MyResource.Resource.PSP_SPALTE_LADEPRIO;
            _colLaedtBis.Text = MyResource.Resource.PSP_SPALTE_LAEDT_BIS;

            // --- Entladepriorität -----------------------------------------------------
            _lblEntladeprio.Text = MyResource.Resource.PSP_LABEL_ENTLADEPRIORITAET;
            _btnUebernehmen.Text = MyResource.Resource.PSP_BTN_UEBERNEHMEN;
            _btnSchliessen.Text = MyResource.Resource.PSP_BTN_SCHLIESSEN;
        }

        /// <summary>
        /// Füllt das Verwendungs-Dropdown. Steht hier statt im Designer, weil
        /// <see cref="VerwendungItem"/> keine serialisierbare Entwurfszeit-Zutat ist.
        /// </summary>
        private void VerwendungslisteFuellen()
        {
            // Befund L0-2: DB-Wert und Anzeigetext getrennt (VerwendungItem).
            //
            // ETAPPE D5b, VORGEZOGEN (Nacharbeit I-K2-4): Der KOMBISPEICHER als dritte,
            // reguläre Option. Ohne sie zeigte die Bearbeitungsmaske für einen per
            // Datenbank angelegten Kombi-Puffer „Heizung" (kein Treffer in
            // VerwendungWaehlen -> Rückfall auf Index 0) und schrieb ihn beim nächsten
            // „Übernehmen" still auf Verwendung = 'Heizung' zurück - stiller Datenverlust
            // an genau der Konfiguration, die D5a einführt. Die Rückfrage
            // VerwendungswechselBestaetigt greift dabei nicht, weil sie nur bei bereits
            // REFERENZIERTEN Speichern anschlägt.
            _cbVerwendung.Items.AddRange(new object[]
            {
                new VerwendungItem
                {
                    DbWert = WaermesenkeClass.VERWENDUNG_HEIZUNG,
                    Anzeige = MyResource.Resource.PSP_VERWENDUNG_HEIZUNG_ANZEIGE
                },
                new VerwendungItem
                {
                    DbWert = WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                    Anzeige = MyResource.Resource.PSP_VERWENDUNG_BRAUCHWASSER_ANZEIGE
                },
                new VerwendungItem
                {
                    DbWert = WaermesenkeClass.VERWENDUNG_KOMBI,
                    Anzeige = MyResource.Resource.PSP_VERWENDUNG_KOMBI_ANZEIGE
                }
            });
        }

        // ==================================================================
        // Klassen-Set (Paket K2, Migrationsschritt 49, Konzept 6.1)
        // ==================================================================
        //
        // WAS SICH ÄNDERT. Ein Pufferspeicher trug bisher EINE Verwendung: Heizung,
        // Brauchwasser oder „Kombi" für beides. Jetzt trägt er ein SET aus drei
        // unabhängigen Klassen - auch {Heizung, Prozesswärme} oder {H, B, P} sind
        // möglich, und „Kombi" ist nur noch der Anzeigename des Sets {H, B}.
        //
        // WARUM DIE COMBOBOX BLEIBT. Der risikoärmere Weg (Konzept 10 nennt die
        // Häkchen „statt der ComboBox"): Bis Paket S2 lesen Anzeigen, Auswahllisten und
        // Meldungen weiter Tab_Pufferspeicher.Verwendung - die Puffer-Liste dieses
        // Dialogs, der Senkendialog, die Speicherkarte, die Entladereihenfolge. Wäre
        // die ComboBox schon jetzt weg, verschwände auch die Anzeige dieses Altwerts,
        // während er noch wirkt. Sie bleibt deshalb sichtbar und funktionsfähig; die
        // Häkchen spiegeln das Set, und beim Speichern wird IMMER BEIDES geschrieben:
        // die Flags als führende Wahrheit, die Verwendung als abgeleiteter Altwert.
        //
        // DIE INTERAKTIONSREGEL. Beide Richtungen ziehen einander nach:
        //   ComboBox geändert -> Häkchen werden abgeleitet gesetzt (Heizung -> {H},
        //     Brauchwasser -> {B}, Kombi -> {H, B}).
        //   Häkchen geändert  -> ComboBox geht auf den passenden Altwert. Für Sets
        //     OHNE Alt-Entsprechung ({P}, {H, P}, {B, P}, {H, B, P}) gibt es keinen -
        //     dann steht dort der NÄCHSTLIEGENDE Wert, und ein Kurzhinweis am
        //     Steuerelement sagt, dass die Häkchen führen.
        // _klassenSetSpiegelt verhindert dabei das gegenseitige Aufschaukeln.
        //
        // PFLICHT. Mindestens ein Häkchen (Konzept 6.1) - ein Speicher, den niemand
        // entlädt, wäre sinnlos. Geprüft wird beim Übernehmen, nicht beim Klicken:
        // Wer von {H} auf {B} umstellt, muss zwischendurch durch das leere Set gehen.

        /// <summary>Höhe der neu eingefügten Häkchenzeile [px].</summary>
        private const int KLASSENSET_ZEILENHOEHE = 30;

        /// <summary>
        /// Oberkante, ab der die Steuerelemente der Eigenschaftengruppe nachrücken.
        /// Die Verwendungszeile liegt bei 86/90, die Vorlaufzeile bei 121/124 — der
        /// Schnitt liegt dazwischen.
        /// </summary>
        private const int KLASSENSET_SCHNITT = 110;

        /// <summary>Oberkante der Häkchen in der neuen Zeile.</summary>
        private const int KLASSENSET_Y = 120;

        /// <summary>Linke Kante der Häkchenreihe — dieselbe Spalte wie alle Eingabefelder.</summary>
        private const int KLASSENSET_X = 180;

        /// <summary>Abstand zwischen zwei Häkchen [px].</summary>
        private const int KLASSENSET_ABSTAND = 14;

        /// <summary>
        /// Baut die Häkchenzeile PROGRAMMATISCH auf und schafft ihr Platz.
        ///
        /// <para>Die Maske ist ein <c>FixedDialog</c> mit fest gerechneten
        /// Pixelpositionen (siehe Geometrieblock oben) und läuft DpiUnaware. Eine neue
        /// Zeile heißt deshalb: alles unterhalb um <see cref="KLASSENSET_ZEILENHOEHE"/>
        /// nachrücken, die Eigenschaftengruppe und die Fensterhöhe um denselben Betrag
        /// wachsen lassen. Genau das tut diese Methode — in derselben Rechnung, mit der
        /// die Abnahmebefunde die Zeilen bisher von Hand verschoben haben.</para>
        ///
        /// <para>Die Beschriftungen der drei Häkchen kommen aus den KANAL-Ressourcen und
        /// nicht aus den <c>PSP_VERWENDUNG_*</c>-Texten: Gefragt ist hier, welche KANÄLE
        /// der Speicher bedient, und diese Ressourcenfamilie ist genau dafür da und in
        /// beiden Sprachen vollständig (einschließlich Prozesswärme, wo es keinen
        /// Verwendungs-Text gibt).</para>
        /// </summary>
        private void KlassenSetAufbauen()
        {
            // --- 1. Platz schaffen ----------------------------------------------------
            foreach (Control c in _gbDaten.Controls)
                if (c.Top > KLASSENSET_SCHNITT) c.Top += KLASSENSET_ZEILENHOEHE;

            _gbDaten.Height += KLASSENSET_ZEILENHOEHE;

            foreach (Control c in this.Controls)
                if (c != _gbDaten && c.Top > _gbDaten.Top) c.Top += KLASSENSET_ZEILENHOEHE;

            this.ClientSize = new Size(this.ClientSize.Width,
                                       this.ClientSize.Height + KLASSENSET_ZEILENHOEHE);

            // --- 2. Steuerelemente ----------------------------------------------------
            _lblKlassenSet = new Label();
            _lblKlassenSet.Name = "_lblKlassenSet";
            _lblKlassenSet.AutoSize = true;
            _lblKlassenSet.Location = new Point(16, KLASSENSET_Y + 2);
            _lblKlassenSet.Text = MyResource.Resource.PSP_LABEL_KLASSENSET;

            _chkNutzungHeizung =
                KlassenSetHaekchen("_chkNutzungHeizung", MyResource.Resource.KANAL_HEIZUNG_ANZEIGE);
            _chkNutzungBrauchwasser =
                KlassenSetHaekchen("_chkNutzungBrauchwasser", MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE);
            _chkNutzungProzess =
                KlassenSetHaekchen("_chkNutzungProzess", MyResource.Resource.KANAL_PROZESS_ANZEIGE);

            _gbDaten.Controls.Add(_lblKlassenSet);
            _gbDaten.Controls.Add(_chkNutzungHeizung);
            _gbDaten.Controls.Add(_chkNutzungBrauchwasser);
            _gbDaten.Controls.Add(_chkNutzungProzess);

            // Nebeneinander, jedes so breit wie sein Text - die deutsche und die
            // englische Beschriftung sind unterschiedlich lang, feste Breiten schnitten
            // in einer der beiden Sprachen ab.
            int x = KLASSENSET_X;
            foreach (CheckBox c in new[] { _chkNutzungHeizung, _chkNutzungBrauchwasser, _chkNutzungProzess })
            {
                c.Location = new Point(x, KLASSENSET_Y);
                x += c.PreferredSize.Width + KLASSENSET_ABSTAND;
            }

            // Tabreihenfolge: unmittelbar hinter die Verwendungs-ComboBox. Alle
            // Steuerelemente dieser Maske tragen TabIndex 0, die Reihenfolge ergibt sich
            // damit aus dem Platz in der Kindliste.
            int nachVerwendung = _gbDaten.Controls.GetChildIndex(_cbVerwendung) + 1;
            _gbDaten.Controls.SetChildIndex(_lblKlassenSet, nachVerwendung);
            _gbDaten.Controls.SetChildIndex(_chkNutzungHeizung, nachVerwendung + 1);
            _gbDaten.Controls.SetChildIndex(_chkNutzungBrauchwasser, nachVerwendung + 2);
            _gbDaten.Controls.SetChildIndex(_chkNutzungProzess, nachVerwendung + 3);

            // --- 3. Verdrahtung -------------------------------------------------------
            // Der Kurzhinweis kommt in den Komponentenbehälter des Formulars, damit ihn
            // dessen Dispose mit abräumt. Der Designer hat ihn nie angelegt (die Maske
            // führte bisher keine Komponente), also entsteht er hier.
            if (components == null) components = new System.ComponentModel.Container();
            _ttKlassenSet = new ToolTip(components);
            _ttKlassenSet.AutoPopDelay = 10000;

            _cbVerwendung.SelectedIndexChanged += Verwendung_Geaendert;
            _chkNutzungHeizung.CheckedChanged += KlassenSet_Geaendert;
            _chkNutzungBrauchwasser.CheckedChanged += KlassenSet_Geaendert;
            _chkNutzungProzess.CheckedChanged += KlassenSet_Geaendert;

            // Anfangszustand: die Vorbelegung der ComboBox (Heizung) auf die Häkchen.
            KlassenSetSetzen(PufferSpCtrl.KlassenSetAusVerwendung(GewaehlteVerwendung()));
        }

        private static CheckBox KlassenSetHaekchen(string name, string text)
        {
            CheckBox c = new CheckBox();
            c.Name = name;
            c.AutoSize = true;
            c.Text = text;
            return c;
        }

        /// <summary>Das an den Häkchen abgelesene Set — die führende Wahrheit der Maske.</summary>
        private PufferSpCtrl.KlassenSet GewaehltesKlassenSet()
        {
            return new PufferSpCtrl.KlassenSet(_chkNutzungHeizung.Checked,
                                               _chkNutzungBrauchwasser.Checked,
                                               _chkNutzungProzess.Checked);
        }

        /// <summary>
        /// Setzt die Häkchen OHNE die ComboBox nachzuziehen (die Sperre
        /// <see cref="_klassenSetSpiegelt"/> unterbricht die Gegenrichtung) und frischt
        /// den Kurzhinweis auf.
        /// </summary>
        private void KlassenSetSetzen(PufferSpCtrl.KlassenSet set)
        {
            if (set == null || _chkNutzungHeizung == null) return;

            _klassenSetSpiegelt = true;
            try
            {
                _chkNutzungHeizung.Checked = set.Heizung;
                _chkNutzungBrauchwasser.Checked = set.Brauchwasser;
                _chkNutzungProzess.Checked = set.Prozess;
            }
            finally
            {
                _klassenSetSpiegelt = false;
            }

            KlassenSetHinweisAktualisieren();
        }

        /// <summary>
        /// Der Kurzhinweis an der ComboBox: Nur für Sets, die der Altwert nicht
        /// verlustfrei abbildet, steht dort „führend sind die Häkchen". Für {H}, {B} und
        /// {H, B} bleibt er leer — dort sagt die ComboBox die volle Wahrheit.
        /// </summary>
        private void KlassenSetHinweisAktualisieren()
        {
            if (_ttKlassenSet == null) return;

            PufferSpCtrl.KlassenSet set = GewaehltesKlassenSet();
            string hinweis = set.HatAltEntsprechung
                ? ""
                : MyResource.Resource.PSP_HINWEIS_KLASSENSET_OHNE_ALTWERT;

            _ttKlassenSet.SetToolTip(_cbVerwendung, hinweis);
            _ttKlassenSet.SetToolTip(_lblKlassenSet, hinweis);
        }

        /// <summary>ComboBox geändert → die Häkchen werden abgeleitet gesetzt.</summary>
        private void Verwendung_Geaendert(object sender, EventArgs e)
        {
            if (_klassenSetSpiegelt) return;
            KlassenSetSetzen(PufferSpCtrl.KlassenSetAusVerwendung(GewaehlteVerwendung()));
        }

        /// <summary>
        /// Häkchen geändert → die ComboBox geht auf den abgeleiteten Altwert.
        ///
        /// Das LEERE Set wird hier NICHT abgefangen: Wer von „nur Heizung" auf „nur
        /// Brauchwasser" umstellt, muss zwischendurch durch das leere Set gehen. Eine
        /// Meldung an dieser Stelle machte jede zweite Umstellung zum Hindernislauf; die
        /// Pflichtprüfung sitzt deshalb im Übernehmen (<see cref="EingabenLesen"/>).
        /// </summary>
        private void KlassenSet_Geaendert(object sender, EventArgs e)
        {
            if (_klassenSetSpiegelt) return;

            string vorher = GewaehlteVerwendung();

            _klassenSetSpiegelt = true;
            try { VerwendungWaehlen(GewaehltesKlassenSet().Verwendung); }
            finally { _klassenSetSpiegelt = false; }

            KlassenSetHinweisAktualisieren();

            // Hat sich der Altwert mitgeändert, hat die ComboBox über den
            // Designer-Behandler Daten_Geaendert bereits aufgefrischt - sonst hier
            // nachholen. Die Reihenfolge-Anzeigen fragen die Datenbank ab; sie zweimal
            // je Klick zu holen, wäre der teurere Weg zum selben Bild.
            if (!_aktualisiert && string.Equals(vorher, GewaehlteVerwendung(), StringComparison.Ordinal))
                AnzeigenAktualisieren();

            // PAKET P1: Die Entnahmehöhen werden nur für die Kanäle des Sets angeboten -
            // eine Entnahmehöhe für einen Kanal, den der Speicher nicht bedient, wäre
            // eine Angabe ohne Wirkung.
            EntnahmezeileOrdnen();
        }

        // ==================================================================
        // Schichtung und Leistungsgrenzen (Paket P1, Schritt 53, Konzept 7.2)
        // ==================================================================
        //
        // WAS DIE GRUPPE LEISTET. Ein Pufferspeicher war bisher EIN Vorrat mit einer
        // Temperatur. Ab N > 1 rechnet die Engine ihn als N übereinanderliegende
        // Schichten (Konzept 7.1) - oben warm, unten kalt, mit idealer Einschichtung
        // beim Laden und Verdrängung am Anschluss beim Entladen. Die Gruppe pflegt die
        // dafür nötigen Größen; alle sind so vorbelegt, dass ein Speicher ohne Eingabe
        // exakt wie bisher rechnet.
        //
        // ZWEI SICHTBARKEITSSTUFEN.
        //   IMMER   Schichtenzahl, Lade- und Entladeleistung. Die beiden Leistungsgrenzen
        //           wirken AUCH bei N = 1 (Konzept 6.3) und dürfen deshalb nicht hinter
        //           der Schichtung verschwinden.
        //   AB N>1  Höhe, λ_eff, Nutztemperatur Brauchwasser, Entnahmehöhen. Sie
        //           beschreiben die Schichtebene und haben bei einem Ein-Zonen-Speicher
        //           keine Bedeutung - sichtbar wären sie dort nur Fragen ohne Antwort.
        //
        // DIE MASKE WÄCHST UND SCHRUMPFT dabei um die Höhendifferenz der beiden Stufen,
        // wie schon bei der Häkchenzeile aus K2: FixedDialog mit fest gerechneten
        // Pixelpositionen, DpiUnaware. Nach jeder Änderung läuft FensterEinpassung.Anwenden
        // erneut - auf einem kleinen Schirm bekommt der Dialog dadurch Bildlaufleisten,
        // auf einem großen ändert sich nichts (die Klasse ist dort wirkungslos).
        //
        // DER 55-°C-VORSCHLAG (Konzept 7.2/7.5). Wechselt der Anwender auf N > 1 und ist
        // die Nutztemperatur leer, trägt der Dialog EINMALIG 55 °C ein - der Wert, ab dem
        // ein Kombispeicher eine echte Brauchwasser-Bereitschaftszone bekommt. Es ist ein
        // VORSCHLAG und keine stille Festschreibung: Der Spalten-Default bleibt NULL
        // (= Rücklauf), und wer das Feld wieder leert, bekommt genau das.

        /// <summary>Waagerechter Abstand zwischen zwei Gruppen [px].</summary>
        private const int SCHICHT_ABSTAND = 12;

        /// <summary>Linke Kante der Beschriftungen in der Gruppe.</summary>
        private const int SCHICHT_X_LABEL = 16;

        /// <summary>
        /// Linke Kante der Eingabefelder. WEITER RECHTS als in der Eigenschaftengruppe
        /// (180): „Nutztemperatur Brauchwasser [°C]:" misst rund 200 px und stieße bei 180
        /// unter das Feld — derselbe Fehler, den die Designpolitur bei
        /// <c>_lblGesamtvolumen</c> behoben hat.
        /// </summary>
        private const int SCHICHT_X_FELD = 250;

        /// <summary>Linke Kante der leisen Hinweistexte rechts neben den Feldern.</summary>
        private const int SCHICHT_X_HINWEIS = 336;

        private const int SCHICHT_FELD_BREITE = 70;
        private const int SCHICHT_ENTNAHME_BREITE = 50;

        /// <summary>Zeilenraster der Gruppe [px].</summary>
        private const int SCHICHT_ZEILE = 30;

        private const int SCHICHT_Y_ANZAHL = 24;
        private const int SCHICHT_Y_LADELEISTUNG = SCHICHT_Y_ANZAHL + SCHICHT_ZEILE;
        private const int SCHICHT_Y_ENTLADELEISTUNG = SCHICHT_Y_LADELEISTUNG + SCHICHT_ZEILE;

        private const int SCHICHT_Y_HOEHE = SCHICHT_Y_ENTLADELEISTUNG + 34;
        private const int SCHICHT_Y_LAMBDA = SCHICHT_Y_HOEHE + SCHICHT_ZEILE;
        private const int SCHICHT_Y_TNUTZ = SCHICHT_Y_LAMBDA + SCHICHT_ZEILE;
        private const int SCHICHT_Y_ENTNAHME_KOPF = SCHICHT_Y_TNUTZ + 32;
        private const int SCHICHT_Y_ENTNAHME = SCHICHT_Y_ENTNAHME_KOPF + 24;

        /// <summary>Höhe der Gruppe bei N = 1 (nur die drei stets sichtbaren Zeilen).</summary>
        private const int SCHICHT_HOEHE_KOMPAKT = SCHICHT_Y_ENTLADELEISTUNG + 38;

        /// <summary>Höhe der Gruppe bei N &gt; 1 (alle Zeilen).</summary>
        private const int SCHICHT_HOEHE_ERWEITERT = SCHICHT_Y_ENTNAHME + 38;

        /// <summary>Vorschlagswert der Nutztemperatur Brauchwasser [°C] (Konzept 7.2).</summary>
        private const int T_NUTZ_VORSCHLAG = 55;

        /// <summary>
        /// Baut die Gruppe „Schichtung und Leistungsgrenzen" PROGRAMMATISCH auf und
        /// schafft ihr Platz — dieselbe Rechnung wie <see cref="KlassenSetAufbauen"/>,
        /// nur um eine ganze Gruppe statt um eine Zeile.
        /// </summary>
        private void SchichtungAufbauen()
        {
            // --- 1. Gruppe anlegen (Lage NOCH aus den unverschobenen Maßen) -----------
            _gbSchichtung = new GroupBox();
            _gbSchichtung.Name = "_gbSchichtung";
            _gbSchichtung.Text = MyResource.Resource.PSP_GRUPPE_SCHICHTUNG;
            _gbSchichtung.Location = new Point(_gbDaten.Left, _gbDaten.Bottom + SCHICHT_ABSTAND);
            _gbSchichtung.Size = new Size(_gbDaten.Width, SCHICHT_HOEHE_KOMPAKT);

            // AUSDRÜCKLICHER Anker statt des Vorgabewerts: Bekommt der Dialog auf einem
            // kleinen Schirm AutoScroll (FensterEinpassung), verschiebt WinForms beim
            // Rollen nur direkte Kinder MIT Handle - ein handle-loses Kind mit
            // Default-Anker bliebe stehen und verdeckte seine Nachbarn (Befundmuster
            // d49075e). Ein gesetzter Anker heilt das, weil der Layoutlauf das
            // Steuerelement dann gegen das DisplayRectangle neu positioniert.
            _gbSchichtung.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // --- 2. Platz schaffen ---------------------------------------------------
            int platz = SCHICHT_HOEHE_KOMPAKT + SCHICHT_ABSTAND;

            foreach (Control c in this.Controls)
                if (c.Top > _gbDaten.Top) c.Top += platz;

            this.ClientSize = new Size(this.ClientSize.Width, this.ClientSize.Height + platz);
            _schichtSollHoehe = this.ClientSize.Height;   // Sollmaß, noch ungeklemmt
            this.Controls.Add(_gbSchichtung);

            // Tabreihenfolge: unmittelbar hinter die Eigenschaftengruppe. Alle
            // Steuerelemente dieser Maske tragen TabIndex 0, die Reihenfolge ergibt sich
            // damit aus dem Platz in der Kindliste (Muster KlassenSetAufbauen).
            this.Controls.SetChildIndex(_gbSchichtung,
                                        this.Controls.GetChildIndex(_gbDaten) + 1);

            // --- 3. Stets sichtbare Zeilen -------------------------------------------
            SchichtZeile(MyResource.Resource.PSP_LABEL_SCHICHTEN, SCHICHT_Y_ANZAHL,
                         MyResource.Resource.PSP_HINWEIS_SCHICHTEN, false);

            _nudSchichten = new NumericUpDown();
            _nudSchichten.Name = "_nudSchichten";
            _nudSchichten.Minimum = PufferSpModel.SCHICHTEN_DEFAULT;
            _nudSchichten.Maximum = PufferSpModel.SCHICHTEN_MAX;
            _nudSchichten.Value = PufferSpModel.SCHICHTEN_DEFAULT;
            _nudSchichten.Location = new Point(SCHICHT_X_FELD, SCHICHT_Y_ANZAHL);
            _nudSchichten.Size = new Size(60, 22);
            _nudSchichten.ValueChanged += Schichten_Geaendert;
            _gbSchichtung.Controls.Add(_nudSchichten);

            SchichtZeile(MyResource.Resource.PSP_LABEL_LADELEISTUNG_MAX,
                         SCHICHT_Y_LADELEISTUNG,
                         MyResource.Resource.PSP_HINWEIS_LEISTUNG_UNBEGRENZT, false);
            _tbLadeleistung = SchichtFeld("_tbLadeleistung", SCHICHT_Y_LADELEISTUNG,
                                          SCHICHT_FELD_BREITE, false);

            SchichtZeile(MyResource.Resource.PSP_LABEL_ENTLADELEISTUNG_MAX,
                         SCHICHT_Y_ENTLADELEISTUNG,
                         MyResource.Resource.PSP_HINWEIS_LEISTUNG_UNBEGRENZT, false);
            _tbEntladeleistung = SchichtFeld("_tbEntladeleistung", SCHICHT_Y_ENTLADELEISTUNG,
                                             SCHICHT_FELD_BREITE, false);

            // --- 4. Zeilen ab N > 1 ---------------------------------------------------
            SchichtZeile(MyResource.Resource.PSP_LABEL_HOEHE, SCHICHT_Y_HOEHE,
                         MyResource.Resource.PSP_HINWEIS_HOEHE, true);
            _tbHoehe = SchichtFeld("_tbHoehe", SCHICHT_Y_HOEHE, SCHICHT_FELD_BREITE, true);

            SchichtZeile(MyResource.Resource.PSP_LABEL_LAMBDA_EFF, SCHICHT_Y_LAMBDA,
                         MyResource.Resource.PSP_HINWEIS_LAMBDA_EFF, true);
            _tbLambda = SchichtFeld("_tbLambda", SCHICHT_Y_LAMBDA, SCHICHT_FELD_BREITE, true);

            SchichtZeile(MyResource.Resource.PSP_LABEL_T_NUTZ_BW, SCHICHT_Y_TNUTZ,
                         MyResource.Resource.PSP_HINWEIS_T_NUTZ_BW, true);
            _tbTNutzBW = SchichtFeld("_tbTNutzBW", SCHICHT_Y_TNUTZ, SCHICHT_FELD_BREITE, true);

            SchichtLabel("_lblEntnahmeKopf", MyResource.Resource.PSP_LABEL_ENTNAHMEHOEHEN,
                         SCHICHT_X_LABEL, SCHICHT_Y_ENTNAHME_KOPF, false, true);

            _lblEntnahmeHeizung = SchichtLabel("_lblEntnahmeHeizung",
                                               MyResource.Resource.KANAL_HEIZUNG_ANZEIGE + ":",
                                               0, SCHICHT_Y_ENTNAHME + 3, false, true);
            _tbEntnahmeHeizung = SchichtFeld("_tbEntnahmeHeizung", SCHICHT_Y_ENTNAHME,
                                             SCHICHT_ENTNAHME_BREITE, true);

            _lblEntnahmeBW = SchichtLabel("_lblEntnahmeBW",
                                          MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE + ":",
                                          0, SCHICHT_Y_ENTNAHME + 3, false, true);
            _tbEntnahmeBW = SchichtFeld("_tbEntnahmeBW", SCHICHT_Y_ENTNAHME,
                                        SCHICHT_ENTNAHME_BREITE, true);

            _lblEntnahmeProzess = SchichtLabel("_lblEntnahmeProzess",
                                               MyResource.Resource.KANAL_PROZESS_ANZEIGE + ":",
                                               0, SCHICHT_Y_ENTNAHME + 3, false, true);
            _tbEntnahmeProzess = SchichtFeld("_tbEntnahmeProzess", SCHICHT_Y_ENTNAHME,
                                             SCHICHT_ENTNAHME_BREITE, true);

            // --- 5. Anfangszustand ----------------------------------------------------
            foreach (Control c in _schichtNurErweitert) c.Visible = false;
            _schichtErweitert = false;
            EntnahmezeileOrdnen();
        }

        /// <summary>Beschriftung und leiser Hinweistext einer Zeile der Schichtgruppe.</summary>
        private void SchichtZeile(string text, int y, string hinweis, bool nurErweitert)
        {
            SchichtLabel(null, text, SCHICHT_X_LABEL, y + 3, false, nurErweitert);

            if (!string.IsNullOrEmpty(hinweis))
                SchichtLabel(null, hinweis, SCHICHT_X_HINWEIS, y + 3, true, nurErweitert);
        }

        /// <summary>Ein Label der Schichtgruppe; <paramref name="leise"/> = Hinweistext.</summary>
        private Label SchichtLabel(string name, string text, int x, int y, bool leise,
                                   bool nurErweitert)
        {
            Label l = new Label();
            if (!string.IsNullOrEmpty(name)) l.Name = name;
            l.AutoSize = true;
            l.Text = text;
            l.Location = new Point(x, y);
            if (leise) l.ForeColor = SystemColors.GrayText;

            _gbSchichtung.Controls.Add(l);
            if (nurErweitert) _schichtNurErweitert.Add(l);
            return l;
        }

        /// <summary>Ein Eingabefeld der Schichtgruppe in der Feldspalte.</summary>
        private TextBox SchichtFeld(string name, int y, int breite, bool nurErweitert)
        {
            TextBox t = new TextBox();
            t.Name = name;
            t.Location = new Point(SCHICHT_X_FELD, y);
            t.Size = new Size(breite, 22);

            _gbSchichtung.Controls.Add(t);
            if (nurErweitert) _schichtNurErweitert.Add(t);
            return t;
        }

        /// <summary>
        /// Ordnet die Entnahmehöhen-Zeile: Angeboten werden NUR die Kanäle des aktuellen
        /// Klassen-Sets, nebeneinander und jeder so breit, wie seine Beschriftung ist.
        ///
        /// <para>Feste Spaltenpositionen gingen hier nicht: Die drei Kanalnamen sind in
        /// den beiden Sprachen unterschiedlich lang („Brauchwasser" gegen „Domestic hot
        /// water"), und welche überhaupt erscheinen, entscheidet das Klassen-Set — beides
        /// steht erst zur Laufzeit fest.</para>
        ///
        /// <para>Die Felder AUSGEBLENDETER Kanäle behalten ihren geladenen Wert und werden
        /// beim Speichern unverändert zurückgeschrieben. Wer den Prozesskanal
        /// vorübergehend abwählt, verliert dessen Entnahmehöhe also nicht.</para>
        /// </summary>
        private void EntnahmezeileOrdnen()
        {
            if (_gbSchichtung == null || _lblEntnahmeHeizung == null) return;

            PufferSpCtrl.KlassenSet set = GewaehltesKlassenSet();

            int x = SCHICHT_X_LABEL + 16;
            x = EntnahmePaar(_lblEntnahmeHeizung, _tbEntnahmeHeizung, set.Heizung, x);
            x = EntnahmePaar(_lblEntnahmeBW, _tbEntnahmeBW, set.Brauchwasser, x);
            EntnahmePaar(_lblEntnahmeProzess, _tbEntnahmeProzess, set.Prozess, x);
        }

        /// <summary>Ein Kanalpaar der Entnahmezeile setzen; liefert die nächste freie x-Kante.</summary>
        private int EntnahmePaar(Label label, TextBox feld, bool imSet, int x)
        {
            bool sichtbar = imSet && _schichtErweitert;

            label.Visible = sichtbar;
            feld.Visible = sichtbar;
            if (!imSet) return x;

            label.Location = new Point(x, SCHICHT_Y_ENTNAHME + 3);
            feld.Location = new Point(x + label.PreferredSize.Width + 4, SCHICHT_Y_ENTNAHME);

            return feld.Right + 16;
        }

        /// <summary>
        /// Schichtenzahl geändert: Sichtbarkeit umstellen und — beim Wechsel auf N &gt; 1
        /// mit noch leerem Feld — die Nutztemperatur mit 55 °C VORSCHLAGEN (Konzept 7.2).
        ///
        /// <para>Der Vorschlag unterbleibt während des Befüllens (<c>_aktualisiert</c>):
        /// Ein gespeicherter Speicher mit N = 5 und bewusst leerer Nutztemperatur soll
        /// beim Öffnen keine 55 bekommen, die er nie hatte.</para>
        /// </summary>
        private void Schichten_Geaendert(object sender, EventArgs e)
        {
            bool erweitert = GewaehlteSchichten() > PufferSpModel.SCHICHTEN_DEFAULT;

            if (!_aktualisiert && erweitert && !_schichtErweitert &&
                _tbTNutzBW.Text.Trim().Length == 0)
                _tbTNutzBW.Text = T_NUTZ_VORSCHLAG.ToString(CultureInfo.InvariantCulture);

            SchichtSichtbarkeitSetzen(erweitert);
        }

        /// <summary>Die eingestellte Schichtenzahl.</summary>
        private int GewaehlteSchichten()
        {
            return _nudSchichten != null
                ? PufferSpCtrl.SchichtenKlemmen((int)_nudSchichten.Value)
                : PufferSpModel.SCHICHTEN_DEFAULT;
        }

        /// <summary>
        /// Blendet die Zeilen ab N &gt; 1 ein oder aus und passt Gruppe, Folgeelemente und
        /// Fensterhöhe an — dieselbe Pixelrechnung wie beim Aufbau, nur mit Vorzeichen.
        /// </summary>
        private void SchichtSichtbarkeitSetzen(bool erweitert)
        {
            if (_gbSchichtung == null || _schichtErweitert == erweitert) return;

            int neueHoehe = erweitert ? SCHICHT_HOEHE_ERWEITERT : SCHICHT_HOEHE_KOMPAKT;
            int delta = neueHoehe - _gbSchichtung.Height;

            this.SuspendLayout();
            try
            {
                _schichtErweitert = erweitert;

                foreach (Control c in _schichtNurErweitert) c.Visible = erweitert;
                EntnahmezeileOrdnen();   // nach dem Umschalten - es liest _schichtErweitert

                _gbSchichtung.Height = neueHoehe;

                foreach (Control c in this.Controls)
                    if (!ReferenceEquals(c, _gbSchichtung) && c.Top > _gbSchichtung.Top)
                        c.Top += delta;

                _schichtSollHoehe += delta;
                this.ClientSize = new Size(this.ClientSize.Width, _schichtSollHoehe);
            }
            finally
            {
                this.ResumeLayout();
            }

            // Die Maske ist gerade gewachsen oder geschrumpft. Auf einem großen Schirm
            // tut dieser Aufruf nichts (FensterEinpassung prüft jede Stufe einzeln); auf
            // einem kleinen sorgt er dafür, dass die Fußknöpfe erreichbar bleiben.
            FensterEinpassung.Anwenden(this);
        }

        /// <summary>Zeigt die Schicht- und Leistungsdaten eines Speichers in der Maske.</summary>
        private void SchichtdatenAnzeigen(PufferSpCtrl.Schichtdaten d)
        {
            if (d == null || _nudSchichten == null) return;

            bool vorher = _aktualisiert;
            _aktualisiert = true;
            try
            {
                _nudSchichten.Value = PufferSpCtrl.SchichtenKlemmen(d.Schichten);

                _tbHoehe.Text = Kommazahl(d.Hoehe);
                _tbLambda.Text = Kommazahl(d.LambdaEff);
                _tbTNutzBW.Text = Kommazahl(d.TNutzBW);
                _tbEntnahmeHeizung.Text = Kommazahl(d.EntnahmeHeizung);
                _tbEntnahmeBW.Text = Kommazahl(d.EntnahmeBW);
                _tbEntnahmeProzess.Text = Kommazahl(d.EntnahmeProzess);

                // 0 = unbegrenzt und wird als leeres Feld gezeigt: Eine „0" in einem
                // Leistungsfeld liest sich wie „keine Leistung" und meint das Gegenteil.
                _tbLadeleistung.Text = d.LadeleistungMax > 0 ? Kommazahl(d.LadeleistungMax) : "";
                _tbEntladeleistung.Text = d.EntladeleistungMax > 0
                    ? Kommazahl(d.EntladeleistungMax) : "";
            }
            finally
            {
                _aktualisiert = vorher;
            }

            // AUSDRÜCKLICH und nicht dem ValueChanged überlassen: Stand die Schichtenzahl
            // schon auf dem Zielwert, feuert das Ereignis nicht, und die Gruppe behielte
            // den Zustand des zuvor angezeigten Speichers (dieselbe Falle wie bei den
            // Klassen-Set-Häkchen in NeuVorbereiten).
            SchichtSichtbarkeitSetzen(GewaehlteSchichten() > PufferSpModel.SCHICHTEN_DEFAULT);
            EntnahmezeileOrdnen();
        }

        /// <summary>Eine nullbare Kommazahl als Feldinhalt; <c>null</c> ergibt das leere Feld.</summary>
        private static string Kommazahl(double? wert)
        {
            return wert.HasValue ? wert.Value.ToString("0.###") : "";
        }

        /// <summary>
        /// Liest die Schicht- und Leistungsfelder; <c>false</c> = Eingabefehler, dann
        /// steht der Grund in <paramref name="fehler"/>.
        ///
        /// <para>LEER ist überall zulässig und bedeutet „Vorbelegung" — genau die
        /// NULL-Bedeutung der Spalten (Konzept 7.2). Geprüft wird nur, was dasteht.</para>
        /// </summary>
        private bool SchichtdatenLesen(out PufferSpCtrl.Schichtdaten schicht, out string fehler)
        {
            schicht = new PufferSpCtrl.Schichtdaten();
            fehler = null;

            schicht.Schichten = GewaehlteSchichten();

            if (!ZahlOderLeer(_tbHoehe, MyResource.Resource.PSP_FEHLER_HOEHE, 0, double.MaxValue,
                              false, out schicht.Hoehe, out fehler)) return false;

            if (!ZahlOderLeer(_tbLambda, MyResource.Resource.PSP_FEHLER_LAMBDA_EFF, 0,
                              double.MaxValue, false, out schicht.LambdaEff, out fehler))
                return false;

            if (!ZahlOderLeer(_tbTNutzBW, MyResource.Resource.PSP_FEHLER_T_NUTZ_BW, 0,
                              double.MaxValue, false, out schicht.TNutzBW, out fehler))
                return false;

            if (!EntnahmehoeheLesen(_tbEntnahmeHeizung, MyResource.Resource.KANAL_HEIZUNG_ANZEIGE,
                                    out schicht.EntnahmeHeizung, out fehler)) return false;
            if (!EntnahmehoeheLesen(_tbEntnahmeBW, MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE,
                                    out schicht.EntnahmeBW, out fehler)) return false;
            if (!EntnahmehoeheLesen(_tbEntnahmeProzess, MyResource.Resource.KANAL_PROZESS_ANZEIGE,
                                    out schicht.EntnahmeProzess, out fehler)) return false;

            double? leistung;
            if (!ZahlOderLeer(_tbLadeleistung,
                              string.Format(MyResource.Resource.PSP_FEHLER_LEISTUNG,
                                            MyResource.Resource.PSP_NAME_LADELEISTUNG_MAX),
                              0, double.MaxValue, true, out leistung, out fehler)) return false;
            schicht.LadeleistungMax = leistung ?? 0;

            if (!ZahlOderLeer(_tbEntladeleistung,
                              string.Format(MyResource.Resource.PSP_FEHLER_LEISTUNG,
                                            MyResource.Resource.PSP_NAME_ENTLADELEISTUNG_MAX),
                              0, double.MaxValue, true, out leistung, out fehler)) return false;
            schicht.EntladeleistungMax = leistung ?? 0;

            return true;
        }

        /// <summary>Eine Entnahmehöhe 0…1; leer bleibt leer (Kanal-Default).</summary>
        private static bool EntnahmehoeheLesen(TextBox tb, string kanal, out double? wert,
                                               out string fehler)
        {
            return ZahlOderLeer(tb,
                                string.Format(MyResource.Resource.PSP_FEHLER_ENTNAHMEHOEHE, kanal),
                                0, 1, true, out wert, out fehler);
        }

        /// <summary>
        /// Ein Zahlenfeld, das auch LEER bleiben darf.
        /// </summary>
        /// <param name="nullErlaubt">
        /// <c>true</c> = der Wert 0 ist eine gültige Angabe (Entnahmehöhe „ganz unten",
        /// Leistung „unbegrenzt"); <c>false</c> = 0 ist Unsinn (Höhe, λ, Temperatur).
        /// Dieselbe Unterscheidung wie in <see cref="SchwelleLesen"/>.
        /// </param>
        private static bool ZahlOderLeer(TextBox tb, string fehlertext, double min, double max,
                                         bool nullErlaubt, out double? wert, out string fehler)
        {
            wert = null;
            fehler = null;

            string text = (tb.Text ?? "").Trim();
            if (text.Length == 0) return true;

            float f;
            if (!WaermequelleClass.ZahlParsen(text, out f) ||
                f < min || f > max || (f == 0 && !nullErlaubt))
            {
                fehler = fehlertext;
                return false;
            }

            wert = f;
            return true;
        }

        // --- Befüllen -----------------------------------------------------------------

        /// <summary>Lädt Katalog und Projektbestand; danach ist der Dialog bereit.</summary>
        public void SetControls()
        {
            _aktualisiert = true;
            try
            {
                KatalogLaden();
                EntladeprioListeFuellen();
                ProjektlisteLaden();
            }
            finally
            {
                _aktualisiert = false;
            }

            // Der Absprung aus dem Senkendialog (Konzept 4.2, "Pufferspeicher
            // anlegen...") gibt die gesuchte VERWENDUNG mit. Der Dialog stellt sich
            // darauf ein:
            //   - passender Speicher im Bestand -> der erste davon ist ausgewählt
            //   - keiner                        -> direkt in die Neuanlage, mit der
            //                                      Verwendung schon vorbelegt
            // Vorher sprang der Dialog immer auf den ersten Speicher der Gesamtliste:
            // Wer aus einer Brauchwasser-Senke kam und noch keinen Brauchwasserspeicher
            // hatte, landete im Heizungsspeicher und musste erst "Neuer Pufferspeicher"
            // drücken - genau der Schritt, den der Absprung ersparen soll.
            // Leere Vorgabe = kein Wunsch (Einstieg über die Fußzeile der Übersicht):
            // dann wie bisher der erste Speicher des Bestands.
            int auswahl = string.IsNullOrEmpty(Verwendung)
                ? (_projektPuffer.Count > 0 ? 0 : -1)
                : ErsterMitVerwendung(Verwendung);

            // ETAPPE D3 (Konzept_KonfigUI_Hydraulik 3a): Das ✎ einer Speicherkarte meint
            // GENAU DIESEN Speicher und gibt seine ID mit. Ohne die Vorwahl landete der
            // Anwender im ersten Speicher der Liste - bei zwei Heizungsspeichern also
            // regelmäßig im falschen. Die ID hat Vorrang vor der Verwendungsregel
            // darüber; ist sie unbekannt (0, oder der Speicher gehört nicht zum
            // Projekt), bleibt es bei der bisherigen Wahl.
            int nachId = IndexVonPuffer(ID_Puffer);
            if (nachId >= 0) auswahl = nachId;

            if (auswahl >= 0) _lbProjekt.SelectedIndex = auswahl;
            else NeuVorbereiten();
        }

        /// <summary>Listenplatz eines Projekt-Puffers; -1, wenn er nicht dabei ist.</summary>
        private int IndexVonPuffer(int idPuffer)
        {
            if (idPuffer <= 0) return -1;

            for (int i = 0; i < _projektPuffer.Count; i++)
                if (_projektPuffer[i].ID == idPuffer) return i;

            return -1;
        }

        /// <summary>
        /// Index des ersten Projekt-Puffers mit der gesuchten Verwendung; -1, wenn es
        /// keinen gibt. Leere <c>Verwendung</c> am Puffer zählt als „Heizung"
        /// (<see cref="WaermesenkeClass.WirksameVerwendung"/>) - dieselbe Regel wie in
        /// den Auswahllisten des Senkendialogs.
        /// </summary>
        private int ErsterMitVerwendung(string verwendung)
        {
            string gesucht = string.IsNullOrEmpty(verwendung)
                ? WaermesenkeClass.VERWENDUNG_HEIZUNG : verwendung;

            for (int i = 0; i < _projektPuffer.Count; i++)
            {
                if (string.Equals(WaermesenkeClass.WirksameVerwendung(_projektPuffer[i]),
                                  gesucht, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void KatalogLaden()
        {
            _katalog = StilleDb.Tabelle(
                "SELECT ID, Bezeichner, Hersteller, Speichertyp, Gesamtvolumen, Bereitschaftsverluste, " +
                "Investitionskosten FROM [" + PufferSpStammCtrl.TABLE + "] ORDER BY Bezeichner");

            _cbKatalog.Items.Clear();
            _cbKatalog.Items.Add(MyResource.Resource.PSP_KATALOG_FREIE_EINGABE);
            if (_katalog != null)
                foreach (DataRow r in _katalog.Rows)
                    _cbKatalog.Items.Add(StilleDb.Text(StilleDb.Feld(r, "Bezeichner")));
            _cbKatalog.SelectedIndex = 0;
        }

        private void ProjektlisteLaden()
        {
            _projektPuffer = WaermesenkeClass.ProjektPufferListe(ID_Projekt, null);

            _lbProjekt.Items.Clear();
            foreach (WaermesenkeClass.PufferInfo p in _projektPuffer)
            {
                // Befund L0-2: Der DB-Wert der Verwendung wird für die Anzeige übersetzt.
                _lbProjekt.Items.Add(
                    string.Format(MyResource.Resource.PSP_LISTE_EINTRAG,
                                  p.Bezeichner,
                                  WaermesenkeClass.VerwendungAnzeige(WaermesenkeClass.WirksameVerwendung(p)),
                                  p.Gesamtvolumen) +
                    (p.VerwendungFehlt ? MyResource.Resource.PSP_LISTE_VERWENDUNG_FEHLT : ""));
            }
        }

        private void EntladeprioListeFuellen()
        {
            _cbEntladeprio.Items.Clear();
            _cbEntladeprio.Items.Add(new PrioItem { Wert = 0, Text = MyResource.Resource.PSP_PRIO_AUTOMATISCH });
            for (int p = Ladeordnung.PRIO_MIN; p <= Ladeordnung.PRIO_MAX; p++)
                _cbEntladeprio.Items.Add(new PrioItem { Wert = p, Text = p.ToString() });
            _cbEntladeprio.SelectedIndex = 0;
        }

        /// <summary>Setzt die Maske auf „neuer Speicher".</summary>
        private void NeuVorbereiten()
        {
            _aktualisiert = true;
            try
            {
                _bearbeiteteId = 0;
                _lbProjekt.ClearSelected();

                _cbKatalog.SelectedIndex = 0;
                _tbBezeichner.Text = "";
                _tbVolumen.Text = "";
                _tbVerluste.Text = "0";

                // D5a/D5b: Die Vorbelegung aus dem Senken-Dialog kann jetzt auch „Kombi"
                // sein - der Absprung „Pufferspeicher anlegen…" gibt sie vor.
                VerwendungWaehlen(
                    WaermesenkeClass.IstKombiVerwendung(Verwendung)
                        ? WaermesenkeClass.VERWENDUNG_KOMBI
                        : string.Equals(Verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                        StringComparison.OrdinalIgnoreCase)
                            ? WaermesenkeClass.VERWENDUNG_BRAUCHWASSER
                            : WaermesenkeClass.VERWENDUNG_HEIZUNG);

                // PAKET K2: die Häkchen mitziehen. Ausdrücklich und nicht dem Ereignis
                // überlassen - hat die ComboBox schon auf dem Zielwert gestanden, feuert
                // SelectedIndexChanged nicht, und die Häkchen behielten das Set des
                // zuvor bearbeiteten Speichers.
                KlassenSetSetzen(PufferSpCtrl.KlassenSetAusVerwendung(GewaehlteVerwendung()));

                // Vorbelegung aus den SYSTEMVORGABEN des Projekts (Konzept 4.3, Punkt 3):
                // kleinster Vorlauf und größter Rücklauf über die Erzeuger. Fehlen sie,
                // bleiben die Felder leer - eine erfundene Vorbelegung wäre bei einem
                // Niedertemperatursystem falsch (ProjektPuffer.PufferParameter).
                int? vorlauf = PufferSpCtrl.SystemVorlauf(ID_Projekt);
                int? ruecklauf = PufferSpCtrl.SystemRuecklauf(ID_Projekt);
                _tbVorlauf.Text = vorlauf.HasValue ? vorlauf.Value.ToString(CultureInfo.InvariantCulture) : "";
                _tbRuecklauf.Text = ruecklauf.HasValue ? ruecklauf.Value.ToString(CultureInfo.InvariantCulture) : "";

                _tbSchwelleEin.Text = ProjektPuffer.SCHWELLE_EIN_DEFAULT.ToString("0.#");
                _tbSchwelleAus.Text = ProjektPuffer.SCHWELLE_AUS_DEFAULT.ToString("0.#");
                _tbSchwelleNachrang.Text = ProjektPuffer.SCHWELLE_AUS_DEFAULT.ToString("0.#");
                // PAKET BHKW-REGULÄR: dieselbe Vorbelegung, die Migrationsschritt 13 in den
                // Bestand schreibt - ein neuer Puffer verhält sich wie ein migrierter.
                _tbSchwelleReserve.Text = ProjektPuffer.SCHWELLE_RESERVE_DEFAULT.ToString("0.#");
                _cbEntladeprio.SelectedIndex = 0;

                _btnUebernehmen.Text = MyResource.Resource.PSP_BTN_ANLEGEN;
                _btnEntfernen.Enabled = false;
                _cbKatalog.Enabled = true;
            }
            finally
            {
                _aktualisiert = false;
            }

            // PAKET P1: die Schichtgruppe auf die Vorbelegung - Ein-Zonen-Speicher ohne
            // Leistungsgrenzen. Ausdrücklich und nicht dem Ereignis überlassen, aus
            // demselben Grund wie beim Klassen-Set oben.
            SchichtdatenAnzeigen(new PufferSpCtrl.Schichtdaten());

            AnzeigenAktualisieren();
        }

        /// <summary>Lädt einen vorhandenen Projekt-Puffer in die Maske.</summary>
        private void PufferAnzeigen(WaermesenkeClass.PufferInfo p)
        {
            if (p == null) return;

            _aktualisiert = true;
            try
            {
                _bearbeiteteId = p.ID;
                ID_Puffer = p.ID;

                _cbKatalog.SelectedIndex = 0;
                _cbKatalog.Enabled = false;   // ein vorhandener Speicher wird nicht neu übernommen

                _tbBezeichner.Text = p.Bezeichner;
                _tbVolumen.Text = p.Gesamtvolumen.ToString(CultureInfo.InvariantCulture);
                _tbVerluste.Text = p.Bereitschaftsverluste.ToString("0.###");
                VerwendungWaehlen(WaermesenkeClass.WirksameVerwendung(p));

                // PAKET K2: Das KLASSEN-SET ist führend und kann mehr aussagen als der
                // Altwert ({Heizung, Prozess} etwa steht in der ComboBox als „Heizung").
                // Es wird deshalb NACH der ComboBox gesetzt und überschreibt die eben
                // abgeleiteten Häkchen. Gelesen wird es aus der Datenbank statt aus
                // PufferInfo: Die Puffer-Liste des Dialogs stammt aus
                // WaermesenkeClass.ProjektPufferListe, und deren Datensatz führt bis zur
                // Engine-Umstellung nur die Verwendung.
                KlassenSetSetzen(PufferSpCtrl.KlassenSetLesen(p.ID));

                _tbVorlauf.Text = p.Vorlauf > 0 ? p.Vorlauf.ToString(CultureInfo.InvariantCulture) : "";
                _tbRuecklauf.Text = p.Ruecklauf > 0 ? p.Ruecklauf.ToString(CultureInfo.InvariantCulture) : "";

                _tbSchwelleEin.Text = p.SchwelleEin.ToString("0.#");
                _tbSchwelleAus.Text = p.SchwelleAus.ToString("0.#");
                _tbSchwelleNachrang.Text = p.SchwelleAusNachrang.ToString("0.#");
                _tbSchwelleReserve.Text = p.SchwelleReserve.ToString("0.#");

                PrioWaehlen(_cbEntladeprio, p.Entladeprio);

                _btnUebernehmen.Text = MyResource.Resource.PSP_BTN_UEBERNEHMEN;
                _btnEntfernen.Enabled = true;
            }
            finally
            {
                _aktualisiert = false;
            }

            // PAKET P1: Schichtung und Leistungsgrenzen. Wie das Klassen-Set aus der
            // DATENBANK gelesen und nicht aus PufferInfo - deren Datensatz führt die
            // Spalten des Schritts 53 nicht. Spaltentolerant: Ohne die Spalten kommt die
            // Vorbelegung zurück, und die Gruppe zeigt N = 1 mit leeren Feldern.
            SchichtdatenAnzeigen(PufferSpCtrl.SchichtdatenLesen(p.ID));

            AnzeigenAktualisieren();
        }

        /// <summary>
        /// Wählt den Verwendungseintrag zu einem DB-Wert (Befund L0-2). Ohne Treffer
        /// bleibt es beim ersten Eintrag („Heizung") — dieselbe Wirkung wie bisher,
        /// wenn <c>SelectedItem</c> auf einen unbekannten Wert gesetzt wurde.
        /// </summary>
        private void VerwendungWaehlen(string dbWert)
        {
            foreach (object o in _cbVerwendung.Items)
            {
                VerwendungItem it = o as VerwendungItem;
                if (it != null && string.Equals(it.DbWert, dbWert, StringComparison.OrdinalIgnoreCase))
                {
                    _cbVerwendung.SelectedItem = o;
                    return;
                }
            }
            if (_cbVerwendung.Items.Count > 0) _cbVerwendung.SelectedIndex = 0;
        }

        /// <summary>
        /// Der DB-Wert der gewählten Verwendung — der Steuerwert, der in die Datenbank
        /// geht und gegen den geprüft wird (Befund L0-2). Leer, solange nichts gewählt ist.
        /// </summary>
        private string GewaehlteVerwendung()
        {
            VerwendungItem it = _cbVerwendung.SelectedItem as VerwendungItem;
            return it != null ? it.DbWert : "";
        }

        private static void PrioWaehlen(ComboBox cb, int wert)
        {
            foreach (object o in cb.Items)
            {
                PrioItem it = o as PrioItem;
                if (it != null && it.Wert == wert) { cb.SelectedItem = o; return; }
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private static int GewaehltePrio(ComboBox cb)
        {
            PrioItem it = cb.SelectedItem as PrioItem;
            return it != null ? it.Wert : 0;
        }

        // --- Anzeigen (Q_max, Ladereihenfolge, Entladung) -----------------------------

        /// <summary>
        /// Tippen in Volumen/Vorlauf/Rücklauf rechnet nur Q_max neu. Die beiden
        /// Reihenfolge-Anzeigen fragen die Datenbank ab und dürfen nicht an jedem
        /// Tastendruck hängen; sie werden bei Auswahlwechseln aufgefrischt.
        /// </summary>
        private void Kapazitaet_Geaendert(object sender, EventArgs e)
        {
            if (_aktualisiert) return;
            QmaxAnzeigen();
        }

        private void Daten_Geaendert(object sender, EventArgs e)
        {
            if (_aktualisiert) return;
            AnzeigenAktualisieren();
        }

        private void AnzeigenAktualisieren()
        {
            QmaxAnzeigen();
            LadereihenfolgeAnzeigen();
            EntladungAnzeigen();
        }

        /// <summary>Nutzbare Kapazität aus Volumen und Spreizung (dieselbe Formel wie die Engine).</summary>
        private void QmaxAnzeigen()
        {
            int volumen, vorlauf, ruecklauf;
            if (!int.TryParse(_tbVolumen.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out volumen) ||
                !int.TryParse(_tbVorlauf.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out vorlauf) ||
                !int.TryParse(_tbRuecklauf.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ruecklauf) ||
                volumen <= 0 || vorlauf <= ruecklauf)
            {
                _lblQmax.Text = "";
                return;
            }

            double qmax = volumen * 1.16 * (vorlauf - ruecklauf) / 1000.0;
            _lblQmax.Text = string.Format(MyResource.Resource.PSP_ANZEIGE_QMAX, qmax.ToString("0.0"));
        }

        private void LadereihenfolgeAnzeigen()
        {
            _lvLaden.Items.Clear();
            if (_bearbeiteteId <= 0)
            {
                _lvLaden.Items.Add(new ListViewItem(new[]
                    { "", MyResource.Resource.PSP_LADEN_NOCH_NICHT_ANGELEGT, "", "", "", "" }));
                return;
            }

            List<Ladeordnung.LadeEintrag> liste = Ladeordnung.Ladereihenfolge(ID_Projekt, _bearbeiteteId);
            if (liste.Count == 0)
            {
                _lvLaden.Items.Add(new ListViewItem(new[]
                    { "", MyResource.Resource.PSP_LADEN_KEINE_ANLAGE, "", "", "", "" }));
                return;
            }

            for (int i = 0; i < liste.Count; i++)
            {
                Ladeordnung.LadeEintrag e = liste[i];

                // e.Erzeuger kommt aus Ladeordnung.ErzeugerName und ist bereits der
                // lokalisierte ANZEIGEname (nicht der Persistenzwert - der steht in
                // Ladeordnung.KaskadenLiteral).
                string ladeprio = e.PrioManuell
                    ? string.Format(MyResource.Resource.PSP_LADEPRIO_MANUELL, e.Ladeprio)
                    : e.Ladeprio.ToString();
                string obergrenze = e.ObergrenzeEigen
                    ? string.Format(MyResource.Resource.PSP_OBERGRENZE_EIGEN, e.Obergrenze.ToString("0.#"))
                    : e.Obergrenze.ToString("0.#") + " %";

                _lvLaden.Items.Add(new ListViewItem(new[]
                {
                    (i + 1) + ".",
                    e.Bezeichner,
                    e.Erzeuger,
                    // Zelleninhalt einer Tabelle = Beschriftung, deshalb die gross
                    // geschriebenen Schluessel (SIM_ROLLE_* ist die Satzform).
                    e.Zweitsenke ? MyResource.Resource.SIM_SPALTE_ZWEITSENKE
                                 : MyResource.Resource.SIM_GRUPPE_HAUPTSENKE,
                    ladeprio,
                    obergrenze
                }));
            }
        }

        private void EntladungAnzeigen()
        {
            if (_bearbeiteteId <= 0)
            {
                _lblEntladeInfo.Text = "";
                AutomatikTextSetzen(Ladeordnung.PRIO_SONSTIGE);
                return;
            }

            int automatik = Ladeordnung.EntladeprioAutomatik(ID_Projekt, _bearbeiteteId);
            AutomatikTextSetzen(automatik);

            string verwendung = GewaehlteVerwendung();
            if (verwendung.Length == 0) verwendung = WaermesenkeClass.VERWENDUNG_HEIZUNG;

            // ETAPPE D5b: Ein KOMBISPEICHER steht in BEIDEN Entladereihenfolgen — je
            // Kanal an der Stelle seiner Entladepriorität (D5a, Konzept Abschnitt 5).
            // D5a zeigte nur die Position im Heizkanal, weil „Kombi" selbst kein Kanal
            // ist; die Warmwasserposition fehlte und war als Restpunkt vermerkt. Jetzt
            // stehen BEIDE da, jede mit ihrem Kanalnamen — ohne den Kanalnamen wären zwei
            // Zahlen nebeneinander nicht zuzuordnen.
            if (WaermesenkeClass.IstKombiVerwendung(verwendung))
            {
                _lblEntladeInfo.Text = KombiPositionstext();
                return;
            }

            List<Ladeordnung.EntladeEintrag> reihe = Ladeordnung.Entladereihenfolge(ID_Projekt, verwendung);
            int pos = Ladeordnung.Position(reihe, _bearbeiteteId);

            _lblEntladeInfo.Text = pos > 0
                ? string.Format(MyResource.Resource.PSP_ENTLADE_POSITION,
                                pos, reihe.Count, KanalSpeicherWort(verwendung, reihe.Count))
                : "";
        }

        /// <summary>
        /// Die Positionen eines KOMBISPEICHERS in beiden Kanälen, zeilenweise
        /// untereinander (Etappe D5b).
        ///
        /// Die Zeilen sind bewusst kürzer gefasst als der Satz für den einkanaligen Fall
        /// (<c>PSP_ENTLADE_POSITION</c>): In das Feld passen zwei Zeilen, und der
        /// Kanalname trägt dort die Aussage, die im einkanaligen Fall im Speicherwort
        /// steckt („von 2 Heizungsspeichern"). Ein Kanal, in dem der Speicher nicht
        /// auftaucht — das kann nur passieren, während der Verwendungswechsel noch nicht
        /// übernommen ist —, bleibt weg statt eine „0. von 0" zu zeigen.
        /// </summary>
        private string KombiPositionstext()
        {
            List<string> zeilen = new List<string>();

            zeilen.Add(KanalPositionstext(WaermesenkeClass.VERWENDUNG_HEIZUNG,
                                          MyResource.Resource.PSP_ENTLADE_POSITION_KANAL_HEIZUNG));
            zeilen.Add(KanalPositionstext(WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                          MyResource.Resource.PSP_ENTLADE_POSITION_KANAL_WARMWASSER));

            zeilen.RemoveAll(delegate (string z) { return z.Length == 0; });
            return string.Join(Environment.NewLine, zeilen.ToArray());
        }

        /// <summary>Eine Kanalzeile für <see cref="KombiPositionstext"/>; "" = nicht enthalten.</summary>
        private string KanalPositionstext(string verwendung, string muster)
        {
            List<Ladeordnung.EntladeEintrag> reihe =
                Ladeordnung.Entladereihenfolge(ID_Projekt, verwendung);
            int pos = Ladeordnung.Position(reihe, _bearbeiteteId);

            return pos > 0 ? string.Format(muster, pos, reihe.Count) : "";
        }

        /// <summary>
        /// Der Kanalname in der grammatisch richtigen Form: „Heizungsspeicher" bzw.
        /// „Brauchwasserspeicher", im Plural mit „n".
        ///
        /// Vorher wurde die Verwendung kleingeschrieben und ein „s-Speicher(n)"
        /// angehängt - das ergab „von 2 heizungs-Speicher(n) entladen".
        /// </summary>
        private static string KanalSpeicherWort(string verwendung, int anzahl)
        {
            bool brauchwasser = string.Equals(verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                              StringComparison.OrdinalIgnoreCase);

            // Singular und Plural sind je EIGENE Ressourcen. Das frühere „basis + \"n\""
            // war eine deutsche Beugungsregel im Quelltext und im Englischen falsch
            // (dort trägt der Plural ein „s" an anderer Stelle).
            if (brauchwasser)
                return anzahl == 1
                    ? MyResource.Resource.PSP_KANALWORT_BRAUCHWASSERSPEICHER
                    : MyResource.Resource.PSP_KANALWORT_BRAUCHWASSERSPEICHER_PLURAL;

            return anzahl == 1
                ? MyResource.Resource.PSP_KANALWORT_HEIZUNGSSPEICHER
                : MyResource.Resource.PSP_KANALWORT_HEIZUNGSSPEICHER_PLURAL;
        }

        /// <summary>Beschriftet den Automatik-Eintrag mit dem errechneten Wert.</summary>
        private void AutomatikTextSetzen(int automatik)
        {
            if (_cbEntladeprio.Items.Count == 0) return;

            PrioItem it = _cbEntladeprio.Items[0] as PrioItem;
            if (it == null) return;

            it.Text = string.Format(MyResource.Resource.PSP_PRIO_AUTOMATISCH_WERT, automatik);

            // ComboBox neu zeichnen lassen, ohne die Auswahl zu verlieren
            int auswahl = _cbEntladeprio.SelectedIndex;
            _aktualisiert = true;
            try
            {
                _cbEntladeprio.Items[0] = it;
                _cbEntladeprio.SelectedIndex = auswahl >= 0 ? auswahl : 0;
            }
            finally
            {
                _aktualisiert = false;
            }
        }

        // --- Ereignisse ---------------------------------------------------------------

        private void lbProjekt_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_aktualisiert) return;
            int i = _lbProjekt.SelectedIndex;
            if (i < 0 || i >= _projektPuffer.Count) return;
            PufferAnzeigen(_projektPuffer[i]);
        }

        private void btnNeu_Click(object sender, EventArgs e)
        {
            NeuVorbereiten();
            _tbBezeichner.Focus();
        }

        private void cbKatalog_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_aktualisiert) return;
            if (_cbKatalog.SelectedIndex <= 0) return;   // (freie Eingabe)
            if (_katalog == null) return;

            int zeile = _cbKatalog.SelectedIndex - 1;
            if (zeile < 0 || zeile >= _katalog.Rows.Count) return;

            DataRow r = _katalog.Rows[zeile];
            _aktualisiert = true;
            try
            {
                _tbBezeichner.Text = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                _tbVolumen.Text = StilleDb.Zahl(StilleDb.Feld(r, "Gesamtvolumen"))
                                          .ToString(CultureInfo.InvariantCulture);
                _tbVerluste.Text = StilleDb.Kommazahl(StilleDb.Feld(r, "Bereitschaftsverluste"))
                                           .ToString("0.###");
            }
            finally
            {
                _aktualisiert = false;
            }

            AnzeigenAktualisieren();
        }

        private void btnKatalog_Click(object sender, EventArgs e)
        {
            // Katalogbrowser wie im Bestand (Konzept 4.3): nur Ansicht.
            Form_PufferSp_Admin frm = new Form_PufferSp_Admin();
            frm.m_bReadOnly = true;
            frm.ShowDialog(this);

            _aktualisiert = true;
            try { KatalogLaden(); }
            finally { _aktualisiert = false; }
        }

        private void btnUebernehmen_Click(object sender, EventArgs e)
        {
            string bezeichner, verwendung, fehler;
            int volumen, entladeprio;
            double verluste, schwelleEin, schwelleAus, schwelleNachrang, schwelleReserve;
            int? vorlauf, ruecklauf;
            PufferSpCtrl.KlassenSet klassenSet;
            PufferSpCtrl.Schichtdaten schicht;

            if (!EingabenLesen(out bezeichner, out verwendung, out volumen, out verluste,
                               out vorlauf, out ruecklauf, out schwelleEin, out schwelleAus,
                               out schwelleNachrang, out entladeprio, out schwelleReserve,
                               out klassenSet, out schicht, out fehler))
            {
                MessageBox.Show(fehler, MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // KRITERIUM W6 - HART, ABGEWIESEN (Konzept 6.2/6.3, Entscheidung F8).
            if (!SchichtungAmVerbundGeprueft(schicht, bezeichner)) return;

            string hersteller = "", speichertyp = ProjektPuffer.SPEICHERTYP_PUFFER;
            double investition = 0;
            KatalogfelderLesen(ref hersteller, ref speichertyp, ref investition);

            if (_bearbeiteteId <= 0)
            {
                // Konzept 5.2 / E7: die EXPLIZITE Anlage legt immer eine neue Zeile an -
                // Mehrfachanlage desselben Katalogtyps ist ausdrücklich zulässig.
                int neueId = PufferSpCtrl.ProjektPufferAnlegen(
                    ID_Projekt, bezeichner, hersteller, speichertyp, volumen, verluste,
                    investition, verwendung, vorlauf, ruecklauf,
                    schwelleEin, schwelleAus, schwelleNachrang, entladeprio, schwelleReserve,
                    klassenSet.Heizung, klassenSet.Brauchwasser, klassenSet.Prozess, schicht);

                if (neueId <= 0)
                {
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_ANLEGEN_FEHLGESCHLAGEN,
                                    MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ID_Puffer = neueId;
                _bearbeiteteId = neueId;
                Verwendung = verwendung;
                Status(MyResource.Resource.PSP_STATUS_ANGELEGT);
            }
            else
            {
                // Konzept 5.2, Konsistenzregel: Ein Nutzungswechsel an einem bereits
                // zugeordneten Speicher darf nicht still durchgehen (K2-O8: gemessen wird
                // seit S2 das KLASSEN-SET, nicht mehr der abgeleitete Altwert).
                if (!KlassenSetWechselBestaetigt(klassenSet)) return;

                if (!PufferSpCtrl.ProjektPufferAendern(
                        _bearbeiteteId, ID_Projekt, bezeichner, hersteller, speichertyp, volumen,
                        verluste, investition, verwendung, vorlauf, ruecklauf,
                        schwelleEin, schwelleAus, schwelleNachrang, entladeprio, schwelleReserve,
                        klassenSet.Heizung, klassenSet.Brauchwasser, klassenSet.Prozess, schicht))
                {
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_AENDERN_FEHLGESCHLAGEN,
                                    MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ID_Puffer = _bearbeiteteId;
                Verwendung = verwendung;
                Status(MyResource.Resource.PSP_STATUS_AENDERUNGEN_UEBERNOMMEN);
            }

            BestandNeuLaden(ID_Puffer);

            // KRITERIUM W4 - WEICH, NACH dem Speichern (Konzept 7.2). Derselbe Umgang wie
            // im Senkendialog: Die Angabe ist zulässig und wird gespeichert, der Anwender
            // erfährt nur, was der Lauf damit macht.
            KlemmhinweisZeigen(schicht, bezeichner, vorlauf, ruecklauf);
        }

        /// <summary>
        /// KRITERIUM W6 — die HARTE Abweisung im Dialog (Konzept 6.2/6.3, Entscheidung
        /// F8); <c>true</c> = speichern darf weitergehen.
        ///
        /// <para><b>Warum abgewiesen und nicht gewarnt.</b> Ein Parallelverbund ist EIN
        /// Wärmevorrat aus mehreren Behältern, und sein <c>Q_max</c> ist die aufsummierte
        /// Kapazität ALLER Mitglieder (<c>SimulationControl</c>, Verbundaufbau). Eine
        /// Schichtebene, die aus dem Volumen des Leitspeichers abgeleitet wäre, beschriebe
        /// damit einen Behälter, den es so nicht gibt — die Schicht-Invariante aus
        /// Konzept 7.3 ließe sich nicht erfüllen. Anders als W1 bis W5 ist das kein
        /// unplausibler, sondern ein UNMÖGLICHER Zustand; der Katalog führt ihn deshalb
        /// als harten Befund, und hier steht die Sperre.</para>
        ///
        /// <para>Die GEGENRICHTUNG — ein Verbund, dessen Leitspeicher schon geschichtet
        /// ist — hält <c>AnlagePufferVerbundCtrl.KonfliktPruefen</c> ab
        /// (<c>GRUND_LEIT_GESCHICHTET</c>). Beide Wege in denselben Zustand sind damit
        /// verschlossen.</para>
        ///
        /// <para>Bei der NEUANLAGE kann der Fall nicht auftreten: Ein Speicher, den es
        /// noch nicht gibt, ist kein Leitspeicher. Die Abfrage entfällt deshalb dort.</para>
        /// </summary>
        private bool SchichtungAmVerbundGeprueft(PufferSpCtrl.Schichtdaten schicht,
                                                 string bezeichner)
        {
            if (schicht == null || !schicht.Geschichtet) return true;
            if (_bearbeiteteId <= 0) return true;
            if (!AnlagePufferVerbundCtrl.IstLeitspeicher(_bearbeiteteId)) return true;

            MessageBox.Show(
                string.Format(
                    Zeilenumbruch.Normalisieren(
                        MyResource.Resource.PSP_FEHLER_SCHICHTUNG_AM_VERBUND),
                    bezeichner),
                MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return false;
        }

        /// <summary>
        /// KRITERIUM W4 — der WEICHE Hinweis auf die Laufzeit-Klemmung (Konzept 7.2).
        ///
        /// <para>Liegt die Nutztemperatur Brauchwasser über dem WIRKSAMEN Vorlauf des
        /// Speichers, könnte keine Schicht sie je erreichen: Der Brauchwasserkanal wäre
        /// still komplett abgeschaltet. Der Lauf klemmt sie deshalb auf <c>VL_eff</c> und
        /// schreibt eine Protokollwarnung — gespeichert wird die Angabe trotzdem
        /// unverändert, denn sie ist nicht falsch, sondern nur nicht erreichbar (etwa
        /// weil das Temperaturpaar des Speichers noch fehlt).</para>
        ///
        /// <para><c>VL_eff</c> kommt aus <see cref="Warnkriterien.WirksamerVorlauf"/> —
        /// derselben Rückfallregel, mit der der Katalog und die Engine rechnen. Ein
        /// eigener Vergleich hier könnte eine andere Antwort geben als die
        /// Protokollwarnung, die der Anwender gleich darauf liest.</para>
        /// </summary>
        private void KlemmhinweisZeigen(PufferSpCtrl.Schichtdaten schicht, string bezeichner,
                                        int? vorlauf, int? ruecklauf)
        {
            if (schicht == null || !schicht.TNutzBW.HasValue) return;

            double tNutz = schicht.TNutzBW.Value;
            double vlEff = Warnkriterien.WirksamerVorlauf(vorlauf ?? 0, ruecklauf ?? 0,
                                                          bezeichner);
            if (tNutz <= vlEff) return;

            MessageBox.Show(
                MyResource.Resource.SIMWARN_DIALOG_KOPF + Environment.NewLine +
                Environment.NewLine + "  • " +
                string.Format(MyResource.Resource.SIMWARN_W4_TNUTZ_UEBER_VLEFF,
                              bezeichner,
                              tNutz.ToString("0.#", CultureInfo.CurrentCulture),
                              vlEff.ToString("0.#", CultureInfo.CurrentCulture)),
                MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Rückfrage vor dem Wechsel der NUTZUNG (Klassen-Set) eines bereits
        /// REFERENZIERTEN Speichers; <c>true</c> = weitermachen.
        ///
        /// <para><b>PAKET S2, Ticket K2-O8 — geprüft wird das SET, nicht mehr der
        /// Altwert.</b> Bis K2 verglich die Rückfrage <c>Verwendung</c> gegen
        /// <c>Verwendung</c>. Seit dem Klassen-Set hat dieser Altwert für vier der acht
        /// Sets keine genaue Entsprechung mehr (<c>PufferSpCtrl.KlassenSet.Verwendung</c>
        /// liefert dann den nächstliegenden): Ein Wechsel von {H} auf {H, P} ändert die
        /// abgeleitete Verwendung NICHT — beide ergeben „Heizung" — und ging deshalb
        /// still durch, obwohl der Speicher danach einen Kanal mehr bedient. Verglichen
        /// werden jetzt die drei Flags; damit schlägt jede echte Änderung an.</para>
        ///
        /// <para><b>Die Begründung hat sich mitgeändert.</b> Früher war eine
        /// unpassende Zuordnung nach dem Wechsel GESPERRT — der Senkendialog wies sie
        /// beim nächsten Öffnen ab. Seit S2 (Konzept 6.2) ist sie zulässig und erzeugt
        /// eine Warnung (Kriterium W1). Die Rückfrage bleibt trotzdem: Sie ist der
        /// einzige Moment, in dem der Anwender sieht, WELCHE Anlagen sein Wechsel
        /// betrifft. Nur der Meldungstext sagt jetzt „wird gewarnt" statt „muss neu
        /// gesetzt werden" (<c>PSP_MELDUNG_KLASSENSETWECHSEL</c>).</para>
        ///
        /// <para>Die Rückfrage sitzt hier im Dialog und nicht in
        /// <c>PufferSpCtrl.ProjektPufferAendern</c>: die Ctrl-Bausteine aus Paket 2 sind
        /// durchgehend dialogfrei (Konzept 13.4), damit die headless laufenden Proben und
        /// der Referenzlauf sie benutzen können. Eine MessageBox dort brächte den
        /// nächsten Lauf zum Stehen.</para>
        /// </summary>
        private bool KlassenSetWechselBestaetigt(PufferSpCtrl.KlassenSet setNeu)
        {
            if (_bearbeiteteId <= 0 || setNeu == null) return true;

            WaermesenkeClass.PufferInfo alt = WaermesenkeClass.PufferLesen(_bearbeiteteId);
            if (alt == null) return true;

            PufferSpCtrl.KlassenSet setAlt = PufferSpCtrl.KlassenSetLesen(_bearbeiteteId);
            if (setAlt.Heizung == setNeu.Heizung &&
                setAlt.Brauchwasser == setNeu.Brauchwasser &&
                setAlt.Prozess == setNeu.Prozess)
                return true;

            List<string> referenzen = PufferSpCtrl.ReferenzenAufPuffer(_bearbeiteteId);
            if (referenzen.Count == 0) return true;

            // Die beiden Sets sind Steuerwerte und werden für die Meldung übersetzt
            // (Befund L0-2) - sonst mischte die englische Meldung die Sprachen.
            return MessageBox.Show(
                string.Format(
                    // Umbrüche der Ressource VOR dem Einsetzen auf die Plattformform
                    // bringen (Details in Zeilenumbruch).
                    Zeilenumbruch.Normalisieren(MyResource.Resource.PSP_MELDUNG_KLASSENSETWECHSEL),
                    alt.Bezeichner,
                    Warnkriterien.KlassenSetAnzeige(setAlt),
                    Warnkriterien.KlassenSetAnzeige(setNeu),
                    string.Join(Environment.NewLine + "  • ", referenzen)),
                MyResource.Resource.PSP_TITEL_KLASSENSET_AENDERN, MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void btnEntfernen_Click(object sender, EventArgs e)
        {
            if (_bearbeiteteId <= 0) return;

            // Konzept 5.2: blockieren, solange eine Anlage den Puffer referenziert
            List<string> referenzen = PufferSpCtrl.ReferenzenAufPuffer(_bearbeiteteId);
            if (referenzen.Count > 0)
            {
                MessageBox.Show(
                    string.Format(
                        Zeilenumbruch.Normalisieren(MyResource.Resource.PSP_MELDUNG_ENTFERNEN_BLOCKIERT),
                        _tbBezeichner.Text,
                        string.Join(Environment.NewLine + "  • ", referenzen)),
                    MyResource.Resource.PSP_TITEL_PUFFER_ENTFERNEN,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                    string.Format(
                        Zeilenumbruch.Normalisieren(MyResource.Resource.PSP_MELDUNG_ENTFERNEN_BESTAETIGEN),
                        _tbBezeichner.Text),
                    MyResource.Resource.PSP_TITEL_PUFFER_ENTFERNEN, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;

            if (!PufferSpCtrl.ProjektPufferEntfernen(_bearbeiteteId, ID_Projekt))
            {
                MessageBox.Show(MyResource.Resource.PSP_MELDUNG_ENTFERNEN_FEHLGESCHLAGEN,
                                MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ID_Puffer == _bearbeiteteId) ID_Puffer = 0;
            Status(MyResource.Resource.PSP_STATUS_ENTFERNT);
            BestandNeuLaden(0);
        }

        private void BestandNeuLaden(int auswahlId)
        {
            _aktualisiert = true;
            try { ProjektlisteLaden(); }
            finally { _aktualisiert = false; }

            for (int i = 0; i < _projektPuffer.Count; i++)
            {
                if (_projektPuffer[i].ID == auswahlId)
                {
                    _lbProjekt.SelectedIndex = i;   // löst PufferAnzeigen aus
                    return;
                }
            }

            NeuVorbereiten();
        }

        private void Status(string text)
        {
            _lblStatus.ForeColor = Color.ForestGreen;
            _lblStatus.Text = "✔ " + text;
        }

        /// <summary>Hersteller/Speichertyp/Investition — aus dem Katalog oder aus dem Bestand.</summary>
        private void KatalogfelderLesen(ref string hersteller, ref string speichertyp,
                                        ref double investition)
        {
            if (_cbKatalog.Enabled && _cbKatalog.SelectedIndex > 0 && _katalog != null)
            {
                int zeile = _cbKatalog.SelectedIndex - 1;
                if (zeile >= 0 && zeile < _katalog.Rows.Count)
                {
                    DataRow r = _katalog.Rows[zeile];
                    hersteller = StilleDb.Text(StilleDb.Feld(r, "Hersteller"));
                    string typ = StilleDb.Text(StilleDb.Feld(r, "Speichertyp"));
                    if (typ.Length > 0) speichertyp = typ;
                    investition = StilleDb.Kommazahl(StilleDb.Feld(r, "Investitionskosten"));
                    return;
                }
            }

            if (_bearbeiteteId <= 0) return;

            DataTable dt = StilleDb.Tabelle(
                "SELECT Hersteller, Speichertyp, Investitionskosten FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", System.Data.OleDb.OleDbType.Integer, _bearbeiteteId));
            if (dt == null || dt.Rows.Count == 0) return;

            hersteller = StilleDb.Text(StilleDb.Feld(dt.Rows[0], "Hersteller"));
            string typBestand = StilleDb.Text(StilleDb.Feld(dt.Rows[0], "Speichertyp"));
            if (typBestand.Length > 0) speichertyp = typBestand;
            investition = StilleDb.Kommazahl(StilleDb.Feld(dt.Rows[0], "Investitionskosten"));
        }

        // --- Validierung ---------------------------------------------------------------

        private bool EingabenLesen(out string bezeichner, out string verwendung, out int volumen,
                                   out double verluste, out int? vorlauf, out int? ruecklauf,
                                   out double schwelleEin, out double schwelleAus,
                                   out double schwelleNachrang, out int entladeprio,
                                   out double schwelleReserve,
                                   out PufferSpCtrl.KlassenSet klassenSet,
                                   out PufferSpCtrl.Schichtdaten schicht, out string fehler)
        {
            schicht = new PufferSpCtrl.Schichtdaten();
            bezeichner = (_tbBezeichner.Text ?? "").Trim();

            // PAKET K2: Gespeichert wird IMMER BEIDES - die Häkchen als führende
            // Wahrheit und die davon abgeleitete Verwendung als Altwert für die bis
            // Paket S2 nicht umgestellten Anzeigen. Die ComboBox wird dafür NICHT
            // gelesen: Sie folgt den Häkchen, und bei einem Set ohne Alt-Entsprechung
            // zeigt sie ohnehin nur den nächstliegenden Wert.
            klassenSet = GewaehltesKlassenSet();
            verwendung = klassenSet.Verwendung;   // DB-Wert, nicht der Anzeigetext (L0-2)

            volumen = 0;
            verluste = 0;
            vorlauf = null;
            ruecklauf = null;
            schwelleEin = ProjektPuffer.SCHWELLE_EIN_DEFAULT;
            schwelleAus = ProjektPuffer.SCHWELLE_AUS_DEFAULT;
            schwelleNachrang = ProjektPuffer.SCHWELLE_AUS_DEFAULT;
            schwelleReserve = ProjektPuffer.SCHWELLE_RESERVE_DEFAULT;
            entladeprio = GewaehltePrio(_cbEntladeprio);
            fehler = null;

            if (bezeichner.Length == 0)
            {
                fehler = MyResource.Resource.PSP_FEHLER_BEZEICHNER_FEHLT;
                return false;
            }

            // PFLICHT: mindestens ein Häkchen (Konzept 6.1). Das leere Set wäre ein
            // Speicher, den keine einzige Entladeordnung führt - er nähme Wärme auf und
            // gäbe sie nie ab. Die frühere Pflichtprüfung „Verwendung gewählt?" ist damit
            // abgelöst: Die Verwendung wird jetzt abgeleitet und kann nicht leer sein.
            if (klassenSet.Leer)
            {
                fehler = MyResource.Resource.PSP_FEHLER_KLASSENSET_LEER;
                return false;
            }

            if (!int.TryParse(_tbVolumen.Text.Trim(), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out volumen) || volumen <= 0)
            {
                fehler = MyResource.Resource.PSP_FEHLER_VOLUMEN;
                return false;
            }

            float f;
            if (_tbVerluste.Text.Trim().Length > 0)
            {
                if (!WaermequelleClass.ZahlParsen(_tbVerluste.Text, out f) || f < 0)
                {
                    fehler = MyResource.Resource.PSP_FEHLER_VERLUSTE;
                    return false;
                }
                verluste = f;
            }

            // Temperaturen: leeres PAAR ist erlaubt (dann greift der Engine-Rückfall),
            // ein vollständiges Paar läuft durch die gemeinsame Prüfung.
            string vorText = _tbVorlauf.Text.Trim();
            string rueText = _tbRuecklauf.Text.Trim();
            if (vorText.Length > 0 || rueText.Length > 0)
            {
                int v, r;
                if (!ProjektPuffer.TemperaturenPruefen(vorText, rueText, out v, out r, out fehler))
                    return false;
                vorlauf = v;
                ruecklauf = r;
            }

            if (!SchwelleLesen(_tbSchwelleEin, MyResource.Resource.PSP_NAME_EINSCHALTSCHWELLE,
                               out schwelleEin, out fehler)) return false;
            if (!SchwelleLesen(_tbSchwelleAus, MyResource.Resource.PSP_NAME_ABSCHALTSCHWELLE,
                               out schwelleAus, out fehler)) return false;
            if (!SchwelleLesen(_tbSchwelleNachrang, MyResource.Resource.PSP_NAME_ABSCHALTSCHWELLE_NACHRANG,
                               out schwelleNachrang, out fehler)) return false;

            if (schwelleEin >= schwelleAus)
            {
                fehler = MyResource.Resource.PSP_FEHLER_EIN_KLEINER_AUS;
                return false;
            }

            if (schwelleNachrang > schwelleAus)
            {
                fehler = MyResource.Resource.PSP_FEHLER_NACHRANG_UEBER_AUS;
                return false;
            }

            if (schwelleNachrang <= schwelleEin)
            {
                fehler = MyResource.Resource.PSP_FEHLER_NACHRANG_UNTER_EIN;
                return false;
            }

            // PAKET BHKW-REGULÄR: Mindestfüllstand/Notreserve. Er wird mit nullErlaubt
            // gelesen - anders als die drei Schaltschwellen ist 0 hier eine GÜLTIGE Angabe
            // und bedeutet „dieser Speicher darf leergefahren werden".
            if (!SchwelleLesen(_tbSchwelleReserve, MyResource.Resource.PSP_NAME_MINDESTFUELLSTAND,
                               out schwelleReserve, out fehler, true)) return false;

            // Läge die Reserve auf oder über der Abschaltschwelle, wäre der Speicher für die
            // Bedarfsdeckung wirkungslos: Er dürfte nie unter eine Marke entladen, die
            // oberhalb seines Ladeziels liegt.
            if (schwelleReserve >= schwelleAus)
            {
                fehler = MyResource.Resource.PSP_FEHLER_RESERVE_UEBER_AUS;
                return false;
            }

            // PAKET P1: Schichtung und Leistungsgrenzen. ZULETZT geprüft, weil ihre
            // Felder in der Maske unter allen anderen stehen - die Fehlermeldungen
            // erscheinen damit in derselben Reihenfolge, in der der Anwender liest.
            if (!SchichtdatenLesen(out schicht, out fehler)) return false;

            return true;
        }

        /// <param name="nullErlaubt">
        /// <c>true</c> = der Wert 0 ist eine gültige Angabe (Paket BHKW-Regulär: die
        /// Notreserve darf ausdrücklich 0 sein). Für die drei Schaltschwellen bleibt es beim
        /// bisherigen Bereich „größer 0 bis 100".
        /// </param>
        private static bool SchwelleLesen(TextBox tb, string name, out double wert, out string fehler,
                                          bool nullErlaubt = false)
        {
            wert = 0;
            fehler = null;

            float f;
            if (!WaermequelleClass.ZahlParsen(tb.Text, out f))
            {
                fehler = string.Format(MyResource.Resource.PSP_FEHLER_SCHWELLE_ZAHL, name);
                return false;
            }

            if (f < 0 || (f == 0 && !nullErlaubt) || f > 100)
            {
                fehler = string.Format(MyResource.Resource.PSP_FEHLER_SCHWELLE_BEREICH, name);
                return false;
            }

            wert = f;
            return true;
        }
    }
}
