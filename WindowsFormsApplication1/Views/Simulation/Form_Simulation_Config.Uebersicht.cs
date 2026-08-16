using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Erzeuger-Übersicht der Simulationskonfiguration (Konzept 4.1) — die EDITOREN, die
    /// aus der Anzeige heraus geöffnet werden, und die Abfragen, die sie füttern.
    ///
    /// Aus <c>Form_Simulation_Config.cs</c> herausgelöst (Paket 2): die Hauptdatei hatte
    /// über 2000 Zeilen und mischte Auswahl, Alt-Zuordnung und Übersicht.
    ///
    /// <b>ETAPPE D2/D3.</b> Die ANZEIGE ist aus dieser Datei ausgezogen: Die
    /// neunspaltige <c>listView_Uebersicht</c>, ihre Breitenarithmetik und die Fußzeile
    /// mit der Pufferaufzählung sind durch die Kartenspalten in
    /// <c>Form_Simulation_Config.Karten.cs</c> ersetzt. Hier bleibt, was die Karten
    /// AUFRUFEN: Senken-, Quellen-, Modus- und Prioritätsdialog samt der Abfrage
    /// <see cref="AnlagenImProjekt"/> und den Anzeigetexten. Konzept Abschnitt 3:
    /// „Doppelklick/✎ öffnet überall die bestehenden Dialoge — unveränderte Editoren."
    ///
    /// Was NICHT hier steht: die Alt-Zuordnung <c>listView1</c>/<c>_zuordnungen</c> und
    /// ihr Speicherpfad. Sie bleibt in der Hauptdatei, weil sie mit Etappe B (Konzept 4.4)
    /// im Ganzen entfällt.
    /// </summary>
    public partial class Form_Simulation_Config : BaseForm
    {
        // --- Zuordnungs-Rubrik (Konzept 4.4) ------------------------------------------
        //
        // Der Rückwegschalter RUBRIK_SICHTBAR ist mit ETAPPE D1 entfallen. Er hielt seit
        // Paket 2 / Etappe A die Möglichkeit offen, die alte Bedienung wieder
        // einzuschalten; die Rubrik selbst wird jetzt gar nicht mehr angelegt
        // (Form_Simulation_Config.Karten.AltSteuerelementeStilllegen), damit hätte der
        // Schalter nichts mehr zu schalten. Der Rückweg ist ab hier die
        // Versionsverwaltung.
        //
        // UNVERÄNDERT bleibt der Datenpfad: _zuordnungen wird weiter aus
        // Z_ProjektPufferSp geladen und beim Speichern zurückgeschrieben, und die
        // Spiegel-Brücke WaermesenkeClass.WpSenkeSpiegeln arbeitet weiter
        // (Konzeptvorgabe: bis zur Abnahme unangetastet).

        // --- Steuerelemente -----------------------------------------------------------

        // Fußzeile, rechts: Feature-Flag der zweikanaligen Kaskade (Konzept Kapitel 9)
        private CheckBox checkBox_KaskadeZweikanalig;
        private bool _kaskadeUiUpdate = false;   // verhindert Schreiben beim Vorbelegen

        // Bewusste Abwahl der zweikanaligen Kaskade TROTZ erfüllter Notwendigkeitsregel.
        // Sie gilt für die Dauer dieses Dialogs und legt die Automatik still - ohne sie
        // hätte die Abwahl keine Wirkung: Der nächste OK-Knopf im Senkendialog (oder das
        // nächste Speichern) würde den Haken sofort wieder setzen.
        private bool _kaskadeAutomatikZurueckgestellt = false;

        // Fußzeile, rechts: Einstellung Extrapolation_erlaubt (Paket 8, Konzept 13.4)
        private CheckBox checkBox_Extrapolation;
        private bool _extrapolationUiUpdate = false;

        // Inline-Editor für die Wärmequelle in der Übersicht
        private ComboBox _wqCombo;
        private AnlagenInfo _wqInfo;
        private bool _wqUpdating = false;

        /// <summary>
        /// Die STEUERWERTE, die im Inline-Editor gerade angeboten werden (Etappe D5b).
        /// Seit der Freischaltung je <c>ID_Type</c> ist das nicht mehr immer
        /// <see cref="WaermequelleClass.TypWerte"/>: Der Heizkessel bekommt eine eigene,
        /// zweielementige Liste. Das Auswahlereignis liefert nur einen Index — ohne diese
        /// Merkstelle zeigte er auf die falsche Liste.
        /// </summary>
        private string[] _wqTypen = new string[0];

        // Außentemperatur der Klimaregion (8760 Stundenwerte) für die Vorschau des
        // Erdreichdialogs. Wird beim ersten Öffnen einmal geladen und gecacht
        // (Konzept 4.5) - nicht bei jeder Parameteränderung.
        private float[] _aussentempCache = null;
        private bool _aussentempGeladen = false;

        // Mouseover-Hinweise der Fußzeilenschalter (die Karten bringen ihre eigenen mit)
        private ToolTip _uebersichtTip = new ToolTip();

        /// <summary>Eine im Projekt angelegte Anlage (eine Erzeugerkarte).</summary>
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

            // D2: Auslegungstemperaturen der ANLAGE (Tab_Energieanlagen.Vorlauf /
            // [Rücklauf] - die Spalte trägt dort den Umlaut, siehe
            // ProjektPuffer.SQL_SYSTEM_RUECKLAUF). Sie tragen den Temperaturchip, wenn
            // der Erzeuger keinen Puffer lädt, und die Warnregel aus Konzept 5, wenn er
            // einen lädt. 0 = nicht gepflegt (Access-Spaltenvorgabe, nie NULL).
            public int Vorlauf;
            public int Ruecklauf;

            /// <summary>Haupt- und Zweitsenke (Konzept 5.3), aus derselben Abfrage gelesen.</summary>
            public WaermesenkeClass.SenkeDaten Senke = new WaermesenkeClass.SenkeDaten();

            public bool IstWaermepumpe
            {
                get { return ID_Type == ProjektPuffer.TYP_WP; }
            }
        }

        // --- Fußzeilenschalter --------------------------------------------------------

        /// <summary>
        /// Schalter „Zweikanalige Kaskade" in der Fußzeile
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

            // D2/D3: PLATZIERT wird der Schalter nicht mehr hier, sondern zusammen mit
            // der übrigen Fußzeile in FusszeilePlatzieren. Vorher rechnete er seine
            // Position aus groupBox_Uebersicht und btn_PufferVerwalten — beide gibt es in
            // dieser Form nicht mehr (die Übersicht ist eine Kartenspalte, der
            // Verwalten-Knopf steht in der Speicherspalte).

            // Zeilenumbrüche der Ressource auf die Plattformform bringen. Der
            // Ressourcenleser liefert sie zur Laufzeit bereits als CRLF (nachgemessen
            // an den kompilierten .resources beider Sprachen) — das frühere
            // Replace("\n", Environment.NewLine) machte daraus CR+CRLF, also eine
            // Leerzeile je Umbruch. Details in Zeilenumbruch.
            _uebersichtTip.SetToolTip(checkBox_KaskadeZweikanalig,
                Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_KASKADE_TOOLTIP));

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

            // ABWAHL-GUARD: Der Haken geht heraus, obwohl die Konfiguration Warmwasser
            // und Heizwärme getrennt führt. Das ist erlaubt - aber nicht stillschweigend,
            // denn danach fallen Brauchwasser-/Kombi-Senken und Quellbezüge aus der
            // Rechnung, ohne dass irgendwo etwas fehlt.
            if (!wert && KonfigurationCtrl.KaskadeNotwendig(m_ID_Projekt))
            {
                // Ohne Replace auf Environment.NewLine: Der Ressourcenleser liefert die
                // Umbrüche dieser .resx bereits als CRLF (gemessen). Die anderswo übliche
                // Umsetzung machte daraus CR+CRLF und damit eine Leerzeile zu viel.
                DialogResult wahl = MessageBox.Show(
                    MyResource.Resource.SIM_MSG_KASKADE_ABWAHL,
                    MyResource.Resource.SIM_TITEL_KASKADE,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (wahl != DialogResult.Yes)
                {
                    _kaskadeUiUpdate = true;
                    try { checkBox_KaskadeZweikanalig.Checked = true; }
                    finally { _kaskadeUiUpdate = false; }
                    return;
                }

                // Bewusste Entscheidung - sie wird respektiert (siehe Feldkommentar).
                _kaskadeAutomatikZurueckgestellt = true;
            }

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

        // --- Automatik der zweikanaligen Kaskade ---------------------------------------
        //
        // GRUNDSATZ: Geschrieben wird ausschließlich bei einer DIALOG-AKTION des Anwenders
        // (OK im Senken- oder Quellendialog, „Konfiguration speichern"), nie still zur
        // Laufzeit und nie beim bloßen Öffnen eines Fensters. Der Lesepfad der Engine
        // bleibt unangetastet: Sie liest weiter nur Tab_Einstellungen.Kaskade_Zweikanalig
        // und kennt die Regel nicht. Ein Referenzlauf mit ausgeschaltetem Flag rechnet
        // deshalb unverändert.
        //
        // ZWEI AUSPRÄGUNGEN, mit Absicht verschieden:
        //   * Nach einer SENKEN- oder QUELLEN-Änderung hat der Anwender die Notwendigkeit
        //     gerade selbst hergestellt. Hier wird eingeschaltet und gemeldet - eine
        //     Rückfrage wäre eine Frage nach etwas, das er soeben entschieden hat.
        //   * Beim SPEICHERN der Konfiguration kann derselbe Stand beliebig oft
        //     vorbeikommen (Altbestand, SQL-Pflege, wiederholtes Speichern). Dort wird
        //     GEFRAGT statt gesetzt. Das ist die robustere der beiden im Auftrag
        //     genannten Varianten: Sie braucht keinen gemerkten Vergleichsstand in der
        //     Datenbank - und ein gemerkter Stand wäre genau die Stelle, an der eine
        //     bewusste Abwahl später doch wieder verloren ginge.
        //
        // Eine bewusste Abwahl (_kaskadeAutomatikZurueckgestellt) legt BEIDE Ausprägungen
        // still, solange dieser Dialog offen ist. Ohne das wäre der Abwahl-Guard eine
        // Frage ohne Folgen.

        /// <summary>
        /// Schaltet die zweikanalige Kaskade nach einer Senken- oder Quellenänderung ein,
        /// wenn die Konfiguration sie jetzt braucht — mit einmaliger Meldung.
        ///
        /// Aufzurufen NACH dem Schreiben der Senke bzw. des Quellbezugs: Die Regel liest
        /// den gespeicherten Stand.
        /// </summary>
        private void KaskadeAutomatikNachAenderung()
        {
            if (m_ID_Projekt <= 0 || _kaskadeAutomatikZurueckgestellt) return;
            if (KonfigurationCtrl.KaskadeZweikanaligLesen(m_ID_Projekt)) return;
            if (!KonfigurationCtrl.KaskadeNotwendig(m_ID_Projekt)) return;

            if (!KonfigurationCtrl.KaskadeZweikanaligSchreiben(m_ID_Projekt, true))
            {
                // Kein Einstellungssatz oder Spalte fehlt (Datenbank nicht auf
                // Schemastand 6) - dieselbe Behandlung wie beim Schalter: melden und
                // nichts behaupten.
                ShowStatus(MyResource.Resource.SIM_STATUS_EINSTELLUNG_FEHLER, Color.DarkRed);
                return;
            }

            AktualisiereKaskadeSchalter();

            // Umbrüche unverändert (Begründung im Abwahl-Guard).
            MessageBox.Show(
                MyResource.Resource.SIM_MSG_KASKADE_AUTOMATISCH,
                MyResource.Resource.SIM_TITEL_KASKADE,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Fragt beim Speichern der Konfiguration nach, wenn die Regel erfüllt ist und der
        /// Haken fehlt (Altbestand, SQL-Pflege). „Nein" wird wie eine bewusste Abwahl
        /// behandelt und in diesem Dialog nicht noch einmal gefragt.
        /// </summary>
        private void KaskadeAutomatikBeimSpeichern()
        {
            if (m_ID_Projekt <= 0 || _kaskadeAutomatikZurueckgestellt) return;
            if (KonfigurationCtrl.KaskadeZweikanaligLesen(m_ID_Projekt)) return;
            if (!KonfigurationCtrl.KaskadeNotwendig(m_ID_Projekt)) return;

            DialogResult wahl = MessageBox.Show(
                MyResource.Resource.SIM_MSG_KASKADE_FRAGE,
                MyResource.Resource.SIM_TITEL_KASKADE,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (wahl != DialogResult.Yes)
            {
                _kaskadeAutomatikZurueckgestellt = true;
                return;
            }

            if (!KonfigurationCtrl.KaskadeZweikanaligSchreiben(m_ID_Projekt, true))
            {
                ShowStatus(MyResource.Resource.SIM_STATUS_EINSTELLUNG_FEHLER, Color.DarkRed);
                return;
            }

            AktualisiereKaskadeSchalter();
            ShowStatus(MyResource.Resource.SIM_STATUS_KASKADE_EIN, Color.DarkGreen);
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

            _uebersichtTip.SetToolTip(checkBox_Extrapolation,
                Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_EXTRAPOLATION_TOOLTIP));

            checkBox_Extrapolation.CheckedChanged += checkBox_Extrapolation_CheckedChanged;
            this.Controls.Add(checkBox_Extrapolation);
            checkBox_Extrapolation.BringToFront();

            // D2/D3: ExtrapolationSchalterPlatzieren ist ENTFALLEN. Die Methode setzte
            // den Schalter eine Zeile unter den Kaskadenschalter und vergrößerte das
            // Formular anschließend um die Pixel, die zur Knopfzeile fehlten (Befund
            // N13a) — die letzte der vier Selbstkorrekturen des alten Layouts. Beide
            // Schalter stehen jetzt nebeneinander in einer Fußzeile mit fester Höhe
            // (FusszeilePlatzieren); eine Kollision mit der Knopfzeile kann dort nicht
            // mehr entstehen, weil Schalter und Knöpfe getrennte Zeilen haben.
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

        // D3: AktualisierePufferFusszeile ist ENTFALLEN. Die einzeilige Aufzählung
        // „Pufferspeicher im Projekt: Name (Heizung, 800 l) · …" (label_PufferListe) hat
        // die Speicherspalte abgelöst — dieselbe Auskunft steht jetzt je Speicher auf
        // einer Karte, zusammen mit Ladern, Versorgung, Quellnutzung, Schwellen und
        // Temperaturherkunft (Konzept 3a). Die Aufrufstellen rufen stattdessen
        // AktualisiereSpeicherKarten.

        private void btn_PufferVerwalten_Click(object sender, EventArgs e)
        {
            PufferVerwaltungOeffnen(0);
        }

        /// <summary>
        /// Öffnet die Puffer-Verwaltung <see cref="Form_PufferSp_Projekt"/> und frischt
        /// danach die Anzeige auf.
        ///
        /// <paramref name="idPuffer"/> ist der Speicher, den der Dialog vorwählen soll —
        /// das ✎ einer Speicherkarte gibt ihn mit (D3), der Einstieg über die
        /// Spaltenfußzeile lässt ihn auf 0. Aus <c>btn_PufferVerwalten_Click</c>
        /// herausgelöst, damit beide Wege denselben Nachlauf haben; der Nachlauf ist
        /// Wort für Wort der bisherige.
        /// </summary>
        private void PufferVerwaltungOeffnen(int idPuffer)
        {
            if (m_ID_Projekt <= 0) return;

            Form_PufferSp_Projekt frm = new Form_PufferSp_Projekt();
            frm.ID_Projekt = m_ID_Projekt;
            frm.ID_Puffer = idPuffer;
            // Einstieg über die Spalte: KEINE Verwendungsvorgabe - hier will der
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
            AktualisiereSpeicherKarten();
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
            //
            // D2: dazu Vorlauf und Rücklauf der ANLAGE für den Temperaturchip und die
            // Warnregel aus Konzept Abschnitt 5. ACHTUNG: Die Rücklaufspalte heißt in
            // Tab_Energieanlagen MIT Umlaut (an der Datenbank verifiziert, Befund B0-4,
            // siehe ProjektPuffer.SQL_SYSTEM_RUECKLAUF) - anders als in
            // Tab_Pufferspeicher. Alias auf den umlautfreien Namen, damit der Lesecode
            // unten nicht von der Schreibweise abhängt.
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT a.ID, a.Bezeichner, a.Prioritaet, a.WQ_Typ, a.WQ_Temp, a.WS_Typ, a.BM_Typ, " +
                "       a.WS_Ziel, a.WS_ID_Puffer, a.WS_Ladeprio, a.WS_Ladegrenze, a.WS_Ladeprio_PV, " +
                "       a.WS_Ziel2, a.WS_ID_Puffer2, a.WS_Ladeprio2, a.WS_Ladegrenze2, " +
                "       a.Vorlauf, a.[Rücklauf] AS Ruecklauf, " +
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
                if (r["Vorlauf"] != DBNull.Value) info.Vorlauf = Convert.ToInt32(r["Vorlauf"]);
                if (r["Ruecklauf"] != DBNull.Value) info.Ruecklauf = Convert.ToInt32(r["Ruecklauf"]);
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

        /// <summary>
        /// Kompakte Anzeige der Wärmequelle einer Wärmepumpe.
        ///
        /// <b>ETAPPE D4:</b> Der Text entsteht in
        /// <see cref="WaermequelleClass.QuelleAnzeige"/>. Er stand bis D5b hier als
        /// private Methode; die Schema-Ansicht braucht ihn ein zweites Mal, und zwei
        /// Fassungen desselben Textes wären zwei Wahrheiten über die Quelle. Verschoben,
        /// nicht geändert — diese Methode reicht nur die Felder durch.
        /// </summary>
        private string WaermequelleAnzeige(AnlagenInfo a)
        {
            return WaermequelleClass.QuelleAnzeige(m_ID_Projekt, a.ID, a.WpTyp, a.WQ_Typ, a.WQ_Temp);
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

        // --- Editoren (aus den Karten aufgerufen) --------------------------------------

        // D2: listView_Uebersicht_MouseMove und listView_Uebersicht_MouseDoubleClick sind
        // ENTFALLEN. Beide arbeiteten über SPALTENINDIZES: der eine wählte den
        // Mouseover-Hinweis, der andere den zu öffnenden Dialog (Whitelist
        // SPALTEN_MIT_DIALOG). Eine Karte hat keine Spalten - Hinweis und Editorziel
        // hängen jetzt am einzelnen Chip (ErzeugerKarte.ChipDaten.Hinweis bzw. .Ziel),
        // verteilt in Form_Simulation_Config.Karten.ErzeugerChips und aufgelöst in
        // ChipEditorOeffnen. Die Zuordnung Chip -> Dialog ist dieselbe wie vorher
        // Spalte -> Dialog; die Dialoge selbst sind unverändert.
        //
        // Die WP-Sonderfälle der alten Hinweistexte (SIM_TIP_WPPRIO_NICHT_WP,
        // SIMQ_TIP_QUELLE_NICHT_WP, SIM_TIP_BETRIEBSMODUS_NICHT_WP) werden nicht mehr
        // gebraucht: Die betroffenen Chips entstehen bei Nicht-Wärmepumpen gar nicht
        // erst, statt sie anzuzeigen und den Klick darauf mit einer Meldung
        // abzuweisen. Die Meldungen in WpPrioritaetBearbeiten, WaermequelleBearbeiten
        // und BetriebsmodusBearbeiten bleiben trotzdem stehen - sie sind der Schutz der
        // Methode, nicht der Anzeige.

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

        /// <summary>
        /// Wärmequelle: Wärmepumpe (dort nur Sole-/Wasser-Wasser) und seit ETAPPE D5b
        /// auch der HEIZKESSEL — für ihn allerdings nur mit den zwei Möglichkeiten
        /// „Systemrücklauf" und „Pufferspeicher" (Kaskade, Konzept Anforderung 6).
        ///
        /// Die Freischaltung selbst steht in
        /// <see cref="WaermequelleClass.QuellenwahlMoeglich"/> und
        /// <see cref="WaermequelleClass.TypWerteFuer"/>; diese Methode ist nur noch der
        /// Türsteher davor. Die Luft-Wasser-Sperre bleibt WP-spezifisch: Sie sagt etwas
        /// über die Bauart der Wärmepumpe aus, nicht über die Quellenwahl allgemein.
        /// </summary>
        private void WaermequelleBearbeiten(AnlagenInfo info, Rectangle zelle)
        {
            if (!WaermequelleClass.QuellenwahlMoeglich(info.ID_Type))
            {
                MessageBox.Show(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_MSG_QUELLE_ART),
                    MyResource.Resource.SIMQ_TITEL_WAERMEQUELLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (info.IstWaermepumpe &&
                (string.IsNullOrEmpty(info.WpTyp) || info.WpTyp == DbWerte.WP_BAUART_LUFT_WASSER))
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
            // Der Dialog unterdrückt seinen Übergangshinweis nur, solange die Automatik
            // die Kaskade wirklich einschalten wird - nach einer bewussten Abwahl bleibt
            // der Hinweis richtig und muss stehen bleiben.
            frm.KaskadeAutomatikAktiv = !_kaskadeAutomatikZurueckgestellt;
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

                // Automatik NACH dem Schreiben: Erst jetzt steht die Senke, an der die
                // Notwendigkeitsregel hängt.
                KaskadeAutomatikNachAenderung();
            }

            // Auch nach Abbruch neu aufbauen: der Dialog kann über
            // "Pufferspeicher anlegen..." einen neuen Projekt-Puffer erzeugt haben.
            ZuordnungenLaden();
            RefreshZuordnungAnzeige();
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
        /// Zeigt das Wärmequellen-Dropdown unmittelbar an der Karte an - wie es vorher in
        /// der Zelle der Übersicht aufklappte.
        ///
        /// <b>ETAPPE D5b:</b> Die Einträge kommen aus
        /// <see cref="WaermequelleClass.TypWerteFuer"/>/<c>TypAnzeigeFuer</c> und hängen
        /// damit an der Erzeugerart: Die Wärmepumpe bekommt die sechs bekannten Typen,
        /// der Heizkessel genau zwei („Systemrücklauf", „Pufferspeicher"). Die gezeigte
        /// Werteliste wird in <see cref="_wqTypen"/> festgehalten — das Ereignis liefert
        /// nur einen INDEX, und der zeigt seit dieser Etappe je nach Art auf eine andere
        /// Liste.
        ///
        /// <b>D2:</b> <paramref name="zellBounds"/> kommt jetzt bereits in
        /// FORMULARKOORDINATEN (die aufrufende Karte rechnet sie in
        /// <c>KarteAlsZelle</c> um). Vorher lag hier eine Umrechnung
        /// <c>listView_Uebersicht.PointToScreen</c> → <c>PointToClient</c>; die Liste
        /// gibt es nicht mehr, und die Karten liegen im scrollenden
        /// <c>FlowLayoutPanel</c> — die Umrechnung muss deshalb dort stattfinden, wo das
        /// Steuerelement bekannt ist.
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
            _wqTypen = WaermequelleClass.TypWerteFuer(info.ID_Type);
            if (_wqTypen.Length == 0) return;

            _wqUpdating = true;
            _wqCombo.Items.Clear();
            _wqCombo.Items.AddRange(WaermequelleClass.TypAnzeigeFuer(info.ID_Type));

            // Vorauswahl: der gespeicherte Typ. Beim Heizkessel ist die leere Angabe ein
            // REGULÄRER Eintrag („Systemrücklauf"), bei der Wärmepumpe steht sie wie
            // bisher für Außenluft.
            string aktuellerTyp = info.WQ_Typ ?? "";
            if (info.IstWaermepumpe && aktuellerTyp.Length == 0)
                aktuellerTyp = WaermequelleClass.TYP_AUSSENLUFT;

            int aktuell = Array.IndexOf(_wqTypen, aktuellerTyp);
            _wqCombo.SelectedIndex = aktuell >= 0 ? aktuell : 0;
            _wqUpdating = false;

            _wqCombo.Bounds = new Rectangle(zellBounds.Location,
                                            new Size(Math.Max(zellBounds.Width, 190), zellBounds.Height));
            _wqCombo.Visible = true;
            _wqCombo.BringToFront();
            _wqCombo.Focus();
            _wqCombo.DroppedDown = true;
        }

        private void WqCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_wqUpdating || _wqInfo == null || _wqCombo.SelectedIndex < 0) return;
            if (_wqCombo.SelectedIndex >= _wqTypen.Length) return;

            string typNeu = _wqTypen[_wqCombo.SelectedIndex];
            AnlagenInfo info = _wqInfo;
            _wqCombo.Visible = false;

            switch (typNeu)
            {
                case WaermequelleClass.TYP_OHNE:
                    // ETAPPE D5b, Heizkessel: „Systemrücklauf" - die Kaskade wird
                    // ABGEBAUT. Mit dem Typ geht auch der Fremdschlüssel weg: Ein
                    // stehengebliebener WQ_ID_Puffer wäre genau der Altdatenrest aus
                    // Befund E-K2-4, der den Kessel ohne Wirkung in die Stundenschleife
                    // zieht. NULL statt 0 wegen der erzwungenen Beziehung aus Schritt 4
                    // der SchemaMigration (dieselbe Regel wie in WaermesenkeClass).
                    WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                    WaermequelleClass.WertSchreiben(info.ID, "WQ_ID_Puffer",
                        System.Data.OleDb.OleDbType.Integer, DBNull.Value);
                    break;

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
                        // D5b: Der Dialog bedient jetzt zwei Erzeugerarten - beim Kessel
                        // beschreibt er die KASKADE statt der Verdampferwärme und blendet
                        // die Verdampfer-Parameter aus.
                        frmQuelle.ID_Type = info.ID_Type;
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

                        // ETAPPE D5b — die beiden Dialogprüfungen aus Konzept Abschnitt 7,
                        // BEVOR irgendetwas geschrieben wird (Konzept 4.6 Kurzschluss,
                        // Kaskadenzyklus). Die Engine-Guards bleiben als zweite
                        // Verteidigungslinie; hier soll die Konfiguration gar nicht erst
                        // entstehen, statt später mit Warnung wirkungslos zu bleiben (E-K2-1)
                        // oder den ganzen Lauf abzubrechen (Zyklus-Guard).
                        WaermesenkeClass.QuellPruefErgebnis pruef =
                            WaermesenkeClass.QuellePruefen(m_ID_Projekt, info.ID, frmQuelle.ID_Puffer);
                        if (!pruef.Ok)
                        {
                            MessageBox.Show(pruef.Fehler,
                                MyResource.Resource.SIMQ_TITEL_WAERMEQUELLE,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;   // nichts geschrieben - der Bestand bleibt gültig
                        }

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

                        // D5b: Die vier Parameter darunter beschreiben die VERDAMPFERseite
                        // (Quelltemperatur, nutzbare Spreizung, Regeneration, unbegrenzte
                        // Quelle) und werden ausschließlich von SimulationWaermepumpe bzw.
                        // WaermequelleClass.Quellspeicher gelesen. Der Kessel bezieht seinen
                        // Temperaturhub aus dem VORLAUF des Quellpuffers
                        // (SimulationControl.KesselTemperaturpaar) - für ihn hat der Dialog
                        // die Felder gar nicht gezeigt, und dann darf er sie auch nicht
                        // schreiben: Sonst überschriebe eine Kesselbearbeitung die Vorgaben
                        // mit den Vorbelegungen 10 °C / 5 K.
                        if (info.IstWaermepumpe)
                        {
                            WaermequelleClass.WertSchreiben(info.ID, "WQ_Temp", frmQuelle.Quelltemperatur);
                            WaermequelleClass.WertSchreiben(info.ID, "WQ_Spreizung", frmQuelle.Spreizung);
                            WaermequelleClass.WertSchreiben(info.ID, "WQ_Regeneration", frmQuelle.Regeneration);
                            WaermequelleClass.WertSchreiben(info.ID, "WQ_Unbegrenzt", frmQuelle.Unbegrenzt);
                        }

                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);

                        // Ein AUFGELÖSTER Quellbezug (Fremdschlüssel, nicht der
                        // Alt-Bezeichner) entsteht nur im zweikanaligen Weg - deshalb hier
                        // dieselbe Automatik wie nach einer Senkenänderung. Ohne gesetzte
                        // ID gibt es keinen Quellbezug und nichts zu entscheiden.
                        if (frmQuelle.ID_Puffer > 0) KaskadeAutomatikNachAenderung();
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
                                // Umbrüche VOR dem Einsetzen normalisieren (Zeilenumbruch).
                                frmErde.HinweisErgebnis = string.Format(
                                    Zeilenumbruch.Normalisieren(
                                        MyResource.Resource.SIMQ_ERDREICH_WIRKUNGSLOS), erdErg.Grenze);
                            else if (!erdErg.MaxEntzugBelastbar)
                                frmErde.HinweisErgebnis = string.Format(
                                    Zeilenumbruch.Normalisieren(
                                        MyResource.Resource.SIMQ_ERDREICH_KEINE_PRUEFUNG), erdErg.Grenze);
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
