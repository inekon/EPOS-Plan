using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE D4 (Konzept_KonfigUI_Hydraulik, Abschnitt 3 und 6) — die Ansicht „Schema"
    /// der Simulationskonfiguration und ihre Kopplung an die Kartenansicht.
    ///
    /// <b>Eine Seite, zwei synchronisierte Ansichten, unveränderte Editoren.</b> Der
    /// Umschalter rechts oben wechselt zwischen den Kartenspalten (D2/D3) und dem
    /// gezeichneten Hydraulikschema (<see cref="SchemaAnsicht"/>). BEIDE zeigen dieselben
    /// Daten und teilen die Auswahl: Ein Klick auf einen Schema-Kasten hebt die zugehörige
    /// Karte hervor und umgekehrt; die Auswahl überlebt das Umschalten. Doppelklick öffnet
    /// hier wie dort die BESTEHENDEN Dialoge — die neue Ansicht ist Lesefläche, keine
    /// Parallel-Editierwelt.
    ///
    /// <b>Was hier NICHT steht.</b> Die Fachregeln der Zeichnung (welche Kante, welcher
    /// Knoten, welche Kette) stehen in <see cref="SchemaModell"/>, die Zeichnung selbst in
    /// <see cref="SchemaAnsicht"/>. Diese Datei ist die Verdrahtung: Umschalter,
    /// Auffrischung, Auswahl und die Zuordnung Schema-Element → Editor.
    /// </summary>
    public partial class Form_Simulation_Config : BaseForm
    {
        // --- Steuerwerte der Ansicht (sprachneutral, Drei-Schichten-Regel) ------------

        /// <summary>Steuerwert der Kartenansicht.</summary>
        private const string ANSICHT_LISTE = "LISTE";

        /// <summary>Steuerwert der Schema-Ansicht.</summary>
        private const string ANSICHT_SCHEMA = "SCHEMA";

        private Label label_Ansicht;
        private Button btn_AnsichtListe;
        private Button btn_AnsichtSchema;
        private SchemaAnsicht panel_Schema;

        /// <summary>Die gerade gezeigte Ansicht (Steuerwert, nicht Anzeigetext).</summary>
        private string _ansicht = ANSICHT_LISTE;

        /// <summary>
        /// Das gemeinsame Auswahlelement beider Ansichten — der sprachneutrale
        /// Knotenschlüssel aus <see cref="SchemaModell"/> („ERZEUGER_11203",
        /// „SPEICHER_1018023", …); "" = keine Auswahl.
        ///
        /// Bewusst der SCHEMA-Schlüssel und nicht etwa ein Kartenobjekt: Die Karten werden
        /// bei jeder Auffrischung neu gebaut und entsorgt (siehe <c>SpalteLeeren</c>), eine
        /// Objektreferenz überlebte das nicht. Der Schlüssel ist datenbezogen und übersteht
        /// jeden Neuaufbau.
        /// </summary>
        private string _auswahl = "";

        // --- Aufbau -------------------------------------------------------------------

        /// <summary>
        /// Legt Umschalter und Schemafläche an. Gerufen aus
        /// <c>KartenLayoutAufbauen</c>, NACHDEM der Kartenbereich steht — die
        /// Schemafläche übernimmt dessen Rechteck und Verankerung.
        /// </summary>
        private void SchemaAufbauen()
        {
            label_Ansicht = new Label();
            label_Ansicht.Name = "label_Ansicht";
            label_Ansicht.Text = MyResource.Resource.SIM_ANSICHT_LABEL;
            label_Ansicht.AutoSize = true;
            label_Ansicht.ForeColor = KartenStil.TEXT_LEISE;
            label_Ansicht.BackColor = Color.Transparent;

            btn_AnsichtListe = AnsichtSchalter("btn_AnsichtListe",
                                               MyResource.Resource.SIM_ANSICHT_LISTE,
                                               ANSICHT_LISTE);
            btn_AnsichtSchema = AnsichtSchalter("btn_AnsichtSchema",
                                                MyResource.Resource.SIM_ANSICHT_SCHEMA,
                                                ANSICHT_SCHEMA);

            panel_Schema = new SchemaAnsicht();
            panel_Schema.Name = "panel_Schema";
            panel_Schema.Location = tableLayout_Karten.Location;
            panel_Schema.Size = tableLayout_Karten.Size;
            panel_Schema.Anchor = tableLayout_Karten.Anchor;
            panel_Schema.Visible = false;
            panel_Schema.Ausgewaehlt += SchemaAuswahl;
            panel_Schema.Bearbeiten += SchemaBearbeiten;

            Controls.Add(label_Ansicht);
            Controls.Add(btn_AnsichtListe);
            Controls.Add(btn_AnsichtSchema);
            Controls.Add(panel_Schema);

            panel_Schema.BringToFront();
            label_Ansicht.BringToFront();
            btn_AnsichtListe.BringToFront();
            btn_AnsichtSchema.BringToFront();

            UmschalterPlatzieren();
            AnsichtAnwenden();
        }

        private Button AnsichtSchalter(string name, string text, string steuerwert)
        {
            Button b = new Button();
            b.Name = name;
            b.Text = text;
            b.AutoSize = false;
            b.Height = 24;
            b.Width = Math.Max(74, TextRenderer.MeasureText(text, Font).Width + 26);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = KartenStil.RAHMEN;
            b.Tag = steuerwert;           // Steuerwert am Element, nicht der Anzeigetext
            b.Click += AnsichtSchalter_Click;
            return b;
        }

        /// <summary>
        /// Setzt den Umschalter rechts oben — dieselbe Stelle wie im Entwurf
        /// („Ansicht: [Liste][Schema]" in der Kopfzeile). Verankert oben rechts, damit er
        /// beim Vergrößern mitwandert.
        /// </summary>
        private void UmschalterPlatzieren()
        {
            int oben = Math.Max(8, KARTEN_OBEN - 32);
            // D3 (28.08.2026): rechts das Randmaß der Fußzeilen-Norm, damit Umschalter,
            // Kartenfläche und Knopfreihe in EINER Flucht enden (siehe KARTEN_RAND_RECHTS).
            int rechts = ClientSize.Width - KARTEN_RAND_RECHTS;

            btn_AnsichtSchema.Location = new Point(rechts - btn_AnsichtSchema.Width, oben);
            btn_AnsichtListe.Location =
                new Point(btn_AnsichtSchema.Left - btn_AnsichtListe.Width + 1, oben);
            label_Ansicht.Location =
                new Point(btn_AnsichtListe.Left - label_Ansicht.PreferredWidth - 8, oben + 4);

            btn_AnsichtSchema.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_AnsichtListe.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label_Ansicht.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        }

        private void AnsichtSchalter_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;

            string wunsch = b.Tag as string;
            if (string.IsNullOrEmpty(wunsch) ||
                string.Equals(wunsch, _ansicht, StringComparison.Ordinal)) return;

            _ansicht = wunsch;
            AnsichtAnwenden();
        }

        /// <summary>
        /// Blendet die gewählte Ansicht ein und die andere aus.
        ///
        /// <b>Der Auswahlzustand überlebt.</b> Er hängt an <see cref="_auswahl"/> und
        /// nicht an einem Steuerelement; beide Ansichten stellen ihn beim Einblenden
        /// wieder her (Verifikationsvorgabe „Umschalter-Roundtrip erhält Auswahl").
        /// </summary>
        private void AnsichtAnwenden()
        {
            if (panel_Schema == null || tableLayout_Karten == null) return;

            bool schema = string.Equals(_ansicht, ANSICHT_SCHEMA, StringComparison.Ordinal);

            tableLayout_Karten.Visible = !schema;
            panel_Schema.Visible = schema;

            SchalterZeichnen(btn_AnsichtListe, !schema);
            SchalterZeichnen(btn_AnsichtSchema, schema);

            if (schema)
            {
                AktualisiereSchema();
                panel_Schema.Auswahl = _auswahl;
            }
            else
            {
                AuswahlInKartenZeigen();
            }
        }

        private void SchalterZeichnen(Button b, bool aktiv)
        {
            if (b == null) return;

            b.BackColor = aktiv ? KartenStil.FLAECHE : Color.White;
            b.ForeColor = aktiv ? KartenStil.TEXT : KartenStil.TEXT_LEISE;
            b.FlatAppearance.BorderColor = aktiv ? KartenStil.RAHMEN : KartenStil.RAHMEN_LEISE;
            b.Font = new Font(b.Font, aktiv ? FontStyle.Bold : FontStyle.Regular);
        }

        // --- Auffrischung -------------------------------------------------------------

        /// <summary>
        /// Baut das Schemamodell neu auf — aus DENSELBEN Quellen wie die Karten:
        /// die Kaskadenbelegung aus den vier Auswahlfeldern (<c>KaskadeBelegt</c>) und
        /// die Verschaltung aus <see cref="Hydraulikbild"/>.
        ///
        /// Wird nur gerechnet, wenn die Schema-Ansicht auch sichtbar ist. Das Modell holt
        /// je Aufbau eine Anlagen-, eine Puffer- und je beteiligtem Speicher eine
        /// Ladeordnungs-Abfrage; in der Kartenansicht wäre das verschenkte Arbeit, und der
        /// Seitenaufbau wird bei jeder Kleinigkeit angestoßen (acht Aufrufstellen).
        /// </summary>
        private void AktualisiereSchema()
        {
            if (panel_Schema == null || !panel_Schema.Visible) return;

            SchemaModell modell = SchemaModell.Aufbauen(m_ID_Projekt, KaskadeBelegt());
            SchemaHinweiseSetzen(modell);

            panel_Schema.Setzen(modell);
            panel_Schema.Auswahl = _auswahl;
        }

        /// <summary>
        /// Ersetzt die Modell-Hinweise durch die KURZINFO DER KARTE (Aufgabe D4-2:
        /// „Tooltip am Element = Kurzinfo der Karte").
        ///
        /// Das Modell baut sich einen eigenen, knappen Hinweis — es soll ohne Oberfläche
        /// tragfähig bleiben. Sobald es aber IN diesem Dialog gezeigt wird, ist die
        /// Kartenkurzinfo die bessere Auskunft: Sie ist dieselbe, die der Anwender in der
        /// Listenansicht liest, und läuft damit nicht gegen sie.
        /// </summary>
        private void SchemaHinweiseSetzen(SchemaModell modell)
        {
            if (modell == null || m_ID_Projekt <= 0) return;

            // PAKET S2: Der Warn-Chip ist Teil der Kartenkurzinfo und muss deshalb auch
            // hier frisch sein — die Schema-Ansicht kann aufgefrischt werden, ohne dass
            // die Kartenspalte neu gebaut wurde (Umschalter).
            WarnbefundeSammeln();

            // PAKET B1 (F9): Das Booster-Badge ist ebenso Teil der Kartenkurzinfo und
            // erscheint deshalb auch in den Schema-Hinweisen — eine Quelle für beide.
            BoosterAnlagenSammeln();

            // Erzeugerkarten: die Chips der Karte als Zeilen.
            //
            // ANWENDERENTSCHEID F2: Der Modul-Ausweis („Modul n von m") ist Teil der
            // Kartenkurzinfo und steht deshalb auch hier - eine Quelle für beide, wie
            // beim Booster-Badge. Dazu zählt diese Schleife dieselbe Stelle mit, die die
            // Kartenspalte zählt: die Anzeigereihenfolge aus AnlagenImProjekt.
            Dictionary<int, string> chips = new Dictionary<int, string>();
            foreach (string dbWert in KaskadeBelegt())
            {
                List<AnlagenInfo> anlagen = AnlagenImProjekt(dbWert);
                for (int a = 0; a < anlagen.Count; a++)
                {
                    AnlagenInfo info = anlagen[a];
                    List<string> zeilen = new List<string>();
                    foreach (ErzeugerKarte.ChipDaten c in
                             ErzeugerChips(info, a + 1, anlagen.Count))
                        if (c != null && !string.IsNullOrEmpty(c.Text)) zeilen.Add(c.Text);

                    chips[info.ID] = string.Join(Environment.NewLine, zeilen.ToArray());
                }
            }

            // Speicherkarten: die Detailzeilen der Karte.
            Dictionary<int, string> speicher = new Dictionary<int, string>();
            _quellnutzer = QuellnutzerSammeln();
            _geladenePuffer = GeladenePufferSammeln();
            _systemVorlauf = PufferSpCtrl.SystemVorlauf(m_ID_Projekt);
            _systemRuecklauf = PufferSpCtrl.SystemRuecklauf(m_ID_Projekt);

            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(m_ID_Projekt, null))
            {
                if (p == null || modell.Finden(SchemaModell.PRAEFIX_SPEICHER + p.ID) == null) continue;

                SpeicherKarte.Daten d = SpeicherKarteDaten(p);
                List<string> zeilen = new List<string>(d.Detailzeilen);
                if (!string.IsNullOrEmpty(d.Schwellentext)) zeilen.Add(d.Schwellentext);
                speicher[p.ID] = string.Join(Environment.NewLine, zeilen.ToArray());
            }

            foreach (SchemaModell.Knoten k in modell.Knotenliste)
            {
                string text;
                if (k.Art == SchemaModell.Knotenart.Erzeuger && chips.TryGetValue(k.ID, out text))
                    k.Hinweis = text;
                else if (k.Art == SchemaModell.Knotenart.Speicher && speicher.TryGetValue(k.ID, out text))
                    k.Hinweis = text;
            }
        }

        // --- Auswahl-Synchronisation --------------------------------------------------

        /// <summary>Klick im Schema — die zugehörige Karte wird hervorgehoben.</summary>
        private void SchemaAuswahl(string schluessel)
        {
            _auswahl = schluessel ?? "";

            // Ein Speicher im Schema klappt seine Karte auf (Konzept 3a: höchstens eine).
            int idPuffer = IdAusSchluessel(_auswahl, SchemaModell.PRAEFIX_SPEICHER);
            if (idPuffer > 0) _offenerSpeicher = idPuffer;

            AuswahlInKartenZeigen();
        }

        /// <summary>Doppelklick im Schema — derselbe Editor wie an der Karte.</summary>
        private void SchemaBearbeiten(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel) || m_ID_Projekt <= 0) return;

            _auswahl = schluessel;

            int idPuffer = IdAusSchluessel(schluessel, SchemaModell.PRAEFIX_SPEICHER);
            if (idPuffer > 0)
            {
                _offenerSpeicher = idPuffer;
                PufferVerwaltungOeffnen(idPuffer);
                AktualisiereSchema();
                return;
            }

            int idAnlage = IdAusSchluessel(schluessel, SchemaModell.PRAEFIX_ERZEUGER);
            if (idAnlage > 0)
            {
                AnlagenInfo info = AnlageSuchen(idAnlage);
                if (info == null) return;

                // Der Senkendialog ist der Standard-Editor einer Erzeugerkarte (Konzept
                // 4.2) - der Doppelklick im Schema führt an dieselbe Stelle.
                WaermesenkeBearbeiten(info);
                AktualisiereSchema();
                return;
            }

            int idQuelle = IdAusSchluessel(schluessel, SchemaModell.PRAEFIX_QUELLE);
            if (idQuelle > 0)
            {
                AnlagenInfo info = AnlageSuchen(idQuelle);
                if (info == null) return;

                // Wie der Quellen-Chip der Karte: nur Arten mit Quellenwahl bekommen den
                // Inlineeditor, die übrigen bleiben stumm (D5b, Freischaltung je ID_Type).
                if (!WaermequelleClass.QuellenwahlMoeglich(info.ID_Type)) return;

                WaermequelleBearbeiten(info, SchemaElementAlsZelle(schluessel));
            }
        }

        /// <summary>
        /// Rechteck eines Schema-Elements in Formularkoordinaten — die Stelle, an der der
        /// Quellen-Inlineeditor aufklappt (Gegenstück zu <c>KarteAlsZelle</c>).
        /// </summary>
        private Rectangle SchemaElementAlsZelle(string schluessel)
        {
            Rectangle imModell = panel_Schema.FlaecheVon(schluessel);
            if (imModell.IsEmpty)
                return new Rectangle(panel_Schema.Left + 20, panel_Schema.Top + 20, 240, 24);

            Point aufDemSchirm = panel_Schema.PointToScreen(
                new Point(imModell.Left + panel_Schema.AutoScrollPosition.X,
                          imModell.Bottom + panel_Schema.AutoScrollPosition.Y));

            return new Rectangle(PointToClient(aufDemSchirm),
                                 new Size(Math.Min(imModell.Width + 60, 260), 24));
        }

        /// <summary>Anlage zu einer ID aus den AUFGENOMMENEN Erzeugern; <c>null</c> = keine.</summary>
        private AnlagenInfo AnlageSuchen(int idAnlage)
        {
            if (idAnlage <= 0) return null;

            foreach (string dbWert in KaskadeBelegt())
                foreach (AnlagenInfo info in AnlagenImProjekt(dbWert))
                    if (info.ID == idAnlage) return info;

            return null;
        }

        /// <summary>Zahl hinter einem Schlüsselpräfix; 0, wenn das Präfix nicht passt.</summary>
        private static int IdAusSchluessel(string schluessel, string praefix)
        {
            if (string.IsNullOrEmpty(schluessel) || !schluessel.StartsWith(praefix, StringComparison.Ordinal))
                return 0;

            int id;
            return Int32.TryParse(schluessel.Substring(praefix.Length), out id) ? id : 0;
        }

        /// <summary>Klick auf eine Erzeugerkarte — die Auswahl wandert ins Schema.</summary>
        private void KarteAusgewaehlt(int idAnlage)
        {
            _auswahl = idAnlage > 0 ? SchemaModell.PRAEFIX_ERZEUGER + idAnlage : "";
            AuswahlInKartenZeigen();
            if (panel_Schema != null) panel_Schema.Auswahl = _auswahl;
        }

        /// <summary>Klick auf eine Speicherkarte — die Auswahl wandert ins Schema.</summary>
        private void SpeicherkarteAusgewaehlt(int idPuffer)
        {
            _auswahl = idPuffer > 0 ? SchemaModell.PRAEFIX_SPEICHER + idPuffer : "";
            AuswahlInKartenZeigen();
            if (panel_Schema != null) panel_Schema.Auswahl = _auswahl;
        }

        /// <summary>
        /// Führt die Hervorhebung in beiden Kartenspalten nach.
        ///
        /// Ein Quellknoten (<c>QUELLE_&lt;Anlage&gt;</c>) hebt die Karte SEINER Anlage
        /// hervor: In der Liste gibt es keinen eigenen Quellkasten, die Quelle steht dort
        /// als Chip auf der Erzeugerkarte.
        /// </summary>
        private void AuswahlInKartenZeigen()
        {
            int idAnlage = IdAusSchluessel(_auswahl, SchemaModell.PRAEFIX_ERZEUGER);
            if (idAnlage == 0) idAnlage = IdAusSchluessel(_auswahl, SchemaModell.PRAEFIX_QUELLE);
            int idPuffer = IdAusSchluessel(_auswahl, SchemaModell.PRAEFIX_SPEICHER);

            if (flow_Erzeuger != null)
                foreach (Control c in flow_Erzeuger.Controls)
                {
                    ErzeugerKarte karte = c as ErzeugerKarte;
                    if (karte == null) continue;

                    AnlagenInfo info = karte.Tag as AnlagenInfo;
                    karte.Hervorgehoben = info != null && idAnlage > 0 && info.ID == idAnlage;
                }

            if (flow_Speicher != null)
                foreach (Control c in flow_Speicher.Controls)
                {
                    SpeicherKarte karte = c as SpeicherKarte;
                    if (karte == null) continue;

                    karte.Hervorgehoben = idPuffer > 0 && karte.ID_Puffer == idPuffer;
                }
        }
    }
}
