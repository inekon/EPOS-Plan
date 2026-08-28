using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wärmesenken einer Wärmeerzeuger-Anlage als GEORDNETE SENKENLISTE
    /// (Konzept_Brauchwasser_Heizung_Pufferspeicher 5.1/5.3, Paket S1).
    ///
    /// <para><b>Was sich mit S1 geändert hat.</b> Bis dahin hatte jede Anlage genau EINE
    /// Hauptsenke (vier Radiobuttons) und optional EINE Zweitsenke (eigene Gruppe) — zwei
    /// feste Plätze, hart im Spaltenpaar <c>WS_*</c>/<c>WS_*2</c> abgebildet. Jetzt trägt
    /// die Anlage eine Liste beliebig vieler Senken in Rangfolge: Rang für Rang wird
    /// beliefert, jede kWh geht genau einmal entweder in eine Direktsenke oder in einen
    /// Puffer (Konzept 5.2). Neu gegenüber dem Bestand sind damit drei Dinge — mehr als
    /// zwei Senken, freie Reihenfolge und Direktsenken ab Rang 2 (bisher musste jede
    /// Zweitsenke ein Puffer sein).</para>
    ///
    /// <para><b>Sechs Ziele</b> (Konzept 5.1): <c>Heizkreis</c> und <c>Prozesswaerme</c>
    /// als Direktsenken, <c>PufferHeizung</c>, <c>PufferBrauchwasser</c>,
    /// <c>PufferProzess</c> und <c>PufferKombi</c> als Ladeziele. Die Steuerwerte kommen
    /// aus <see cref="DbWerte"/> und bleiben deutsch und eingefroren; sichtbar wird
    /// ausschließlich <c>MyResource.Resource.*</c> (Drei-Schichten-Regel).</para>
    ///
    /// <para><b>Persistenz einspurig (PAKET A1).</b> Führend und ALLEIN maßgeblich ist
    /// <c>Z_AnlageSenke</c> (<see cref="Z_AnlageSenkeCtrl"/>) mit der vollständigen Liste;
    /// die Verbundmitglieder gehen im selben Zug nach <c>Z_AnlagePufferVerbund</c>.
    /// Geschrieben wird beides hier, in <see cref="ListeSpeichern"/>. Bis A1 wurden die
    /// Ränge 1 und 2 zusätzlich auf die Altspalten <c>WS_*</c> gespiegelt, weil der
    /// einkanalige Altpfad der Engine von dort las (S1-O5). Der Altpfad ist abgerissen,
    /// die Spiegelung mit ihm — zwei Ablagen für dieselbe Aussage wären ab hier nur noch
    /// eine Fehlerquelle.</para>
    ///
    /// Aufbau wie beim Bestandsmuster <see cref="Form_QuellePufferspeicher"/>: komplett
    /// programmatisch, kein Designer, keine .resx; Datenübergabe über öffentliche Felder;
    /// Validierung im OK-Klick mit <c>DialogResult.None</c>.
    ///
    /// Die Fachlogik (Lesen, Schreiben, Prüfen nach 4.6, Ladeordnung nach 3.4) steht in
    /// <see cref="WaermesenkeClass"/> und <see cref="Ladeordnung"/> — dieser Dialog ist
    /// reine Oberfläche darüber.
    /// </summary>
    public class Form_Waermesenke : Form
    {
        // --- Übergabe ----------------------------------------------------------------

        /// <summary>Projekt der Anlage.</summary>
        public int ID_Projekt;

        /// <summary>Tab_Energieanlagen.ID der Anlage.</summary>
        public int ID_Anlage;

        /// <summary>ID_Type der Anlage (1 WP, 2 Solarthermie, 10 Heizkessel, 11 BHKW).</summary>
        public int ID_Type;

        /// <summary>Bezeichner der Anlage (Fenstertitel).</summary>
        public string AnlagenName = "";

        /// <summary>Betriebsmodus der Anlage — die PV-Zeile erscheint nur bei „PV" (Konzept 3.5).</summary>
        public string BM_Typ = "";

        /// <summary>
        /// Die Mitglieder des PARALLELVERBUNDS am Rang-1-Speicher: beim Öffnen die
        /// Vorbelegung aus <c>Z_AnlagePufferVerbund</c>, nach OK der gespeicherte Stand.
        ///
        /// Die Senkenliste selbst geht nicht über ein Übergabefeld — sie steht nach OK in
        /// <c>Z_AnlageSenke</c>, und wer sie für eine Statusmeldung braucht, liest sie
        /// über <see cref="Senkenliste"/> (PAKET A1).
        /// </summary>
        public List<int> VerbundMitglieder = new List<int>();

        /// <summary>
        /// Die Senkenliste in Rangfolge, so wie sie der Dialog zuletzt gezeigt bzw.
        /// gespeichert hat — für die Statusmeldung des Aufrufers. Nie <c>null</c>.
        /// </summary>
        public List<Z_AnlageSenkeModel> Senkenliste
        {
            get { return _zeilen; }
        }

        /// <summary>
        /// true = das Speichern nach <c>Z_AnlageSenke</c> und <c>Z_AnlagePufferVerbund</c>
        /// ist durchgekommen. Nur bei <c>DialogResult.OK</c> aussagekräftig.
        ///
        /// Der Dialog schreibt seit PAKET A1 selbst; damit wandert auch die Frage „hat es
        /// geklappt?" hierher. Vorher hing die Fehlermeldung des Aufrufers am
        /// Rückgabewert von <c>WaermesenkeClass.Schreiben</c>.
        /// </summary>
        public bool SpeichernOk = true;

        // --- Senkenliste ---------------------------------------------------------------

        /// <summary>
        /// Die Senken der Anlage in RANGFOLGE — der Index ist der Rang minus eins. Die
        /// Rangnummern werden erst beim Speichern festgeschrieben
        /// (<see cref="ListeSpeichern"/>); solange der Dialog offen ist, ist allein die
        /// Listenreihenfolge maßgeblich. Das erspart es, bei jedem Rauf/Runter n Zeilen
        /// umzunummerieren, und es gibt keinen Zustand, in dem Reihenfolge und Rangfeld
        /// auseinanderlaufen können.
        /// </summary>
        private readonly List<Z_AnlageSenkeModel> _zeilen = new List<Z_AnlageSenkeModel>();

        private ListView _lvSenken;
        private Button _btnHinzu;
        private Button _btnEntfernen;
        private Button _btnRauf;
        private Button _btnRunter;

        // --- Oberfläche der gewählten Zeile -------------------------------------------

        private GroupBox _gbZeile;
        private ComboBox _cbZiel;
        private ComboBox _cbPuffer;
        private ComboBox _cbBedarfsart;

        private GroupBox _gbLaden;
        private ComboBox _cbLadeprio;
        private Label _lblPosition;
        private CheckBox _chkLadegrenze;
        private TextBox _tbLadegrenze;
        private Label _lblLadegrenzeEinheit;
        private Label _lblPV;
        private ComboBox _cbLadeprioPV;

        // --- Einspeisehöhe der Senkenzeile (PAKET P2, Konzept 5.1/7.4) ----------------
        //
        // Der Pfad Z_AnlageSenke.Anschlusshoehe → Ladeauftrag.Einspeisehoehe →
        // SimulationPufferspeicher.EinspeisehoeheAktuell steht seit Paket P1 vollständig;
        // was fehlte, war die Pflege im Dialog (P1-O2 / S1-O3). Aufbau wie die
        // Ladeobergrenze eine Zeile darüber: Haken + Zahlenfeld + Einheitentext, damit
        // „nicht gesetzt" (= oben, die Vorgabe) und „0" (= ganz unten) unterscheidbar
        // bleiben — 0 ist eine GÜLTIGE Höhe, ein leeres Feld darf deshalb nicht auf 0
        // hinauslaufen.
        private CheckBox _chkEinspeisehoehe;
        private TextBox _tbEinspeisehoehe;
        private Label _lblEinspeisehoeheEinheit;

        /// <summary>
        /// Erklärt die Wirkung der Einspeisehöhe an Haken und Eingabefeld. Ein eigener
        /// ToolTip statt eines weiteren Hinweistextes: Die Höhe wirkt nur bei einem
        /// geschichteten Zielspeicher, und diese Bedingung dauerhaft ins Bild zu setzen
        /// hieße, die Gruppe um eine Textzeile zu verlängern, die in den meisten
        /// Projekten nichts zu sagen hat.
        /// </summary>
        private readonly ToolTip _tips = new ToolTip();

        private Label _lblHinweis;
        private Button _btnPufferAnlegen;

        // --- Parallelverbund (Paket Parallelverbund, Entscheidung 17.08.2026) ----------
        //
        // GEWÄHLTE VARIANTE: Der Leitspeicher bleibt das Speicher-Dropdown der Zeile, die
        // zusätzlichen Speicher kommen in EINER CheckedListBox darunter.
        //
        // Warum diese und nicht „erster Haken = Leitspeicher": Die drei Fugen des Dialogs
        // (FuelleCombo, AktuelleId, Zeile lesen/schreiben) bleiben damit in ihrer
        // Bedeutung unangetastet — das Dropdown ist weiterhin die Quelle der Puffer-ID,
        // und die gesamte Bestandslogik daran (PufferWaehlen, AktuellerHauptPuffer,
        // PositionsText, btnPufferAnlegen_Click) rechnet unverändert weiter. Ein „erster
        // Haken"-Modell hätte den Leitspeicher-Begriff in eine Liste ohne stabile
        // Reihenfolge verlegt: Beim Abwählen des ersten Hakens wäre der Leitspeicher
        // stillschweigend ein anderer geworden — und damit die ID, unter der Schwellen,
        // Entladepriorität und die Ergebniszeile laufen. Hinzu kommt die Fachlage: Der
        // Leitspeicher ist KEIN gleichrangiges Element, er trägt die Regelung des
        // Verbunds.
        //
        // PAKET S1: Der Verbund hängt weiterhin an RANG 1. Konzept 5.1 sieht dafür die
        // Spalte Z_AnlagePufferVerbund.ID_Senke vor, damit ein Verbund an jeder
        // Puffersenke möglich wird; solange die Spalte fehlt, gibt es nur die eine
        // Anlagen-Referenz, und ein Verbund an Rang 3 wäre von einem an Rang 1 nicht zu
        // unterscheiden. Die Gruppe ist deshalb nur scharf, wenn Rang 1 gewählt ist.
        private GroupBox _gbVerbund;
        private CheckedListBox _clbVerbund;
        private Label _lblVerbundSumme;

        /// <summary>
        /// Die Puffer, die aktuell in <see cref="_clbVerbund"/> stehen — index-parallel zur
        /// Liste. Das Gegenstück zu den <c>_puffer*</c>-Listen der Dropdowns; ohne sie wäre
        /// aus einem Hakenindex keine Puffer-ID zu gewinnen.
        /// </summary>
        private List<WaermesenkeClass.PufferInfo> _verbundKandidaten =
            new List<WaermesenkeClass.PufferInfo>();

        private List<WaermesenkeClass.PufferInfo> _pufferHeizung =
            new List<WaermesenkeClass.PufferInfo>();
        private List<WaermesenkeClass.PufferInfo> _pufferBrauchwasser =
            new List<WaermesenkeClass.PufferInfo>();

        /// <summary>Projekt-Puffer mit Verwendung „Kombi" (Etappe D5a).</summary>
        private List<WaermesenkeClass.PufferInfo> _pufferKombi =
            new List<WaermesenkeClass.PufferInfo>();

        /// <summary>
        /// PAKET S2 — ALLE Projekt-Puffer, ungefiltert (Konzept 6.2). Das ist seit S2 die
        /// Auswahlmenge des Speicher-Dropdowns: Zuordnungen sind frei, unplausible
        /// bekommen eine Warnung statt einer Sperre.
        /// </summary>
        private List<WaermesenkeClass.PufferInfo> _pufferAlle =
            new List<WaermesenkeClass.PufferInfo>();

        /// <summary>
        /// Klassen-Set je Puffer-ID — die Gruppierung des Dropdowns und der Gruppenkopf.
        /// EINMAL beim Öffnen gelesen; ein Nachschlag je Listeneintrag wäre bei 80
        /// Pufferkopien (Projekt 1023) eine Abfrage je Zeile.
        /// </summary>
        private Dictionary<int, PufferSpCtrl.KlassenSet> _klassenSetJePuffer =
            new Dictionary<int, PufferSpCtrl.KlassenSet>();

        private bool _aktualisiert;   // verhindert Event-Rückkopplung beim Befüllen

        // --- Sichtbare Texte des Parallelverbunds ------------------------------------
        //
        // Sie kommen aus dem Ressourcenkatalog wie alle übrigen Texte dieses Dialogs
        // (Paket 9 / L7, Konzept 13.6). Die vier Verweise stehen hier gebündelt, weil sie
        // in mehreren Methoden gebraucht werden - so ist auf einen Blick zu sehen, welche
        // Schlüssel dieser Dialog für den Verbund führt.
        private static string SIM_GB_VERBUND
        { get { return MyResource.Resource.SIM_GB_VERBUND; } }

        private static string SIM_LBL_VERBUND_ZUSATZ
        { get { return MyResource.Resource.SIM_LBL_VERBUND_ZUSATZ; } }

        private static string SIM_VERBUND_SUMME
        { get { return MyResource.Resource.SIM_VERBUND_SUMME; } }

        private static string SIM_VERBUND_KEIN_VERBUND
        { get { return MyResource.Resource.SIM_VERBUND_KEIN_VERBUND; } }

        /// <summary>Anzeige für ein leeres Feld der Senkenliste.</summary>
        private const string LEER = "–";

        /// <summary>
        /// Ersatzwerte in <c>Ladegrenze</c> für eine Eingabe, die noch nicht zu einer Zahl
        /// taugt: Sie erlauben es, den Fehler dort zu MELDEN, wo er hingehört (beim OK, mit
        /// Nennung des Rangs), statt ihn beim Zeilenwechsel stillschweigend auf 0 zu
        /// runden. Negative Werte können sonst nicht auftreten — die Ladegrenze ist ein
        /// Prozentsatz, und <c>WaermesenkeClass.Normalisieren</c> klemmt Negatives ohnehin.
        /// </summary>
        private const double GRENZE_UNLESBAR = -1;
        private const double GRENZE_BEREICH = -2;

        /// <summary>
        /// Dieselben Ersatzwerte für die EINSPEISEHÖHE (PAKET P2). Sie beginnen erst bei
        /// −2, weil <c>Z_AnlageSenkeModel.Anschlusshoehe</c> die −1 bereits mit der
        /// Bedeutung „nicht gesetzt" belegt (in der Datenbank NULL) — ein Ersatzwert −1
        /// wäre von einer leeren Angabe nicht zu unterscheiden und würde die fehlerhafte
        /// Eingabe stillschweigend als „speist oben ein" speichern.
        /// </summary>
        private const double HOEHE_UNLESBAR = -2;
        private const double HOEHE_BEREICH = -3;

        /// <summary>Eintrag der Ladeprioritäts-Dropdowns (0 = nach Vorgabe).</summary>
        private class PrioItem
        {
            public int Wert;
            public string Text = "";
            public override string ToString() { return Text; }
        }

        /// <summary>
        /// Eintrag der Ziel-Auswahl: sprachneutraler Persistenzwert plus Anzeigetext
        /// (Drei-Schichten-Regel — kein Anzeigetext darf Steuerwert sein).
        /// </summary>
        private class ZielItem
        {
            public string Wert = "";
            public string Text = "";
            public override string ToString() { return Text; }
        }

        public Form_Waermesenke()
        {
            BaueOberflaeche();
            FensterEinpassung.Einhaengen(this);
        }

        // --- Ziele (Konzept 5.1) ------------------------------------------------------

        /// <summary>
        /// true = das Ziel meint einen Pufferspeicher — die Frage des Dialogs an EINER
        /// Stelle. Sie geht an <see cref="WaermesenkeClass.IstPufferZiel"/>, das seit
        /// Paket S1 auch <c>PufferProzess</c> kennt. Die zweite Bedingung ist die
        /// Rückversicherung: Dieselbe Methode ist zugleich die Normalisierungsregel der
        /// ALTSPALTEN, und sollte sie dafür wieder auf die drei Altziele eingeschränkt
        /// werden, denkt der Dialog trotzdem in seinen sechs.
        /// </summary>
        private static bool IstPufferZiel(string ziel)
        {
            return WaermesenkeClass.IstPufferZiel(ziel) ||
                   string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_PROZESS, StringComparison.Ordinal);
        }

        /// <summary>true = Direktsenke Heizkreis (nur dort ist die Bedarfsart wirksam).</summary>
        private static bool IstHeizkreis(string ziel)
        {
            return string.Equals(ziel, DbWerte.WS_ZIEL_HEIZKREIS, StringComparison.Ordinal);
        }

        /// <summary>
        /// Anzeigename eines Ziels — inklusive der beiden S1-Ziele.
        ///
        /// ÖFFENTLICH, weil auch die Erzeugerkarte die Senkenkette beschriftet
        /// (<c>Form_Simulation_Config.Karten</c>). Der Ort ist ein Zwischenstand: Sobald
        /// <see cref="WaermesenkeClass"/> die beiden neuen Ziele kennt, gehört die
        /// Abbildung dorthin, neben <c>WaermesenkeClass.ZielAnzeige</c> — bis dahin steht
        /// sie hier EINMAL statt zweimal in zwei Formularen.
        /// </summary>
        public static string ZielAnzeige(string ziel)
        {
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PROZESS, StringComparison.Ordinal))
                return MyResource.Resource.KANAL_PROZESS_ANZEIGE;
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_PROZESS, StringComparison.Ordinal))
                return MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_PROZESS;
            return WaermesenkeClass.ZielAnzeige(ziel);
        }

        /// <summary>
        /// Kompakte Anzeige EINER Senkenzeile für Karten, Übersichten und Schema:
        /// „Ziel: Speicher" beim Ladeziel, beim HEIZKREIS das Ziel samt Bedarfsart
        /// („Heizkreis (nur Warmwasser)"), <c>–</c> für „keine Zeile".
        ///
        /// <para><b>PAKET A1 — die EINE Stelle.</b> Bis dahin gab es zwei: diese für die
        /// Kette ab Rang 3 und <c>WaermesenkeClass.HauptsenkeAnzeige</c>/
        /// <c>ZweitsenkeAnzeige</c> für die gespiegelten Ränge 1 und 2. Mit der Spiegelung
        /// sind die beiden anderen entfallen. Damit sich am BILD nichts ändert, sind
        /// ihre beiden Eigenheiten hierher gewandert: die Kurzform des Ladeziels
        /// (<c>WaermesenkeClass.KurzformZuZiel</c>) und die Bedarfsart-Feinsteuerung des
        /// Heizkreises (Konzept 3.1).</para>
        /// </summary>
        public static string SenkeAnzeige(Z_AnlageSenkeModel z)
        {
            if (z == null) return LEER;

            if (IstPufferZiel(z.Ziel))
            {
                // KURZFORM („Puffer Heizung"), nicht der lange Name der Auswahlliste:
                // Karte, Übersicht und Schemaknoten haben die Senke schon immer so
                // beschriftet, und daran ändert A1 nichts.
                string ladeziel = WaermesenkeClass.KurzformZuZiel(z.Ziel);
                if (z.ID_Puffer <= 0) return ladeziel;

                string name = WaermesenkeClass.PufferName(z.ID_Puffer);
                return name.Length > 0 ? ladeziel + ": " + name : ladeziel;
            }

            // Prozesswärme ist einkanalig - dort gibt es keine Bedarfsart zu unterscheiden.
            if (string.Equals(z.Ziel, DbWerte.WS_ZIEL_PROZESS, StringComparison.Ordinal))
                return MyResource.Resource.KANAL_PROZESS_ANZEIGE;

            switch (z.Bedarfsart)
            {
                case WaermequelleClass.SENKE_WARMWASSER:
                    return MyResource.Resource.SIM_HEIZKREIS_NUR_WARMWASSER;
                case WaermequelleClass.SENKE_HEIZUNG:
                    return MyResource.Resource.SIM_HEIZKREIS_NUR_HEIZWAERME;
                default:
                    return MyResource.Resource.SIM_HEIZKREIS_BEIDES;
            }
        }

        // PAKET A1: IstProzessZiel ist ENTFALLEN. Die Abfrage diente genau EINEM Zweck -
        // eine Anzeige, die aus den Altspalten WS_* las, musste die beiden Prozess-Ziele
        // erkennen, weil sie sich dort als „Heizung" spiegelten. Anzeigen lesen jetzt die
        // Senkenliste, und die trägt das wahre Ziel; SenkeAnzeige genügt.

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.SIM_SENKE_TITEL;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(620, 618);

            // Die vier Gruppen liegen untereinander; jede Höhe steht als Konstante da,
            // damit die nächste Oberkante eine RECHNUNG ist und keine abgeschriebene Zahl
            // (dieselbe Denkweise wie der VERBUND_ZUWACHS des Bestands).
            const int LISTE_OBEN = 10;
            const int LISTE_HOEHE = 200;
            const int ZEILE_OBEN = LISTE_OBEN + LISTE_HOEHE + 8;
            const int ZEILE_HOEHE = 116;
            const int VERBUND_OBEN = ZEILE_OBEN + ZEILE_HOEHE + 8;
            const int VERBUND_HOEHE = 138;
            const int LADEN_OBEN = VERBUND_OBEN + VERBUND_HOEHE + 8;
            // PAKET P2: 140 → 176 — die Gruppe trägt jetzt eine vierte Zeile
            // (Einspeisehöhe) unter der PV-Zeile. Alles darunter hängt an dieser
            // Konstante und rückt mit; das Fenster wächst um dieselben 36 px und wird
            // wie bisher von FensterEinpassung in die Arbeitsfläche eingepasst.
            const int LADEN_HOEHE = 176;

            // --- Senkenliste ---------------------------------------------------------
            GroupBox gbListe = new GroupBox
            {
                Text = MyResource.Resource.SIM_GRUPPE_SENKENLISTE,
                Location = new Point(12, LISTE_OBEN),
                Size = new Size(596, LISTE_HOEHE)
            };
            this.Controls.Add(gbListe);

            _lvSenken = new ListView
            {
                Location = new Point(16, 22),
                Size = new Size(564, 128),
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            // PAKET P2: sechste Spalte „Höhe" (Einspeisehöhe). Die Gesamtbreite der
            // Spalten bleibt bei 560 px und damit innerhalb der Listenbreite von 564 —
            // die vier bestehenden Spalten geben die 62 px dafür ab, statt eine
            // Bildlaufleiste unter der Liste zu erzeugen.
            _lvSenken.Columns.Add(MyResource.Resource.SIM_SPALTE_RANG, 40);
            _lvSenken.Columns.Add(MyResource.Resource.SIM_SPALTE_ZIEL, 146);
            _lvSenken.Columns.Add(MyResource.Resource.SIM_SPALTE_SPEICHER, 150);
            _lvSenken.Columns.Add(MyResource.Resource.SIM_SPALTE_BEDARFSART, 96);
            _lvSenken.Columns.Add(MyResource.Resource.SIM_SPALTE_LADEN, 66);
            _lvSenken.Columns.Add(MyResource.Resource.SIM_SPALTE_EINSPEISEHOEHE, 62);
            _lvSenken.SelectedIndexChanged += Zeile_Gewechselt;
            gbListe.Controls.Add(_lvSenken);

            _btnHinzu = new Button
            {
                Text = MyResource.Resource.SIM_BTN_SENKE_HINZU,
                Location = new Point(16, 158),
                Size = new Size(110, 26)
            };
            _btnHinzu.Click += btnHinzu_Click;

            _btnEntfernen = new Button
            {
                Text = MyResource.Resource.SIM_BTN_SENKE_ENTFERNEN,
                Location = new Point(132, 158),
                Size = new Size(110, 26)
            };
            _btnEntfernen.Click += btnEntfernen_Click;

            _btnRauf = new Button
            {
                Text = MyResource.Resource.SIM_BTN_SENKE_RAUF,
                Location = new Point(346, 158),
                Size = new Size(114, 26)
            };
            _btnRauf.Click += btnRauf_Click;

            _btnRunter = new Button
            {
                Text = MyResource.Resource.SIM_BTN_SENKE_RUNTER,
                Location = new Point(466, 158),
                Size = new Size(114, 26)
            };
            _btnRunter.Click += btnRunter_Click;

            gbListe.Controls.Add(_btnHinzu);
            gbListe.Controls.Add(_btnEntfernen);
            gbListe.Controls.Add(_btnRauf);
            gbListe.Controls.Add(_btnRunter);

            // --- Die gewählte Zeile --------------------------------------------------
            _gbZeile = new GroupBox
            {
                Text = MyResource.Resource.SIM_GRUPPE_SENKENZEILE,
                Location = new Point(12, ZEILE_OBEN),
                Size = new Size(596, ZEILE_HOEHE)
            };
            this.Controls.Add(_gbZeile);

            Label lblZiel = new Label
            {
                Text = MyResource.Resource.SIM_LBL_ZIEL2,
                AutoSize = true,
                Location = new Point(16, 26)
            };
            _cbZiel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 22),
                Width = 300
            };
            ZielListeFuellen();
            _cbZiel.SelectedIndexChanged += Auswahl_Geaendert;

            Label lblPuffer = new Label
            {
                Text = MyResource.Resource.PSP_RUBRIK_LABEL,
                AutoSize = true,
                Location = new Point(16, 58)
            };
            _cbPuffer = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 54),
                Width = 430
            };
            // PAKET S2: Die Kopfkorrektur steht VOR der Übernahme in die Zeile — sonst
            // stünde ein Gruppenkopf für einen Wimpernschlag als Puffer der Zeile da.
            _cbPuffer.SelectedIndexChanged += Puffer_Ausgewaehlt;
            _cbPuffer.SelectedIndexChanged += Auswahl_Geaendert;

            Label lblBedarf = new Label
            {
                Text = MyResource.Resource.SIM_LBL_BEDARFSART,
                AutoSize = true,
                Location = new Point(16, 90)
            };
            _cbBedarfsart = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 86),
                Width = 210
            };
            _cbBedarfsart.Items.AddRange(new object[]
            {
                MyResource.Resource.SIM_BEDARF_BEIDES, MyResource.Resource.SIM_BEDARF_WARMWASSER, MyResource.Resource.SIM_BEDARF_HEIZWAERME
            });
            _cbBedarfsart.SelectedIndexChanged += Auswahl_Geaendert;

            Label lblBedarfHinweis = new Label
            {
                Text = MyResource.Resource.SIM_LBL_BEDARF_HINWEIS,
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Location = new Point(370, 90)
            };

            _gbZeile.Controls.Add(lblZiel);
            _gbZeile.Controls.Add(_cbZiel);
            _gbZeile.Controls.Add(lblPuffer);
            _gbZeile.Controls.Add(_cbPuffer);
            _gbZeile.Controls.Add(lblBedarf);
            _gbZeile.Controls.Add(_cbBedarfsart);
            _gbZeile.Controls.Add(lblBedarfHinweis);

            // --- Parallelverbund der Senke auf Rang 1 --------------------------------
            _gbVerbund = new GroupBox
            {
                Text = SIM_GB_VERBUND,
                Location = new Point(12, VERBUND_OBEN),
                Size = new Size(596, VERBUND_HOEHE)
            };
            this.Controls.Add(_gbVerbund);

            Label lblVerbund = new Label
            {
                Text = SIM_LBL_VERBUND_ZUSATZ,
                AutoSize = true,
                Location = new Point(16, 20)
            };

            // CheckedListBox im Bestandsstil der Auswahlfelder: dieselbe Breite wie das
            // Speicher-Dropdown (430 + Beschriftungsspalte), volle Gruppenbreite minus
            // Rand. CheckOnClick, damit ein Klick genügt - ohne die Eigenschaft verlangt
            // WinForms zwei Klicks (erst Auswahl, dann Haken), und das liest sich wie ein
            // Defekt.
            _clbVerbund = new CheckedListBox
            {
                Location = new Point(16, 40),
                Size = new Size(564, 68),
                CheckOnClick = true,
                IntegralHeight = false
            };
            _clbVerbund.ItemCheck += VerbundHaken_Geaendert;

            _lblVerbundSumme = new Label
            {
                AutoSize = false,
                Location = new Point(16, 114),
                Size = new Size(564, 16),
                ForeColor = SystemColors.GrayText,
                Text = ""
            };

            _gbVerbund.Controls.Add(lblVerbund);
            _gbVerbund.Controls.Add(_clbVerbund);
            _gbVerbund.Controls.Add(_lblVerbundSumme);

            // --- Ladeverhalten der gewählten Zeile -----------------------------------
            _gbLaden = new GroupBox
            {
                Text = MyResource.Resource.SIM_GB_LADEVERHALTEN,
                Location = new Point(12, LADEN_OBEN),
                Size = new Size(596, LADEN_HOEHE)
            };
            this.Controls.Add(_gbLaden);

            Label lblPrio = new Label { Text = MyResource.Resource.SIM_LBL_LADEPRIO, AutoSize = true, Location = new Point(16, 28) };
            _cbLadeprio = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 24),
                Width = 210
            };
            _cbLadeprio.SelectedIndexChanged += Auswahl_Geaendert;

            _lblPosition = new Label
            {
                AutoSize = false,
                Location = new Point(370, 28),
                Size = new Size(210, 32),
                Text = ""
            };

            _chkLadegrenze = new CheckBox
            {
                Text = MyResource.Resource.SIM_CHK_LADEGRENZE,
                AutoSize = true,
                Location = new Point(19, 66)
            };
            _chkLadegrenze.CheckedChanged += Auswahl_Geaendert;
            _tbLadegrenze = new TextBox { Location = new Point(196, 63), Width = 60, Text = "70" };
            _tbLadegrenze.TextChanged += Auswahl_Geaendert;
            _lblLadegrenzeEinheit = new Label
            {
                Text = MyResource.Resource.SIM_LBL_LADEGRENZE_EINHEIT,
                AutoSize = true,
                Location = new Point(262, 66),

                // PAKET L (P2-O6, Bestandsbefund): Der Einheitentext ist der laengste der
                // Gruppe und ragte auf der ENGLISCHEN Oberflaeche ueber den Gruppenrahmen
                // hinaus - "% of the storage (otherwise the switch-off threshold …)" misst
                // rund 356 px, verfuegbar sind ab x = 262 nur 318.
                //
                // MaximumSize deckelt die Breite, AutoSize bleibt an: Der Text BRICHT UM
                // und waechst nach unten, statt am Rahmen abgeschnitten zu werden (die
                // Zeile darunter beginnt erst bei y = 98, zwei Zeilen enden bei y ~ 92).
                // Der deutsche Text bleibt mit ~310 px einzeilig und steht Pixel fuer
                // Pixel wie bisher - die Aenderung ist auf deutscher Oberflaeche unsichtbar.
                MaximumSize = new Size(_gbLaden.Width - 262 - 16, 0)
            };

            _lblPV = new Label { Text = MyResource.Resource.SIM_LBL_PV_UEBERSCHUSS, AutoSize = true, Location = new Point(16, 102) };
            _cbLadeprioPV = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 98),
                Width = 210
            };
            _cbLadeprioPV.SelectedIndexChanged += Auswahl_Geaendert;

            // --- Einspeisehöhe (PAKET P2, Konzept 5.1/7.4 Punkt 1) -------------------
            //
            // Dieselbe Spaltenordnung wie die Ladeobergrenze darüber: Haken links,
            // Zahlenfeld auf x = 196, Einheitentext daneben. Der Haken trägt die
            // Unterscheidung „nicht gesetzt" ↔ „gesetzt" — ohne ihn wäre ein leeres Feld
            // von der gültigen Höhe 0 (ganz unten) nicht zu trennen.
            _chkEinspeisehoehe = new CheckBox
            {
                Text = MyResource.Resource.SIM_CHK_EINSPEISEHOEHE,
                AutoSize = true,
                Location = new Point(19, 138)
            };
            _chkEinspeisehoehe.CheckedChanged += Auswahl_Geaendert;

            _tbEinspeisehoehe = new TextBox { Location = new Point(196, 135), Width = 60, Text = "1" };
            _tbEinspeisehoehe.TextChanged += Auswahl_Geaendert;

            _lblEinspeisehoeheEinheit = new Label
            {
                Text = MyResource.Resource.SIM_LBL_EINSPEISEHOEHE_EINHEIT,
                AutoSize = true,
                Location = new Point(262, 138)
            };

            // Die Wirkungsbedingung („nur bei geschichtetem Zielspeicher, N > 1") steht
            // im ToolTip und nicht im Bild - sie ist eine Eigenschaft des SPEICHERS, die
            // dieser Dialog nicht einstellt.
            string tipHoehe = Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_TIP_EINSPEISEHOEHE);
            _tips.SetToolTip(_chkEinspeisehoehe, tipHoehe);
            _tips.SetToolTip(_tbEinspeisehoehe, tipHoehe);
            _tips.SetToolTip(_lblEinspeisehoeheEinheit, tipHoehe);

            _gbLaden.Controls.Add(lblPrio);
            _gbLaden.Controls.Add(_cbLadeprio);
            _gbLaden.Controls.Add(_lblPosition);
            _gbLaden.Controls.Add(_chkLadegrenze);
            _gbLaden.Controls.Add(_tbLadegrenze);
            _gbLaden.Controls.Add(_lblLadegrenzeEinheit);
            _gbLaden.Controls.Add(_lblPV);
            _gbLaden.Controls.Add(_cbLadeprioPV);
            _gbLaden.Controls.Add(_chkEinspeisehoehe);
            _gbLaden.Controls.Add(_tbEinspeisehoehe);
            _gbLaden.Controls.Add(_lblEinspeisehoeheEinheit);

            // --- Hinweis und Absprung -------------------------------------------------
            //
            // NACHARBEIT I-K1-1 — DIE HÖHE WIRD GERECHNET, NICHT GESCHÄTZT.
            //
            // Der Hinweistext ist mit dem Kombi-Satz von rund 116 auf rund 271 Zeichen
            // gewachsen; die feste Fläche 390 × 56 px trug davon drei Zeilen, der Rest
            // wurde unten abgeschnitten — ausgerechnet die Knappheitsregel „Warmwasser
            // zuerst", für die der Satz da ist. Weil der Text zudem übersetzt wird und
            // die englische Fassung anders umbricht, ist jede feste Höhe die nächste
            // Fehlerquelle. TextRenderer.MeasureText misst den Umbruch mit DERSELBEN
            // Schrift und DERSELBEN Breite, mit der das Label ihn später zeichnet;
            // Trenner, Knöpfe und ClientSize hängen an dem Ergebnis.
            const int HINWEIS_LINKS = 14;
            const int HINWEIS_BREITE = 390;
            const int HINWEIS_OBEN = LADEN_OBEN + LADEN_HOEHE + 8;
            const int HINWEIS_MIN = 56;     // nie kleiner als der Bestand
            const int HINWEIS_MAX = 160;    // Notbremse gegen eine entgleiste Übersetzung

            string hinweisText = MyResource.Resource.SIM_LBL_HINWEIS_PUFFER +
                                 Environment.NewLine + MyResource.Resource.SIM_LBL_HINWEIS_KOMBI;

            int hinweisHoehe = TextRenderer.MeasureText(
                hinweisText, this.Font, new Size(HINWEIS_BREITE, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height + 4;

            if (hinweisHoehe < HINWEIS_MIN) hinweisHoehe = HINWEIS_MIN;
            if (hinweisHoehe > HINWEIS_MAX) hinweisHoehe = HINWEIS_MAX;

            _lblHinweis = new Label
            {
                AutoSize = false,
                Location = new Point(HINWEIS_LINKS, HINWEIS_OBEN),
                Size = new Size(HINWEIS_BREITE, hinweisHoehe),
                // D5a: Der Bestandshinweis bleibt; der Kombi-Satz kommt dazu, weil die
                // Knappheitsregel (Warmwasser zuerst) sonst nirgends sichtbar wäre.
                Text = hinweisText
            };
            this.Controls.Add(_lblHinweis);

            _btnPufferAnlegen = new Button
            {
                Text = MyResource.Resource.PSP_BTN_PUFFER_ANLEGEN,
                Location = new Point(410, HINWEIS_OBEN + 4),
                Size = new Size(198, 28)
            };
            _btnPufferAnlegen.Click += btnPufferAnlegen_Click;
            this.Controls.Add(_btnPufferAnlegen);

            // Trenner und Fußzeile hängen am gemessenen Hinweis. Die Abstände sind die
            // bisherigen (10 px über dem Trenner, 18 px darunter, 23 px Knopfhöhe,
            // 23 px Rand) — bei einem 56-px-Hinweis kommt exakt das alte Raster heraus.
            int trennerOben = HINWEIS_OBEN + hinweisHoehe + 10;
            int knopfOben = trennerOben + 18;

            Label trenner = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(12, trennerOben),
                Size = new Size(596, 2)
            };
            this.Controls.Add(trenner);

            Button btnOk = new Button
            {
                Text = MyResource.Resource.SIM_BTN_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, knopfOben),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, knopfOben),
                Width = 85
            };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbruch);
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbruch;

            // Fensterhöhe zuletzt, aus der gemessenen Fußzeile. Der Absprungknopf reicht
            // bei einem kurzen Hinweis tiefer als das Label - beide gehen in die Rechnung.
            int unten = Math.Max(knopfOben + btnOk.Height,
                                 Math.Max(HINWEIS_OBEN + hinweisHoehe,
                                          _btnPufferAnlegen.Bottom));
            this.ClientSize = new Size(this.ClientSize.Width, unten + 23);
        }

        /// <summary>
        /// Die sechs Ziele in der Reihenfolge des Konzepts (5.1): erst die beiden
        /// Direktsenken, dann die vier Ladeziele.
        /// </summary>
        private void ZielListeFuellen()
        {
            _cbZiel.Items.Clear();
            _cbZiel.Items.Add(new ZielItem
            {
                Wert = DbWerte.WS_ZIEL_HEIZKREIS,
                Text = MyResource.Resource.SIM_RB_HEIZKREIS
            });
            _cbZiel.Items.Add(new ZielItem
            {
                Wert = DbWerte.WS_ZIEL_PROZESS,
                Text = MyResource.Resource.KANAL_PROZESS_ANZEIGE
            });
            _cbZiel.Items.Add(new ZielItem
            {
                Wert = DbWerte.WS_ZIEL_PUFFER_HEIZUNG,
                Text = MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_HEIZUNG
            });
            _cbZiel.Items.Add(new ZielItem
            {
                Wert = DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER,
                Text = MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_BRAUCHWASSER
            });
            _cbZiel.Items.Add(new ZielItem
            {
                Wert = DbWerte.WS_ZIEL_PUFFER_PROZESS,
                Text = MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_PROZESS
            });
            _cbZiel.Items.Add(new ZielItem
            {
                Wert = DbWerte.WS_ZIEL_PUFFER_KOMBI,
                Text = MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_KOMBI
            });
            _cbZiel.SelectedIndex = 0;
        }

        // --- Befüllen -----------------------------------------------------------------

        /// <summary>Füllt den Dialog aus der Senkenliste der Anlage und den Projekt-Puffern.</summary>
        public void SetControls()
        {
            if (!string.IsNullOrEmpty(AnlagenName))
                this.Text = string.Format(MyResource.Resource.SIM_SENKE_TITEL_ANLAGE, AnlagenName);

            _aktualisiert = true;
            try
            {
                PufferListenLaden();
                PrioListeFuellen(_cbLadeprio, false);
                PrioListeFuellen(_cbLadeprioPV, true);

                ZeilenLaden();
                ListeAufbauen(0);

                // PAKET PARALLELVERBUND — ZULETZT: Die Liste schließt den Leitspeicher und
                // alle übrigen Ziele aus, und die stehen erst jetzt fest. Der Aufbau der
                // Liste und das Setzen der Haken sind zwei Schritte, weil die Liste eine
                // vorherige Auswahl nachzieht (VerbundListeFuellen) - beim ERSTEN Öffnen
                // gibt es die noch nicht, sie kommt aus VerbundMitglieder.
                VerbundListeFuellen();
                if (VerbundMitglieder == null) VerbundMitglieder = new List<int>();
                for (int i = 0; i < _verbundKandidaten.Count; i++)
                    _clbVerbund.SetItemChecked(
                        i, VerbundMitglieder.Contains(_verbundKandidaten[i].ID));
            }
            finally
            {
                _aktualisiert = false;
            }

            AnzeigeAktualisieren();
        }

        /// <summary>
        /// Baut <see cref="_zeilen"/> aus <c>Z_AnlageSenke</c> auf — der einzigen Ablage
        /// der Senken (PAKET A1).
        ///
        /// <para><b>Der Rückfall auf die Altspalten <c>WS_*</c> ist entfallen.</b> Er
        /// überbrückte die Zeit zwischen einem Programmstand mit Senkenliste und einer
        /// Datenbank ohne die Tabelle. Diesen Zustand gibt es hier nicht mehr: Die
        /// Migration läuft bei jedem Programmstart, und kommt sie nicht durch, sperrt
        /// <c>Form_Simulation_Config</c> den ganzen Simulationsbereich
        /// (<c>SchemaMigration.SimulationGesperrt</c>) — dieser Dialog ist dann gar nicht
        /// erreichbar. Ein Rückfall auf eine Ablage, aus der niemand mehr rechnet, hätte
        /// nur eine Senke angezeigt, die kein Lauf mehr kennt.</para>
        ///
        /// Die Invariante „Rang 1 ist Pflicht" (Konzept 5.1) wird hier hergestellt: Ohne
        /// jede Zeile entsteht <c>Heizkreis/Beides</c> — dieselbe Normalisierungsregel,
        /// die die Engine anwendet, wenn sie keine Zeile findet.
        /// </summary>
        private void ZeilenLaden()
        {
            _zeilen.Clear();

            if (Z_AnlageSenkeCtrl.SpalteVorhanden() && ID_Anlage > 0)
            {
                List<Z_AnlageSenkeModel> gelesen =
                    new Z_AnlageSenkeCtrl().LesenJeAnlage(ID_Anlage);
                if (gelesen != null)
                    foreach (Z_AnlageSenkeModel z in gelesen)
                        if (z != null) _zeilen.Add(z);
            }

            if (_zeilen.Count == 0) _zeilen.Add(NeueZeile(DbWerte.WS_ZIEL_HEIZKREIS));
        }

        private Z_AnlageSenkeModel NeueZeile(string ziel)
        {
            return new Z_AnlageSenkeModel
            {
                ID_Anlage = ID_Anlage,
                Ziel = string.IsNullOrEmpty(ziel) ? DbWerte.WS_ZIEL_HEIZKREIS : ziel,
                Bedarfsart = WaermequelleClass.SENKE_BEIDES
            };
        }

        // --- Die Liste als ListView ---------------------------------------------------

        /// <summary>
        /// Baut die ListView aus <see cref="_zeilen"/> neu auf und wählt
        /// <paramref name="auswahl"/> aus. Aufgerufen bei jedem STRUKTUR-Wechsel
        /// (hinzufügen, entfernen, verschieben); eine reine Wertänderung fasst nur die
        /// betroffene Zeile an (<see cref="ZeileAnzeigen"/>).
        /// </summary>
        private void ListeAufbauen(int auswahl)
        {
            bool vorher = _aktualisiert;
            _aktualisiert = true;
            try
            {
                _lvSenken.Items.Clear();
                for (int i = 0; i < _zeilen.Count; i++)
                {
                    ListViewItem it = new ListViewItem((i + 1).ToString());
                    it.SubItems.Add("");
                    it.SubItems.Add("");
                    it.SubItems.Add("");
                    it.SubItems.Add("");
                    it.SubItems.Add("");   // PAKET P2: Spalte „Höhe"
                    _lvSenken.Items.Add(it);
                    ZeileAnzeigen(i);
                }

                if (_zeilen.Count == 0) return;

                if (auswahl < 0) auswahl = 0;
                if (auswahl >= _zeilen.Count) auswahl = _zeilen.Count - 1;
                _lvSenken.Items[auswahl].Selected = true;
                _lvSenken.Items[auswahl].Focused = true;
            }
            finally
            {
                _aktualisiert = vorher;
            }

            if (_zeilen.Count > 0) ZeileInOberflaeche(AktuellerIndex());
        }

        /// <summary>Schreibt EINE Modellzeile in ihre ListView-Zeile.</summary>
        private void ZeileAnzeigen(int index)
        {
            if (index < 0 || index >= _zeilen.Count || index >= _lvSenken.Items.Count) return;

            Z_AnlageSenkeModel z = _zeilen[index];
            ListViewItem it = _lvSenken.Items[index];

            it.SubItems[0].Text = (index + 1).ToString();
            it.SubItems[1].Text = ZielAnzeige(z.Ziel);
            it.SubItems[2].Text = IstPufferZiel(z.Ziel)
                ? (z.ID_Puffer > 0 ? WaermesenkeClass.PufferName(z.ID_Puffer) : LEER)
                : LEER;
            it.SubItems[3].Text = IstHeizkreis(z.Ziel) ? BedarfsartAnzeige(z.Bedarfsart) : LEER;
            it.SubItems[4].Text = LadespalteText(z);
            it.SubItems[5].Text = HoehenspalteText(z);
        }

        /// <summary>
        /// Spalte „Höhe": die Einspeisehöhe der Zeile, sonst das Leerzeichen der Liste
        /// (PAKET P2). „Nicht gesetzt" ist der Regelfall und heißt „oben" — dort eine 1
        /// hinzuschreiben behauptete eine Pflege, die es nicht gibt.
        /// </summary>
        private static string HoehenspalteText(Z_AnlageSenkeModel z)
        {
            if (!IstPufferZiel(z.Ziel) || z.Anschlusshoehe < 0 || z.Anschlusshoehe > 1) return LEER;
            return z.Anschlusshoehe.ToString("0.##");
        }

        /// <summary>Anzeigetext der Bedarfsart (Steuerwert bleibt der deutsche Persistenzwert).</summary>
        private static string BedarfsartAnzeige(string bedarfsart)
        {
            if (string.Equals(bedarfsart, WaermequelleClass.SENKE_WARMWASSER, StringComparison.Ordinal))
                return MyResource.Resource.SIM_BEDARF_WARMWASSER;
            if (string.Equals(bedarfsart, WaermequelleClass.SENKE_HEIZUNG, StringComparison.Ordinal))
                return MyResource.Resource.SIM_BEDARF_HEIZWAERME;
            return MyResource.Resource.SIM_BEDARF_BEIDES;
        }

        /// <summary>
        /// Spalte „Laden": Ladepriorität und Obergrenze in Kurzform. Ohne Puffer-Ziel und
        /// ohne eigene Werte steht dort das Leerzeichen der Liste — „nach Vorgabe" ist die
        /// Regel, nicht die Ausnahme, und eine Zahl dafür zu erfinden wäre irreführend.
        /// </summary>
        private static string LadespalteText(Z_AnlageSenkeModel z)
        {
            if (!IstPufferZiel(z.Ziel)) return LEER;

            string s = z.Ladeprio > 0 ? z.Ladeprio.ToString() : LEER;
            if (z.Ladegrenze > 0) s += " · " + z.Ladegrenze.ToString("0.#") + " %";
            return s;
        }

        private int AktuellerIndex()
        {
            if (_lvSenken.SelectedIndices.Count == 0) return -1;
            int i = _lvSenken.SelectedIndices[0];
            return (i >= 0 && i < _zeilen.Count) ? i : -1;
        }

        private Z_AnlageSenkeModel AktuelleZeile()
        {
            int i = AktuellerIndex();
            return i >= 0 ? _zeilen[i] : null;
        }

        // --- Zeilenwechsel und Bearbeiten ---------------------------------------------

        private void Zeile_Gewechselt(object sender, EventArgs e)
        {
            if (_aktualisiert) return;

            ZeileInOberflaeche(AktuellerIndex());
            AnzeigeAktualisieren();
        }

        /// <summary>Überträgt eine Modellzeile in die Bedienelemente der Zeilengruppe.</summary>
        private void ZeileInOberflaeche(int index)
        {
            if (index < 0 || index >= _zeilen.Count) return;

            bool vorher = _aktualisiert;
            _aktualisiert = true;
            try
            {
                Z_AnlageSenkeModel z = _zeilen[index];

                ZielWaehlen(z.Ziel);
                PufferListeFuerZiel(z.Ziel);
                PufferWaehlen(_cbPuffer, z.ID_Puffer);

                if (string.Equals(z.Bedarfsart, WaermequelleClass.SENKE_WARMWASSER, StringComparison.Ordinal))
                    _cbBedarfsart.SelectedIndex = 1;
                else if (string.Equals(z.Bedarfsart, WaermequelleClass.SENKE_HEIZUNG, StringComparison.Ordinal))
                    _cbBedarfsart.SelectedIndex = 2;
                else
                    _cbBedarfsart.SelectedIndex = 0;

                PrioWaehlen(_cbLadeprio, z.Ladeprio);
                PrioWaehlen(_cbLadeprioPV, z.Ladeprio_PV);

                _chkLadegrenze.Checked = z.Ladegrenze != 0;
                if (z.Ladegrenze > 0) _tbLadegrenze.Text = z.Ladegrenze.ToString("0.#");

                // PAKET P2: Die Einspeisehöhe ist gesetzt, sobald sie im Bereich 0…1
                // liegt - 0 gehört ausdrücklich dazu (ganz unten). Alles andere (−1 aus
                // der Datenbank-NULL, die Ersatzwerte einer fehlerhaften Eingabe) heißt
                // „nicht gesetzt"; das Feld behält dann seinen letzten Text als
                // Vorschlag, der Haken ist aber aus.
                bool hoeheGesetzt = z.Anschlusshoehe >= 0 && z.Anschlusshoehe <= 1;
                _chkEinspeisehoehe.Checked = hoeheGesetzt;
                if (hoeheGesetzt) _tbEinspeisehoehe.Text = z.Anschlusshoehe.ToString("0.##");
            }
            finally
            {
                _aktualisiert = vorher;
            }
        }

        /// <summary>
        /// Liest die Bedienelemente in die gewählte Modellzeile zurück.
        ///
        /// Felder, die zum gewählten Ziel nicht passen, werden GELÖSCHT statt stehen
        /// gelassen: Eine Ladepriorität an einer Direktsenke ist kein harmloser Rest, sie
        /// stünde in der Ladeordnung und würde beim nächsten Zielwechsel unbemerkt wieder
        /// wirksam. Dieselbe Regel wie in <c>WaermesenkeClass.Normalisieren</c>.
        /// </summary>
        private void ZeileAusOberflaeche(int index)
        {
            if (index < 0 || index >= _zeilen.Count) return;

            Z_AnlageSenkeModel z = _zeilen[index];
            z.Ziel = GewaehltesZiel();

            bool puffer = IstPufferZiel(z.Ziel);
            z.ID_Puffer = puffer ? AktuelleId(_cbPuffer) : 0;
            z.Ladeprio = puffer ? GewaehltePrio(_cbLadeprio) : 0;
            z.Ladegrenze = puffer ? LadegrenzeLesen() : 0;

            // PAKET P2: Die Einspeisehöhe gehört zum LADEVORGANG und hat an einer
            // Direktsenke keinen Gegenstand - dort wird sie wie Ladepriorität und
            // Obergrenze gelöscht, nicht stehen gelassen (§ 7.4 Punkt 1).
            z.Anschlusshoehe = puffer ? EinspeisehoeheLesen() : -1;

            // Konzept 5.1: Die PV-Sonderpriorität gibt es nur auf Rang 1 — sie hängt heute
            // konstruktiv an der Hauptsenke (Ladeordnung.cs:270-273), und eine zweite
            // Spalte dafür existiert nicht.
            z.Ladeprio_PV = (puffer && index == 0 && PvModus()) ? GewaehltePrio(_cbLadeprioPV) : 0;

            // Die Bedarfsart ist allein beim Heizkreis die Feinsteuerung (Konzept 3.1);
            // bei jedem anderen Ziel steht der Kanal fest.
            z.Bedarfsart = IstHeizkreis(z.Ziel) ? GewaehlteBedarfsart() : WaermequelleClass.SENKE_BEIDES;
        }

        private bool PvModus()
        {
            return string.Equals(BM_Typ, WaermequelleClass.MODUS_PV, StringComparison.Ordinal);
        }

        private string GewaehltesZiel()
        {
            ZielItem it = _cbZiel.SelectedItem as ZielItem;
            return it != null ? it.Wert : DbWerte.WS_ZIEL_HEIZKREIS;
        }

        private void ZielWaehlen(string ziel)
        {
            for (int i = 0; i < _cbZiel.Items.Count; i++)
            {
                ZielItem it = _cbZiel.Items[i] as ZielItem;
                if (it != null && string.Equals(it.Wert, ziel, StringComparison.Ordinal))
                {
                    _cbZiel.SelectedIndex = i;
                    return;
                }
            }
            _cbZiel.SelectedIndex = 0;
        }

        private string GewaehlteBedarfsart()
        {
            switch (_cbBedarfsart.SelectedIndex)
            {
                case 1: return WaermequelleClass.SENKE_WARMWASSER;
                case 2: return WaermequelleClass.SENKE_HEIZUNG;
                default: return WaermequelleClass.SENKE_BEIDES;
            }
        }

        /// <summary>
        /// Ladeobergrenze der gewählten Zeile [%]; 0 = nicht gesetzt. Eine Eingabe, die
        /// (noch) keine gültige Zahl ist, wird als Ersatzwert festgehalten und beim OK
        /// gemeldet (siehe <see cref="GRENZE_UNLESBAR"/>).
        /// </summary>
        private double LadegrenzeLesen()
        {
            if (!_chkLadegrenze.Checked) return 0;

            float zahl;
            if (!WaermequelleClass.ZahlParsen(_tbLadegrenze.Text, out zahl)) return GRENZE_UNLESBAR;
            if (zahl <= 0 || zahl > 100) return GRENZE_BEREICH;
            return zahl;
        }

        /// <summary>
        /// Einspeisehöhe der gewählten Zeile [0…1]; <b>−1 = nicht gesetzt</b> und damit
        /// „speist oben ein" (PAKET P2, Konzept 5.1/7.4).
        ///
        /// <para>Anders als bei der Ladeobergrenze ist <b>0 ein gültiger Wert</b> (ganz
        /// unten). Die Prüfung lässt deshalb <c>0 ≤ h ≤ 1</c> zu; eine noch unlesbare
        /// oder unplausible Eingabe wird - wie dort - als Ersatzwert festgehalten und
        /// beim OK mit Nennung des Rangs gemeldet, statt beim Zeilenwechsel
        /// stillschweigend zu verschwinden.</para>
        /// </summary>
        private double EinspeisehoeheLesen()
        {
            if (!_chkEinspeisehoehe.Checked) return -1;

            float zahl;
            if (!WaermequelleClass.ZahlParsen(_tbEinspeisehoehe.Text, out zahl)) return HOEHE_UNLESBAR;
            if (zahl < 0 || zahl > 1) return HOEHE_BEREICH;
            return zahl;
        }

        // --- Knöpfe der Liste ---------------------------------------------------------

        /// <summary>
        /// Hängt eine Senke an. Vorbelegt wird der Regelfall „Rest in den Heizungspuffer"
        /// — gibt es keinen Heizungspuffer im Projekt, bleibt es beim Heizkreis, damit
        /// keine Zeile mit Puffer-Ziel und leerem Speicher entsteht.
        /// </summary>
        private void btnHinzu_Click(object sender, EventArgs e)
        {
            Z_AnlageSenkeModel neu;
            if (_pufferHeizung.Count > 0)
            {
                neu = NeueZeile(DbWerte.WS_ZIEL_PUFFER_HEIZUNG);
                neu.ID_Puffer = ErsterFreierPuffer(_pufferHeizung);
                if (neu.ID_Puffer <= 0) neu = NeueZeile(DbWerte.WS_ZIEL_HEIZKREIS);
            }
            else
            {
                neu = NeueZeile(DbWerte.WS_ZIEL_HEIZKREIS);
            }

            _zeilen.Add(neu);
            ListeAufbauen(_zeilen.Count - 1);
            VerbundListeNeu();
            AnzeigeAktualisieren();
        }

        /// <summary>
        /// Erster Puffer der Liste, der noch nicht Ziel dieser Anlage ist; 0, wenn alle
        /// belegt sind. Verhindert, dass „Hinzufügen" eine Zeile erzeugt, die die
        /// Doppelbelegungsprüfung sofort wieder abweist.
        /// </summary>
        private int ErsterFreierPuffer(List<WaermesenkeClass.PufferInfo> liste)
        {
            foreach (WaermesenkeClass.PufferInfo p in liste)
            {
                bool belegt = false;
                foreach (Z_AnlageSenkeModel z in _zeilen)
                    if (z.ID_Puffer == p.ID) { belegt = true; break; }

                if (!belegt) return p.ID;
            }
            return 0;
        }

        private void btnEntfernen_Click(object sender, EventArgs e)
        {
            int i = AktuellerIndex();
            if (i < 0) return;

            // INVARIANTE „Rang 1 ist Pflicht" (Konzept 5.1): Findet die Engine keine
            // Zeile, rechnet sie Heizkreis/Beides mit Protokollwarnung - eine Anlage ohne
            // jede Senke ist also keine gültige Einstellung, sondern eine, die stillschweigend
            // ersetzt würde.
            if (_zeilen.Count <= 1)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_SENKE_LETZTE_ZEILE,
                                MyResource.Resource.SIM_SENKE_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _zeilen.RemoveAt(i);
            ListeAufbauen(i);
            VerbundListeNeu();
            AnzeigeAktualisieren();
        }

        private void btnRauf_Click(object sender, EventArgs e)
        {
            Tauschen(AktuellerIndex(), -1);
        }

        private void btnRunter_Click(object sender, EventArgs e)
        {
            Tauschen(AktuellerIndex(), +1);
        }

        /// <summary>Verschiebt die gewählte Zeile um <paramref name="richtung"/> Ränge.</summary>
        private void Tauschen(int index, int richtung)
        {
            int ziel = index + richtung;
            if (index < 0 || ziel < 0 || ziel >= _zeilen.Count) return;

            Z_AnlageSenkeModel merker = _zeilen[index];
            _zeilen[index] = _zeilen[ziel];
            _zeilen[ziel] = merker;

            // Der Rangwechsel kann eine PV-Sonderpriorität von Rang 1 wegtragen - sie gibt
            // es dort nicht mehr, und stehen zu lassen, was nicht mehr gilt, wäre der
            // Anfang einer stillen Falschrechnung.
            for (int i = 1; i < _zeilen.Count; i++) _zeilen[i].Ladeprio_PV = 0;

            ListeAufbauen(ziel);
            VerbundListeNeu();
            AnzeigeAktualisieren();
        }

        // --- Parallelverbund: Liste, Haken und Summenanzeige --------------------------

        /// <summary>
        /// Füllt die Verbundliste mit den Puffern, die ZUSÄTZLICH zum Leitspeicher in
        /// Frage kommen: dieselbe Verwendungsfilterung wie das Speicher-Dropdown von
        /// Rang 1, ohne den Leitspeicher selbst und ohne jedes andere Ziel dieser Anlage.
        ///
        /// <b>Dieselbe Filterung wie <see cref="PufferListenLaden"/></b> — die Liste greift
        /// auf genau die Listen zu, die dort geladen wurden (SENKENZIEL-Sicht, nicht
        /// Kanalsicht). Ein Verbund mischt keine Verwendungen: Ein Behälter, der als
        /// Brauchwasserspeicher gepflegt ist, gehört nicht in den Heizungsvorrat, und
        /// <c>WaermesenkeClass.Pruefen</c> weist genau das beim Speichern ab. Auswahl und
        /// Validierung dürfen nicht auseinanderlaufen.
        ///
        /// <b>Der LEITSPEICHER fehlt in der Liste</b>, denn er ist schon Teil des Verbunds
        /// (er ist der Vorratsbehälter, an dem die Regelung hängt).
        ///
        /// <b>Die übrigen SENKEN fehlen ebenfalls</b>: Jede ist ein eigenes Ladeziel mit
        /// eigener Priorität und Obergrenze und kann nicht gleichzeitig im Hauptvorrat
        /// stecken (dieselbe Regel wie in <c>WaermesenkeClass.VerbundNormalisieren</c>).
        /// Sie aus der Liste zu nehmen, ist freundlicher als eine Fehlermeldung beim
        /// Speichern.
        ///
        /// GESETZTE HAKEN BLEIBEN, soweit der Puffer noch in der Liste steht — Muster
        /// <see cref="FuelleCombo"/>, das die alte Auswahl ebenso nachzieht.
        /// </summary>
        private void VerbundListeFuellen()
        {
            List<int> vorher = GewaehlteVerbundMitglieder();
            int idLeit = AktuellerHauptPuffer();

            _verbundKandidaten = new List<WaermesenkeClass.PufferInfo>();
            foreach (WaermesenkeClass.PufferInfo p in Hauptsenkenliste())
            {
                if (p.ID == idLeit) continue;
                if (AndereSenkeBelegt(p.ID)) continue;
                _verbundKandidaten.Add(p);
            }

            _clbVerbund.Items.Clear();
            foreach (WaermesenkeClass.PufferInfo p in _verbundKandidaten)
                _clbVerbund.Items.Add(p);

            for (int i = 0; i < _verbundKandidaten.Count; i++)
                if (vorher.Contains(_verbundKandidaten[i].ID))
                    _clbVerbund.SetItemChecked(i, true);
        }

        /// <summary>Verbundliste unter dem Rückkopplungsschutz neu aufbauen.</summary>
        private void VerbundListeNeu()
        {
            bool vorher = _aktualisiert;
            _aktualisiert = true;
            try { VerbundListeFuellen(); }
            finally { _aktualisiert = vorher; }
        }

        /// <summary>true, wenn der Puffer Ziel einer Senke ab Rang 2 ist.</summary>
        private bool AndereSenkeBelegt(int idPuffer)
        {
            if (idPuffer <= 0) return false;
            for (int i = 1; i < _zeilen.Count; i++)
                if (_zeilen[i].ID_Puffer == idPuffer) return true;
            return false;
        }

        /// <summary>
        /// Die Puffer-Liste des Ziels auf RANG 1 (Bezugsgröße des Verbunds) — weiterhin
        /// die nach <c>Verwendung</c> GEFILTERTE Liste, siehe
        /// <see cref="PufferlisteVerbund"/>.
        /// </summary>
        private List<WaermesenkeClass.PufferInfo> Hauptsenkenliste()
        {
            if (_zeilen.Count == 0) return new List<WaermesenkeClass.PufferInfo>();
            return PufferlisteVerbund(_zeilen[0].Ziel);
        }

        /// <summary>Die gehakten Verbundmitglieder als Puffer-IDs; nie <c>null</c>.</summary>
        private List<int> GewaehlteVerbundMitglieder()
        {
            return GewaehlteVerbundMitglieder(-1, false);
        }

        /// <summary>
        /// Wie <see cref="GewaehlteVerbundMitglieder()"/>, aber mit einem ERSATZZUSTAND für
        /// genau einen Eintrag.
        ///
        /// Nötig für <see cref="VerbundHaken_Geaendert"/>: Das Ereignis
        /// <c>CheckedListBox.ItemCheck</c> feuert, BEVOR der neue Hakenzustand im
        /// Steuerelement steht. Ohne den Ersatz zeigte die Summenanzeige eine Zeile lang
        /// den vorherigen Stand — also genau in dem Moment die falsche Zahl, in dem der
        /// Anwender hinsieht.
        /// </summary>
        private List<int> GewaehlteVerbundMitglieder(int indexErsatz, bool gehaktErsatz)
        {
            List<int> ids = new List<int>();

            for (int i = 0; i < _verbundKandidaten.Count; i++)
            {
                bool gehakt = i == indexErsatz ? gehaktErsatz : _clbVerbund.GetItemChecked(i);
                if (gehakt) ids.Add(_verbundKandidaten[i].ID);
            }

            return ids;
        }

        /// <summary>
        /// Schreibt die Summenzeile „Verbund: n Speicher · Q_max gesamt x kWh" bzw. den
        /// Hinweis, dass kein Verbund gewählt ist.
        ///
        /// Die Kapazität kommt aus <c>WaermesenkeClass.VerbundKapazitaet</c> — derselben
        /// Summe über die EINZELkapazitäten, mit der die Engine rechnet. Der Dialog
        /// wiederholt die Formel nicht.
        /// </summary>
        private void VerbundSummeAnzeigen(List<int> mitglieder)
        {
            int idLeit = AktuellerHauptPuffer();

            if (idLeit <= 0 || mitglieder == null || mitglieder.Count == 0)
            {
                _lblVerbundSumme.Text = idLeit > 0 ? SIM_VERBUND_KEIN_VERBUND : "";
                return;
            }

            double q = WaermesenkeClass.VerbundKapazitaet(idLeit, mitglieder);
            _lblVerbundSumme.Text = string.Format(SIM_VERBUND_SUMME,
                                                  mitglieder.Count + 1, q.ToString("0.#"));
        }

        private void VerbundHaken_Geaendert(object sender, ItemCheckEventArgs e)
        {
            if (_aktualisiert) return;

            // Positionstext MIT: Der Verbund ändert die Kapazität des Ladeziels, und die
            // Ladeordnung-Vorschau nennt eine Obergrenze in Prozent davon. Ohne diesen
            // Aufruf blieben Summenzeile und Positionsangabe verschieden aktuell.
            VerbundSummeAnzeigen(GewaehlteVerbundMitglieder(e.Index, e.NewValue == CheckState.Checked));
        }

        // --- Puffer-Auswahllisten -----------------------------------------------------

        /// <summary>
        /// Lädt die Auswahlmengen des Dialogs.
        ///
        /// <para><b>PAKET S2 — die sperrende Filterung ist gefallen</b> (Konzept 6.2,
        /// Entscheidung F6). Bis S1 zeigte das Speicher-Dropdown nur die Puffer, deren
        /// <c>Verwendung</c> genau zum gewählten Ziel passte, und
        /// <c>WaermesenkeClass.Pruefen</c> wies beim Speichern alles andere ab. Seit S2
        /// zeigt es <see cref="_pufferAlle"/> — ALLE Projekt-Puffer, gruppiert nach
        /// Klassen-Set —, und eine unplausible Zuordnung erzeugt eine WARNUNG
        /// (<see cref="Warnkriterien"/>, Kriterium W1) statt einer Sperre.</para>
        ///
        /// <para><b>Die drei gefilterten Listen bleiben</b>, aber nur noch für zwei
        /// Zwecke: die Vorbelegung von „Hinzufügen" (der Regelfall bleibt der
        /// Heizungspuffer) und die Kandidatenliste des PARALLELVERBUNDS
        /// (<see cref="PufferlisteVerbund"/>). Für den Verbund ist die Filterung KEINE
        /// Altlast: <c>AnlagePufferVerbundCtrl.KonfliktPruefen</c> weist einen Verbund
        /// aus gemischten Verwendungen beim Speichern weiterhin ab (Grund
        /// <c>GRUND_PASST_NICHT</c>), und eine Auswahl anzubieten, die die Prüfung
        /// zurückweist, wäre eine Sackgasse. Der Verbund ist eigenes Paketgebiet.</para>
        /// </summary>
        private void PufferListenLaden()
        {
            _pufferHeizung = WaermesenkeClass.ProjektPufferListe(ID_Projekt, WaermesenkeClass.VERWENDUNG_HEIZUNG);
            _pufferBrauchwasser = WaermesenkeClass.ProjektPufferListe(ID_Projekt, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER);
            _pufferKombi = WaermesenkeClass.ProjektPufferListe(ID_Projekt, WaermesenkeClass.VERWENDUNG_KOMBI);

            _pufferAlle = WaermesenkeClass.ProjektPufferListe(ID_Projekt, null);
            _klassenSetJePuffer = PufferSpCtrl.KlassenSetsJeProjekt(ID_Projekt);
        }

        /// <summary>
        /// Das Klassen-Set eines Puffers aus dem beim Öffnen gelesenen Verzeichnis;
        /// unbekannte IDs bekommen die Vorbelegung {Heizung} — dieselbe Antwort wie
        /// <c>PufferSpCtrl.KlassenSetLesen</c> auf einen unbekannten Speicher.
        /// </summary>
        private PufferSpCtrl.KlassenSet KlassenSetVon(int idPuffer)
        {
            PufferSpCtrl.KlassenSet set;
            if (_klassenSetJePuffer.TryGetValue(idPuffer, out set) && set != null) return set;
            return new PufferSpCtrl.KlassenSet(true, false, false);
        }

        /// <summary>
        /// Ein GRUPPENKOPF im Speicher-Dropdown — „— Heizung + Brauchwasser —".
        ///
        /// Eine gewöhnliche <c>ComboBox</c> kennt keine Gruppen; der Kopf ist deshalb ein
        /// eigener Eintrag, der nicht ausgewählt bleiben kann
        /// (<see cref="Puffer_Ausgewaehlt"/> springt auf den nächsten echten Eintrag).
        /// Er trägt sein Klassen-Set mit, damit die Ordnung nachvollziehbar bleibt.
        /// </summary>
        private sealed class Gruppenkopf
        {
            public string Text = "";
            public override string ToString() { return Text; }
        }

        /// <summary>
        /// Die Auswahlliste, die zu einem Ziel gehört; leer bei Direktsenken.
        ///
        /// <para><b>PAKET S2: Für JEDES Puffer-Ziel ist das dieselbe Liste</b> — alle
        /// Projekt-Puffer (Konzept 6.2). Bis S1 hing die Liste am Ziel: Ein Kombi-Ziel
        /// verlangte einen Kombi-Puffer, ein Heizungs-Ziel einen Heizungs-Puffer, und
        /// <c>PufferProzess</c> bekam ersatzweise die Heizungsliste, weil es dafür gar
        /// keine <c>Verwendung</c> gibt. Was damals GESPERRT war — ein Prozess-Ziel auf
        /// einen Brauchwasserspeicher, ein Heizungs-Ziel auf einen Kombispeicher —, ist
        /// jetzt WÄHLBAR und erzeugt beim Speichern eine Warnung (Kriterium W1), falls
        /// das Klassen-Set des Speichers den Kanal nicht führt.</para>
        /// </summary>
        private List<WaermesenkeClass.PufferInfo> PufferlisteZuZiel(string ziel)
        {
            return IstPufferZiel(ziel) ? _pufferAlle : new List<WaermesenkeClass.PufferInfo>();
        }

        /// <summary>
        /// Die Kandidatenliste des PARALLELVERBUNDS — sie behält die Filterung nach
        /// <c>Verwendung</c> (Begründung in <see cref="PufferListenLaden"/>).
        /// </summary>
        private List<WaermesenkeClass.PufferInfo> PufferlisteVerbund(string ziel)
        {
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal) ||
                string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_PROZESS, StringComparison.Ordinal))
                return _pufferHeizung;
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                return _pufferBrauchwasser;
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                return _pufferKombi;
            return new List<WaermesenkeClass.PufferInfo>();
        }

        /// <summary>Setzt das Speicher-Dropdown auf die Liste des Ziels.</summary>
        private void PufferListeFuerZiel(string ziel)
        {
            FuelleCombo(_cbPuffer, PufferlisteZuZiel(ziel));
        }

        /// <summary>
        /// Füllt das Speicher-Dropdown mit GRUPPENKÖPFEN je Klassen-Set (Konzept 6.2:
        /// „die Puffer-Auswahl zeigt alle Projekt-Puffer, gruppiert nach Klassen-Set").
        ///
        /// <para>Die Gruppen stehen in einer festen, sprachneutralen Ordnung: nach der
        /// Bitmaske Heizung=1, Brauchwasser=2, Prozess=4 aufsteigend, also {H}, {B},
        /// {H,B}, {P}, {H,P}, {B,P}, {H,B,P}. Sie ist unabhängig von der Sprache der
        /// Oberfläche — sonst sprängen die Gruppen beim Sprachwechsel um. Innerhalb
        /// einer Gruppe bleibt die Reihenfolge der Datenbankabfrage (Bezeichner, ID).</para>
        ///
        /// <para>Ein EINZIGES Klassen-Set im Projekt — der Regelfall jedes
        /// Bestandsprojekts — bekommt KEINEN Kopf: Eine Gruppenüberschrift über der
        /// ganzen Liste teilt nichts ein und kostet nur eine Zeile.</para>
        /// </summary>
        private void FuelleCombo(ComboBox cb, List<WaermesenkeClass.PufferInfo> liste)
        {
            int alteId = AktuelleId(cb);
            cb.Items.Clear();

            int letzteMaske = -1;
            bool mitKoepfen = MaskenAnzahl(liste) > 1;

            foreach (WaermesenkeClass.PufferInfo p in liste)
            {
                if (p == null) continue;

                int maske = Maske(KlassenSetVon(p.ID));
                if (mitKoepfen && maske != letzteMaske)
                {
                    cb.Items.Add(new Gruppenkopf
                    {
                        Text = string.Format(MyResource.Resource.SIM_PUFFERGRUPPE_KOPF,
                                             Warnkriterien.KlassenSetAnzeige(KlassenSetVon(p.ID)))
                    });
                    letzteMaske = maske;
                }

                cb.Items.Add(p);
            }

            ErstenEchtenWaehlen(cb);
            if (alteId > 0) PufferWaehlen(cb, alteId);
        }

        /// <summary>Bitmaske eines Klassen-Sets: Heizung 1, Brauchwasser 2, Prozess 4.</summary>
        private static int Maske(PufferSpCtrl.KlassenSet set)
        {
            if (set == null) return 0;
            return (set.Heizung ? 1 : 0) + (set.Brauchwasser ? 2 : 0) + (set.Prozess ? 4 : 0);
        }

        /// <summary>Zahl der VERSCHIEDENEN Klassen-Sets in einer Pufferliste.</summary>
        private int MaskenAnzahl(List<WaermesenkeClass.PufferInfo> liste)
        {
            List<int> masken = new List<int>();
            foreach (WaermesenkeClass.PufferInfo p in liste)
            {
                if (p == null) continue;
                int m = Maske(KlassenSetVon(p.ID));
                if (!masken.Contains(m)) masken.Add(m);
            }
            return masken.Count;
        }

        /// <summary>Wählt den ersten Eintrag, der kein Gruppenkopf ist.</summary>
        private static void ErstenEchtenWaehlen(ComboBox cb)
        {
            for (int i = 0; i < cb.Items.Count; i++)
                if (cb.Items[i] is WaermesenkeClass.PufferInfo) { cb.SelectedIndex = i; return; }
        }

        /// <summary>
        /// Ein Gruppenkopf ist kein Speicher: Wird er gewählt (Tastatur, Mausrad), rückt
        /// die Auswahl auf den Eintrag darunter. Der Kopf bleibt sichtbar, aber nie
        /// gewählt — sonst stünde in der Zeile eine Senke ohne Puffer.
        /// </summary>
        private void Puffer_Ausgewaehlt(object sender, EventArgs e)
        {
            if (!(_cbPuffer.SelectedItem is Gruppenkopf)) return;

            for (int i = _cbPuffer.SelectedIndex + 1; i < _cbPuffer.Items.Count; i++)
                if (_cbPuffer.Items[i] is WaermesenkeClass.PufferInfo)
                {
                    _cbPuffer.SelectedIndex = i;
                    return;
                }

            ErstenEchtenWaehlen(_cbPuffer);
        }

        /// <summary>
        /// Stellt das Dropdown auf einen Puffer.
        ///
        /// <para>PAKET S2: Gesucht wird über die EINTRÄGE des Steuerelements, nicht über
        /// den Index in der Quellliste — zwischen den Puffern stehen seit S2
        /// Gruppenköpfe, und beide Zählungen laufen deshalb auseinander. Die Quellliste
        /// wird dafür gar nicht mehr gebraucht.</para>
        /// </summary>
        private static void PufferWaehlen(ComboBox cb, int idPuffer)
        {
            if (idPuffer <= 0) return;

            for (int i = 0; i < cb.Items.Count; i++)
            {
                WaermesenkeClass.PufferInfo p = cb.Items[i] as WaermesenkeClass.PufferInfo;
                if (p != null && p.ID == idPuffer) { cb.SelectedIndex = i; return; }
            }
        }

        private static int AktuelleId(ComboBox cb)
        {
            WaermesenkeClass.PufferInfo p = cb.SelectedItem as WaermesenkeClass.PufferInfo;
            return p != null ? p.ID : 0;
        }

        /// <summary>Füllt ein Prioritäts-Dropdown: „nach Vorgabe" plus die Werte 1…99.</summary>
        private void PrioListeFuellen(ComboBox cb, bool pvVariante)
        {
            cb.Items.Clear();

            int vorgabe = Ladeordnung.VorgabeLadeprio(ID_Type);
            cb.Items.Add(new PrioItem
            {
                Wert = 0,
                Text = pvVariante
                    ? MyResource.Resource.SIM_PRIO_UNVERAENDERT
                    : string.Format(MyResource.Resource.SIM_PRIO_VORGABE,
                                    vorgabe, Ladeordnung.ErzeugerName(ID_Type))
            });

            for (int p = Ladeordnung.PRIO_MIN; p <= Ladeordnung.PRIO_MAX; p++)
                cb.Items.Add(new PrioItem { Wert = p, Text = p.ToString() });

            cb.SelectedIndex = 0;
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

        // --- Ereignisse ---------------------------------------------------------------

        private void Auswahl_Geaendert(object sender, EventArgs e)
        {
            if (_aktualisiert) return;

            int index = AktuellerIndex();
            if (index < 0) return;

            // Der ZIELWECHSEL stellt zuerst die Auswahlliste des Speichers um; erst danach
            // steht fest, welchen Puffer die Zeile bekommt. Der Wächter verhindert, dass
            // das Neubefüllen selbst wieder als Bedienhandlung ankommt.
            if (sender == _cbZiel)
            {
                _aktualisiert = true;
                try { PufferListeFuerZiel(GewaehltesZiel()); }
                finally { _aktualisiert = false; }
            }

            ZeileAusOberflaeche(index);
            ZeileAnzeigen(index);

            // PAKET PARALLELVERBUND: Die Kandidatenliste hängt am Ziel von Rang 1
            // (Verwendungsfilter), am Leitspeicher und an den übrigen Senken - alle drei
            // stellen diese Bedienelemente ein. Sie neu aufzubauen ist billig (die
            // Puffer-Listen sind schon geladen) und hält Auswahl und Fachregel beisammen.
            if (sender == _cbZiel || sender == _cbPuffer) VerbundListeNeu();

            AnzeigeAktualisieren();
        }

        /// <summary>Blendet die Bereiche passend zur Auswahl ein und rechnet die Position neu.</summary>
        private void AnzeigeAktualisieren()
        {
            int index = AktuellerIndex();
            bool hatZeile = index >= 0;
            bool rang1 = index == 0;

            Z_AnlageSenkeModel z = hatZeile ? _zeilen[index] : null;
            bool pufferSenke = z != null && IstPufferZiel(z.Ziel);

            _gbZeile.Enabled = hatZeile;
            _cbPuffer.Enabled = pufferSenke;

            // Bedarfsart ist nur beim Heizkreis die Feinsteuerung (Konzept 3.1)
            _cbBedarfsart.Enabled = z != null && IstHeizkreis(z.Ziel);

            _gbLaden.Enabled = pufferSenke;
            _tbLadegrenze.Enabled = pufferSenke && _chkLadegrenze.Checked;

            // PAKET P2: Das Zahlenfeld der Einspeisehöhe folgt seinem Haken, genau wie
            // das der Ladeobergrenze darüber. Sichtbar bleibt die Zeile auch an einer
            // Direktsenke - die ganze Gruppe ist dort ohnehin gesperrt.
            _tbEinspeisehoehe.Enabled = pufferSenke && _chkEinspeisehoehe.Checked;

            // Die PV-Sonderregel greift nur bei Betriebsmodus PV (Konzept 3.5) und nur auf
            // Rang 1 (Konzept 5.1: eine Spalte WS_Ladeprio_PV2 gibt es nicht).
            bool pvModus = PvModus();
            _lblPV.Visible = pvModus;
            _cbLadeprioPV.Visible = pvModus;
            _cbLadeprioPV.Enabled = pvModus && rang1 && pufferSenke;

            // Nur eine PUFFER-Senke kann einen Verbund haben - bei einer Direktsenke gibt
            // es keinen Vorratsbehälter, dem etwas hinzuzufügen wäre. Und nur Rang 1,
            // solange Z_AnlagePufferVerbund keine Senkenreferenz trägt (siehe oben).
            _gbVerbund.Enabled = rang1 && pufferSenke;

            _btnEntfernen.Enabled = hatZeile && _zeilen.Count > 1;
            _btnRauf.Enabled = hatZeile && index > 0;
            _btnRunter.Enabled = hatZeile && index < _zeilen.Count - 1;

            _lblPosition.Text = PositionsText();
            VerbundSummeAnzeigen(GewaehlteVerbundMitglieder());
        }

        /// <summary>
        /// „Lädt als n. von m" für die aktuell gewählte Zeile (Konzept 3.4/4.2).
        ///
        /// PAKET PARALLELVERBUND: Bezugsgröße ist bei Rang 1 der LEITSPEICHER und damit der
        /// Verbund als Ganzes; die Ladeordnung kennt ohnehin nur diese eine ID (die
        /// Mitglieder stehen in keiner <c>WS_ID_Puffer</c>-Referenz). Die Ladereihenfolge
        /// eines Verbunds ist deshalb dieselbe Frage wie die eines Einzelspeichers.
        ///
        /// PAKET S1: Die Ladeordnung unterscheidet Haupt- und Zweitsenke als BOOLEAN
        /// (<c>Ladeordnung.LadeEintrag.Zweitsenke</c>). Für die Vorschau gilt deshalb
        /// „Rang 1 = Hauptsenke, alles darüber = Zweitsenke" — dieselbe Ableitung, die die
        /// Engine für <c>Ladeauftrag.Zweitsenke</c> benutzt.
        /// </summary>
        private string PositionsText()
        {
            Z_AnlageSenkeModel z = AktuelleZeile();
            if (z == null || !IstPufferZiel(z.Ziel) || z.ID_Puffer <= 0) return "";

            bool zweitsenke = AktuellerIndex() > 0;
            double grenze = z.Ladegrenze > 0 ? z.Ladegrenze : 0;

            List<Ladeordnung.LadeEintrag> vorschau = Ladeordnung.LadereihenfolgeVorschau(
                ID_Projekt, z.ID_Puffer, ID_Anlage, ID_Type, zweitsenke,
                z.Ladeprio, grenze, z.Ladeprio_PV);

            int pos = Ladeordnung.Position(vorschau, ID_Anlage, zweitsenke);
            if (pos <= 0) return "";

            // Formatangabe „0.#" der Obergrenze aus dem Bestand übernommen; der Katalog
            // führt den Platzhalter normalisiert als {0} (Lesehinweis des Katalogs).
            string text = string.Format(MyResource.Resource.SIM_POSITION_LAEDT_ALS, pos, vorschau.Count);
            if (vorschau.Count > 0 && pos <= vorschau.Count)
                text += Environment.NewLine + string.Format(MyResource.Resource.SIM_POSITION_BIS,
                                                            vorschau[pos - 1].Obergrenze.ToString("0.#"));
            return text;
        }

        /// <summary>Leitspeicher des Verbunds — der Puffer auf Rang 1.</summary>
        private int AktuellerHauptPuffer()
        {
            if (_zeilen.Count == 0) return 0;
            if (!IstPufferZiel(_zeilen[0].Ziel)) return 0;
            return _zeilen[0].ID_Puffer;
        }

        private void btnPufferAnlegen_Click(object sender, EventArgs e)
        {
            Z_AnlageSenkeModel z = AktuelleZeile();
            string ziel = z != null ? z.Ziel : DbWerte.WS_ZIEL_PUFFER_HEIZUNG;

            Form_PufferSp_Projekt frm = new Form_PufferSp_Projekt();
            frm.ID_Projekt = ID_Projekt;

            // Vorbelegung der Verwendung passend zur gerade gewählten Senke.
            // D5a: Die Puffer-VERWALTUNG kennt „Kombi" seit der Nacharbeit I-K2-4 als
            // reguläre dritte Option — die Vorbelegung kommt dort also an und wird beim
            // Übernehmen unverändert zurückgeschrieben. Für das S1-Ziel PufferProzess
            // gibt es keinen Altwert; dort bleibt es bei der Heizungs-Vorbelegung, den
            // Kanal stellt das Klassen-Set des Speichers ein (Konzept 6.1).
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                frm.Verwendung = WaermesenkeClass.VERWENDUNG_KOMBI;
            else if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                frm.Verwendung = WaermesenkeClass.VERWENDUNG_BRAUCHWASSER;
            else
                frm.Verwendung = WaermesenkeClass.VERWENDUNG_HEIZUNG;
            frm.SetControls();
            frm.ShowDialog(this);

            // Die Verwaltung schreibt sofort in die Datenbank (siehe Klassenkommentar
            // dort) - die Dropdowns werden deshalb UNABHÄNGIG vom DialogResult neu
            // aufgebaut, sonst bliebe ein über das Fensterkreuz verlassener Neuanlage-
            // Vorgang unsichtbar.
            int index = AktuellerIndex();
            _aktualisiert = true;
            try
            {
                PufferListenLaden();
                if (index >= 0)
                {
                    PufferListeFuerZiel(_zeilen[index].Ziel);
                    if (frm.ID_Puffer > 0)
                        PufferWaehlen(_cbPuffer, frm.ID_Puffer);
                }

                // PAKET PARALLELVERBUND: Ein gerade angelegter Puffer soll auch als
                // Verbundmitglied wählbar sein, ohne den Dialog neu zu öffnen.
                VerbundListeFuellen();
            }
            finally
            {
                _aktualisiert = false;
            }

            if (index >= 0)
            {
                ZeileAusOberflaeche(index);
                ZeileAnzeigen(index);
            }
            AnzeigeAktualisieren();
        }

        // --- Übernahme und Validierung (Konzept 4.6 / 5.1) ----------------------------

        private void btnOk_Click(object sender, EventArgs e)
        {
            string eingabefehler = ListePruefen();
            if (eingabefehler != null)
            {
                MessageBox.Show(eingabefehler, MyResource.Resource.SIM_SENKE_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Die BESTANDSPRÜFUNG deckt Projektzugehörigkeit des Speichers, Doppelbelegung,
            // Kurzschluss Quelle=Senke, Verbundkonflikte und die Kanalwarnung ab
            // (Konzept 4.6). PAKET A1: Sie läuft auf der SENKENLISTE und sieht damit alle
            // Ränge — bis dahin bekam sie die auf zwei Plätze gespiegelte Fassung.
            List<int> verbund = GewaehlteVerbundMitglieder();

            WaermesenkeClass.PruefErgebnis erg =
                WaermesenkeClass.Pruefen(ID_Projekt, ID_Anlage, _zeilen, verbund);
            if (!erg.Ok)
            {
                if (erg.AbsprungPufferVerwaltung)
                {
                    // Konzept 4.6: Meldung MIT Absprung "Pufferspeicher anlegen..."
                    DialogResult wahl = MessageBox.Show(
                        string.Format(Zeilenumbruch.Normalisieren(
                            MyResource.Resource.SIM_MSG_PUFFER_ANLEGEN_FRAGE), erg.Fehler),
                        MyResource.Resource.SIM_TITEL_SENKE_PUFFER_FEHLT,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    this.DialogResult = DialogResult.None;
                    if (wahl == DialogResult.Yes) btnPufferAnlegen_Click(sender, e);
                    return;
                }

                MessageBox.Show(erg.Fehler, MyResource.Resource.SIM_SENKE_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // ERST JETZT schreiben: die vollständige Liste nach Z_AnlageSenke und die
            // Verbundmitglieder nach Z_AnlagePufferVerbund - beides in ListeSpeichern.
            //
            // Der Erfolg geht als SpeichernOk an den Aufrufer. Bis A1 hing seine
            // Fehlermeldung am Rückgabewert von WaermesenkeClass.Schreiben; die
            // Schreibstelle ist umgezogen, die Meldung darf deshalb nicht verloren gehen.
            SpeichernOk = ListeSpeichern(verbund);

            // Warnung ohne Blockerwirkung (Kanal ohne Bedarf).
            //
            // PAKET A1: Der ÜBERGANGSHINWEIS zur Brauchwasser-Senke ist entfallen. Er
            // sagte, dass eine Brauchwasser-/Kombi-Senke ohne die zweikanalige Kaskade
            // gespeichert wird, aber nicht mitrechnet. Den einkanaligen Altpfad gibt es
            // nicht mehr; jede Senke rechnet mit.
            List<string> hinweise = new List<string>();
            if (!string.IsNullOrEmpty(erg.Warnung)) hinweise.Add(erg.Warnung);

            // PAKET S2 (Konzept 6.2): die WEICHEN Warnkriterien. Sie blockieren nicht —
            // gespeichert ist zu diesem Zeitpunkt schon —, sie begründen nur, warum die
            // eben gespeicherte Zuordnung als unplausibel gilt. Sie gehen in DIESELBE
            // MessageBox wie der Hinweis darüber; nur das Symbol wird zum Warnzeichen,
            // damit der Unterschied zu einer bloßen Auskunft sichtbar ist.
            List<string> warnungen = WeicheBefunde();
            if (warnungen.Count > 0)
                hinweise.Add(MyResource.Resource.SIMWARN_DIALOG_KOPF + Environment.NewLine +
                             "  • " + string.Join(Environment.NewLine + "  • ", warnungen.ToArray()));

            if (hinweise.Count > 0)
                MessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, hinweise),
                                MyResource.Resource.SIM_SENKE_TITEL, MessageBoxButtons.OK,
                                warnungen.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            VerbundMitglieder = verbund;
        }

        /// <summary>
        /// Die WEICHEN Befunde des Warnkriterienkatalogs über die gespeicherte
        /// Senkenliste, als fertige Anzeigezeilen; nie <c>null</c>.
        ///
        /// <para>Geprüft wird NACH dem Schreiben: Der Katalog liest den Quellbezug und
        /// die Speicherdaten aus der Datenbank, und die Senkenzeilen sollen zu dem
        /// Zustand passen, der jetzt dort steht. Die HARTEN Befunde stehen nicht darin —
        /// sie sind schon in <see cref="ListePruefen"/> abgefangen worden, lange bevor
        /// geschrieben wurde.</para>
        /// </summary>
        private List<string> WeicheBefunde()
        {
            List<string> texte = new List<string>();

            foreach (Warnbefund b in Warnkriterien.NurWeiche(
                         Warnkriterien.PruefeSenken(ID_Projekt, ID_Anlage, _zeilen)))
                if (b != null && !string.IsNullOrEmpty(b.Text))
                    texte.Add(Zeilenumbruch.Einzeilig(b.Text));

            return texte;
        }

        /// <summary>
        /// Prüft die GANZE Liste — die Fälle, die <c>WaermesenkeClass.Pruefen</c> mit
        /// seinen zwei Plätzen nicht sehen kann. Rückgabe <c>null</c> = in Ordnung.
        /// </summary>
        private string ListePruefen()
        {
            // Invariante Rang 1 (Konzept 5.1). Kann über die Knöpfe nicht entstehen,
            // steht aber hier, weil alles Folgende sie voraussetzt.
            if (_zeilen.Count == 0) return MyResource.Resource.SIM_MSG_SENKE_LETZTE_ZEILE;

            List<int> gesehen = new List<int>();

            for (int i = 0; i < _zeilen.Count; i++)
            {
                Z_AnlageSenkeModel z = _zeilen[i];
                string rolle = string.Format(MyResource.Resource.SIM_ROLLE_RANG, i + 1);

                if (z.Ladegrenze == GRENZE_UNLESBAR)
                    return string.Format(MyResource.Resource.SIM_MSG_LADEGRENZE_ZAHL, rolle);
                if (z.Ladegrenze == GRENZE_BEREICH)
                    return string.Format(MyResource.Resource.SIM_MSG_LADEGRENZE_BEREICH, rolle);

                // PAKET P2: dieselbe Prüfung für die Einspeisehöhe, an derselben Stelle
                // wie die der Ladeobergrenze. Eine Zeile, die inzwischen auf eine
                // Direktsenke steht, trägt hier ohnehin −1 (ZeileAusOberflaeche löscht
                // den Wert mit) und läuft durch.
                if (z.Anschlusshoehe == HOEHE_UNLESBAR)
                    return string.Format(MyResource.Resource.SIM_MSG_EINSPEISEHOEHE_ZAHL, rolle);
                if (z.Anschlusshoehe == HOEHE_BEREICH)
                    return string.Format(MyResource.Resource.SIM_MSG_EINSPEISEHOEHE_BEREICH, rolle);

                if (!IstPufferZiel(z.Ziel)) continue;

                if (z.ID_Puffer <= 0)
                    return string.Format(MyResource.Resource.SIM_MSG_SENKE_PUFFER_FEHLT,
                                         i + 1, ZielAnzeige(z.Ziel));

                // Ein Behälter kann nicht zweimal Ziel derselben Anlage sein - er hat EINEN
                // Füllstand, und zwei Ladeaufträge darauf verplanten denselben Raum doppelt.
                if (gesehen.Contains(z.ID_Puffer))
                    return string.Format(
                        MyResource.Resource.SIM_MSG_SENKE_DOPPELT,
                        WaermesenkeClass.PufferName(z.ID_Puffer));
                gesehen.Add(z.ID_Puffer);
            }

            // KURZSCHLUSS (derselbe Puffer als Quelle UND Ladeziel dieser Anlage):
            //
            // PAKET S2 — der Guard steht jetzt im WARNKRITERIENKATALOG und nicht mehr
            // hier. Er prüft dasselbe über alle Ränge, mit derselben Auflösung des
            // Quellpuffers (Fremdschlüssel, sonst Alt-Bezeichner) und mit demselben
            // Meldungstext; die Zeile darüber ist deshalb ersatzlos entfallen, statt
            // zwei Stellen mit derselben Regel zu pflegen. Der zweite Guard in
            // WaermesenkeClass.Pruefen (Ränge 1/2) und der Engine-Guard E-K2-1 bleiben
            // als tiefere Verteidigungslinien unangetastet.
            Warnbefund hart = Warnkriterien.ErsterHarter(
                Warnkriterien.PruefeSenken(ID_Projekt, ID_Anlage, _zeilen));

            return hart != null ? Zeilenumbruch.Normalisieren(hart.Text) : null;
        }

        /// <summary>
        /// Schreibt die Rangnummern fest und speichert die Senkenliste nach
        /// <c>Z_AnlageSenke</c> sowie die Verbundmitglieder nach
        /// <c>Z_AnlagePufferVerbund</c>.
        ///
        /// <para><b>PAKET A1 — hier endet der Speicherweg.</b> Bis dahin schrieb der
        /// Aufrufer zusätzlich die auf zwei Plätze gespiegelte Fassung über
        /// <c>WaermesenkeClass.Schreiben</c>, und die nahm die Verbundmitglieder mit. Beide
        /// Wege sind zusammengelegt: Leitspeicher und Mitglieder gehören zusammen, und es
        /// gibt jetzt genau EINE Stelle, die die Senken einer Anlage schreibt.</para>
        ///
        /// Die Mitgliederliste geht IMMER heraus, auch leer — das ist der Weg, auf dem ein
        /// Verbund im Dialog wieder aufgelöst wird (Delete/Insert in
        /// <c>AnlagePufferVerbundCtrl.Schreiben</c>).
        /// </summary>
        /// <returns>true, wenn beide Schreibvorgänge durchkamen.</returns>
        private bool ListeSpeichern(List<int> verbundMitglieder)
        {
            for (int i = 0; i < _zeilen.Count; i++)
            {
                _zeilen[i].ID_Anlage = ID_Anlage;
                _zeilen[i].Rang = i + 1;
            }

            if (ID_Anlage <= 0) return false;

            bool ok = true;
            if (Z_AnlageSenkeCtrl.SpalteVorhanden())
                ok &= new Z_AnlageSenkeCtrl().SchreibenJeAnlage(ID_Anlage, _zeilen);

            ok &= AnlagePufferVerbundCtrl.Schreiben(ID_Anlage, verbundMitglieder);
            return ok;
        }

        // PAKET A1: SpiegelBauen, AltZiel, AltBedarfsart und BrauchwasserUebergangsHinweis
        // sind ENTFALLEN.
        //
        //   - SpiegelBauen/AltZiel/AltBedarfsart bauten aus den Rängen 1 und 2 die
        //     Altspalten-Fassung WS_*/WS_*2 (S1-O5). Sie mussten dabei abbilden, was die
        //     Altspalten nicht ausdrücken können: die beiden Prozess-Ziele wurden auf ihre
        //     Heizungs-Entsprechung abgebildet, eine Direktsenke ab Rang 2 fiel weg, Ränge
        //     ab 3 wurden gar nicht gespiegelt. Da niemand mehr aus den Altspalten rechnet,
        //     wäre das nur noch eine zweite, ärmere Wahrheit.
        //   - BrauchwasserUebergangsHinweis meldete, dass eine Brauchwasser-/Kombi-Senke
        //     ohne die zweikanalige Kaskade zwar gespeichert wird, aber nicht mitrechnet.
        //     Der einkanalige Rechenweg ist mit Schritt 51 abgerissen; die Aussage hat
        //     keinen Gegenstand mehr.
    }
}
