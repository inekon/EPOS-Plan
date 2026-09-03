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
    /// Was NICHT hier steht: die Alt-Zuordnung <c>Z_ProjektPufferSp</c>. Sie ist mit
    /// PAKET A1 (Migrationsschritt 51) im Ganzen abgerissen — samt Spiegel-Brücke,
    /// Ladepfad und Speicherpfad.
    /// </summary>
    public partial class Form_Simulation_Config : BaseForm
    {
        // --- Zuordnungs-Rubrik (Konzept 4.4) — ABGERISSEN MIT PAKET A1 ----------------
        //
        // Die Rubrik selbst war seit Etappe D1 unsichtbar; der DATENPFAD lief weiter:
        // _zuordnungen wurde aus Z_ProjektPufferSp geladen, beim Speichern in einem
        // Delete/Insert-Zyklus zurückgeschrieben, und die Spiegel-Brücke
        // WaermesenkeClass.WpSenkeSpiegeln hielt beide Modelle im Gleichstand. Mit
        // Schritt 51 ist Z_ProjektPufferSp stillgelegt: Die Betriebstemperaturen sind
        // einmalig per DML an Tab_Pufferspeicher übernommen (dort pflegt sie
        // Form_PufferSp_Projekt weiter), die Senken stehen in Z_AnlageSenke. Damit ist
        // alles entfallen - Feld, Laden, Schreiben, Brücke und die Nachführung der
        // Betriebstemperaturen über PufferSpCtrl.SetTemperaturen.

        // --- Steuerelemente -----------------------------------------------------------

        // Fußzeile: Einstellung Extrapolation_erlaubt (Paket 8, Konzept 13.4)
        //
        // PAKET A1: Der zweite Schalter „Zweikanalige Kaskade" ist entfallen. Er war das
        // Feature-Flag des einkanaligen Altpfads; Schritt 51 setzt
        // Tab_Einstellungen.Kaskade_Zweikanalig in Bestandsdaten auf WAHR und nimmt es aus
        // der Weiche - ein Schalter ohne Weiche wäre eine Zusage ohne Wirkung. Mit ihm
        // sind seine Automatiken gegangen (Einschalten nach einer Senken-/Quellenänderung,
        // Rückfrage beim Speichern): Sie hielten den Haken mit dem Rechenweg im
        // Gleichstand, und es gibt nur noch einen Rechenweg.
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
            public string BM_Typ = "";  // Betriebsmodus (WaermequelleClass.MODUS_*)

            // D2: Auslegungstemperaturen der ANLAGE (Tab_Energieanlagen.Vorlauf /
            // [Rücklauf] - die Spalte trägt dort den Umlaut, siehe
            // ProjektPuffer.SQL_SYSTEM_RUECKLAUF). Sie tragen den Temperaturchip, wenn
            // der Erzeuger keinen Puffer lädt, und die Warnregel aus Konzept 5, wenn er
            // einen lädt. 0 = nicht gepflegt (Access-Spaltenvorgabe, nie NULL).
            public int Vorlauf;
            public int Ruecklauf;

            /// <summary>
            /// Die SENKENLISTE der Anlage in Rangfolge (Konzept 5.1/5.3), aus EINER
            /// Projektabfrage auf <c>Z_AnlageSenke</c> zugeteilt. Nie <c>null</c>, nie
            /// leer — ohne eigene Zeile steht hier die Rang-1-Vorbelegung
            /// <c>Heizkreis/Beides</c>, dieselbe, mit der die Engine rechnet.
            ///
            /// <para>PAKET A1: Bis dahin stand hier eine <c>WaermesenkeClass.SenkeDaten</c>
            /// aus den Altspalten <c>WS_*</c> — zwei Plätze, ohne die Ränge ab 3 und mit
            /// den Prozess-Zielen als „Heizung" verkleidet. Die Karten holten die echte
            /// Kette anschließend je Karte noch einmal nach; jetzt ist es eine Abfrage
            /// für das ganze Projekt.</para>
            /// </summary>
            public List<Z_AnlageSenkeModel> Senken = new List<Z_AnlageSenkeModel>();

            /// <summary>Die Senkenzeile eines Rangs (0-basiert); <c>null</c>, wenn es sie nicht gibt.</summary>
            public Z_AnlageSenkeModel SenkeAufRang(int index)
            {
                return index >= 0 && index < Senken.Count ? Senken[index] : null;
            }

            public bool IstWaermepumpe
            {
                get { return ID_Type == ProjektPuffer.TYP_WP; }
            }
        }

        // --- Fußzeilenschalter --------------------------------------------------------
        //
        // PAKET A1: InitKaskadeSchalter, AktualisiereKaskadeSchalter, der Handler
        // checkBox_KaskadeZweikanalig_CheckedChanged samt Abwahl-Guard sowie die beiden
        // Automatiken KaskadeAutomatikNachAenderung und KaskadeAutomatikBeimSpeichern sind
        // ERSATZLOS ENTFALLEN (Begründung beim Feld checkBox_Extrapolation).
        //
        // Was daran hing und mitgegangen ist: die Rückfrage vor der Abwahl, die Meldung
        // nach dem automatischen Einschalten und die Statuszeilen „Kaskade ein/aus". Der
        // Übergangshinweis des Senkendialogs (Form_Waermesenke) ist aus demselben Grund
        // entfallen.

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
        /// Aufbau programmatisch, kein
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
            // den Schalter eine Zeile tiefer und vergrößerte das Formular anschließend um
            // die Pixel, die zur Knopfzeile fehlten (Befund N13a) — die letzte der vier
            // Selbstkorrekturen des alten Layouts. Der Schalter steht jetzt in einer
            // Fußzeile mit fester Höhe (FusszeilePlatzieren); eine Kollision mit der
            // Knopfzeile kann dort nicht mehr entstehen, weil Schalter und Knöpfe
            // getrennte Zeilen haben.
        }

        /// <summary>Belegt den Schalter aus der Datenbank vor, sobald das Projekt bekannt ist.</summary>
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

            // Sofort schreiben: Die Einstellung gehört nicht zu dem Satz, den
            // btn_Speichern_Click über KonfigurationCtrl.Update wegschreibt.
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

        // --- Booster-Lesepunkt (Paket B2, Nutzerauftrag 28.08.2026) -------------------

        /// <summary>
        /// Schalter „Booster liest Speicherzustand vom Stundenanfang (konservativ)" in der
        /// Fußzeile — die Projekteinstellung <c>Tab_Einstellungen.Booster_Lesepunkt</c>
        /// (Schema-Schritt 55).
        ///
        /// <para><b>Eine Checkbox statt einer ComboBox.</b> Es gibt genau zwei Zustände,
        /// und einer davon ist die Vorbelegung. Die Beschriftung nennt den ANGEHAKTEN
        /// Zustand („Stundenanfang, konservativ"); den anderen erklärt der Mouseover-Text
        /// — dieselbe Bauart wie beim Extrapolationsschalter daneben.</para>
        ///
        /// <para><b>Angehakt = „Davor".</b> Das ist die Vorbelegung des Nutzerauftrags,
        /// und ein Haken, den niemand anfasst, führt damit zum vorbelegten Verhalten.</para>
        ///
        /// <para>Aufbau programmatisch wie beim Nachbarschalter, Texte aus
        /// <c>MyResource</c> (deutsch und englisch).</para>
        /// </summary>
        private CheckBox checkBox_BoosterLesepunkt;
        private bool _lesepunktUiUpdate = false;

        private void InitBoosterLesepunktSchalter()
        {
            checkBox_BoosterLesepunkt = new CheckBox();
            checkBox_BoosterLesepunkt.Name = "checkBox_BoosterLesepunkt";
            checkBox_BoosterLesepunkt.Text = MyResource.Resource.SIM_BOOSTER_LESEPUNKT_SCHALTER;
            checkBox_BoosterLesepunkt.AutoSize = true;
            checkBox_BoosterLesepunkt.Checked = true;    // Vorbelegung wie im Datenmodell
            // UNSICHTBAR bis erwiesen ist, dass das Projekt einen Booster führt: Ein
            // Schalter für eine Konstellation, die es nicht gibt, wäre eine Zusage ohne
            // Wirkung (dieselbe Regel wie bei den Verdampfer-Parametern am Kessel).
            checkBox_BoosterLesepunkt.Visible = false;

            _uebersichtTip.SetToolTip(checkBox_BoosterLesepunkt,
                Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_BOOSTER_LESEPUNKT_TOOLTIP));

            checkBox_BoosterLesepunkt.CheckedChanged += checkBox_BoosterLesepunkt_CheckedChanged;
            this.Controls.Add(checkBox_BoosterLesepunkt);
            checkBox_BoosterLesepunkt.BringToFront();
        }

        /// <summary>
        /// Belegt den Schalter vor und blendet ihn ein, sobald das Projekt mindestens
        /// einen gekoppelten Booster führt.
        ///
        /// <para>Die Booster-Frage beantwortet <see cref="Warnkriterien.BoosterAnlagen"/>
        /// — dieselbe EINE Wahrheit, aus der auch das Booster-Badge der Erzeugerkarte und
        /// die Schema-Hinweise kommen (Entscheidung F9). Eine zweite Auslegung daneben
        /// wäre eine zweite Wahrheit über dieselbe Konstellation.</para>
        /// </summary>
        private void AktualisiereBoosterLesepunktSchalter()
        {
            if (checkBox_BoosterLesepunkt == null) return;

            bool mitBooster = m_ID_Projekt > 0 &&
                              Warnkriterien.BoosterAnlagen(m_ID_Projekt).Count > 0;

            _lesepunktUiUpdate = true;
            try
            {
                checkBox_BoosterLesepunkt.Visible = mitBooster;
                checkBox_BoosterLesepunkt.Enabled = mitBooster;
                checkBox_BoosterLesepunkt.Checked =
                    !mitBooster ||
                    !string.Equals(KonfigurationCtrl.BoosterLesepunktLesen(m_ID_Projekt),
                                   DbWerte.BOOSTER_LESEPUNKT_DANACH, StringComparison.Ordinal);
            }
            finally { _lesepunktUiUpdate = false; }
        }

        private void checkBox_BoosterLesepunkt_CheckedChanged(object sender, EventArgs e)
        {
            if (_lesepunktUiUpdate || m_ID_Projekt <= 0) return;

            bool davor = checkBox_BoosterLesepunkt.Checked;
            string wert = davor ? DbWerte.BOOSTER_LESEPUNKT_DAVOR : DbWerte.BOOSTER_LESEPUNKT_DANACH;

            // Sofort schreiben wie beim Nachbarschalter: Die Einstellung gehört nicht zu
            // dem Satz, den btn_Speichern_Click über KonfigurationCtrl.Update wegschreibt.
            if (KonfigurationCtrl.BoosterLesepunktSchreiben(m_ID_Projekt, wert))
            {
                ShowStatus(davor
                    ? MyResource.Resource.SIM_STATUS_LESEPUNKT_DAVOR
                    : MyResource.Resource.SIM_STATUS_LESEPUNKT_DANACH,
                    Color.DarkGreen);
                return;
            }

            _lesepunktUiUpdate = true;
            try { checkBox_BoosterLesepunkt.Checked = !davor; }
            finally { _lesepunktUiUpdate = false; }

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
            // (ReferenzenLoesen).
            //
            // PAKET A1: Der Umweg über die Übergangsbrücke ist entfallen. Er sorgte
            // dafür, dass „Speichern" die hier geänderten Betriebstemperaturen nicht
            // wieder mit dem Stand der Alt-Zuordnung überschrieb — es gibt weder die
            // Alt-Zuordnung noch die Temperatur-Nachführung beim Speichern.
            // Tab_Pufferspeicher ist die einzige Ablage, und dieser Dialog hat gerade
            // hineingeschrieben.
            AktualisiereErzeugerUebersicht();
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

            // PAKET A1: Die WS_*-Spalten sind aus der Abfrage heraus. Die Senken kommen
            // aus Z_AnlageSenke - EINE Abfrage für das ganze Projekt, unten je Anlage
            // zugeteilt (SenkenJeAnlage).
            //
            // D2: Vorlauf und Rücklauf der ANLAGE für den Temperaturchip und die
            // Warnregel aus Konzept Abschnitt 5. ACHTUNG: Die Rücklaufspalte heißt in
            // Tab_Energieanlagen MIT Umlaut (an der Datenbank verifiziert, Befund B0-4,
            // siehe ProjektPuffer.SQL_SYSTEM_RUECKLAUF) - anders als in
            // Tab_Pufferspeicher. Alias auf den umlautfreien Namen, damit der Lesecode
            // unten nicht von der Schreibweise abhängt.
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT a.ID, a.Bezeichner, a.Prioritaet, a.WQ_Typ, a.WQ_Temp, a.BM_Typ, " +
                "       a.Vorlauf, a.[Rücklauf] AS Ruecklauf, " +
                "       w.Typ AS WPTyp " +
                "FROM Tab_Energieanlagen AS a LEFT JOIN Tab_WP AS w ON a.ID_WP = w.ID " +
                "WHERE a.ID_Projekt=" + m_ID_Projekt + " AND a.ID_Type=" + typ +
                // Ungepflegte Priorität (NULL oder 0) ans ENDE, nicht an den Anfang -
                // dieselbe Regel wie in der Ladeordnung (ANLAGENPRIO_UNGEPFLEGT).
                " ORDER BY " + Ladeordnung.SqlAnlagenprio("a") + ", a.ID");
            if (dt == null) return anlagen;

            Dictionary<int, List<Z_AnlageSenkeModel>> senken = SenkenJeAnlage();

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
                if (r["BM_Typ"] != DBNull.Value) info.BM_Typ = r["BM_Typ"].ToString();

                List<Z_AnlageSenkeModel> kette;
                info.Senken = senken.TryGetValue(info.ID, out kette) && kette.Count > 0
                    ? kette : VorbelegungRang1(info.ID);

                if (!string.IsNullOrEmpty(info.Bezeichner)) anlagen.Add(info);
            }

            return anlagen;
        }

        /// <summary>
        /// Die Senkenzeilen ALLER Anlagen des Projekts, nach Anlagen-ID gebündelt und in
        /// Rangfolge — EINE Abfrage auf <c>Z_AnlageSenke</c> (Paket A1). Nie <c>null</c>.
        ///
        /// Fehlt die Tabelle (Migration nicht durchgekommen), bleibt die Sammlung leer;
        /// der Aufrufer setzt dann die Rang-1-Vorbelegung. Dass dieser Dialog auf einem
        /// solchen Schema überhaupt aufgeht, verhindert bereits
        /// <c>SchemaMigration.SimulationGesperrt</c> in <c>SetControls</c>.
        /// </summary>
        private Dictionary<int, List<Z_AnlageSenkeModel>> SenkenJeAnlage()
        {
            Dictionary<int, List<Z_AnlageSenkeModel>> map =
                new Dictionary<int, List<Z_AnlageSenkeModel>>();
            if (m_ID_Projekt <= 0 || !Z_AnlageSenkeCtrl.SpalteVorhanden()) return map;

            foreach (Z_AnlageSenkeModel z in new Z_AnlageSenkeCtrl().LesenJeProjekt(m_ID_Projekt))
            {
                if (z == null || z.ID_Anlage <= 0) continue;

                List<Z_AnlageSenkeModel> kette;
                if (!map.TryGetValue(z.ID_Anlage, out kette))
                {
                    kette = new List<Z_AnlageSenkeModel>();
                    map[z.ID_Anlage] = kette;
                }
                kette.Add(z);
            }

            return map;
        }

        /// <summary>
        /// Die RANG-1-INVARIANTE als Liste (Konzept 5.1): <c>Heizkreis/Beides</c> — genau
        /// das, was die Engine für eine Anlage ohne Senkenzeile rechnet.
        /// </summary>
        private static List<Z_AnlageSenkeModel> VorbelegungRang1(int idAnlage)
        {
            List<Z_AnlageSenkeModel> kette = new List<Z_AnlageSenkeModel>();
            kette.Add(new Z_AnlageSenkeModel
            {
                ID_Anlage = idAnlage,
                Rang = 1,
                Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                Bedarfsart = WaermequelleClass.SENKE_BEIDES
            });
            return kette;
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
        /// Kompakte Anzeige der Senke auf Rang 1 (Konzept 4.1): Heizkreis mit Bedarfsart
        /// oder „Puffer Heizung: &lt;Name&gt;".
        ///
        /// PAKET A1: gelesen aus der Senkenliste statt aus den Altspalten — nur so stehen
        /// auch die beiden Prozess-Ziele richtig da.
        /// </summary>
        private string WaermesenkeAnzeige(AnlagenInfo a)
        {
            return WaermesenkeClass.SenkeAnzeige(a.SenkeAufRang(0));
        }

        /// <summary>Kompakte Anzeige der Senke auf Rang 2; „–" ohne zweite Senke (Konzept 4.1).</summary>
        private string ZweitsenkeAnzeige(AnlagenInfo a)
        {
            return WaermesenkeClass.SenkeAnzeige(a.SenkeAufRang(1));
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

            // Die Maske ist seit iU9-W10a.1 eine Razor-Komponente
            // (EPOS.UI/Dialoge/Simulation/BetriebsmodusDialog); die Huelle zeigt sie in
            // einer BlazorWebView und liefert den gewaehlten BM_Typ zurueck.
            // Abgebrochen heisst null - dann bleibt hier alles unberuehrt.
            string modus = BetriebsmodusHuelle.Oeffnen(this, info.Bezeichner, info.BM_Typ);
            if (modus == null) return;

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
        /// Öffnet den Senkendialog <see cref="Form_Waermesenke"/>.
        ///
        /// <para><b>PAKET A1 — der Dialog speichert selbst.</b> Bis dahin nahm der
        /// Aufrufer die auf zwei Plätze gespiegelte Fassung entgegen und schrieb sie über
        /// <c>WaermesenkeClass.Schreiben</c> in die Altspalten <c>WS_*</c>; anschließend
        /// hielt die Übergangsbrücke <c>WpSenkeSpiegeln</c> noch die Alt-Zuordnung
        /// <c>Z_ProjektPufferSp</c> nach. Beides ist entfallen: Die Senkenliste und die
        /// Verbundmitglieder gehen in <c>Form_Waermesenke.ListeSpeichern</c> heraus, und
        /// zwar vollständig. Hier bleibt das Öffnen, die Statusmeldung und der
        /// Neuaufbau der Anzeige.</para>
        ///
        /// Auch nach Abbruch wird neu aufgebaut: Der Dialog kann über „Pufferspeicher
        /// anlegen…" einen neuen Projekt-Puffer erzeugt haben.
        /// </summary>
        private void WaermesenkeBearbeiten(AnlagenInfo info)
        {
            Form_Waermesenke frm = new Form_Waermesenke();
            frm.ID_Projekt = m_ID_Projekt;
            frm.ID_Anlage = info.ID;
            frm.ID_Type = info.ID_Type;
            frm.AnlagenName = info.Bezeichner;
            frm.BM_Typ = info.BM_Typ;
            frm.VerbundMitglieder = WaermesenkeClass.VerbundLesen(info.ID);
            frm.SetControls();

            DialogResult ergebnis = frm.ShowDialog(this);

            if (ergebnis == DialogResult.OK)
            {
                if (!frm.SpeichernOk)
                {
                    ShowStatus(MyResource.Resource.SIM_STATUS_SENKE_FEHLER, Color.Firebrick);
                }
                else
                {
                    Z_AnlageSenkeModel rang1 =
                        frm.Senkenliste.Count > 0 ? frm.Senkenliste[0] : null;

                    ShowStatus(string.Format(MyResource.Resource.SIM_STATUS_SENKE_GESPEICHERT,
                                             WaermesenkeClass.SenkeAnzeige(rang1)),
                               Color.ForestGreen);
                }
            }

            AktualisiereErzeugerUebersicht();
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

            // PAKET Q1 (Konzept 8.1 Punkt 4): SCHLÜSSEL- STATT INDEXKOPPLUNG. Jeder
            // Eintrag trägt seinen Steuerwert selbst; die Auswertung im Ereignis liest
            // ihn aus dem Eintrag statt über SelectedIndex in eine zweite Liste zu
            // greifen. Die Falle „Liste umsortiert -> Bestandsprojekte zeigen auf die
            // falsche Quelle" hat damit keinen Angriffspunkt mehr.
            string[] anzeige = WaermequelleClass.TypAnzeigeFuer(info.ID_Type);
            for (int i = 0; i < _wqTypen.Length; i++)
                _wqCombo.Items.Add(new SchluesselEintrag(
                    _wqTypen[i], i < anzeige.Length ? anzeige[i] : _wqTypen[i]));

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

            // PAKET Q1: der STEUERWERT kommt aus dem Eintrag, nicht aus dem Index.
            SchluesselEintrag eintrag =
                _wqCombo.SelectedItem as SchluesselEintrag;
            if (eintrag == null) return;

            string typNeu = eintrag.Wert as string;
            if (typNeu == null) return;

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
                        DbParamTyp.Integer, DBNull.Value);
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
                        // PAKET Q1: die Quell-Entnahmehöhe (Schema-Schritt 54); NULL
                        // bleibt NULL und heißt „oben".
                        object oHoehe = WaermequelleClass.WertLesen(info.ID, "WQ_Anschlusshoehe");
                        if (oHoehe != null) frmQuelle.Anschlusshoehe = Convert.ToDouble(oHoehe);

                        // PAKET B2 (Schema-Schritt 55): Temperaturbezug der Kessel-Kaskade
                        // und - als seine feste Vorgabe - das Temperaturpaar der ANLAGE.
                        // Der Dialog zeigt beides nur beim Heizkessel; gelesen wird es
                        // trotzdem für beide Arten, damit eine Wärmepumpen-Bearbeitung die
                        // Werte unverändert zurückschreibt statt sie zu leeren.
                        frmQuelle.TemperaturModus = DbWerte.TemperaturModusOderDefault(
                            WaermequelleClass.WertLesen(info.ID,
                                SchemaKatalog.SPALTE_ANLAGE_WQ_TEMPERATURMODUS));
                        frmQuelle.VorlaufAnlage = info.Vorlauf;
                        frmQuelle.RuecklaufAnlage = info.Ruecklauf;

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
                        // Überladung mit ausdrücklichem DbParamTyp weg — 0 ist keine
                        // gültige Puffer-ID, und die erzwungene Beziehung aus Schritt 4
                        // der SchemaMigration würde sie abweisen (dieselbe Regel wie in
                        // WaermesenkeClass.Schreiben).
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_ID_Puffer",
                            DbParamTyp.Integer,
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

                        // PAKET Q1: die Quell-Entnahmehöhe gilt für Wärmepumpe UND
                        // Heizkessel (Konzept 8.4) und steht deshalb außerhalb des
                        // Verdampfer-Blocks. Über die Überladung mit ausdrücklichem
                        // DbParamTyp, weil NULL hier der Regelfall ist („oben") und ACE aus
                        // DBNull allein keinen Spaltentyp ableitet.
                        // Erst in eine lokale Variable: Ein Formular ist eine
                        // MarshalByRefObject-Klasse, und der Zugriff auf HasValue/Value
                        // eines Nullable-FELDES darauf zieht CS1690 nach sich.
                        double? hoehe = frmQuelle.Anschlusshoehe;
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Anschlusshoehe",
                            DbParamTyp.Double,
                            hoehe.HasValue ? (object)hoehe.Value : DBNull.Value);

                        // PAKET B2 (Nutzerauftrag 28.08.2026): Der Temperaturbezug gilt nur
                        // für den HEIZKESSEL - bei der Wärmepumpe hat der Dialog die
                        // Auswahl gar nicht gezeigt, und dann darf er sie auch nicht
                        // schreiben (dieselbe Regel wie beim Verdampfer-Block darüber).
                        //
                        // Das TEMPERATURPAAR geht nur im Modus „fest" weg. Bei „berechnet"
                        // bleibt ein einmal gepflegtes Paar an der Anlage stehen: Es ist
                        // dort auch für andere Auswertungen die Systemvorgabe (W3,
                        // PufferSpCtrl.SystemVorlauf), und der Modus sagt nur, dass der
                        // Quellanteil es nicht als Vorgabe benutzt.
                        if (!info.IstWaermepumpe)
                        {
                            WaermequelleClass.WertSchreiben(info.ID,
                                SchemaKatalog.SPALTE_ANLAGE_WQ_TEMPERATURMODUS,
                                frmQuelle.TemperaturModus);

                            if (string.Equals(frmQuelle.TemperaturModus,
                                              DbWerte.WQ_TEMPMODUS_FEST, StringComparison.Ordinal))
                            {
                                WaermequelleClass.WertSchreiben(info.ID, "Vorlauf",
                                                                frmQuelle.VorlaufAnlage);
                                // Die Spalte trägt an der Datenbank den UMLAUT
                                // (ProjektPuffer.SQL_SYSTEM_RUECKLAUF); WertSchreiben
                                // klammert den Namen, der Zugriff trägt.
                                WaermequelleClass.WertSchreiben(info.ID, "Rücklauf",
                                                                frmQuelle.RuecklaufAnlage);
                            }
                        }

                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);

                        // PAKET A1: Hier stand die Kaskaden-Automatik. Sie schaltete den
                        // zweikanaligen Weg ein, wenn ein aufgelöster Quellbezug entstand -
                        // den gibt es nur dort. Mit dem Abriss des einkanaligen Wegs
                        // (Schritt 51) rechnet jeder Lauf so; es gibt nichts mehr zu
                        // entscheiden.
                        break;
                    }

                case WaermequelleClass.TYP_PROFIL:
                    {
                        // PAKET Q1 (Konzept 8.1 Punkt 2/3): Das Quellprofil ist ein
                        // eigener Gegenstand in Tab_Quellprofil/Tab_QuellprofilDaten mit
                        // den Betriebsarten Monat (12), Tag (365) und Stunde (8760); die
                        // Anlage verweist über WQ_ID_Quellprofil darauf.
                        Form_Quellprofil frmProfil = new Form_Quellprofil();
                        frmProfil.WPName = info.Bezeichner;
                        frmProfil.ID_Projekt = m_ID_Projekt;

                        object oIdProfil = WaermequelleClass.WertLesen(info.ID, "WQ_ID_Quellprofil");
                        if (oIdProfil != null) frmProfil.ID_Quellprofil = Convert.ToInt32(oIdProfil);

                        // ALTWEG als Vorbelegung: Solange die Anlage kein Profil führt,
                        // startet der Dialog mit dem, was die Engine heute rechnet
                        // (WQ_Monatswerte/WQ_Wochenwerte, Konzept 15 Lese-Altlast).
                        frmProfil.Monatswerte = WaermequelleClass.WertLesen(info.ID, "WQ_Monatswerte") as string;
                        frmProfil.Wochenwerte = WaermequelleClass.WertLesen(info.ID, "WQ_Wochenwerte") as string;
                        frmProfil.SetControls();

                        if (frmProfil.ShowDialog(this) != DialogResult.OK) return;

                        // FÜHREND ist der Fremdschlüssel. Er geht über die Überladung mit
                        // ausdrücklichem DbParamTyp weg - 0 ist keine gültige Profil-ID,
                        // und die Beziehung FK_Anlage_Quellprofil aus Schritt 54 würde sie
                        // abweisen (dieselbe Regel wie bei WQ_ID_Puffer).
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_ID_Quellprofil",
                            DbParamTyp.Integer,
                            frmProfil.ID_Quellprofil > 0 ? (object)frmProfil.ID_Quellprofil : DBNull.Value);

                        // WQ_Monatswerte/WQ_Wochenwerte werden NICHT mehr geschrieben:
                        // Sie sind Lese-Altlast (Konzept 15). Sie stehenzulassen ist der
                        // Rückweg - wer das Profil wieder entfernt, rechnet wie zuvor.
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
                        // Die Maske ist seit iU9-W10a.3 eine Razor-Komponente
                        // (EPOS.UI/Dialoge/Simulation/QuelleErdreichDialog). Aus achtzehn
                        // einzeln gesetzten Feldern wird EIN Satz; die Ergebnisanbindung
                        // der Auslegungspruefung baut die Huelle selbst (sie kennt die
                        // Dreistufenlogik der Zuordnung).
                        //
                        // Die Klimazone ist eine Eigenschaft der REGION, nicht der Anlage
                        // (Konzept 13.1) - deshalb wird sie hier vorher gemerkt und eine
                        // Aenderung an die Region zurueckgeschrieben.
                        int zoneVorher = KlimazoneDesProjekts();

                        string quellsystem = WaermequelleClass.WertLesen(info.ID, "WQ_Quellsystem") as string;
                        object oTiefe = WaermequelleClass.WertLesen(info.ID, "WQ_Tiefe");
                        object oFlaeche = WaermequelleClass.WertLesen(info.ID, "WQ_Flaeche");
                        object oAnzahl = WaermequelleClass.WertLesen(info.ID, "WQ_Anzahl");
                        string bodentyp = WaermequelleClass.WertLesen(info.ID, "WQ_Bodentyp") as string;
                        // Nutzbare Spreizung (Konzept 13.1) - dieselbe Spalte wie beim
                        // Pufferspeicher-Quellendialog, jetzt auch hier pflegbar.
                        object oSpreizErde = WaermequelleClass.WertLesen(info.ID, "WQ_Spreizung");

                        var erdDaten = new EPOS.UI.Dialoge.Simulation.QuelleErdreichDaten
                        {
                            WPName = info.Bezeichner,
                            IdProjekt = m_ID_Projekt,
                            IdAnlage = info.ID,
                            Quellsystem = string.IsNullOrEmpty(quellsystem) ? "" : quellsystem,
                            Tiefe = (oTiefe != null && Convert.ToDouble(oTiefe) > 0)
                                ? Convert.ToDouble(oTiefe) : 0.0,
                            Flaeche = oFlaeche != null ? Convert.ToDouble(oFlaeche) : 0.0,
                            Anzahl = (oAnzahl != null && Convert.ToInt32(oAnzahl) > 0)
                                ? Convert.ToInt32(oAnzahl) : 0,
                            Bodentyp = string.IsNullOrEmpty(bodentyp) ? "" : bodentyp,
                            Klimazone = zoneVorher,
                            Spreizung = (oSpreizErde != null && Convert.ToDouble(oSpreizErde) > 0)
                                ? Convert.ToDouble(oSpreizErde) : 0.0,
                            Aussentemperatur = AussentemperaturLaden()
                        };

                        EPOS.UI.Dialoge.Simulation.QuelleErdreichDaten erdNeu =
                            QuelleErdreichHuelle.Oeffnen(this, erdDaten);
                        if (erdNeu == null) return;

                        if (erdNeu.Klimazone != zoneVorher) KlimazoneSpeichern(erdNeu.Klimazone);

                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Quellsystem", erdNeu.Quellsystem);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Tiefe", erdNeu.Tiefe);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Flaeche", erdNeu.Flaeche);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Anzahl", erdNeu.Anzahl);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Bodentyp", erdNeu.Bodentyp);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Spreizung", erdNeu.Spreizung);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }
            }

            AktualisiereErzeugerUebersicht();
        }

        /// <summary>
        /// Kleiner modaler Eingabedialog (Titel, Beschriftung, Vorgabewert).
        /// Liefert den eingegebenen Text oder null bei Abbruch.
        ///
        /// <para><b>PAKET Q1:</b> Der Rumpf steht jetzt in <see cref="Eingabefrage"/> —
        /// der Quellprofil-Dialog braucht denselben Baustein, und zwei Fassungen liefen
        /// unweigerlich auseinander. Diese Methode bleibt als Durchreiche stehen, damit
        /// die sechs Aufrufstellen in diesem Formular unverändert sind.</para>
        /// </summary>
        private string EingabeDialog(string titel, string beschriftung, string vorgabe)
        {
            return Eingabefrage.Fragen(this, titel, beschriftung, vorgabe);
        }
    }
}
