using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Kapitalwert-Vergleichsansicht „Wirtschaftlichkeit"
    /// (Konzept_Wirtschaftlichkeit.md, Kap. 6; Phase 6 = Ausbaustufe W1).
    ///
    /// Zeigt die Kapitalwert-Ergebnisse der Vergleichsgruppe (Stamm + gewählte
    /// Varianten) je Szenario. Vorbedingungen laut Vorgabe: Varianten müssen
    /// ausgewählt und berechnet sein — fehlende Simulationsergebnisse werden beim
    /// Berechnen automatisch headless nachgerechnet (BerichtsDatenSammler, gleiche
    /// Prüfkette wie der Bericht). Ergebnisse werden persistiert
    /// (Tab_ErgebnisWirtschaftlichkeit) — Seite, Word- und Excel-Bericht zeigen
    /// damit garantiert identische Zahlen.
    ///
    /// <para><b>Herkunft.</b> Der Inhalt stand bis zum Umbau „Berichte &amp; Kosten"
    /// direkt im Wirtschaftlichkeitsdialog. Er ist unverändert in dieses
    /// UserControl gehoben worden, damit ihn die Seite „Wirtschaftlichkeit" des
    /// Reiters einbetten kann; die Dialoghülle darum ist mit iU9-W0 entfallen
    /// (Anwenderentscheid iF29). Geändert wurden allein die
    /// Wirtsbeziehungen: Formular-Eigenschaften (ClientSize/Text/StartPosition) sind
    /// zu Control-Eigenschaften geworden, <c>Load</c>/<c>FormClosing</c> zu
    /// <see cref="LadeDaten"/>/<see cref="Beschaeftigt"/>+<see cref="Abbrechen"/>,
    /// und die Dialogaufrufe nehmen das umgebende Formular als Besitzer.</para>
    ///
    /// Ist das übergebene Projekt eine Variante, wird automatisch ihr Stamm verwendet.
    ///
    /// <para>Die Oberfläche steht in <c>UcWirtschaftlichkeit.Designer.cs</c>, ohne eigene
    /// <c>.resx</c>: Im Designer stehen nur Platzhalter (der Feldname), die echten
    /// deutschen Texte setzt <see cref="TexteSetzen"/> unmittelbar nach
    /// <c>InitializeComponent()</c> — die Lokalisierung dieser Seite ist ein eigener
    /// Vorgang. Nicht serialisierbar und deshalb im Konstruktor-Nachlauf: die
    /// Szenarioliste (<see cref="SzenarienFuellen"/> — das sind DB-Persistenzwerte und
    /// gehören nicht in Designer-Code).</para>
    /// </summary>
    public partial class UcWirtschaftlichkeit : UserControl
    {
        private readonly int _idStamm;
        private readonly string _stammName;

        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();
        private CancellationTokenSource _cts;
        private bool _initialisiere;
        private bool _geladen;

        private List<WirtschaftlichkeitErgebnis> _ergebnisse = new List<WirtschaftlichkeitErgebnis>();
        private readonly Dictionary<int, string> _namen = new Dictionary<int, string>();

        // W3: Emissionsbilanzen + Parameter werden je Datenstand EINMAL ermittelt
        // (Review Phase 8 — nicht bei jedem Szenariowechsel im UI-Thread rechnen).
        private WirtschaftlichkeitParameter _parameterCache;

        /// <summary>
        /// ETAPPE E7: Der Tarif entscheidet über die Beschriftung der Stromkostenzeile
        /// (Bezug im Zonenmodell, Reststrom im Rollenmodell). Er wird EINMAL je Sitzung
        /// gelesen — <see cref="ZeigeErgebnisse"/> läuft bei jedem Szenariowechsel, und
        /// eine Datenbankabfrage je Wechsel wäre Verschwendung.
        /// </summary>
        private TarifParameter _tarifCache;

        // ---- KD6a: Kennzahl-Kacheln (gleiche Karte wie die Kosten-Seite) --------
        private UcBkKosten.Kachel _kKapitalwert, _kAnnuitaet, _kAmortisation, _kIrr;
        private readonly Dictionary<int, EmissionsBilanz> _bilanzen = new Dictionary<int, EmissionsBilanz>();

        /// <summary>Stammprojekt-ID der angezeigten Vergleichsgruppe.</summary>
        public int IdStamm { get { return _idStamm; } }

        /// <summary>Der Anwender hat „Schließen" gedrückt (nur im Dialog-Wrapper belegt).</summary>
        public event EventHandler SchliessenAngefordert;

        public UcWirtschaftlichkeit(int idProjekt)
        {
            // Variante → Stamm auflösen (Muster Form_AlsVariante/UcBericht).
            int idStamm = idProjekt;
            try
            {
                int refId = new VariantenCtrl().StammRefDerVariante(idProjekt);
                if (refId > 0) idStamm = refId;
            }
            catch { }
            _idStamm = idStamm;

            var pc = new ProjektCtrl();
            pc.ReadSingle(_idStamm);
            _stammName = pc.rows > 0 ? pc.m_szProjektname : "";

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Bisher stand hier AutoScaleMode.Font OHNE
            // AutoScaleDimensions, der Skalierfaktor blieb damit (1,1) — es wurde also
            // faktisch nie skaliert. Die Anwendung läuft ohnehin DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). None hält genau dieses Verhalten
            // fest und verhindert, dass ein Designer-Speichern die Skalierung erstmals
            // scharf schaltet — dasselbe Muster wie in den Kostenmasken.
            InitializeComponent();
            KachelnBauen();
            TexteSetzen();
            SzenarienFuellen();
            BauePhotovoltaikKnopf();
        }

        // ================================================================= P5

        /// <summary>Andockpunkt des PV-Vergütungsdialogs (PV-Konzept § 7).</summary>
        private Button btnPhotovoltaik;

        /// <summary>
        /// Ä18: BHKW-Sicht der Tarifstruktur (Rollenmodell samt Referenz und
        /// Einspeisung) — sichtbar, wenn die Gruppe ein BHKW führt.
        ///
        /// <para><b>ETAPPE B5 (K8 = c, Anwenderentscheid 03.09.2026):</b> Der Knopf
        /// öffnet jetzt den Sammeldialog „BHKW-Wirtschaftlichkeit" — seit B5b die
        /// Razor-Komponente <c>EPOS.UI/Dialoge/Wirtschaftlichkeit/</c>
        /// <c>BhkwWirtschaftlichkeitDialog</c> in der Hülle
        /// <see cref="BhkwWirtschaftlichkeitHuelle"/>. Die Fußleiste ist
        /// voll — ein fünfter Sichtknopf läge bei x = −50 —, deshalb übernimmt der
        /// vorhandene Knopf die neue Sicht. Die BHKW-Tarifsicht ist NICHT verloren: Der
        /// neue Dialog trägt einen Sprungknopf „BHKW-Tarif…" in seine Stromsteuergruppe.
        /// Der Feldname bleibt, damit die Andockstelle im Layout und in
        /// <see cref="SetBusy"/> dieselbe ist.</para>
        /// </summary>
        private Button btnBhkwTarif;

        /// <summary>Ä18: Einkaufsseite der Tarifstruktur (Zonen-Bezugspreise,
        /// Staffel, Bezugsrolle) — sichtbar bei Wärmepumpe in der Gruppe oder
        /// aktiver Tarifstruktur (sonst gäbe es keinen Weg, sie abzuschalten).</summary>
        private Button btnStromTarif;

        /// <summary>
        /// ETAPPE P5: Knopf „Photovoltaik…" links neben „Tarifstruktur…" —
        /// PROGRAMMATISCH, damit die Designer-Datei unberührt bleibt (dasselbe
        /// Muster wie die übrigen Bestands-Zusätze). Sichtbar nur, wenn die
        /// Vergleichsgruppe PV-Anlagen führt (<c>ErzeugerDerGruppe</c>).
        /// </summary>
        private void BauePhotovoltaikKnopf()
        {
            btnPhotovoltaik = new Button
            {
                Size = btnTarif.Size,
                Anchor = btnTarif.Anchor,
                // Ä16: nimmt den Platz des entfallenen Tarif-Knopfs ein.
                Location = btnTarif.Location,
                UseVisualStyleBackColor = true,
                Text = "Photovoltaik…"
            };
            try
            {
                string t = MyResource.Resource.ResourceManager.GetString("PVW_KNOPF");
                if (!string.IsNullOrEmpty(t)) btnPhotovoltaik.Text = t;
            }
            catch { }
            WirtschaftlichkeitCtrl.ErzeugerFlags flags = null;
            try { flags = new WirtschaftlichkeitCtrl().ErzeugerDerGruppe(_idStamm); }
            catch { }
            btnPhotovoltaik.Visible = flags != null && flags.Photovoltaik;
            btnPhotovoltaik.Click += btnPhotovoltaik_Click;
            Controls.Add(btnPhotovoltaik);

            // Ä18 (Nutzerauftrag 26.08.2026): Die Tarifstruktur wird KOMPONENTEN-
            // BEZOGEN gepflegt — der Sammel-Einstieg btnTarif bleibt unsichtbar
            // (Ä16). „BHKW-Tarif…“ öffnet die BHKW-Sicht (Differenzmethode),
            // „Strombezug…“ die Einkaufsseite (Wärmepumpe & Verbraucher); der
            // PV-Anteil liegt im PV-Vergütungsdialog (Knopf „Einspeise-Tarif…“).
            btnBhkwTarif = new Button
            {
                Size = btnTarif.Size,
                Anchor = btnTarif.Anchor,
                Location = new Point(btnTarif.Left - btnTarif.Width - 6, btnTarif.Top),
                UseVisualStyleBackColor = true,
                // ETAPPE B5 (K8 = c): Beschriftung und Ziel des Knopfes wechseln auf den
                // neuen Sammeldialog. Der Text wird hier im Code gesetzt — die
                // Designer-Datei bleibt unberührt (Hausregel), und der Rückfall gilt,
                // solange der Schlüssel im resx-Sammelnachtrag fehlt.
                Text = T("BHW_KNOPF", "BHKW-Wirtschaftlichkeit…")
            };
            btnBhkwTarif.Visible = flags != null && flags.Bhkw;
            btnBhkwTarif.Click += btnBhkwWirtschaftlichkeit_Click;
            Controls.Add(btnBhkwTarif);

            bool tarifAktiv = false;
            try { tarifAktiv = _ctrl.LadeTarif(_idStamm).Aktiv; }
            catch { }
            btnStromTarif = new Button
            {
                Size = btnTarif.Size,
                Anchor = btnTarif.Anchor,
                Location = new Point(btnTarif.Left - 2 * (btnTarif.Width + 6), btnTarif.Top),
                UseVisualStyleBackColor = true,
                Text = "Strombezug…"
            };
            try
            {
                string t = MyResource.Resource.ResourceManager.GetString("WIRT_BTN_STROM_TARIF");
                if (!string.IsNullOrEmpty(t)) btnStromTarif.Text = t;
            }
            catch { }
            btnStromTarif.Visible = (flags != null && flags.Waermepumpe) || tarifAktiv;
            btnStromTarif.Click += delegate { TarifSichtOeffnen(TarifSicht.Strombezug); };
            Controls.Add(btnStromTarif);
        }

        /// <summary>Ä18: öffnet die Tarifstruktur in einer Komponentensicht —
        /// derselbe Nachlauf wie beim früheren Sammel-Einstieg (btnTarif_Click).</summary>
        private void TarifSichtOeffnen(TarifSicht sicht)
        {
            // iU9-W2.3: der Tarifdialog als Razor-Komponente ueber
            // TarifstrukturHuelle; Form_Tarifstruktur ist geloescht (Regel M1).
            if (TarifstrukturHuelle.Oeffnen(Besitzer, _idStamm, sicht))
            {
                _tarifCache = null;   // E7: Beschriftung der Stromkostenzeile neu holen
                ZeigeParameterzeile();
                Melde("Tarifstruktur gespeichert — bitte neu berechnen.");
            }
        }

        private void btnPhotovoltaik_Click(object sender, EventArgs e)
        {
            // iU9-W2.4: der PV-Verguetungsdialog als Razor-Komponente ueber
            // PhotovoltaikVerguetungHuelle; Form_PhotovoltaikVerguetung ist
            // geloescht (Regel M1).
            if (PhotovoltaikVerguetungHuelle.Oeffnen(Besitzer, _idStamm))
                Melde("PV-Vergütung gespeichert — bitte neu berechnen.");
        }

        /// <summary>
        /// ETAPPE B5 (BW9/K8 = c): öffnet den Sammeldialog „BHKW-Wirtschaftlichkeit".
        ///
        /// <para>Die Ergebnisse des zuletzt gerechneten Laufs werden
        /// <b>durchgereicht</b>. Zwei ihrer Bestandteile sind nicht persistiert und aus
        /// der Datenbank nicht zu holen: die Kohärenzhinweise (B2-O4) und die
        /// KWKG-Modulnachweise mit der Mengenkette (E7/B3b). Hier liegen sie bereits im
        /// Speicher — der Dialog bekommt sie, statt sie ein zweites Mal zu rechnen.</para>
        ///
        /// <para><b>ETAPPE B5b (03.09.2026):</b> Der Dialog ist eine Razor-Komponente in
        /// <c>EPOS.UI</c> (<c>BhkwWirtschaftlichkeitDialog</c>); die WinForms-Fassung
        /// <c>Form_BhkwWirtschaftlichkeit</c> ist gelöscht (Regel M1). Angezeigt wird sie
        /// von <see cref="BhkwWirtschaftlichkeitHuelle"/> — Vorbild
        /// <c>EnergietraegerVarianteDialog</c> (iU8-9). Für diesen Handler ändert
        /// sich nur die Zeile, die den Dialog öffnet; Rückmeldung und Nachlauf bleiben.</para>
        /// </summary>
        private void btnBhkwWirtschaftlichkeit_Click(object sender, EventArgs e)
        {
            if (BhkwWirtschaftlichkeitHuelle.Oeffnen(Besitzer, _idStamm, _ergebnisse))
            {
                ZeigeParameterzeile();
                Melde(T("BHW_MELD_GESPEICHERT",
                        "BHKW-Wirtschaftlichkeit gespeichert — bitte neu berechnen."));
            }
        }

        /// <summary>Titelzeile für den Dialog-Wrapper bzw. die Seitenüberschrift.</summary>
        public string Titel { get { return _titel; } }
        private string _titel = "";

        // -------------------------------------------------- Aufbau-Nachlauf

        /// <summary>
        /// Setzt alle sichtbaren Texte. Läuft direkt nach <c>InitializeComponent()</c> und
        /// ersetzt die dortigen Platzhalter. Die Texte sind (wie im Bestand) deutsche
        /// Literale — die Lokalisierung dieser Seite ist ein eigener Vorgang; hier steht
        /// nur, dass sie an genau einer Stelle liegen. <see cref="Titel"/> gehört dazu:
        /// Er ist zwar kein Steuerelementtext, wird aber vom Wirt als Fenster- bzw.
        /// Seitenüberschrift angezeigt.
        /// </summary>
        private void TexteSetzen()
        {
            _titel = "Wirtschaftlichkeit (Kapitalwertmethode DIN EN 17463) — Stamm: " + _stammName;
            lblVarianten.Text = "Vergleichsgruppe (Referenz: Stamm, fest gewählt):";
            colArt.Text = "Art";
            colBez.Text = "Bezeichner";
            colName.Text = "Projektname";
            colSim.Text = "Simulation";
            lblSzenario.Text = "Szenario:";
            // Ä16: Tarifstruktur (und Strom-Leistungspreis) werden im
            // Energieträgerdialog gepflegt — der Einstieg hier entfällt.
            // (Der PV-Knopf entsteht erst NACH TexteSetzen und nimmt den Platz
            // bei seiner Erzeugung ein — hier wäre er noch null.)
            btnTarif.Visible = false;
            btnParameter.Text = "Parameter…";
            btnVerlauf.Text = "Verlauf…";
            btnBerechnen.Text = "Berechnen";
            btnSchliessen.Text = "Schließen";   // im Reiter blendet SetBusy auf „Abbrechen" um
        }

        /// <summary>
        /// Füllt die Szenarioliste. Steht bewusst NICHT im Designer: Die drei Werte sind
        /// DB-Persistenzwerte (<c>Tab_ErgebnisWirtschaftlichkeit.Szenario</c>) und dürfen
        /// nicht als Literale in Designer-Code oder gar in eine <c>.resx</c> geraten.
        ///
        /// <para>Läuft unter dem <c>_initialisiere</c>-Wächter: Im Bestand wurde
        /// <c>SelectedIndexChanged</c> erst NACH <c>SelectedIndex = 0</c> angehängt, das
        /// Vorbelegen löste also kein <see cref="ZeigeErgebnisse"/> aus. Der Designer
        /// verdrahtet den Handler zwangsläufig vorher — der Wächter stellt denselben
        /// Zustand her.</para>
        /// </summary>
        private void SzenarienFuellen()
        {
            _initialisiere = true;
            try
            {
                cbSzenario.Items.AddRange(new object[]
                {
                    WirtschaftlichkeitSzenario.ERWARTET,
                    WirtschaftlichkeitSzenario.BEST,
                    WirtschaftlichkeitSzenario.WORST
                });
                cbSzenario.SelectedIndex = 0;
            }
            finally { _initialisiere = false; }
        }

        /// <summary>Umgebendes Formular als Dialog-Besitzer (im Reiter das Startformular).</summary>
        private IWin32Window Besitzer
        {
            get { Form f = this.FindForm(); return f != null ? (IWin32Window)f : this; }
        }

        // ------------------------------------------------------------- Laden

        /// <summary>
        /// Baut Liste und Kennzahlen auf. Wird vom Wrapper bzw. vom Reiter genau einmal
        /// gerufen; wiederholte Aufrufe sind wirkungslos (frühere Load-Ereigniskette).
        /// </summary>
        public void LadeDaten()
        {
            if (_geladen || this.DesignMode) return;
            _geladen = true;

            AktualisiereListe(false);
            ZeigeParameterzeile();

            // Persistierte Ergebnisse anzeigen, solange sie zum Simulationsstand passen.
            _ergebnisse = _ctrl.LadeErgebnisse(GewaehlteIds(true));
            bool veraltet = _ergebnisse.Count > 0 &&
                            _ergebnisse.Any(x => x.Fehlgrund == null && !_ctrl.ErgebnisAktuell(x));
            AktualisiereBilanzen();
            ZeigeErgebnisse();
            Melde(_ergebnisse.Count == 0
                ? "Noch keine Wirtschaftlichkeitsberechnung gespeichert — bitte „Berechnen“."
                : veraltet
                    ? "⚠ Gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand — bitte „Berechnen“."
                    : "Gespeicherte Ergebnisse vom " +
                      _ergebnisse[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm") + ".");
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            LadeDaten();
        }

        /// <summary>Variantenliste (neu) aufbauen; bewahreAuswahl = Häkchen erhalten.</summary>
        private void AktualisiereListe(bool bewahreAuswahl)
        {
            var abgewaehlt = new HashSet<int>();
            if (bewahreAuswahl)
                foreach (ListViewItem it in lvVarianten.Items)
                {
                    var alt = it.Tag as BerichtsDatenSammler.VariantenStatus;
                    if (alt != null && !it.Checked) abgewaehlt.Add(alt.IdProjekt);
                }

            _initialisiere = true;
            try
            {
                lvVarianten.Items.Clear();
                _namen.Clear();
                foreach (BerichtsDatenSammler.VariantenStatus st in
                         BerichtsDatenSammler.ErmittleStatus(_idStamm, _stammName))
                {
                    var it = new ListViewItem(new[]
                    {
                        st.IstStamm ? "Stamm" : "Variante",
                        st.IstStamm ? "(Stammprojekt)" : st.Variantenname,
                        st.Projektname,
                        st.SimStandText
                    });
                    it.Tag = st;
                    // Vorgabe: standardmäßig alle Varianten der Gruppe vergleichen.
                    it.Checked = st.IstStamm || !abgewaehlt.Contains(st.IdProjekt);
                    if (!st.SimStand.HasValue || st.Veraltet) it.ForeColor = Color.Firebrick;
                    lvVarianten.Items.Add(it);
                    _namen[st.IdProjekt] = st.IstStamm ? "Stamm"
                        : (string.IsNullOrEmpty(st.Variantenname) ? st.Projektname : st.Variantenname);
                }
            }
            finally { _initialisiere = false; }
        }

        private void ZeigeParameterzeile()
        {
            WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
            TarifParameter t = _ctrl.LadeTarif(_idStamm);
            // LEITENTSCHEIDUNGEN L12/L13 — der AUSWEIS der Bilanzierungsregeln steht
            // neben dem Parameternachweis, nicht in ihm: Er hat eine eigene Herkunft
            // (Gesetzeskatalog statt Projektzeile) und eigene Lokalisierung.
            lblParameter.Text = "Parameter: " + p.Nachweis(BerichtTexte.Kultur) +
                                " · Referenz: Stammprojekt · Restwert linear · " +
                                t.Nachweis(BerichtTexte.Kultur) + " · " +
                                BilanzKonvention.Bestimme(p, new GesetzKatalog())
                                                .Ausweis(BerichtTexte.Kultur);
        }

        // ------------------------------------------------------------- Ereignisse

        private void lvVarianten_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_initialisiere) return;
            var st = lvVarianten.Items[e.Index].Tag as BerichtsDatenSammler.VariantenStatus;
            if (st != null && st.IstStamm && e.NewValue != CheckState.Checked)
            {
                e.NewValue = CheckState.Checked;
                Melde("Das Stammprojekt ist die Referenz und immer enthalten.");
            }
        }

        /// <summary>
        /// Szenariowechsel — zeigt die Kennzahlen des gewählten Szenarios. Im Bestand ein
        /// Lambda in <c>InitializeComponent()</c>; als benannte Methode, weil der
        /// Designer-Parser Lambdas in <c>InitializeComponent()</c> nicht liest.
        /// </summary>
        private void cbSzenario_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initialisiere) return;   // Vorbelegen aus SzenarienFuellen()
            ZeigeErgebnisse();
        }

        private void btnTarif_Click(object sender, EventArgs e)
        {
            // iU9-W2.3: siehe TarifSichtOeffnen.
            if (TarifstrukturHuelle.Oeffnen(Besitzer, _idStamm))
            {
                _tarifCache = null;   // E7: Beschriftung der Stromkostenzeile neu holen
                ZeigeParameterzeile();
                Melde("Tarifstruktur gespeichert — bitte neu berechnen.");
            }
        }

        private void btnParameter_Click(object sender, EventArgs e)
        {
            // iU9-W2.5: der Parameterdialog als Razor-Komponente ueber
            // WirtschaftlichkeitParameterHuelle; Form_WirtschaftlichkeitParameter
            // ist geloescht (Regel M1).
            if (WirtschaftlichkeitParameterHuelle.Oeffnen(Besitzer, _idStamm))
            {
                ZeigeParameterzeile();
                Melde("Parameter gespeichert — bitte neu berechnen.");
            }
        }

        private void btnVerlauf_Click(object sender, EventArgs e)
        {
            // Kapitalwert-Verlauf (Phase 11): Zeitraum frei wählbar (auch > T).
            var variantenIds = new List<int>();
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                if (st != null && !st.IstStamm && it.Checked) variantenIds.Add(st.IdProjekt);
            }
            // iU9-W1.6: der Verlaufsdialog als Razor-Komponente über
            // KapitalwertVerlaufHuelle; Form_WirtschaftlichkeitVerlauf ist im
            // selben Schritt gelöscht (Regel M1).
            {
                bool datenNeuGesammelt =
                    KapitalwertVerlaufHuelle.Oeffnen(Besitzer, _idStamm, _stammName, variantenIds);

                // Der Verlaufsdialog kann neu simuliert haben (Stundenreihen) — dann
                // passen die persistierten Ergebnisse nicht mehr zum Simulationsstand
                // (Review Phase 11): Anzeige auffrischen und offen darauf hinweisen.
                if (datenNeuGesammelt)
                {
                    AktualisiereListe(true);
                    _ergebnisse = _ctrl.LadeErgebnisse(GewaehlteIds(true));
                    AktualisiereBilanzen();
                    ZeigeErgebnisse();
                    if (_ergebnisse.Any(x => x.Fehlgrund == null && !_ctrl.ErgebnisAktuell(x)))
                        Melde("⚠ Für den Verlauf wurde neu simuliert — gespeicherte Ergebnisse " +
                              "passen nicht mehr zum Simulationsstand, bitte „Berechnen“.");
                }
            }
        }

        private void btnSchliessen_Click(object sender, EventArgs e)
        {
            if (_cts != null) { _cts.Cancel(); return; }
            EventHandler h = SchliessenAngefordert;
            if (h != null) h(this, EventArgs.Empty);
        }

        /// <summary>true, solange ein Berechnungslauf aussteht (Wrapper darf dann nicht schließen).</summary>
        public bool Beschaeftigt { get { return _cts != null; } }

        /// <summary>Bricht einen laufenden Berechnungslauf ab (Wrapper beim Schließen).</summary>
        public void Abbrechen()
        {
            if (_cts != null) _cts.Cancel();
        }

        // ------------------------------------------------------------- Berechnen

        private List<int> GewaehlteIds(bool mitStamm)
        {
            var ids = new List<int>();
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                if (st == null || !it.Checked) continue;
                if (st.IstStamm && !mitStamm) continue;
                ids.Add(st.IdProjekt);
            }
            return ids;
        }

        private async void btnBerechnen_Click(object sender, EventArgs e)
        {
            if (_cts != null) return;

            var variantenIds = new List<int>();
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                if (st != null && !st.IstStamm && it.Checked) variantenIds.Add(st.IdProjekt);
            }

            _cts = new CancellationTokenSource();
            SetBusy(true);
            var melder = new Progress<BerichtsDatenSammler.Fortschritt>(f =>
            {
                if (f.Gesamt > 0)
                {
                    progress.Maximum = f.Gesamt;
                    progress.Value = Math.Min(f.Aktuell, f.Gesamt);
                }
                Melde(string.Format("({0}/{1}) {2}", f.Aktuell, f.Gesamt, f.Text));
            });

            try
            {
                CancellationToken ct = _cts.Token;
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                TarifParameter tarif = _ctrl.LadeTarif(_idStamm);

                // W3: Tarifmatrix und KWKG-Split brauchen Stundenreihen — dann wird
                // je Projekt frisch in-memory simuliert (wie beim Ganglinien-Bericht).
                bool mitZeitreihen = tarif.Aktiv || p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0;

                // Prüfkette (Konzept Kap. 6, Punkt 2): fehlende/veraltete Simulations-
                // ergebnisse rechnet der Sammler automatisch headless nach.
                _ergebnisse = await Task.Run(() =>
                {
                    BerichtsDaten daten = new BerichtsDatenSammler().Sammle(
                        _idStamm, _stammName, variantenIds,
                        false, mitZeitreihen, melder, ct);
                    return _ctrl.Berechne(daten, p);
                }, ct);

                AktualisiereListe(true);      // Simulationsstände auffrischen, Auswahl erhalten
                ZeigeParameterzeile();
                AktualisiereBilanzen();
                ZeigeErgebnisse();            // frisch berechnete Ergebnisse anzeigen
                Melde("Berechnet am " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                      " — Ergebnisse gespeichert (Basis für den Berichts-Baustein Wirtschaftlichkeit).");
            }
            catch (OperationCanceledException) { Melde("Vorgang abgebrochen."); }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der Wirtschaftlichkeitsberechnung: " + ex.Message,
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                SetBusy(false);
            }
        }

        // ------------------------------------------------------------- Anzeige

        /// <summary>
        /// KD6a (Nutzerabnahme 26.08.2026): Die Wirtschaftlichkeitsübersicht bekommt
        /// die Kartensprache der Kosten-Seite — vier Kennzahl-Kacheln über der
        /// Vergleichstabelle. Reine ANZEIGE der bereits berechneten Ergebniswerte
        /// (beste Variante gegenüber Stamm im gewählten Szenario).
        /// </summary>
        private void KachelnBauen()
        {
            var pnl = new TableLayoutPanel
            {
                Location = new System.Drawing.Point(12, 194),
                Size = new System.Drawing.Size(876, 58),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 4,
                RowCount = 1
            };
            for (int i = 0; i < 4; i++)
                pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _kKapitalwert = new UcBkKosten.Kachel();
            _kAnnuitaet = new UcBkKosten.Kachel();
            _kAmortisation = new UcBkKosten.Kachel();
            _kIrr = new UcBkKosten.Kachel();
            _kKapitalwert.Setze(T("WIRT_KACHEL_KW", "Kapitalwert ggue. Stamm"), "");
            _kAnnuitaet.Setze(T("WIRT_KACHEL_ANNUITAET", "Annuität"), "");
            _kAmortisation.Setze(T("WIRT_KACHEL_AMORTISATION", "Amortisation"), "");
            _kIrr.Setze(T("WIRT_KACHEL_IRR", "Interner Zinsfuß"), "");
            pnl.Controls.Add(_kKapitalwert, 0, 0);
            pnl.Controls.Add(_kAnnuitaet, 1, 0);
            pnl.Controls.Add(_kAmortisation, 2, 0);
            pnl.Controls.Add(_kIrr, 3, 0);
            Controls.Add(pnl);
        }

        private void KachelnAktualisieren(List<WirtschaftlichkeitErgebnis> zeilen)
        {
            var kultur = BerichtTexte.Kultur;
            WirtschaftlichkeitErgebnis beste = null;
            foreach (WirtschaftlichkeitErgebnis x in zeilen)
                if (!x.IstStamm && x.KapitalwertDiff.HasValue &&
                    (beste == null || x.KapitalwertDiff.Value > beste.KapitalwertDiff.Value))
                    beste = x;

            if (beste != null)
            {
                string name = _namen.ContainsKey(beste.IdProjekt)
                    ? _namen[beste.IdProjekt] : beste.Anzeige;
                string quelle = string.Format(T("WIRT_KACHEL_BESTE", "beste Variante: {0}"), name);
                _kKapitalwert.Wert = beste.KapitalwertDiff.Value.ToString("N0", kultur) + " €";
                _kKapitalwert.Quelle = quelle;
                _kAnnuitaet.Wert = beste.AnnuitaetKW.HasValue
                    ? beste.AnnuitaetKW.Value.ToString("N0", kultur) + " €/a" : "—";
                _kAnnuitaet.Quelle = quelle;
                _kAmortisation.Wert = beste.AmortisationJahre.HasValue
                    ? beste.AmortisationJahre.Value.ToString("N1", kultur) + " a"
                    : T("WIRT_KACHEL_KEINE", "keine");
                _kAmortisation.Quelle = quelle;
                _kIrr.Wert = beste.IRR.HasValue
                    ? beste.IRR.Value.ToString("N1", kultur) + " %" : "—";
                _kIrr.Quelle = quelle;
                return;
            }

            WirtschaftlichkeitErgebnis stamm = zeilen.Find(x => x.IstStamm);
            string q = T("WIRT_KACHEL_NUR_STAMM", "nur Stammprojekt gerechnet");
            _kKapitalwert.Wert = stamm != null && stamm.Kapitalwert.HasValue
                ? stamm.Kapitalwert.Value.ToString("N0", kultur) + " €" : "—";
            _kKapitalwert.Quelle = stamm != null
                ? T("WIRT_KACHEL_STAMM_KW", "Nettobarwert des Stammprojekts") : "";
            _kAnnuitaet.Wert = "—"; _kAnnuitaet.Quelle = q;
            _kAmortisation.Wert = "—"; _kAmortisation.Quelle = q;
            _kIrr.Wert = "—"; _kIrr.Quelle = q;
        }

        private static string T(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        private void ZeigeErgebnisse()
        {
            string szenario = cbSzenario.SelectedItem as string ?? WirtschaftlichkeitSzenario.ERWARTET;
            var kultur = BerichtTexte.Kultur;

            grid.Columns.Clear();
            grid.Rows.Clear();

            List<WirtschaftlichkeitErgebnis> zeilen = _ergebnisse
                .Where(x => x.Szenario == szenario)
                .OrderByDescending(x => x.IstStamm)
                .ToList();
            KachelnAktualisieren(zeilen);
            if (zeilen.Count == 0) return;

            grid.Columns.Add("kennzahl", "Kennzahl");
            grid.Columns[0].FillWeight = 190;
            foreach (WirtschaftlichkeitErgebnis erg in zeilen)
            {
                string name = _namen.ContainsKey(erg.IdProjekt) ? _namen[erg.IdProjekt]
                            : (erg.IstStamm ? "Stamm" : erg.Anzeige);
                int idx = grid.Columns.Add("p" + erg.IdProjekt, name);
                grid.Columns[idx].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                grid.Columns[idx].FillWeight = 110;
            }

            // ETAPPE E7: EINE Zeilendefinition für Reiter, Word und Excel
            // (WirtschaftlichkeitZeilen). Bis dahin stand dieselbe Liste dreimal im
            // Code — hier, im Word-Baustein und im Excel-Generator.
            //
            // Die SICHTBARKEIT entscheidet sich über ALLE Ergebnisse der Gruppe (alle
            // Szenarien), nicht über das gerade angezeigte — sonst zeigten Reiter und
            // Bericht verschiedene Tabellen. Eine Zeile, die im angezeigten Szenario
            // nirgends einen Wert hat, fällt darunter trotzdem weg.
            if (_tarifCache == null)
            {
                try { _tarifCache = _ctrl.LadeTarif(_idStamm); }
                catch { _tarifCache = new TarifParameter(); }
            }
            foreach (WirtZeile z in WirtschaftlichkeitZeilen.Kennzahlen(_ergebnisse, _tarifCache))
            {
                bool hatWert = zeilen.Any(x => z.IstText
                    ? !string.IsNullOrEmpty(z.Text(x))
                    : (x.IstStamm && z.StammAnzeige != null) || (z.Wert != null && z.Wert(x).HasValue));
                if (!hatWert) continue;
                Zeile(z.Titel, zeilen, x => z.Anzeige(x, kultur));
            }

            // W3: CO₂-Vermeidung gegenüber getrennter Erzeugung (aus dem Cache;
            // nur für Projekte, deren Wirtschaftlichkeits-Ergebnis zum aktuellen
            // Simulationslauf passt — sonst „—", Review Phase 8).
            // E5/F7: Der Zeilentitel nennt den Modus, in dem die Bilanz gerechnet wurde.
            // Bei uneinheitlichen Projekten trägt die Zeile den Sammeltitel — die
            // Spalten stünden sonst unter einer Methode, in der sie nicht entstanden.
            if (_bilanzen.Values.Any(x => x != null && x.CO2VermeidungT.HasValue))
                Zeile(EmissionsAusweis.BilanzVermeidung(
                          EmissionsAusweis.ModusAusBilanzen(_bilanzen.Values)), zeilen, x =>
                {
                    EmissionsBilanz b = _bilanzen.ContainsKey(x.IdProjekt) ? _bilanzen[x.IdProjekt] : null;
                    return b == null ? "—" : W(b.CO2VermeidungT, "N1", kultur);
                });

            // Hinweiszeilen (nicht-fatal W3 / unvollständige Rechnungen).
            if (zeilen.Any(x => x.Hinweis != null))
                Zeile("Hinweis", zeilen, x => x.Hinweis != null ? "⚠ " + x.Hinweis : "");
            if (zeilen.Any(x => x.Fehlgrund != null))
                Zeile("Hinweis", zeilen, x => x.Fehlgrund != null ? "⚠ " + x.Fehlgrund : "");

            KohaerenzZeilen(zeilen);

            grid.ClearSelection();
        }

        /// <summary>
        /// ETAPPE B2 (Konzept BHKW-Wirtschaftlichkeit § 4.1, BW2/BF2) — die Zeilen der
        /// Kohärenzprüfung als eigene Gridzeilen.
        ///
        /// <para><b>Je Hinweis eine Zeile</b>, nicht alle in einer verketteten Zelle: Die
        /// Sätze tragen Beträge, und ein Einzeiler mit drei Beträgen ist nicht lesbar —
        /// derselbe Grund, aus dem E7 die KWKG-Modulaufzählung aus dem Hinweisfeld
        /// geholt hat. Die Zeilenzahl richtet sich nach dem Projekt mit den meisten
        /// Hinweisen; kürzere Spalten bleiben leer.</para>
        ///
        /// <para><b>Nur nach „Berechnen".</b> Die Liste ist nicht persistiert (wie
        /// <c>KwkgModule</c> und <c>Betriebskosten</c>); ein aus der Datenbank geladener
        /// Stand zeigt deshalb keine Kohärenzzeilen. Das ist gewollt — sie gehören zum
        /// Lauf, nicht zum gespeicherten Ergebnis.</para>
        /// </summary>
        private void KohaerenzZeilen(List<WirtschaftlichkeitErgebnis> zeilen)
        {
            int hoechste = 0;
            foreach (WirtschaftlichkeitErgebnis x in zeilen)
                if (x.KohaerenzHinweise != null && x.KohaerenzHinweise.Count > hoechste)
                    hoechste = x.KohaerenzHinweise.Count;
            if (hoechste == 0) return;

            string titel = T("KOH_ZEILE_TITEL", "Kohärenzprüfung");
            for (int i = 0; i < hoechste; i++)
            {
                int index = i;               // Kopie für den Abschluss
                Zeile(titel, zeilen, x =>
                {
                    List<KohaerenzHinweis> l = x.KohaerenzHinweise;
                    if (l == null || index >= l.Count) return "";
                    KohaerenzHinweis h = l[index];
                    string marke = string.Equals(h.Schwere, KohaerenzSchwere.WARNUNG,
                                                 StringComparison.Ordinal) ? "⚠ " : "· ";
                    return marke + h.Text;
                });
            }
        }

        /// <summary>Emissionsbilanz-Cache neu füllen (nur aktuelle Ergebnisse, W3).</summary>
        private void AktualisiereBilanzen()
        {
            _bilanzen.Clear();
            _parameterCache = _ctrl.LadeParameter(_idStamm);
            if (_parameterCache.IdKraftwerkspark <= 0) return;
            foreach (WirtschaftlichkeitErgebnis erg in _ergebnisse
                     .Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET))
            {
                if (_bilanzen.ContainsKey(erg.IdProjekt)) continue;
                _bilanzen[erg.IdProjekt] = _ctrl.ErgebnisAktuell(erg)
                    ? EmissionsBilanzRechner.Berechne(erg.IdProjekt, _parameterCache)
                    : null;
            }
        }

        private void Zeile(string label, List<WirtschaftlichkeitErgebnis> zeilen,
                           Func<WirtschaftlichkeitErgebnis, string> wert)
        {
            var werte = new List<object> { label };
            foreach (WirtschaftlichkeitErgebnis erg in zeilen) werte.Add(wert(erg));
            int idx = grid.Rows.Add(werte.ToArray());
            grid.Rows[idx].Cells[0].Style.Font = new Font(grid.Font, FontStyle.Bold);
        }

        private static string W(double? v, string format, System.Globalization.CultureInfo kultur)
        { return v.HasValue ? v.Value.ToString(format, kultur) : "—"; }

        private void SetBusy(bool busy)
        {
            progress.Visible = busy;
            if (!busy) progress.Value = 0;
            lvVarianten.Enabled = !busy;
            cbSzenario.Enabled = !busy;
            btnTarif.Enabled = !busy;
            if (btnPhotovoltaik != null) btnPhotovoltaik.Enabled = !busy;
            if (btnBhkwTarif != null) btnBhkwTarif.Enabled = !busy;
            if (btnStromTarif != null) btnStromTarif.Enabled = !busy;
            btnParameter.Enabled = !busy;
            btnVerlauf.Enabled = !busy;
            btnBerechnen.Enabled = !busy;
            // Der Knopf dient allein dem Abbrechen; ausserhalb eines Laufs ist er weg.
            btnSchliessen.Visible = busy;
            btnSchliessen.Text = busy ? "Abbrechen" : "Schließen";
            this.UseWaitCursor = busy;
        }

        private void Melde(string text) { lblStatus.Text = text ?? ""; }
    }
}
