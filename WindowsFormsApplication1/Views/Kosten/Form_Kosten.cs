using Humanizer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Kosten : Form
    {
        private readonly Color Navy = Color.FromArgb(15, 31, 61);
        private readonly Color NavyMid = Color.FromArgb(26, 50, 97);
        private readonly Color Accent = Color.FromArgb(59, 130, 246);
        private readonly Color Surface = Color.FromArgb(248, 249, 252);

        // Kostenkategorien wie in Tab_KostenKategorie. Die DREI BESTANDSREITER stehen in
        // derselben Reihenfolge, dort gilt KategorieID = tabMain.SelectedIndex + 1.
        //
        // ACHTUNG seit K4 (Konzept Kosten/Energieträger, HF4): Es gibt einen VIERTEN
        // Reiter „Kostenprofil", der KEINE Kostenkategorie führt. Die Index-Arithmetik
        // darf deshalb nirgends mehr roh stehen — jede Stelle, die eine Kategorie
        // braucht, geht über AktuelleKategorieOderNull() und behandelt den Fall
        // „keine Kategorie" ausdrücklich.
        internal const int KATEGORIE_INVESTITION = 1;
        internal const int KATEGORIE_BETRIEB = 2;
        // stillgelegt (Konzept Kosten/Energieträger HF1/L1, 19.08.2026): Kategorie 3 wird nicht mehr geschrieben; Konstante bleibt für Migrationsschritt 19b
        internal const int KATEGORIE_ENERGIE = 3;

        public Dictionary<string, NumericUpDown> _Inputs = new Dictionary<string, NumericUpDown>();
        public int m_ID_Projekt = 0;

        private FlowLayoutPanel flp = null;
        private string kategorie = "";
        private int kategorieID = 0;

        /// <summary>
        /// Grund, warum die Betriebskosten einer Komponente NICHT vorbelegt werden konnten
        /// (bzw. Herleitung, wenn sie es wurden) — gefüllt beim ersten Anwählen, angezeigt
        /// als Hinweiszeile über der Gruppe. Schlüssel ist der Komponentenname.
        /// </summary>
        private readonly Dictionary<string, string> _betriebsHinweis =
            new Dictionary<string, string>(StringComparer.Ordinal);

        // ================================================================= K5b (HF5 § 7.5)

        /// <summary>
        /// Ein Gruppenblock der Positionsliste: die Kopfzeile, ihre Spaltenüberschrift und
        /// der Ein-/Ausklappzustand. Die ZEILEN stehen bewusst NICHT hier.
        /// </summary>
        /// <remarks>
        /// <b>Warum die Zeilen nicht mitgeführt werden.</b> Sie werden an drei Stellen aus
        /// <c>flp</c> entfernt und verworfen (<c>Zeile_DeleteRequested</c>,
        /// <c>btnDeleteGroup_Click</c>, der Neuaufbau selbst). Eine zweite Liste daneben
        /// zeigte danach auf entsorgte Steuerelemente. Gesucht wird deshalb jedes Mal über
        /// <c>flp.Controls</c> und das <c>Tag</c> — dieselbe Zuordnung, die der
        /// Löschbefehl schon benutzt.
        /// </remarks>
        private sealed class Gruppenblock
        {
            public string Name;
            public Panel Kopf;
            public Label Titel;
            public Panel Spaltenkopf;
            public bool Eingeklappt;

            /// <summary>„Betriebskosten VDI 2067…", falls der Kopf einen führt. Er wird
            /// nachgerückt, wenn die Beschriftung wächst.</summary>
            public Button Aktion;
        }

        /// <summary>
        /// Die Gruppenblöcke der GERADE angezeigten Positionsliste, Schlüssel ist der
        /// Gruppenname. Wird bei jedem Neuaufbau geleert.
        /// </summary>
        private readonly Dictionary<string, Gruppenblock> _gruppen =
            new Dictionary<string, Gruppenblock>(StringComparer.Ordinal);

        /// <summary>
        /// Sperrt den Aufbau des Energieträger-Blocks, solange
        /// <see cref="FillCarrierComboBox"/> die Liste an die Daten bindet.
        /// </summary>
        private bool _traegerlisteWirdGefuellt;

        // Verweis auf den ANWENDUNGSWEITEN Extender (F5) — keine eigene Instanz
        // mehr. Die HilfeAutomatik erfasst dieses Formular und das zur Laufzeit
        // eingehängte ucFuelSettings ohnehin von selbst.
        private HelpExtender _helpExtender;

        /// <summary>
        /// Der vierte Reiter „Kostenprofil" (K4/HF4) — programmatisch erzeugt, damit
        /// <c>Form_Kosten.Designer.cs</c> unberührt bleibt. <c>null</c>, solange er
        /// nicht aufgebaut ist.
        /// </summary>
        private TabPage tabKostenprofil;

        private EinstiegsKarte _karteKostenprofil;
        private EinstiegsKarte _karteSpotpreise;

        /// <summary>Anzeige für „nicht ermittelbar" — nie eine 0, die nach Zahl aussieht.</summary>
        private const string STRICH = "—";

        public Form_Kosten(int IDProjekt)
        {
            InitializeComponent(); // Lädt die Designer-Struktur

            // Den anwendungsweiten Extender übernehmen (F5)
            _helpExtender = Program.HelpExtender;

            m_ID_Projekt = IDProjekt;
            tabMain.SelectedIndex = 0;
            kategorieID = KATEGORIE_INVESTITION;
            kategorie = tabMain.TabPages[0].Text;
            flp = flpContainer;

            // UI verfeinern
            this.BackColor = Surface;
            this.tabInvest.BackColor = Surface;

            // Einmal initial aufrufen, damit beim Start 0 oder die Startwerte da stehen
            Gesamtkosten();

            // Die beiden Gewerkelisten beschreiben DAS HIER BEARBEITETE PROJEKT
            // (m_ID_Projekt) — siehe <see cref="ProjektKomponenten"/>. Bis 18.08.2026
            // standen hier sieben Bitabfragen auf Program.startfrm.status; das ist der
            // Gewerke-Status des im STARTASSISTENTEN geladenen Projekts und kann ein
            // anderes sein.
            foreach (string komponente in ProjektKomponenten(m_ID_Projekt))
            {
                listBox_Erzeuger.Items.Add(komponente);
                listBox_Betriebskosten.Items.Add(komponente);
            }

            // Double Buffered für ruckelfreiere UI
            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flpContainer_Betriebskosten, new object[] { true });

            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flpContainer, new object[] { true });

            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flpContainer_Energiekosten, new object[] { true });

            // K4/HF4 6.2 (+ Nachtrag): Die blauen Kopfleisten über den linken Listen
            // fallen auf ALLEN DREI Bestandsreitern weg, die Listen rücken nach oben.
            // Muss VOR dem Befüllen laufen, damit die Listen ihre endgültige Höhe schon
            // haben, wenn die Bindung sie zeichnet.
            KopfzeilenEntfernen();

            // Ä1 (Konzept Kostendialoge § 6.4, Etappe KD4): Der Kosteneditor führt nur
            // noch Investitions- und Betriebskosten. Energie-Reiter und Kostenprofil-
            // Reiter sind in die Energieträgerverwaltung (Form_Energietraeger)
            // umgezogen. Der Reiter wird PROGRAMMATISCH entfernt, damit die
            // Designer-Datei unberührt bleibt — dasselbe Muster, mit dem der
            // Kostenprofil-Reiter einst angebaut wurde. Sein Bestandscode
            // (FillCarrierComboBox, RenderEnergieTab, listBox-Handler) bleibt stehen
            // und ist ohne den Reiter unerreichbar.
            tabMain.TabPages.Remove(tabEnergie);
            BaueEnergietraegerKnopf();

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        /// <summary>
        /// Die Kostenkategorie des GERADE GEWÄHLTEN Reiters — <c>null</c>, wenn er keine
        /// führt (seit K4 der vierte Reiter „Kostenprofil").
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ein Wächter und keine Index-Arithmetik mehr.</b> Bis K4 galt
        /// <c>KategorieID = tabMain.SelectedIndex + 1</c> an mehreren Stellen roh. Mit dem
        /// vierten Reiter liefert dieselbe Rechnung die Kategorie 4 — die es in
        /// <c>Tab_KostenKategorie</c> nicht gibt. Ein Datensatz mit <c>KategorieID = 4</c>
        /// wäre in keiner Auswertung mehr sichtbar und in keiner Summe enthalten; er fiele
        /// erst Jahre später auf. Deshalb gibt es genau EINE Stelle, die den Reiter auf
        /// eine Kategorie abbildet, und sie darf ausdrücklich „keine" sagen.
        /// </para>
        /// <para>
        /// Geprüft wird über die IDENTITÄT der Reiterseite, nicht über ihren Text: Die
        /// Beschriftungen sind übersetzbar, die Seite ist es nicht. Der zusätzliche
        /// Indexbereich fängt einen künftigen fünften Reiter mit ab.
        /// </para>
        /// </remarks>
        private int? AktuelleKategorieOderNull()
        {
            if (tabMain == null || tabMain.SelectedTab == null) return null;

            if (tabKostenprofil != null && ReferenceEquals(tabMain.SelectedTab, tabKostenprofil))
                return null;

            int index = tabMain.SelectedIndex;
            if (index < 0 || index > 2) return null;      // nur die drei Bestandsreiter

            return index + 1;                             // 0→1 Investition, 1→2 Betrieb, 2→3 Energie
        }

        /// <summary>
        /// ETAPPE KD6 (§ 9): Vorwahl von Komponente und Kategorie — die Knöpfe
        /// „Investitionskosten…"/„Betriebskosten…" des Anlagendialogs springen
        /// direkt auf die Gruppe der Komponente im passenden Reiter.
        /// </summary>
        public void WaehleKomponente(string komponente, bool betrieb)
        {
            try
            {
                tabMain.SelectedTab = betrieb ? tabWartung : tabInvest;

                Gruppenblock block;
                if (!string.IsNullOrEmpty(komponente) &&
                    _gruppen.TryGetValue(komponente, out block) && block.Kopf != null)
                    flp.ScrollControlIntoView(block.Kopf);
            }
            catch { /* Vorwahl ist Komfort — der Dialog öffnet trotzdem */ }
        }

        /// <summary>
        /// Übergangs-Einstieg (Etappe KD4, bis KD6): unten rechts ein Knopf
        /// „Energieträger…", der die Energieträgerverwaltung im Projektkontext
        /// öffnet. Die endgültigen Projekt-Einstiege (§ 3.2: Anlagendialog
        /// „Energiekosten…", Berichte &amp; Kosten) kommen mit KD6 — bis dahin
        /// bliebe der frühere Energie-Reiter sonst ohne erreichbaren Nachfolger.
        /// </summary>
        private void BaueEnergietraegerKnopf()
        {
            string text = null;
            try { text = MyResource.Resource.ResourceManager.GetString("KDLG_KOSTEN_ET_KNOPF"); }
            catch { }
            if (string.IsNullOrEmpty(text)) text = "Energieträger…";

            Panel fuss = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Surface };
            Button knopf = new Button
            {
                Text = text,
                Size = new Size(190, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            knopf.Click += (s, e) =>
            {
                using (Form_Energietraeger frm = new Form_Energietraeger())
                {
                    frm.SetControls(m_ID_Projekt);
                    frm.ShowDialog(this);
                }
            };
            fuss.Controls.Add(knopf);
            Controls.Add(fuss);
            knopf.Location = new Point(fuss.ClientSize.Width - knopf.Width - 16, 7);
        }

        /// <summary>
        /// Baut den vierten Reiter „Kostenprofil" (K4/HF4 6.1) mit zwei Einstiegskarten.
        /// Seit KD4 (Ä1) NICHT mehr aufgerufen — die Karten leben beim Stromträger der
        /// Energieträgerverwaltung (<see cref="Form_Energietraeger"/>); der Rückbau
        /// dieses Codes folgt mit KD6/FK8 (erst schreibgeschützt, dann entfernen).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ein eigener Reiter.</b> Kostenprofil und Spotmarktpreise sind
        /// PREISVERLÄUFE über das Jahr und damit etwas anderes als die Arbeits- und
        /// Grundpreise je Energieträger, die der Reiter „Energiekosten" pflegt. Bis K4
        /// hingen sie als graues Panel mit zwei Knöpfen unter der Trägerliste
        /// (<c>BauePreisreihenEinstieg</c>) — an einer Stelle, an der sie zur
        /// darüberstehenden Liste zu gehören schienen, obwohl sie projektweit gelten und
        /// keinem einzelnen Träger zugeordnet sind.
        /// </para>
        /// <para>
        /// Programmatisch erzeugt, damit <c>Form_Kosten.Designer.cs</c> unberührt bleibt
        /// (Hausregel CLAUDE.md: Designer-Dateien nicht von Hand editieren).
        /// </para>
        /// </remarks>
        private void BaueKostenprofilReiter()
        {
            try
            {
                tabKostenprofil = new TabPage(MyResource.Resource.KPROF_TAB_TITEL)
                {
                    Name = "tabKostenprofil",
                    BackColor = Surface,
                    AutoScroll = true,
                    UseVisualStyleBackColor = false
                };

                _karteKostenprofil = new EinstiegsKarte
                {
                    Location = new Point(24, 24),
                    Size = new Size(440, 168),
                    Titel = MyResource.Resource.KPROF_KARTE_PROFIL_TITEL,
                    Beschreibung = MyResource.Resource.KPROF_KARTE_PROFIL_INFO
                };
                _karteKostenprofil.Geklickt += (s, e) =>
                {
                    KostenprofilBearbeiten();
                    AktualisiereKostenprofilKarte();
                };

                _karteSpotpreise = new EinstiegsKarte
                {
                    Location = new Point(488, 24),
                    Size = new Size(440, 168),
                    Titel = MyResource.Resource.KPROF_KARTE_SPOT_TITEL,
                    Beschreibung = MyResource.Resource.KPROF_KARTE_SPOT_INFO
                };
                _karteSpotpreise.Geklickt += (s, e) =>
                {
                    using (Form_SpotpreisImport dlg = new Form_SpotpreisImport(m_ID_Projekt))
                        dlg.ShowDialog(this);
                    AktualisiereSpotpreisKarte();
                };

                tabKostenprofil.Controls.Add(_karteKostenprofil);
                tabKostenprofil.Controls.Add(_karteSpotpreise);

                // Ans Ende — der Reiter steht damit als vierter hinter „Energiekosten".
                tabMain.TabPages.Add(tabKostenprofil);

                AktualisiereKostenprofilKarte();
                AktualisiereSpotpreisKarte();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Reiter Kostenprofil konnte nicht aufgebaut werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Statuszeile der Karte „Kostenprofil": Name und Monatsniveau des ersten
        /// Projektprofils, sonst der Hinweis, dass noch keines angelegt ist.
        /// </summary>
        private void AktualisiereKostenprofilKarte()
        {
            if (_karteKostenprofil == null) return;

            try
            {
                KostenprofilCtrl ctrl = new KostenprofilCtrl();
                List<KostenprofilModel> vorhandene = ctrl.ReadAllByProjekt(m_ID_Projekt);

                if (vorhandene.Count == 0)
                {
                    _karteKostenprofil.Status = MyResource.Resource.KPROF_STATUS_KEIN_PROFIL;
                    return;
                }

                KostenprofilModel m = vorhandene[0];
                double min, max;
                if (MonatsniveauSpanne(m.Monatswerte, out min, out max))
                    _karteKostenprofil.Status = string.Format(MyResource.Resource.KPROF_STATUS_PROFIL,
                                                              m.Bezeichner,
                                                              min.ToString("N2", BerichtTexte.Kultur),
                                                              max.ToString("N2", BerichtTexte.Kultur));
                else
                    _karteKostenprofil.Status = m.Bezeichner;
            }
            catch
            {
                // Lesefehler (z. B. Tab_Kostenprofil noch nicht migriert) bleiben still:
                // Der Reiter ist ein Einstieg, kein Prüfbericht — eine MessageBox beim
                // bloßen Öffnen des Kostendialogs wäre hier nur im Weg.
                _karteKostenprofil.Status = STRICH;
            }
        }

        /// <summary>
        /// Statuszeile der Karte „Spotmarktpreise": Anzahl der verfügbaren Reihen und die
        /// Spanne ihrer Kalenderjahre.
        /// </summary>
        private void AktualisiereSpotpreisKarte()
        {
            if (_karteSpotpreise == null) return;

            try
            {
                PreisreiheCtrl ctrl = new PreisreiheCtrl();
                List<PreisreiheModel> reihen = ctrl.ReadVerfuegbare(m_ID_Projekt);

                if (reihen.Count == 0)
                {
                    _karteSpotpreise.Status = MyResource.Resource.KPROF_STATUS_KEINE_REIHEN;
                    return;
                }

                int minJahr = int.MaxValue, maxJahr = int.MinValue;
                foreach (PreisreiheModel r in reihen)
                {
                    if (r.Jahr <= 0) continue;                 // ungepflegtes Jahr nicht mitspannen
                    if (r.Jahr < minJahr) minJahr = r.Jahr;
                    if (r.Jahr > maxJahr) maxJahr = r.Jahr;
                }

                if (minJahr > maxJahr)
                    _karteSpotpreise.Status = string.Format(MyResource.Resource.KPROF_STATUS_REIHEN_OHNE_JAHR,
                                                            reihen.Count);
                else if (minJahr == maxJahr)
                    _karteSpotpreise.Status = string.Format(MyResource.Resource.KPROF_STATUS_REIHEN_EINJAHR,
                                                            reihen.Count, minJahr);
                else
                    _karteSpotpreise.Status = string.Format(MyResource.Resource.KPROF_STATUS_REIHEN,
                                                            reihen.Count, minJahr, maxJahr);
            }
            catch
            {
                _karteSpotpreise.Status = STRICH;
            }
        }

        /// <summary>
        /// Kleinstes und größtes Monatsniveau [ct/kWh] aus dem Ablageformat
        /// „m1;…;m12" (InvariantCulture, wie <see cref="Form_Kostenprofil"/> es schreibt).
        /// <c>false</c>, wenn kein einziger Wert lesbar war.
        /// </summary>
        private static bool MonatsniveauSpanne(string monatswerte, out double min, out double max)
        {
            min = 0; max = 0;
            if (string.IsNullOrWhiteSpace(monatswerte)) return false;

            bool gefunden = false;
            foreach (string teil in monatswerte.Split(';'))
            {
                double w;
                if (!double.TryParse(teil, System.Globalization.NumberStyles.Float,
                                     System.Globalization.CultureInfo.InvariantCulture, out w))
                    continue;

                if (!gefunden) { min = w; max = w; gefunden = true; }
                else { if (w < min) min = w; if (w > max) max = w; }
            }
            return gefunden;
        }

        /// <summary>
        /// Entfernt die blauen Kopfleisten „Energieträger" über den linken Listen ALLER
        /// DREI Bestandsreiter und zieht die jeweilige Liste um die Leistenhöhe nach oben
        /// (K4/HF4 6.2 und K4-Nachtrag 20.08.2026).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Was weg ist.</b> Drei baugleiche Leisten, je 343 × 25 px bei (6, 7) in
        /// <c>#1A3261</c>, mit je einer Beschriftung „Energieträger":
        /// </para>
        /// <list type="table">
        ///   <item><term>Investitionskosten</term>
        ///         <description><c>panel3</c> → <c>panel2</c> → <c>label5</c>, Liste <c>listBox_Erzeuger</c></description></item>
        ///   <item><term>Betriebskosten</term>
        ///         <description><c>panel4</c> → <c>panel5</c> → <c>label1</c>, Liste <c>listBox_Betriebskosten</c></description></item>
        ///   <item><term>Energiekosten</term>
        ///         <description><c>panel8</c> → <c>panel9</c> → <c>label4</c>, Liste <c>listBox_Energieträger</c></description></item>
        /// </list>
        /// <para>
        /// <b>Warum alle drei.</b> K4 nahm zunächst nur die Leiste im Energie-Reiter. Die
        /// Sichtabnahme zeigte die beiden anderen — und auf „Investitionskosten" ist die
        /// Beschriftung obendrein sachlich falsch: Dort stehen GEWERKE (Heizkessel,
        /// Pufferspeicher, BHKW), keine Energieträger. Eine falsche Überschrift ist
        /// schlechter als keine, und da die Listen in ihrem Zusammenhang selbsterklärend
        /// sind, fällt die Zeile ersatzlos weg statt umbenannt zu werden.
        /// </para>
        /// <para>
        /// <b>Warum programmatisch und nicht im Designer.</b> Die Hausregel in
        /// <c>CLAUDE.md</c> untersagt das Editieren von Designer-Dateien von Hand; das
        /// Konzept wiederholt sie für HF4 ausdrücklich. Ein Eingriff im Designer hätte je
        /// Leiste vier Stellen der <c>InitializeComponent</c> treffen müssen
        /// (Felddeklaration, <c>new</c>, <c>SuspendLayout</c>/<c>ResumeLayout</c>,
        /// <c>Controls.Add</c>) und wäre beim nächsten Öffnen im WinForms-Designer erneut
        /// zu verteidigen. Das Entfernen zur Laufzeit steht an EINER Stelle, ist dort
        /// begründet und rückstandsfrei umkehrbar.
        /// </para>
        /// <para>
        /// <b>Was bleibt.</b> <c>label3</c>, <c>label2</c> und <c>label6</c>
        /// („Energieträger auswählen", 18 pt, bei (209, 246) in <c>panel1</c>/<c>panel6</c>/
        /// <c>panel7</c>) gehören NICHT zu den Kopfleisten: Sie stehen als Platzhalter
        /// mitten im rechten Detailbereich und sagen dort, was zu tun ist, solange nichts
        /// gewählt ist. Sie bleiben unberührt.
        /// </para>
        /// </remarks>
        private void KopfzeilenEntfernen()
        {
            KopfleisteEntfernen(panel2, listBox_Erzeuger, "Investitionskosten");
            KopfleisteEntfernen(panel5, listBox_Betriebskosten, "Betriebskosten");
            KopfleisteEntfernen(panel9, listBox_Energieträger, "Energiekosten");
        }

        /// <summary>
        /// Nimmt EINE Kopfleiste aus ihrem Elternpanel und zieht die darunterliegende
        /// Liste um den gewonnenen Platz nach oben — die Unterkante der Liste bleibt, wo
        /// sie war, damit der Abstand zu allem darunter erhalten bleibt.
        /// </summary>
        private void KopfleisteEntfernen(Panel leiste, Control liste, string reiter)
        {
            try
            {
                if (leiste == null || leiste.Parent == null || liste == null) return;

                Control eltern = leiste.Parent;
                int obenNeu = leiste.Top;                 // 7
                int gewinn = liste.Top - obenNeu;         // 37 − 7 = 30

                eltern.Controls.Remove(leiste);
                leiste.Dispose();                         // nimmt die Beschriftung mit

                if (gewinn > 0)
                {
                    liste.Top = obenNeu;
                    liste.Height += gewinn;               // Unterkante bleibt, wo sie war
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Kopfleiste im Reiter " + reiter +
                                  " konnte nicht entfernt werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Öffnet den Kostenprofil-Editor: das erste Profil des Projekts, oder ein neues,
        /// wenn noch keines existiert.
        /// </summary>
        /// <remarks>
        /// Bewusst keine eigene Auswahlmaske: Ein Projekt führt in aller Regel EIN
        /// Kostenprofil. Mehrere Profile bleiben über die Variantenauswahl auf der
        /// Speicher-Parameterseite erreichbar; eine dritte Liste hier wäre Beiwerk.
        /// </remarks>
        private void KostenprofilBearbeiten()
        {
            KostenprofilCtrl ctrl = new KostenprofilCtrl();
            var vorhandene = ctrl.ReadAllByProjekt(m_ID_Projekt);
            int id = vorhandene.Count > 0 ? vorhandene[0].ID : 0;

            using (Form_Kostenprofil dlg = new Form_Kostenprofil(m_ID_Projekt, id))
                dlg.ShowDialog(this);
        }

        private void Form_Kosten_Load(object sender, EventArgs e)
        {
            // Designer-Schutz (wichtig!)
            if (this.DesignMode) return;

            // Die HilfeAutomatik täte das ohnehin; der Aufruf schadet nicht.
            _helpExtender?.RegisterForm(this);

            // Fenster an die aktuelle Bildschirmauflösung anpassen, damit auf
            // kleineren Bildschirmen nichts abgeschnitten wird (Scrollbars in den
            // Tabs übernehmen den Rest).
            FensterAnBildschirmAnpassen();
        }

        /// <summary>
        /// Klemmt die Fenstergröße auf den nutzbaren Bildschirmbereich (ohne
        /// Taskleiste) und zentriert das Fenster. Passt das Formular in seiner
        /// vollen Größe (1015 × 839 zzgl. Rahmen) nicht auf den Bildschirm, wird
        /// es verkleinert; die AutoScroll-Tabs (tabInvest/tabWartung/tabEnergie)
        /// zeigen dann bei Bedarf Scrollleisten. Kopf- (pnlHeader) und Fußzeile
        /// (pnlFooter) bleiben dank Dock=Top/Bottom fixiert.
        /// </summary>
        private void FensterAnBildschirmAnpassen()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;

            int w = Math.Min(this.Width, wa.Width);
            int h = Math.Min(this.Height, wa.Height);
            this.Size = new Size(w, h);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(
                wa.Left + Math.Max(0, (wa.Width - w) / 2),
                wa.Top + Math.Max(0, (wa.Height - h) / 2));
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Befund B6 (11.08.2026): Energiepreise wurden nur über den Speichern-Button
        /// des ucFuelSettings-Controls persistiert — beim Schließen des Formulars
        /// gingen offene Eingaben verloren. Jetzt speichert das Schließen den
        /// aktuell geöffneten Energieträger mit (gleiche Logik wie der Button).
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                foreach (Control c in flpContainer_Energiekosten.Controls)
                {
                    ucFuelSettings uc = c as ucFuelSettings;
                    if (uc == null) continue;

                    // Nur speichern, wenn der Träger dem Projekt noch zugeordnet ist —
                    // sonst würde ein zuvor gelöschter Träger wieder angelegt.
                    int zugeordnet = Convert.ToInt32(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM energy_project_settings " +
                        "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                        new OleDbParameter("@p", m_ID_Projekt),
                        new OleDbParameter("@c", uc.CarrierId)));
                    if (zugeordnet > 0) uc.SaveProjectAndHistory();
                }
            }
            catch { /* Schließen nie am Speichern scheitern lassen */ }
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Summen je Komponente aus <c>Tab_ProjektWerte</c> — <b>getrennt nach Kategorie</b>
        /// (1 Investition, 2 Betrieb, 3 Energie). Spalten der Rückgabe: <c>Komponente</c>,
        /// <c>Summe</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Befund D1 (18.08.2026): Beide Aufrufer lasen zuvor die gespeicherte Abfrage
        /// <c>Abfrage_KostenKomponenten</c>. Die summiert <c>EingegebenerWert</c> nur über
        /// ProjektID und Komponente und filtert <b>nicht</b> nach <c>KategorieID</c> —
        /// Investitions-, Betriebs- und Energiepositionen derselben Komponente landeten in
        /// einer Zahl. Nachweis Projekt 1024: Wärmepumpe 6.100 € = 6.001 € (Investition) +
        /// 99 € (Betrieb), während die Investitions-Kachel der Kostenseite korrekt
        /// 12.001,00 € zeigte und die Tabelle darunter 12.100,00 €.
        /// </para>
        /// <para>
        /// Bewusst als eigenes parametrisiertes SQL statt einer Korrektur der gespeicherten
        /// Abfrage: Die Datenbank liegt außerhalb des Repos, eine Abfrageänderung erreicht
        /// Bestandsinstallationen nur über einen Migrationsschritt.
        /// </para>
        /// <para>
        /// <c>internal</c>, damit die Kompaktanzeige der Seite „Kosten"
        /// (<see cref="UcBkKosten"/>) dieselbe Leselogik verwendet und keine zweite entsteht —
        /// gleiche Begründung wie bei <see cref="WirtschaftlichkeitCtrl.LiesInvestitionen"/>.
        /// </para>
        /// </remarks>
        internal static DataTable LiesKomponentenSummen(int projektID, int kategorieID)
        {
            string sql = @"SELECT k.Komponente, Sum(w.EingegebenerWert) AS Summe
                           FROM Tab_KostenKomponente AS k
                                INNER JOIN Tab_ProjektWerte AS w ON k.ID = w.KomponentenID
                           WHERE w.ProjektID = ? AND w.KategorieID = ?
                           GROUP BY k.Komponente";

            return DataRepository.GetDataTable(sql,
                new OleDbParameter("@pid", projektID),
                new OleDbParameter("@kat", kategorieID));
        }

        /// <summary>
        /// Energiekosten p. a. des Projekts [€/a] aus <see cref="KostenEmissionRechner"/> —
        /// <c>null</c>, wenn kein Simulationsergebnis vorliegt oder der Rechner keine
        /// vollständige Summe bilden kann.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Derselbe Weg, den <see cref="BetriebskostenCtrl"/> für die Brennstoffkosten geht:
        /// <c>KostenEmissionRechner</c> ist die EINE Stelle, die Verbrauchsmengen mit
        /// Trägerpreisen und Heizwerten verrechnet. Eine zweite Preisverrechnung für das
        /// Summen-Label wäre eine doppelte Wahrheit.
        /// </para>
        /// <para>
        /// Der Rechner liefert bewusst <c>null</c> statt einer Teilsumme, wenn für einen
        /// Träger mit Verbrauch der Preis fehlt. Diese Aussage wird hier nicht eingeebnet —
        /// die Fußzeile zeigt dann „—".
        /// </para>
        /// </remarks>
        private double? LiesEnergiekostenProJahr()
        {
            try
            {
                ErgebnisModel erg = new ErgebnisCtrl().Load(m_ID_Projekt);
                if (erg == null) return null;

                VariantenDaten v = new VariantenDaten { IdProjekt = m_ID_Projekt, Ergebnis = erg };
                KostenEmissionRechner.Berechne(v);
                return v.Energiekosten;
            }
            catch { return null; }
        }

        private void Gesamtkosten(string aktuelleSelektion = "")
        {
            // K4-Wächter: Der Reiter „Kostenprofil" führt keine Kostenkategorie — dort gibt
            // es nichts zu summieren. Ohne diese Klammer stünde im Fuß die Summe des zuvor
            // gewählten Reiters unter der Überschrift „Kostenprofil".
            int? kat = AktuelleKategorieOderNull();
            if (!kat.HasValue)
            {
                label_ErzeugerGesamt.Text = "-";
                label_Gesamt.Text = string.Format(MyResource.Resource.KOSTEN_LBL_PROJEKT_GESAMT,
                                                  kategorie, STRICH);
                label_ErzeugerGesamt.Refresh();
                label_Gesamt.Refresh();
                return;
            }

            decimal summeSelektion = 0;

            // Die Summe der AKTUELLEN Selektion direkt aus den Controls lesen (Live-Werte).
            // K5b: Zuschusspositionen sind positiv erfasst und MINDERN die Investition —
            // sie gehen deshalb mit negativem Vorzeichen ein, genau wie in den
            // Gruppensummen darüber. Stünde hier die rohe Addition, widerspräche die
            // Fußzeile den Köpfen auf demselben Bildschirm.
            foreach (Control c in flp.Controls)
            {
                if (c is ucKostenZeile zeile)
                {
                    summeSelektion += zeile.Daten.IstZuschuss
                        ? -zeile.Daten.Betrag : zeile.Daten.Betrag;
                }
            }

            // K5b: Die Köpfe tragen Zähler und Summe — beide ändern sich mit jeder Zahl.
            GruppenkoepfeNachziehen(_letzteKomponente);

            // Anzeige aktualisieren
            if (aktuelleSelektion != "")
                label_ErzeugerGesamt.Text = $"{kategorie} ({aktuelleSelektion}): {summeSelektion:N2} €";
            else
                label_ErzeugerGesamt.Text = "-";

            string gesamtText;

            if (kat.Value == KATEGORIE_ENERGIE)
            {
                // K4/HF4 6.2: Die Fußzeile des Energie-Reiters kommt aus dem
                // KostenEmissionRechner (Energiekosten p. a. des Projekts).
                //
                // Die frühere Kategorie-3-Summe aus Tab_ProjektWerte ist eine TOTE Quelle:
                // Seit HF1/L1 (19.08.2026) wird auf Kategorie 3 nichts mehr geschrieben,
                // die Fußzeile stand deshalb konstant auf „0,00 €". Die Lesestelle
                // LiesKomponentenSummen(…, KATEGORIE_ENERGIE) ist für diesen Reiter damit
                // stillgelegt; die Altzeilen-Löschung folgt K6/E3.
                double? energie = LiesEnergiekostenProJahr();
                gesamtText = energie.HasValue ? energie.Value.ToString("N2") : STRICH;
            }
            else
            {
                // Die Gesamtsumme der GERADE ANGEZEIGTEN Kategorie aus der Datenbank.
                decimal summeGesamt = 0;
                DataTable dt = LiesKomponentenSummen(m_ID_Projekt, kat.Value);

                // Durch die Zeilen loopen (ersetzt den Reader)
                foreach (DataRow row in dt.Rows)
                {
                    decimal betrag = row["Summe"] != DBNull.Value ? Convert.ToDecimal(row["Summe"]) : 0;
                    summeGesamt += betrag;
                }

                // K5b: LiesKomponentenSummen summiert die Kategorie roh — der positiv
                // erfasste Zuschuss steckt darin und würde die Projektsumme ERHÖHEN.
                // Abgezogen wird er hier statt in der gemeinsamen Lesemethode: Die dient
                // auch der Komponententabelle von UcBkKosten, und deren Verhalten soll
                // diese Etappe nicht mitverändern.
                if (kat.Value == KATEGORIE_INVESTITION)
                {
                    try
                    {
                        double zuschuss = WirtschaftlichkeitCtrl.LiesZuschuss(
                            m_ID_Projekt, WirtschaftlichkeitSzenario.ERWARTET);
                        summeGesamt -= (decimal)zuschuss;
                    }
                    catch { }
                }

                gesamtText = summeGesamt.ToString("N2");
            }

            // Die Kategorie steht mit im Text: Investitions-, Betriebs- und Energiekosten
            // haben verschiedene Bezugsgrößen (€ gegenüber €/a) und dürfen nicht als eine
            // Zahl gelesen werden.
            label_Gesamt.Text = string.Format(MyResource.Resource.KOSTEN_LBL_PROJEKT_GESAMT,
                                              kategorie, gesamtText);

            label_ErzeugerGesamt.Refresh();
            label_Gesamt.Refresh();
        }

        // Beispiel: Wenn links eine Komponente (z.B. BHKW) gewählt wird
        private void UpdateDetailPanel(string komponente, List<KostenPosition> faktoren)
        {
            flp.Controls.Clear();
            _gruppen.Clear();                 // K5b: die Blöcke gehören zur alten Liste
            flp.SuspendLayout();

            // Berechnung verfügbare Innenbreite
            // ClientSize.Width zieht die Scrollbar bereits automatisch ab.
            int targetWidth = flp.ClientSize.Width - flp.Padding.Left - flp.Padding.Right;

            // Falls ein kleiner Sicherheitsabstand zum rechten Rand sein soll (z.B. 5 Pixel):
            targetWidth -= 5;

            // Hinweiszeile über der Liste: Grund bzw. Herleitung der
            // Betriebskosten-Vorbelegung. Das ist eine Mitteilung, kein Eingabefeld —
            // deshalb steht sie vor der ersten Gruppe.
            HinweiszeileAnlegen(komponente, targetWidth);

            string aktuelleGruppe = "";

            foreach (var f in faktoren)
            {
                if (f.Gruppenname.Trim() != aktuelleGruppe.Trim())
                {
                    aktuelleGruppe = f.Gruppenname.Trim();

                    // Wir erstellen ein Panel als Container für den Header
                    Panel headerPanel = new Panel
                    {
                        Size = new Size(targetWidth, 30),
                        BackColor = Color.FromArgb(20, 40, 80),
                        Margin = new Padding(0, 10, 0, 0),
                        Tag = aktuelleGruppe.Trim() // Wichtig für die Lösch-Identifizierung
                    };

                    // Das Label für den Text. Der WORTLAUT entsteht erst in
                    // GruppenkoepfeNachziehen (K5b) — dort stehen Positionszähler und
                    // Gruppensumme, und die kennt man erst, wenn alle Zeilen gebaut sind.
                    Label groupTitle = new Label
                    {
                        Text = aktuelleGruppe.ToUpper().Trim(),
                        Font = new Font(this.Font, FontStyle.Bold),
                        ForeColor = Color.White,
                        AutoSize = true, // Wichtig: Passt sich dem Text an
                        Location = new Point(5, 7), // Ein bisschen Padding von oben/links
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    Button btnTest = null;
                    // Der Button erscheint nur in der Hauptgruppe (z.B. "BHKW") und nur auf
                    // dem Reiter, für den er gedacht ist: „Betriebskosten VDI 2067…" beim
                    // BHKW auf dem Betriebskostenreiter.
                    //
                    // NUTZERENTSCHEID 23.08.2026: „Planwert übernehmen…" ist entfallen. Der
                    // Knopf stand auf JEDER Hauptgruppe des Investitionsreiters — auch auf
                    // „Bauliche Anlagen", „Wärmezentrale" und „Stromeinspeisung", die gar
                    // keine Technik und damit keinen Planwert haben; dort meldete er nur
                    // „für … sind keine Technik-Planwerte hinterlegt". Was er anbot, war ohne
                    // Kenntnis der Kostenbasen nicht zu erraten, und zu holen gab es zuletzt
                    // nur beim BHKW etwas.
                    // Die Vorbelegung beim ERSTEN Anwählen bleibt unberührt
                    // (EnsureMainComponentExists, NebenkostenAnlegen); von hier aus wird
                    // kein erfasster Betrag mehr überschrieben.
                    if (f.IsMainComponent && kategorieID == KATEGORIE_BETRIEB &&
                        string.Equals(komponente, DbWerte.ERZEUGER_BHKW, StringComparison.Ordinal))
                    {
                        btnTest = new Button
                        {
                            Text = MyResource.Resource.KOSTEN_BTN_VDI2067,
                            Height = 20,
                            Width = 200,
                            AutoSize = false,
                            FlatStyle = FlatStyle.Flat,
                            ForeColor = Color.White,
                            BackColor = Color.FromArgb(0, 120, 215),
                            Cursor = Cursors.Hand,
                            Font = new Font("Segoe UI", 8, FontStyle.Bold),
                            Location = new Point(groupTitle.PreferredWidth + 20, 5)
                        };
                        btnTest.FlatAppearance.BorderSize = 0;
                        btnTest.Click += (s, e) => btnBetriebskostenVdi_Click(komponente);
                    }

                    // Der Lösch-Button (-)
                    Button btnDeleteGroup = new Button
                    {
                        Text = "-",
                        Size = new Size(25, 25),
                        AutoSize = false,
                        //Anchor = AnchorStyles.Right, // Er bleibt rechts, behält aber seine Größe
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.White,
                        BackColor = Color.Firebrick, // Dezentes Rot
                        Cursor = Cursors.Hand,
                        Tag = aktuelleGruppe.Trim(), // Speichert den Gruppennamen für das Event
                        Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        // Manuelle Positionierung:
                        //X = Panelbreite - Buttonbreite - kleiner Abstand (z.B. 2px)
                        // Y = (Panelhöhe 30 - Buttonhöhe 25) / 2 = 2 oder 3
                        Location = new Point(targetWidth - 27, 2)
                    };

                    btnDeleteGroup.FlatAppearance.BorderSize = 0;
                    btnDeleteGroup.Click += btnDeleteGroup_Click; // Event verknüpfen
                    btnDeleteGroup.MinimumSize = new Size(25, 25);
                    btnDeleteGroup.MaximumSize = new Size(25, 25);

                    // Controls zum Header-Panel hinzufügen
                    headerPanel.Controls.Add(groupTitle);
                    if (btnTest != null)
                    {
                        headerPanel.Controls.Add(btnTest);
                    }
                    headerPanel.Controls.Add(btnDeleteGroup);
                    Panel columnHeader = CreateColumnHeader(aktuelleGruppe.Trim());
                    // WICHTIG: Auch hier exakt targetWidth
                    columnHeader.Width = targetWidth;

                    flp.Controls.Add(headerPanel);
                    flp.Controls.Add(columnHeader);

                    // --- K5b: Der Kopf wird zum Ein-/Ausklapper ------------------------
                    // Angeklickt wird der Kopf selbst oder seine Beschriftung. Die Knöpfe
                    // darauf bleiben unberührt: Ein Click auf ein Kind-Control erreicht das
                    // Panel nicht, „Betriebskosten VDI 2067…" und der Lösch-Knopf arbeiten
                    // also weiter wie bisher.
                    var block = new Gruppenblock
                    {
                        Name = aktuelleGruppe.Trim(),
                        Kopf = headerPanel,
                        Titel = groupTitle,
                        Spaltenkopf = columnHeader,
                        Eingeklappt = false,
                        Aktion = btnTest
                    };
                    _gruppen[block.Name] = block;

                    headerPanel.Cursor = Cursors.Hand;
                    groupTitle.Cursor = Cursors.Hand;
                    headerPanel.Click += (s, e) => GruppeUmschalten(block);
                    groupTitle.Click += (s, e) => GruppeUmschalten(block);
                }

                var zeile = new ucKostenZeile(f);
                zeile.Width = targetWidth;

                // Das Event abfangen
                zeile.ValueChanged += (s, e) =>
                {
                    // 1. Datenbank für genau diese StammID updaten
                    UpdateSingleRowInDatabase(zeile.Daten);

                    // 2. UI Summe aktualisieren
                    Gesamtkosten(listBox_Erzeuger.Text);
                };

                zeile.Tag = aktuelleGruppe.Trim();
                if (f.IsMainComponent)
                {
                    zeile.BackColor = Color.LightSteelBlue;
                    //zeile.Font = new Font(zeile.Font, FontStyle.Bold);
                    zeile.Margin = new Padding(0, 1, 0, 1);
                }
                zeile.DeleteRequested += Zeile_DeleteRequested;
                zeile.Daten.Komponente = komponente; //listBox_Erzeuger.Text; 
                zeile.Height = 25;

                flp.Controls.Add(zeile);
            }

            // K5b: Beschriftungen der Gruppenköpfe — erst jetzt sind alle Zeilen da.
            GruppenkoepfeNachziehen(komponente);

            flp.ResumeLayout();
        }

        // ================================================================= K5b (HF5 § 7.5)

        /// <summary>
        /// Klappt einen Gruppenblock ein oder aus.
        /// </summary>
        /// <remarks>
        /// Eingeklappt werden die Positionszeilen und die Spaltenüberschrift; der Kopf
        /// bleibt stehen und trägt weiterhin Zähler und Summe. Die Zeilen werden nur
        /// UNSICHTBAR, nicht entfernt — jede erfasste Zahl bleibt damit im Speicher, und
        /// <see cref="Gesamtkosten"/> summiert unverändert über <c>flp.Controls</c>. Ein
        /// eingeklappter Block verändert also keine einzige Summe.
        /// </remarks>
        private void GruppeUmschalten(Gruppenblock block)
        {
            if (block == null) return;

            block.Eingeklappt = !block.Eingeklappt;
            bool sichtbar = !block.Eingeklappt;

            flp.SuspendLayout();
            try
            {
                if (block.Spaltenkopf != null && !block.Spaltenkopf.IsDisposed)
                    block.Spaltenkopf.Visible = sichtbar;

                foreach (Control c in flp.Controls)
                    if (c is ucKostenZeile && GleicheGruppe(c, block.Name))
                        c.Visible = sichtbar;

                KopfBeschriften(block, _letzteKomponente);
            }
            finally { flp.ResumeLayout(); }
        }

        /// <summary>Komponente der zuletzt aufgebauten Positionsliste (K5b).</summary>
        private string _letzteKomponente = "";

        /// <summary>true, wenn das Steuerelement zu dieser Gruppe gehört.</summary>
        private static bool GleicheGruppe(Control c, string gruppe)
        {
            return c != null && c.Tag != null &&
                   string.Equals(c.Tag.ToString(), gruppe, StringComparison.Ordinal);
        }

        /// <summary>
        /// Schreibt alle Gruppenköpfe neu: Komponentenname, Positionszähler, Gruppensumme.
        /// </summary>
        private void GruppenkoepfeNachziehen(string komponente)
        {
            _letzteKomponente = komponente ?? "";
            foreach (Gruppenblock b in _gruppen.Values) KopfBeschriften(b, _letzteKomponente);
        }

        /// <summary>
        /// Beschriftet EINEN Gruppenkopf: „▾ Wärmezentrale · 3 Positionen · 42.500 €".
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Komponente steht vorn, nicht die Gruppe.</b> Die Liste zeigt immer genau
        /// eine Komponente (Auswahl links); ihr Name ist die Auskunft, die der Anwender
        /// sucht. Der freie Gruppenname aus <c>Tab_ProjektWerte.Gruppe</c> kommt nur dann
        /// dazu, wenn er nicht die Rückfallgruppe „Allgemein" ist — sonst stünde in der
        /// Regel „WÄRMEZENTRALE · ALLGEMEIN" da, und das Wort ohne Aussage wäre das
        /// auffälligste im Kopf.
        /// </para>
        /// <para>
        /// <b>Zuschusspositionen zählen NEGATIV.</b> Sie sind positiv erfasst und mindern
        /// die Investition (Konzept § 7.4); eine Gruppensumme, die sie aufaddiert, wäre um
        /// das Doppelte des Zuschusses zu hoch.
        /// </para>
        /// <para>
        /// <b>Nur sichtbare Zeilen zählen? Nein — alle.</b> Der Zähler nennt den Inhalt der
        /// Gruppe, nicht den Bildschirmausschnitt. Ein eingeklappter Kopf muss weiter
        /// sagen, was in ihm steckt; genau dafür ist er da.
        /// </para>
        /// </remarks>
        private void KopfBeschriften(Gruppenblock block, string komponente)
        {
            if (block == null || block.Titel == null || block.Titel.IsDisposed) return;

            int anzahl = 0;
            decimal summe = 0;

            foreach (Control c in flp.Controls)
            {
                var zeile = c as ucKostenZeile;
                if (zeile == null || !GleicheGruppe(c, block.Name)) continue;

                anzahl++;
                summe += zeile.Daten.IstZuschuss ? -zeile.Daten.Betrag : zeile.Daten.Betrag;
            }

            string name = string.IsNullOrEmpty(komponente) ? block.Name : komponente;
            if (!string.Equals(block.Name, DbWerte.KOSTEN_GRUPPE_ALLGEMEIN, StringComparison.Ordinal))
                name = name + " · " + block.Name;

            block.Titel.Text = string.Format(
                block.Eingeklappt ? MyResource.Resource.KOSTEN_GRUPPE_KOPF_ZU
                                  : MyResource.Resource.KOSTEN_GRUPPE_KOPF_AUF,
                name.ToUpper(),
                anzahl,
                summe.ToString("N2", BerichtTexte.Kultur));

            // Der Aktionsknopf saß bisher hinter der KURZEN Beschriftung („ALLGEMEIN").
            // Mit Zähler und Summe wird sie länger — ohne Nachrücken läge das Label unter
            // dem Knopf. Der Lösch-Knopf bleibt rechts verankert und ist nicht betroffen.
            if (block.Aktion != null && !block.Aktion.IsDisposed)
            {
                int x = block.Titel.Left + block.Titel.PreferredWidth + 20;
                int grenze = block.Kopf.Width - block.Aktion.Width - 32;   // Platz für „−"
                block.Aktion.Left = Math.Min(x, Math.Max(block.Titel.Left, grenze));
            }
        }

        private void btnDeleteGroup_Click(object sender, EventArgs e)
        {
            // K4-Wächter: Der Löschbefehl unten filtert auf KategorieID. Ohne Kategorie
            // gibt es nichts zu löschen — und ein DELETE mit unbestimmter Kategorie ist
            // genau die Art Befehl, die man nicht ins Blaue absetzt.
            if (!AktuelleKategorieOderNull().HasValue) return;

            Button btn = (Button)sender;
            string gruppenName = btn.Tag.ToString();

            List<ucKostenZeile> gruppenZeilen = new List<ucKostenZeile>();
            bool enthältMainComponent = false;

            // alle Zeilen dieser Gruppe im Container
            foreach (Control c in flp.Controls)
            {
                if (c is ucKostenZeile zeile && c.Tag?.ToString() == gruppenName)
                {
                    gruppenZeilen.Add(zeile);
                    if (zeile.Daten.IsMainComponent)
                    {
                        enthältMainComponent = true;
                    }
                }
            }

            // --- LOGIK-SPERRE ---
            // Wenn die Gruppe eine MainComponent enthält UND dies das einzige Element ist, 
            // oder wenn die Gruppe NUR aus der MainComponent besteht: Nichts tun.
            if (enthältMainComponent && gruppenZeilen.Count <= 1)
            {
                return; // Einfach abbrechen, keine MessageBox, keine Aktion.
            }

            // MessageBox nur zeigen, wenn löschbare (nicht-Main) Komponenten existieren
            string meldung = $"Möchten Sie die Gruppe '{gruppenName}' mit allen Kostenfaktoren löschen? (Die Hauptkomponente bleibt erhalten)";

            var confirm = MessageBox.Show(meldung, "Gruppe leeren", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    // Datenbank: Nur die Faktoren löschen, die KEINE MainComponent sind
                    DeleteGruppeAusDatenbank(gruppenName, m_ID_Projekt, kategorieID);

                    // UI: Nur die Zeilen entfernen, die keine MainComponent sind
                    flp.SuspendLayout();
                    //                    flpContainer.SuspendLayout();
                    //                    for (int i = flpContainer.Controls.Count - 1; i >= 0; i--)
                    for (int i = flp.Controls.Count - 1; i >= 0; i--)
                    {
                        Control c = flp.Controls[i];
                        if (c.Tag?.ToString() == gruppenName)
                        {
                            // Falls es eine Zeile ist, prüfen wir IsMainComponent
                            if (c is ucKostenZeile zeile)
                            {
                                if (zeile.Daten.IsMainComponent) continue; // MainComponent überspringen
                            }

                            // ColumnHeader und normale Zeilen löschen
                            // (Das Header-Panel mit dem Namen lassen wir evtl. auch stehen?)
                            if (c is Panel && c.Height > 25) continue; // Header stehen lassen

                            flp.Controls.Remove(c);
                            c.Dispose();
                        }
                    }
                    flp.ResumeLayout();

                    Gesamtkosten(listBox_Erzeuger.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Bereinigen der Gruppe: " + ex.Message);
                }
            }
        }

        private void DeleteGruppeAusDatenbank(string gruppenName, int projektID, int kategorieID)
        {
            try
            {
                // Löscht alle Faktoren dieser Gruppe aus dem aktuellen Projekt
                string sqlDeleteProjektWerte = "DELETE FROM Tab_ProjektWerte WHERE Gruppe = ? AND ProjektID = ? AND KategorieID=?";

                DataRepository.ExecuteSQL(sqlDeleteProjektWerte,
                    new OleDbParameter("@gName", gruppenName),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@pIDkat", kategorieID));

                // Cleanup Katalog: Lösche Gruppe nur, wenn sie nirgendwo mehr verwendet wird
                // Hinweis: Access braucht den Parameter hier 2x, weil 2 Fragezeichen im SQL sind
                string sqlCleanupKatalog = @"DELETE FROM Tab_KostenGruppenKatalog 
                                     WHERE GruppenName = ? 
                                     AND NOT EXISTS (SELECT 1 FROM Tab_ProjektWerte WHERE Gruppe = ?)";

                DataRepository.ExecuteSQL(sqlCleanupKatalog,
                    new OleDbParameter("@g1", gruppenName),
                    new OleDbParameter("@g2", gruppenName));

                // Optional: UI Logik zum Refresh danach aufrufen
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Löschen der Gruppe: " + ex.Message);
            }
        }

        private void Zeile_DeleteRequested(object sender, EventArgs e)
        {
            if (sender is ucKostenZeile zeile)
            {
                int stammID = zeile.Daten.StammID;
                int datensatzID = zeile.Daten.ID; // Falls du lieber über die Primär-ID löschst

                // UI-Aufräumarbeiten
                zeile.DeleteRequested -= Zeile_DeleteRequested;
                flp.Controls.Remove(zeile);
                zeile.Dispose();

                // Datenbank-Löschung
                string sql = "DELETE FROM Tab_ProjektWerte WHERE ID = ?";

                bool erfolg = DataRepository.ExecuteSQL(sql, new OleDbParameter("@id", datensatzID));

                if (erfolg)
                {
                    Gesamtkosten(listBox_Erzeuger.Text);
                }
            }
        }

        private Panel CreateColumnHeader(string gruppe)
        {
            Panel p = new Panel
            {
                Size = new Size(flpContainer.Width - 25, 20),
                BackColor = Color.LightGray,
                Margin = new Padding(0, 0, 0, 5),
                Tag = gruppe
            };

            // Beispielhafte Labels (Breiten müssen denen im UserControl entsprechen!)
            p.Controls.Add(new Label { Text = "Komponente", Location = new Point(5, 2), Width = 150, Font = new Font(this.Font, FontStyle.Regular) });
            p.Controls.Add(new Label { Text = "Kosten [€]", Location = new Point(160, 2), Width = 80, Font = new Font(this.Font, FontStyle.Regular) });
            p.Controls.Add(new Label { Text = "Einheit", Location = new Point(250, 2), Width = 50, Font = new Font(this.Font, FontStyle.Regular) });
            p.Controls.Add(new Label { Text = "Nutzungsdauer [a]", Location = new Point(310, 2), Width = 100, Font = new Font(this.Font, FontStyle.Regular) });
            p.Controls.Add(new Label { Text = "Worst/Best", Location = new Point(420, 2), Width = 100, Font = new Font(this.Font, FontStyle.Regular) });

            return p;
        }

        private void listBox_Erzeuger_SelectedIndexChanged(object sender, EventArgs e)
        {
            flpContainer.Visible = true;
            btn_Hinzu.Enabled = true;

            string komponente = listBox_Erzeuger.Text;
            //string kategorie = tabMain.SelectedTab.Text;

            EnsureMainComponentExists(m_ID_Projekt, komponente, 0);
            LoadKostenFaktoren(m_ID_Projekt, komponente);
            Gesamtkosten(listBox_Erzeuger.Text);
        }

        public void LoadKostenFaktoren(int projektID, string komponente)
        {
            List<KostenPosition> geladeneFaktoren = new List<KostenPosition>();

            string sql = @"
            SELECT ID, ProjektID, StammID, KategorieName, Komponente, Bezeichnung, 
                   Gruppe, EingegebenerWert, WorstCase, BestCase, Nutzungsdauer, 
                   WorstCase_Nutzungsdauer, BestCase_Nutzungsdauer, Einheit, IsMainComponent
            FROM Abfrage_Kostenfaktoren
            WHERE (KategorieName = ?) AND (Komponente = ?) AND (ProjektID = ?)";

            // Parameter vorbereiten
            OleDbParameter[] ps = {
                new OleDbParameter("@kat", kategorie),
                new OleDbParameter("@komp", komponente),
                new OleDbParameter("@pID", projektID)
            };

            // Repository nutzen, um die Daten zu holen
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Kostenart, Bemessung und Erlöskennzeichen kommen aus einem ZWEITEN Zugriff
            // direkt auf Tab_ProjektWerte und werden über die ID zusammengeführt:
            // Abfrage_Kostenfaktoren ist eine gespeicherte Access-Abfrage AUSSERHALB des
            // Repos; sie zu erweitern erreicht keine Bestandsinstallation (dieselbe
            // Begründung, mit der schon Abfrage_KostenKomponenten abgelöst wurde).
            Dictionary<int, KostenPositionCtrl.Zusatz> zusatz =
                KostenPositionCtrl.LiesZusatz(projektID, kategorieID);

            // Durch die Zeilen loopen (ersetzt den Reader)
            foreach (DataRow row in dt.Rows)
            {
                geladeneFaktoren.Add(new KostenPosition
                {
                    ID = Convert.ToInt32(row["ID"]),
                    Name = row["Bezeichnung"].ToString(),
                    Betrag = row["EingegebenerWert"] != DBNull.Value ? Convert.ToDecimal(row["EingegebenerWert"]) : 0,
                    Einheit = row["Einheit"].ToString(),
                    Nutzungsdauer = row["Nutzungsdauer"] != DBNull.Value ? Convert.ToDecimal(row["Nutzungsdauer"]) : 0,
                    IsMainComponent = Convert.ToBoolean(row["IsMainComponent"]),
                    Gruppenname = row["Gruppe"] != DBNull.Value ? row["Gruppe"].ToString() : "Allgemein",
                    StammID = Convert.ToInt32(row["StammID"]),
                    BestCase = row["BestCase"] != DBNull.Value ? Convert.ToDecimal(row["BestCase"]) : 0,
                    WorstCase = row["WorstCase"] != DBNull.Value ? Convert.ToDecimal(row["WorstCase"]) : 0,
                    BestCase_Nutzungsdauer = row["BestCase_Nutzungsdauer"] != DBNull.Value ? Convert.ToDecimal(row["BestCase_Nutzungsdauer"]) : 0,
                    WorstCase_Nutzungsdauer = row["WorstCase_Nutzungsdauer"] != DBNull.Value ? Convert.ToDecimal(row["WorstCase_Nutzungsdauer"]) : 0
                });
            }

            // Zusatzangaben anhängen. Fehlen sie (nicht migrierte Datenbank), bleibt jede
            // Position bei BEMESSUNG_BETRAG und IstErloes = false — also beim Verhalten
            // vor Etappe E3.
            foreach (KostenPosition p in geladeneFaktoren)
            {
                KostenPositionCtrl.Zusatz z;
                if (!zusatz.TryGetValue(p.ID, out z) || z == null) continue;

                p.IstErloes = z.IstErloes;
                p.Bemessung = z.Bemessung;
                p.Kostenart = z.Kostenart;      // K5: trägt das Zuschuss-Kennzeichen
                p.StartJahr = z.StartJahr;      // KD6 (§ 11, FK10)
                if (p.Abgeleitet && z.Menge.HasValue && z.Einheitpreis.HasValue)
                    p.Herleitung = string.Format(MyResource.Resource.KOSTEN_BEMESSUNG_HERLEITUNG,
                                                 z.Einheitpreis.Value.ToString("N4", BerichtTexte.Kultur),
                                                 BetriebskostenCtrl.SatzEinheit(z.Bemessung),
                                                 z.Menge.Value.ToString("N2", BerichtTexte.Kultur),
                                                 BetriebskostenCtrl.MengenEinheit(z.Bemessung));
            }

            // UI aktualisieren
            UpdateDetailPanel(komponente, geladeneFaktoren);
        }

        /// <summary>
        /// Legt die Hauptposition einer Komponente an, sofern sie im Projekt <b>und in der
        /// gerade geöffneten Kategorie</b> noch fehlt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Befund D3 (18.08.2026): Die Existenzprüfung lief über
        /// <c>ProjektID</c> + <c>StammID</c> <b>ohne</c> <c>KategorieID</c>. Sobald die
        /// Investitions-Hauptposition einer Komponente existierte, galt sie auch für den
        /// Reiter „Betriebskosten" als vorhanden — eine Betriebskosten-Hauptposition konnte
        /// deshalb nie entstehen.
        /// </para>
        /// <para>
        /// Befund D4 (18.08.2026): Die <c>StammID</c> kam aus <c>Abfrage_Kostenfaktoren</c>.
        /// Diese gespeicherte Abfrage ist ein INNER JOIN <b>über <c>Tab_ProjektWerte</c></b>
        /// und liefert ohne bereits erfasste Projektwerte nichts — in einer frisch
        /// ausgelieferten Datenbank unterblieb die automatische Übernahme des Technik-Planwerts
        /// deshalb vollständig. Jetzt kommt die <c>StammID</c> aus der projektfreien
        /// Katalogtabelle <c>Tab_Kostenfaktor</c>.
        /// </para>
        /// <para>
        /// Die Existenzprüfung fragt bewusst über <c>Tab_Kostenfaktor.Bezeichnung</c> statt
        /// über eine feste <c>StammID</c>: Der Katalog führt für „Solarthermie" zwei
        /// Hauptpositions-Zeilen (StammID 82 und 84), und Bestandsprojekte verwenden beide.
        /// Ein Vergleich gegen nur eine der beiden würde für die andere Hälfte der Projekte
        /// eine zweite Hauptposition anlegen.
        /// </para>
        /// <para>
        /// <b>Vorbelegung je Kategorie.</b> Investitionskosten kommen aus
        /// <see cref="GetModulKosten"/> (eindeutige Technikwerte; mehrdeutige Anlagen
        /// tragen 0 bei und werden über die Abweichungsanzeige gemeldet). Betriebskosten
        /// entstehen seit dem 18.08.2026 aus den Wartungsangaben mal der tatsächlich
        /// gerechneten Jahresmenge — <see cref="TechnikPlanwertCtrl.LiesBetriebsplanwert"/>;
        /// liegt kein Simulationsergebnis vor, bleibt die Position bei 0 und der Grund steht
        /// als Hinweiszeile über der Gruppe (Nutzerentscheidung 3). Energiekosten haben ihre
        /// eigene Maske und werden hier nicht vorbelegt.
        /// </para>
        /// <para>
        /// <b>Nebenkosten entstehen als eigene Zeilen</b> (Nutzerentscheidung 2), nicht als
        /// Aufschlag auf die Hauptposition — siehe
        /// <see cref="KostenPositionCtrl.SchreibeNebenkosten"/>. Sie werden bei jedem
        /// Anwählen nur ANGELEGT, wenn sie fehlen; vorhandene Zeilen bleiben unberührt,
        /// damit ein zweites Öffnen weder Dubletten erzeugt noch Anwenderwerte überschreibt.
        /// </para>
        /// </remarks>
        private void EnsureMainComponentExists(int projektID, string komponente, decimal externeKosten)
        {
            try
            {
                // K4-Wächter: ohne Kategorie wird nichts angelegt (Reiter „Kostenprofil").
                int? kat = AktuelleKategorieOderNull();
                if (!kat.HasValue) return;
                int kategorieIDNeu = kat.Value;

                int komponentenID = GetKomponentenID(komponente);
                if (komponentenID <= 0) return;

                // --- Nebenkosten: fehlende Zeilen anlegen, vorhandene NICHT anfassen -----
                if (kategorieIDNeu == KATEGORIE_INVESTITION)
                    NebenkostenAnlegen(projektID, komponente, komponentenID);

                // Hauptposition dieser Komponente in DIESER Kategorie bereits vorhanden?
                int vorhanden = KostenPositionCtrl.FindeHauptposition(projektID, kategorieIDNeu,
                                                                      komponentenID, komponente);

                decimal initialeKosten = 0;

                if (kategorieIDNeu == KATEGORIE_BETRIEB)
                {
                    // komponentenID wird für die Kessel-Einheit „%/a" gebraucht: ihre
                    // Bezugsgröße ist die erfasste Investitionsposition dieser Komponente.
                    TechnikPlanwertCtrl.Betriebsplanwert bp =
                        TechnikPlanwertCtrl.LiesBetriebsplanwert(projektID, komponente, komponentenID);
                    _betriebsHinweis[komponente ?? ""] = bp.Hinweis ?? "";

                    if (vorhanden > 0)
                    {
                        // BETRAG 0 GILT ALS UNGEPFLEGT — dieselbe Hausregel, mit der seit
                        // dem 18.08.2026 auch ein Arbeitspreis 0 behandelt wird. Ohne sie
                        // liefe die Vorbelegung an allen Bestandsprojekten vorbei: deren
                        // Betriebskosten-Hauptposition existiert längst und steht auf 0,
                        // weil sie vor dem ersten Simulationslauf angelegt wurde. Ein
                        // gepflegter Wert wird NIE angefasst (Nutzerentscheidung 4).
                        if (bp.Betrag.HasValue &&
                            Math.Abs(KostenPositionCtrl.LiesBetrag(vorhanden)) < 0.005)
                            KostenPositionCtrl.SetzeBetragNachId(vorhanden, bp.Betrag.Value);
                        return;
                    }

                    if (bp.Betrag.HasValue) initialeKosten = (decimal)bp.Betrag.Value;
                }
                else
                {
                    if (vorhanden > 0) return;

                    if (kategorieIDNeu == KATEGORIE_INVESTITION)
                    {
                        initialeKosten = externeKosten;
                        if (initialeKosten == 0)
                            initialeKosten = (decimal)GetModulKosten(projektID, komponente);
                    }
                }

                // Stammdaten prüfen — projektfreie Quelle (D4).
                int stammID = KostenPositionCtrl.StammIdHaupt(komponente);
                if (stammID <= 0) return;                       // Nichts gefunden, Abbruch

                KostenPositionCtrl.SetzeBetrag(projektID, kategorieIDNeu, komponentenID, stammID,
                                               (double)initialeKosten,
                                               DbWerte.KOSTEN_GRUPPE_ALLGEMEIN, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Initialisieren der Hauptkomponente: " + ex.Message);
            }
        }

        /// <summary>
        /// Legt für jeden Nebenkostenposten der Technik mit Wert &gt; 0 eine eigene
        /// Investitionszeile an, sofern sie noch fehlt (Nutzerentscheidung 2).
        /// </summary>
        private void NebenkostenAnlegen(int projektID, string komponente, int komponentenID)
        {
            var posten = TechnikPlanwertCtrl.Nebensummen(
                TechnikPlanwertCtrl.LiesAnlagen(projektID, komponente));
            if (posten.Count == 0) return;

            KostenPositionCtrl.SchreibeNebenkosten(projektID, KATEGORIE_INVESTITION, komponentenID,
                                                   posten, DbWerte.KOSTEN_GRUPPE_ALLGEMEIN,
                                                   KostenPositionCtrl.Nebenmodus.NurAnlegen);
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            AddKostenItem(listBox_Erzeuger.Text);
        }

        private void AddKostenItem(string komponenete)
        {
            // K4-Wächter: Ohne Kostenkategorie gibt es nichts zu erfassen. Die Prüfung
            // steht VOR dem Dialog — den Anwender erst tippen zu lassen und den Datensatz
            // danach zu verwerfen wäre die schlechtere Hälfte beider Möglichkeiten.
            int? kat = AktuelleKategorieOderNull();
            if (!kat.HasValue) return;

            // Eingabemaske öffnen (bleibt UI-Logik)
            Form_KostenfaktorItem frm = new Form_KostenfaktorItem();

            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                // 2. Werte aus dem Dialog abrufen
                int stammID = frm.gewählteID;
                double nutzungsdauer = Convert.ToDouble(frm.Nutzungsdauer);
                double betrag = Convert.ToDouble(frm.Wert);
                string einheit = frm.Einheit;
                string gewaehlteGruppe = string.IsNullOrWhiteSpace(frm.Gruppe) ? "Allgemein" : frm.Gruppe.Trim();

                // 3. Gruppe in den Katalog aufnehmen ("Lern-Funktion")
                // Wir nutzen den "Insert if not exists" Trick mit deiner neuen Methode
                string sqlKatalog = @"INSERT INTO Tab_KostenGruppenKatalog (GruppenName) 
                              SELECT ?
                              FROM (SELECT COUNT(*)
                              FROM Tab_KostenGruppenKatalog
                              WHERE GruppenName = ?) AS CheckTbl 
                              WHERE CheckTbl.[Expr1000] = 0";

                DataRepository.ExecuteSQL(sqlKatalog,
                    new OleDbParameter("@g1", gewaehlteGruppe),
                    new OleDbParameter("@g2", gewaehlteGruppe));

                // 4. INSERT in Tab_ProjektWerte
                string sqlInsert = @"INSERT INTO Tab_ProjektWerte
                                    (ProjektID, StammID, EingegebenerWert, Nutzungsdauer, Einheit, Gruppe, KomponentenID, KategorieID) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                DataRepository.ExecuteSQL(sqlInsert,
                    new OleDbParameter("@pid", m_ID_Projekt),
                    new OleDbParameter("@sid", stammID),
                    new OleDbParameter("@val", betrag),
                    new OleDbParameter("@nd", nutzungsdauer),
                    new OleDbParameter("@ein", einheit),
                    new OleDbParameter("@grp", gewaehlteGruppe),
                    new OleDbParameter("@kid", GetKomponentenID(komponenete)),
                    new OleDbParameter("@kat", kat.Value)
                );

                // 5. UI aktualisieren
                LoadKostenFaktoren(m_ID_Projekt, komponenete);
                Gesamtkosten();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Verarbeiten der Daten: " + ex.Message);
            }
        }

        /// <summary>
        /// Die Gewerke, die in DIESEM Projekt zu bepreisen sind — Vereinigung aus „Anlage
        /// im Projekt verbaut" (<see cref="TechnikPlanwertCtrl.Verbaut"/> über
        /// <c>Tab_Energieanlagen</c>) und „Kostenposition bereits erfasst"
        /// (<c>Tab_ProjektWerte</c>), in der Reihenfolge von <c>Tab_KostenKomponente</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum nicht mehr <c>Program.startfrm.status</c>.</b> Dieses Feld ist KEIN
        /// Lizenzbit, sondern der Gewerke-Status des im Startassistenten geladenen Projekts:
        /// <c>Form_Start.UpdateWizardSymbole</c> setzt Bit 256 (BHKW), 0x2 (Wärmepumpe) und
        /// die übrigen je nach Zeilenzahl in <c>Tab_Energieanlagen</c> — und zwar für
        /// <c>Form_Start.m_ID_Projekt</c>. Der Kostendialog wird aber auch für ein ANDERES
        /// Projekt geöffnet: <see cref="UcBkKosten"/> reicht die auf der Seite
        /// „Berichte &amp; Kosten" markierte Zeile herein — Stamm ODER Variante. Stand der
        /// Assistent auf einem anderen Projekt oder wurde er in dieser Sitzung nie
        /// geöffnet, zeigte die Liste fremde Gewerke oder blieb leer, und der Anwender
        /// konnte für sein BHKW überhaupt keine Kostenposition anlegen.
        /// </para>
        /// <para>
        /// <b>Warum eine Vereinigung.</b> Ein Gewerk, dessen Anlagenzeilen gelöscht wurden,
        /// führt möglicherweise noch Kostenpositionen. Die dürfen nicht unerreichbar
        /// werden — angeboten wird deshalb alles, was verbaut ist ODER bereits Zahlen
        /// trägt. Gegenüber dem alten Weg wird dadurch nichts enger: auch die Bitabfragen
        /// zeigten nur Gewerke, die im (Assistenten-)Projekt vorkommen.
        /// </para>
        /// </remarks>
        private static List<string> ProjektKomponenten(int projektID)
        {
            var liste = new List<string>();
            if (projektID <= 0) return liste;

            var mitPositionen = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                DataTable p = DataRepository.GetDataTable(
                    "SELECT DISTINCT k.Komponente " +
                    "FROM Tab_KostenKomponente AS k " +
                    "     INNER JOIN Tab_ProjektWerte AS w ON k.ID = w.KomponentenID " +
                    "WHERE w.ProjektID = ?",
                    new OleDbParameter("@pid", projektID));
                if (p != null)
                    foreach (DataRow r in p.Rows)
                        if (r["Komponente"] != DBNull.Value)
                            mitPositionen.Add(r["Komponente"].ToString());
            }
            catch { }

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT Komponente FROM Tab_KostenKomponente ORDER BY ID");
                if (dt == null) return liste;

                foreach (DataRow r in dt.Rows)
                {
                    if (r["Komponente"] == DBNull.Value) continue;
                    string k = r["Komponente"].ToString();
                    if (k.Length == 0) continue;
                    // Ä7: Erfassungsgruppen erscheinen nicht mehr automatisch —
                    // nur noch verbaute Anlagen und Komponenten mit Positionen.
                    if (mitPositionen.Contains(k) || TechnikPlanwertCtrl.Verbaut(projektID, k))
                        liste.Add(k);
                }
            }
            catch { }

            return liste;
        }

        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> zu einem Komponentennamen; 0 = unbekannt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>ETAPPE K5 — die Nummern der sieben Bestandskomponenten bleiben fest
        /// verdrahtet, die neuen kommen aus der Datenbank.</b> Bis K5 war diese Methode
        /// eine reine <c>switch</c>-Kette; sie hätte für die drei Erfassungsgruppen aus
        /// Migrationsschritt 27 eine 0 geliefert, und damit wäre für sie keine einzige
        /// Kostenposition anlegbar gewesen (<c>SetzeBetrag</c> bricht bei
        /// <c>komponentenID &lt;= 0</c> ab).
        /// </para>
        /// <para>
        /// <b>Warum die sieben trotzdem stehen bleiben.</b> Ihre Nummern 1…7 stehen so
        /// auch in <c>BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL/_BHKW</c> und in jeder
        /// Bestandszeile von <c>Tab_ProjektWerte</c>. Sie durch eine Abfrage zu ersetzen
        /// wäre eine Verhaltensänderung an der Stelle, an der am wenigsten passieren
        /// darf — und ohne Not: Der Nachschlag greift nur, wenn der Name keiner der
        /// sieben ist.
        /// </para>
        /// <para>
        /// <b>Ein Lauf je Fenster.</b> Das Ergebnis wird gemerkt; die Methode wird beim
        /// Aufbau jeder Positionsliste mehrfach gerufen.
        /// </para>
        /// </remarks>
        private int GetKomponentenID(string Erzeuger)
        {
            switch (Erzeuger)
            {
                case "Wärmepumpe": return 1;
                case "Heizkessel": return 2;
                case "Photovoltaik": return 3;
                case "Solarthermie": return 4;
                case "Stromspeicher": return 5;
                case "Pufferspeicher": return 6;
                case "BHKW": return 7;
                default: return KomponentenIdAusKatalog(Erzeuger);
            }
        }

        /// <summary>Gemerkte Katalognummern der Komponenten, die nicht fest verdrahtet sind (K5).</summary>
        private readonly Dictionary<string, int> _komponentenIdCache =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> aus dem Katalog; 0, wenn es den Namen dort
        /// nicht gibt (K5). Dieselbe Abfrage wie
        /// <c>KomponentenUebernahmeCtrl</c> und <c>KiAktionenWirtschaft</c>.
        /// </summary>
        private int KomponentenIdAusKatalog(string komponente)
        {
            if (string.IsNullOrEmpty(komponente)) return 0;

            int gemerkt;
            if (_komponentenIdCache.TryGetValue(komponente, out gemerkt)) return gemerkt;

            int id = 0;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT MIN(ID) FROM Tab_KostenKomponente WHERE Komponente = ?",
                    new OleDbParameter("@k", komponente));
                if (o != null && o != DBNull.Value) id = Convert.ToInt32(o);
            }
            catch { }

            _komponentenIdCache[komponente] = id;
            return id;
        }

        private void UpdateSingleRowInDatabase(KostenPosition pos)
        {
            if (pos.ID <= 0) return;

            string sql = @"UPDATE Tab_ProjektWerte 
                   SET EingegebenerWert = ?, 
                       BestCase = ?, 
                       WorstCase = ?,
                       Nutzungsdauer = ?,
                       BestCase_Nutzungsdauer = ?, 
                       WorstCase_Nutzungsdauer = ?,
                       Gruppe = ?
                   WHERE ID = ?";

            // Aufruf der neuen zentralen Methode
            DataRepository.ExecuteSQL(sql,
                new OleDbParameter("@val", (double)pos.Betrag),
                new OleDbParameter("@best", (double)pos.BestCase),
                new OleDbParameter("@worst", (double)pos.WorstCase),
                new OleDbParameter("@nd", (double)pos.Nutzungsdauer),
                new OleDbParameter("@bestNd", (double)pos.BestCase_Nutzungsdauer),
                new OleDbParameter("@worstNd", (double)pos.WorstCase_Nutzungsdauer),
                new OleDbParameter("gn", (string)pos.Gruppenname),
                new OleDbParameter("@id", pos.ID)
            );

            KostenartSichern(pos);
            StartjahrSichern(pos);
        }

        /// <summary>
        /// ETAPPE KD6 (§ 11, FK10): schreibt das Startjahr der Position nach —
        /// getrennt aus demselben Grund wie <see cref="KostenartSichern"/> (die
        /// Spalte stammt aus Migrationsschritt 38 und darf das Speichern der
        /// Beträge nie mitreißen). NULL = t0, nie 0 (Hausregel).
        /// </summary>
        private void StartjahrSichern(KostenPosition pos)
        {
            if (pos == null || pos.ID <= 0) return;
            try
            {
                if (!KostenPositionCtrl.StelleSpaltenSicher()) return;

                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_STARTJAHR +
                    "] = ? WHERE ID = ?",
                    new OleDbParameter("@sj", OleDbType.Integer)
                    { Value = pos.StartJahr > 1 ? (object)pos.StartJahr : DBNull.Value },
                    new OleDbParameter("@id", pos.ID));
            }
            catch { /* Vorsorgeweg — der Betragsspeicherweg bleibt unberührt */ }
        }

        /// <summary>
        /// ETAPPE K5: schreibt die Kostenart der Position nach — sie ist der einzige
        /// Träger des Zuschuss-Kennzeichens (<c>Form_CaseEingabe</c>).
        /// </summary>
        /// <remarks>
        /// <b>Ein zweites UPDATE statt einer erweiterten Anweisung.</b> Die Spalte
        /// <c>Kostenart</c> stammt aus Migrationsschritt 19 und fehlt in einer nie
        /// migrierten Datenbank. Stünde sie in derselben Anweisung, scheiterte dort auch
        /// das Speichern der Beträge — und zwar still, weil <c>ExecuteSQL</c> seinen
        /// Fehler selbst abfängt. Getrennt bleibt der Bestandsweg unberührt: Ohne die
        /// Spalte passiert schlicht nichts (dieselbe Regel wie in
        /// <c>KostenPositionCtrl.SetzeBetragMitZusatz</c>).
        /// </remarks>
        private void KostenartSichern(KostenPosition pos)
        {
            if (pos == null || pos.ID <= 0) return;
            if (string.IsNullOrEmpty(pos.Kostenart)) return;

            try
            {
                if (!KostenPositionCtrl.StelleSpaltenSicher()) return;

                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_KOSTENART +
                    "] = ? WHERE ID = ?",
                    new OleDbParameter("@art", pos.Kostenart),
                    new OleDbParameter("@id", pos.ID));
            }
            catch { }
        }

        private void listBox_Betriebskosten_SelectedIndexChanged(object sender, EventArgs e)
        {
            flpContainer_Betriebskosten.Visible = true;
            btn_Hinzu_Betriebskosten.Enabled = true;

            string komponente = listBox_Betriebskosten.Text;

            EnsureMainComponentExists(m_ID_Projekt, komponente, 0);
            LoadKostenFaktoren(m_ID_Projekt, komponente);
            Gesamtkosten(listBox_Betriebskosten.Text);
        }

        private void btn_Hinzu_Betriebskosten_Click(object sender, EventArgs e)
        {
            AddKostenItem(listBox_Betriebskosten.Text);
        }

        private void tabMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            flpContainer_Energiekosten.Visible = false;
            kategorie = tabMain.SelectedTab.Text;

            // K4: Der vierte Reiter „Kostenprofil" führt keine Kostenkategorie. Er wird
            // ZUERST abgefangen — ohne diesen Zweig behielte kategorieID den Wert des
            // zuvor gewählten Reiters, und jede spätere Schreibaktion hätte auf dessen
            // Kategorie gezielt. Die Karten lesen ihren Stand beim Betreten neu, damit
            // ein Import aus einer anderen Maske hier sichtbar wird.
            if (tabKostenprofil != null && ReferenceEquals(tabMain.SelectedTab, tabKostenprofil))
            {
                kategorieID = 0;
                AktualisiereKostenprofilKarte();
                AktualisiereSpotpreisKarte();
                Gesamtkosten();                       // Wächter setzt die Fußzeile auf „—"
                return;
            }

            // Reihenfolge beachten: kategorieID muss VOR Gesamtkosten() stehen — die
            // Gesamtsumme wird seit Befund D1 nach Kategorie gefiltert und hätte sonst
            // noch die des zuvor gewählten Reiters verwendet.
            if (kategorie == "Investitionskosten")
            {
                kategorieID = KATEGORIE_INVESTITION;
                flp = flpContainer;
                Gesamtkosten(listBox_Erzeuger.Text);
            }
            else if (kategorie == "Betriebskosten")
            {
                kategorieID = KATEGORIE_BETRIEB;
                flp = flpContainer_Betriebskosten;
                Gesamtkosten(listBox_Betriebskosten.Text);
            }
            else if (kategorie == "Energiekosten")
            {
                kategorieID = KATEGORIE_ENERGIE;
                flp = flpContainer_Energiekosten;
                flp.Visible = false;
                Gesamtkosten();
            }

        }

        /// <summary>
        /// Knopf „Betriebskosten VDI 2067…": öffnet die Maske mit den zwölf Positionen
        /// nach VDI 2067 (Etappe E3) und liest die Positionsliste danach neu ein.
        /// </summary>
        /// <remarks>
        /// Geschrieben wird ausschließlich auf ausdrückliche Bestätigung — bricht der
        /// Anwender ab, bleibt jeder erfasste Wert stehen (Nutzerentscheidung 4 der
        /// Kostenübernahme).
        /// </remarks>
        private void btnBetriebskostenVdi_Click(string komponente)
        {
            if (kategorieID != KATEGORIE_BETRIEB) return;

            int geschrieben;
            using (var dlg = new Form_Betriebskosten(m_ID_Projekt))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                geschrieben = dlg.GeschriebeneZeilen;
            }

            LoadKostenFaktoren(m_ID_Projekt, komponente);
            Gesamtkosten(komponente);

            MessageBox.Show(string.Format(MyResource.Resource.VDI_GESPEICHERT, geschrieben),
                            this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hinweiszeile über der Positionsliste: Herleitung oder Grund der ausgebliebenen
        /// Vorbelegung der Betriebskosten. Ohne Mitteilung entsteht keine Zeile.
        /// </summary>
        /// <remarks>
        /// NUTZERENTSCHEID 23.08.2026: Der Investitionszweig ist entfallen. Er meldete
        /// „Weicht vom Technik-Planwert ab: erfasst X, Technik Y. Über ‚Planwert
        /// übernehmen…' angleichen." — ein Verweis auf einen Knopf, den es nicht mehr gibt,
        /// und ein Vergleich zwischen der HAUPTposition und dem Planwert, den der Anwender
        /// an dieser Stelle weder nachvollziehen noch auflösen konnte.
        /// <see cref="KostenPositionCtrl.Pruefe"/> selbst bleibt: davon leben die
        /// Komponentenübernahme (<see cref="KomponentenUebernahmeCtrl"/>) und die
        /// KI-Auskunft, die den Vergleich im Klartext beantworten.
        /// </remarks>
        private void HinweiszeileAnlegen(string komponente, int breite)
        {
            string text = "";
            Color farbe = Color.FromArgb(0x33, 0x33, 0x33);
            Color flaeche = Color.FromArgb(0xF4, 0xF6, 0xFA);

            if (kategorieID == KATEGORIE_BETRIEB)
            {
                string h;
                if (!_betriebsHinweis.TryGetValue(komponente ?? "", out h))
                {
                    // Beim erneuten Öffnen ist die Position längst vorhanden; der Grund
                    // wird deshalb hier frisch ermittelt statt gemerkt.
                    h = TechnikPlanwertCtrl.LiesBetriebsplanwert(
                            m_ID_Projekt, komponente, GetKomponentenID(komponente)).Hinweis;
                    _betriebsHinweis[komponente ?? ""] = h ?? "";
                }
                text = h ?? "";
            }

            if (string.IsNullOrEmpty(text)) return;

            Label lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(Math.Max(200, breite), 34),
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(6, 4, 6, 0),
                BackColor = flaeche,
                ForeColor = farbe
            };
            flp.Controls.Add(lbl);
        }

        /// <summary>
        /// Investitionswert der im Projekt verbauten Technik einer Komponente, soweit er
        /// <b>eindeutig</b> ist — Vorbelegung der Hauptposition beim ersten Anwählen.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Die Ermittlung selbst steht seit dem 18.08.2026 in
        /// <see cref="TechnikPlanwertCtrl"/>; von dort kommt auch der Entdoppelungsschutz
        /// des Befundes D2 (mehrere Anlagenzeilen auf dasselbe Gerät).
        /// </para>
        /// <para>
        /// <b>Seit dem 23.08.2026 ist kein Gewerk mehr mehrdeutig.</b> Beim BHKW
        /// konkurrierten <c>Kosten_Modul</c> und <c>Investition_kwel × Pel</c>; seit
        /// <c>Investition_kwel</c> aus den fünf Einzelposten ABGELEITET wird, ist die
        /// zweite Basis eine Dublette und entfallen. Jedes Gewerk legt damit höchstens
        /// EINE Kostenbasis je Anlage an, <c>TechnikPlanwertCtrl.Hauptsumme</c> liefert
        /// hier also stets den eindeutigen Wert. Die Basiswert-Maschinerie
        /// (<c>BASIS_*</c>, <c>Mehrdeutig</c>) steht weiterhin in
        /// <see cref="TechnikPlanwertCtrl"/>; ihr Rückbau ist ein eigener Schritt.
        /// </para>
        /// </remarks>
        private double GetModulKosten(int projektID, string komponente)
        {
            return TechnikPlanwertCtrl.Hauptsumme(
                TechnikPlanwertCtrl.LiesAnlagen(projektID, komponente), null);
        }

        private void RenderEnergieTab(string filterKategorie = "Alle Kategorien")
        {
            flpContainer_Energiekosten.Controls.Clear();
            flpContainer_Energiekosten.SuspendLayout();
        }

        private void listBox_Energieträger_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Während des Befüllens ist jede Auswahl eine Nebenwirkung der Bindung und
            // keine Entscheidung des Anwenders — Begründung bei FillCarrierComboBox().
            if (_traegerlisteWirdGefuellt) return;

            if (listBox_Energieträger.SelectedItem is EnergyCarrier selectedCarrier)
            {
                flpContainer_Energiekosten.Controls.Clear();
                flpContainer_Energiekosten.Visible = true;
                UserControl uc = null;

                switch (selectedCarrier.PricingModel)
                {
                    case "FUEL":
                    case "LIQUID_FUEL":
                    case "SOLID_FUEL":
                    case "ANIMAL_FAT":
                    case "HEAT":
                    case "GASEOUS_FUEL":
                    case "ELECTRICITY":
                        uc = new ucFuelSettings(m_ID_Projekt, selectedCarrier);
                        break;
                }

                if (uc != null)
                {
                    // WICHTIG: Gib der Instanz einen festen Namen, den wir in der txt-Datei ansprechen können!
                    uc.Name = "ucFuelSettings";

                    // Breite an den Container anpassen
                    uc.Width = flpContainer.ClientSize.Width - 10;
                    flpContainer_Energiekosten.Controls.Add(uc);

                    // JETZT ERST REGISTRIEREN, da das Control nun existiert und im Panel sitzt!
                    // Die HilfeAutomatik zieht dasselbe über ihren ControlAdded-Haken
                    // nach; der ausdrückliche Aufruf ist idempotent und bleibt stehen.
                    _helpExtender?.RegisterControl(uc, "ucFuelSettings");
                }
            }
        }

        public static List<EnergyCarrier> GetAllCarriers(int ID_Projekt)
        {
            List<EnergyCarrier> carriers = new List<EnergyCarrier>();

            //string sql = "SELECT * FROM ENERGY_CARRIER WHERE is_active = true ORDER BY name ASC";

            string sql = @"SELECT
                            energy_project_settings.ID_Projekt,
                            ec.*, 
                            pm.has_hi, 
                            pm.has_hs, 
                            pm.has_powerprice
                        FROM
                            energy_project_settings
                            INNER JOIN (
                                energy_carrier AS ec
                                LEFT JOIN
                                pricing_model AS pm ON ec.pricing_model = pm.code
                            ) ON energy_project_settings.ID_Energieträger = ec.id
                        WHERE energy_project_settings.ID_Projekt=?";

            OleDbParameter[] ps = {
                new OleDbParameter("@p", ID_Projekt),
            };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            foreach (DataRow row in dt.Rows)
            {
                carriers.Add(new EnergyCarrier
                {
                    ID = Convert.ToInt32(row["id"]),
                    Code = row["code"].ToString(),
                    Name = row["name"].ToString(),
                    GroupCode = row["group_code"].ToString(),
                    PricingModel = row["pricing_model"].ToString(),
                    BillingUnit = row["billing_unit"].ToString(),
                    HiKwhPerUnit = row["hi_kwh_per_unit"] != DBNull.Value ? Convert.ToDouble(row["hi_kwh_per_unit"]) : 0,
                    HsKwhPerUnit = row["hs_kwh_per_unit"] != DBNull.Value ? Convert.ToDouble(row["hs_kwh_per_unit"]) : 0,
                    ID_Brennstoff = Convert.ToInt32(row["id_brennstoff"]),
                    price_base = row["price_base"] != DBNull.Value ? Convert.ToDouble(row["price_base"]) : 0,
                    price_work = row["price_work"] != DBNull.Value ? Convert.ToDouble(row["price_work"]) : 0,
                    CO2 = row["co2"] != DBNull.Value ? Convert.ToDouble(row["co2"]) : 0,
                    SO2 = row["so2"] != DBNull.Value ? Convert.ToDouble(row["so2"]) : 0,
                    NOx = row["nox"] != DBNull.Value ? Convert.ToDouble(row["nox"]) : 0,
                    HasHi = row["has_hi"] != DBNull.Value ? Convert.ToBoolean(row["has_hi"]) : false,
                    HasHs = row["has_hs"] != DBNull.Value ? Convert.ToBoolean(row["has_hs"]) : false,
                    HasPowerPrice = row["has_powerprice"] != DBNull.Value ? Convert.ToBoolean(row["has_powerprice"]) : false
                });
            }
            return carriers;
        }

        /// <summary>
        /// Füllt die Energieträgerliste des Projekts — <b>ohne</b> Auswahl und damit
        /// ohne Energieträger-Block im Panel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum die Sperre.</b> Die Bindung meldet unterwegs Auswahlen, die keine
        /// sind: <c>DataSource=</c> setzt die ListBox auf Zeile 0 (ein
        /// <c>SelectedIndexChanged</c>), <c>DisplayMember=</c> baut die Anzeige neu und
        /// meldet dabei zweimal erneut Zeile 0, erst <c>SelectedIndex = -1</c> nimmt die
        /// Auswahl zurück. Der Behandler baute daraus <b>dreimal</b> ein
        /// <c>ucFuelSettings</c> samt <c>ucStromAufschlaege</c> — jedes mit eigenen
        /// Lesezugriffen auf die Datenbank — von denen keines übrig bleiben sollte.
        /// Nachgewiesen am 18.08.2026 für die Projekte 1017 und 1023: drei Aufrufe von
        /// <c>StromAufschlagCtrl.StelleSpaltenSicher</c> je <c>new Form_Kosten(id)</c>.
        /// Seit Commit 87483b4 (Fehlerdialoge beseitigt) fiel das nicht mehr auf, die
        /// dreifache Arbeit blieb.
        /// </para>
        /// <para>
        /// Gleiches Mittel wie in <c>ucStromAufschlaege</c> (<c>_laden</c>): eine Sperre,
        /// die nur das programmatische Befüllen stummschaltet. Die echte Anwenderauswahl
        /// läuft unverändert durch den Behandler — auch die Zuweisung aus
        /// <see cref="btn_Carrier_Click"/> nach dem Anlegen eines Trägers, die erst
        /// <b>nach</b> dem Befüllen erfolgt.
        /// </para>
        /// </remarks>
        private void FillCarrierComboBox()
        {
            // Daten holen
            List<EnergyCarrier> allCarriers = GetAllCarriers(m_ID_Projekt);

            _traegerlisteWirdGefuellt = true;
            try
            {
                // ComboBox konfigurieren
                listBox_Energieträger.DataSource = allCarriers;
                // Darstellung
                listBox_Energieträger.DisplayMember = "Name";
                // Welcher Wert soll im Hintergrund identifizieren?
                listBox_Energieträger.ValueMember = "Id";
                listBox_Energieträger.SelectedIndex = -1; // Start ohne Auswahl
            }
            finally
            {
                _traegerlisteWirdGefuellt = false;
            }

            // Keine Auswahl, also auch kein Block: Bisher blieb der zuletzt während der
            // Bindung gebaute Block im Panel stehen, obwohl in der Liste nichts markiert
            // war. Im Konstruktor räumte ihn RenderEnergieTab() zufällig weg, nach
            // „Hinzufügen" ohne Treffer blieb er sichtbar — und wurde beim Schließen
            // (OnFormClosing) sogar gespeichert.
            flpContainer_Energiekosten.Controls.Clear();
        }

        private string CreateNewEnergyCarrier()
        {
            using (var dlg = new Form_Kosten_Auswahl())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return "";

                try
                {
                    // Default-Werte aus dem Brennstoff-Stamm (Preise/Emissionen)
                    double default_arbeitspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Arbeitspreis", dlg.SelectedBrennstoffID));
                    double default_grundpreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Grundpreis", dlg.SelectedBrennstoffID));
                    double default_leistungspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Leistungspreis", dlg.SelectedBrennstoffID));
                    double default_co2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "CO2", dlg.SelectedBrennstoffID));
                    double default_so2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "SO2", dlg.SelectedBrennstoffID));
                    double default_nox = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "NOx", dlg.SelectedBrennstoffID));

                    // 1) Katalog-Träger suchen; existiert er, wird er wiederverwendet
                    int carrierId = -1;
                    object existing = DataRepository.ExecuteScalar(
                        "SELECT id FROM energy_carrier WHERE name = ?",
                        new OleDbParameter[] { new OleDbParameter("@name", dlg.SelectedName) });
                    if (existing != null && existing != DBNull.Value)
                        carrierId = Convert.ToInt32(existing);

                    if (carrierId < 0)
                    {
                        // Katalog-Datensatz nur anlegen, wenn wirklich neu
                        string insertSql = @"INSERT INTO energy_carrier
                             (ID_Brennstoff, code, name, group_code, pricing_model, billing_unit, hi_kwh_per_unit,
                              hs_kwh_per_unit, price_work, price_base, co2, so2, nox, is_active)
                             VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        OleDbParameter[] ps = {
                            new OleDbParameter("@idB",   dlg.SelectedBrennstoffID),
                            new OleDbParameter("@code",  dlg.SelectedCode),
                            new OleDbParameter("@name",  dlg.SelectedName),
                            new OleDbParameter("@gc",    dlg.SelectedGroupCode),
                            new OleDbParameter("@pm",    dlg.SelectedBrennstoffCode),
                            new OleDbParameter("@unit",  dlg.SelectedBillingUnit),
                            new OleDbParameter("@shi",   dlg.SelectedHi),
                            new OleDbParameter("@shs",   dlg.SelectedHs),
                            new OleDbParameter("@defap", default_arbeitspreis),
                            new OleDbParameter("@defgp", default_grundpreis),
                            new OleDbParameter("@co2",   default_co2),
                            new OleDbParameter("@so2",   default_so2),
                            new OleDbParameter("@nox",   default_nox),
                            new OleDbParameter("@active", OleDbType.Boolean) { Value = true }
                        };
                        carrierId = DataRepository.ExecuteInsertAndGetId(insertSql, ps);
                    }

                    // 2) Ist der Träger diesem Projekt schon zugeordnet? -> nicht doppeln
                    int vorhanden = Convert.ToInt32(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                        new OleDbParameter[] {
                    new OleDbParameter("@pid", m_ID_Projekt),
                    new OleDbParameter("@eid", carrierId)
                        }));
                    if (vorhanden > 0)
                    {
                        MessageBox.Show($"Die Energieträgervariante '{dlg.SelectedName}' ist diesem Projekt bereits zugeordnet.");
                        return dlg.SelectedName;
                    }

                    // 3) Projektbezogene Sätze anlegen (Preis-Historie + Projekt-Einstellungen)
                    // Befund B5 (11.08.2026): der Ersteintrag ließ leistungspreis leer,
                    // obwohl der Standardwert aus Tab_Brennstoff_Stamm ermittelt wurde.
                    string sqlHistory = @"INSERT INTO energy_price
                         (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
                    DataRepository.ExecuteSQL(sqlHistory, new OleDbParameter[] {
                        new OleDbParameter("@cid",  carrierId),
                        new OleDbParameter("@prid", m_ID_Projekt),
                        new OleDbParameter("@ap",   Math.Round(default_arbeitspreis, 4)),
                        new OleDbParameter("@hi",   Math.Round(dlg.SelectedHi, 4)),
                        new OleDbParameter("@gp",   Math.Round(default_grundpreis, 4)),
                        new OleDbParameter("@date", OleDbType.Date) { Value = DateTime.Now },
                        new OleDbParameter("@au",   dlg.SelectedBillingUnit),
                        new OleDbParameter("@lp",   Math.Round(default_leistungspreis, 4))
                    });

                    string sqlInsert = @"INSERT INTO energy_Project_settings
                         (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs,
                          custom_price_base, ID_Umrechnung, co2, so2, nox)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    DataRepository.ExecuteSQL(sqlInsert, new OleDbParameter[] {
                        new OleDbParameter("@pid",    m_ID_Projekt),
                        new OleDbParameter("@eid",    carrierId),
                        new OleDbParameter("@p",      Math.Round(default_arbeitspreis, 4)),
                        new OleDbParameter("@pl",     Math.Round(default_leistungspreis, 4)),
                        new OleDbParameter("@h",      Math.Round(dlg.SelectedHi, 4)),
                        new OleDbParameter("@hs",     Math.Round(dlg.SelectedHs, 4)),
                        new OleDbParameter("@b",      Math.Round(default_grundpreis, 4)),
                        new OleDbParameter("@convid", dlg.SelectedConvID),
                        new OleDbParameter("@co2",    default_co2),
                        new OleDbParameter("@so2",    default_so2),
                        new OleDbParameter("@nox",    default_nox)
                    });

                    MessageBox.Show("Energieträgervariante erfolgreich angelegt.");
                    return dlg.SelectedName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Speichern: " + ex.Message);
                }
            }
            return "";
        }

        // kleiner Helfer gegen null/DBNull
        private static double ToDouble(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }
        private void btn_Carrier_Click(object sender, EventArgs e)
        {
            string carrierName = CreateNewEnergyCarrier();
            FillCarrierComboBox();
            int index = listBox_Energieträger.FindStringExact(carrierName);

            if (index != ListBox.NoMatches)
            {
                listBox_Energieträger.SelectedIndex = index;
            }
        }

        private void btn_Delete_Click(object sender, EventArgs e)
        {
            if (listBox_Energieträger.SelectedItem is EnergyCarrier selectedCarrier)
            {
                DeleteEnergyCarrierWithSettings(selectedCarrier.Name, m_ID_Projekt);
            }
        }

        public bool DeleteEnergyCarrierWithSettings(string carrierName, int ID_Projekt)
        {
            // Erst die ID finden
            int id = DataRepository.GetIdByName("energy_carrier", "name", carrierName);
            if (id == 0) return false;

            // 1. Details löschen (z.B. project_settings)
            var (conn, trans) = DataRepository.BeginTransaction();
            try
            {
                string sqlDetail = $"DELETE FROM energy_project_settings WHERE ID_Energieträger=? AND ID_Projekt=?";
                using (OleDbCommand cmd = new OleDbCommand(sqlDetail, conn, trans))
                {
                    cmd.Parameters.AddWithValue("?", id);
                    cmd.Parameters.AddWithValue("?", ID_Projekt);
                    cmd.ExecuteNonQuery();
                }

                sqlDetail = $"DELETE FROM energy_price WHERE carrier_id=? AND ID_Projekt=?";
                using (OleDbCommand cmd = new OleDbCommand(sqlDetail, conn, trans))
                {
                    cmd.Parameters.AddWithValue("?", id);
                    cmd.Parameters.AddWithValue("?", ID_Projekt);
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();

                // Review-Befund (Phase 7): das offene ucFuelSettings des gelöschten
                // Trägers muss aus dem Panel, sonst legt das Speichern beim
                // Schließen (B6) die Projektzuordnung wieder an.
                flpContainer_Energiekosten.Controls.Clear();
                FillCarrierComboBox();

                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                MessageBox.Show($"Fehler beim Löschen in energy_project_settings: " + ex.Message);
                return false;
            }
            finally { conn.Close(); }
        }

    }

    public class EnergyCarrier
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string PricingModel { get; set; } // GAS, FUEL, GRID
        public string Code { get; set; }                                      // Das ist der Standard-Heizwert aus der Tabelle ENERGY_CARRIER
        public double HiKwhPerUnit { get; set; }
        public double HsKwhPerUnit { get; set; }
        public string GroupCode { get; set; }
        public string BillingUnit { get; set; }
        public int ID_Brennstoff { get; set; }
        public double price_work { get; set; }
        public double price_base { get; set; }
        public double price_power { get; set; }
        public double CO2 { get; set; }
        public double SO2 { get; set; }
        public double NOx { get; set; }
        public bool HasPowerPrice { get; set; }
        public bool HasHi { get; set; }
        public bool HasHs { get; set; }
    }

    public class EnergyConversion
    {
        public int IDBrennstoff { get; set; }
        public string FromUnit { get; set; }
        public string ToUnitCode { get; set; } // z.B. "kg", "L"
        public double Factor { get; set; }

        // Hilfseigenschaft für die ComboBox-Anzeige
        public string ToUnitLabel => $"{ToUnitCode} (Faktor: {Factor})";
    }

}