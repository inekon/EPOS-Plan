using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Variantenvergleich des Stromspeichers (AP9) nach Fachkonzept 7.3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was die Maske zeigt.</b> Je <c>SP_TYP</c>-Anlagenzeile des Projekts eine
    /// Zeile mit den elf Groessen aus 7.3 — Bezeichnung, Betriebsart, Berechnungsart,
    /// Kapazitaet, Leistung, Investition, Ertrag E_a,aeq, dJ, Amortisation,
    /// Kapitalwert, Vollzyklen — dazu die Kennzeichnung der aktiven Variante. Die beste
    /// Variante nach dJ ist farblich hervorgehoben.
    /// </para>
    /// <para>
    /// <b>Ein Rechenweg, keine dritte Welt.</b> Jede Zeile entsteht aus genau demselben
    /// Aufruf, den auch die Ergebnisseite und der gespeicherte Ergebnissatz benutzen:
    /// <c>StromspeicherSimCtrl.RechneVariante</c> auf dem <b>bereits gerechneten</b>
    /// Simulationsobjekt, abgebildet ueber <c>StromspeicherSimCtrl.AlsErgebnismodell</c>.
    /// Deshalb sitzt der Einstieg auf der Speicher-Ergebnisseite in
    /// <c>Form_Simulation_Detail</c> und nicht am Kontextmenue des Hauptformulars: Dort
    /// gibt es das <c>sim</c>-Objekt, hier gaebe es nur die Wahl zwischen einem zweiten
    /// Rechenpfad und einem stillen Simulationslauf. Das Kontextmenue fuehrt statt dessen
    /// einen gesperrten Wegweiser (<c>SpKontextMenuCtrl</c>).
    /// </para>
    /// <para>
    /// <b>dJ</b> ist der Jahresueberschuss nach Kapitaldienst,
    /// <c>dJ = E_a,aeq − I·a(i_z, N)</c> — dieselbe Groesse, die die
    /// Auslegungsoptimierung (AP8) maximiert und die
    /// <c>WirtschaftlichkeitErgebnis.JahresueberschussEur</c> fuehrt. Ein Vergleich nach
    /// Amortisationszeit waere die naheliegende, aber falsche Wahl: Sie ignoriert die
    /// Nutzungsdauer und bevorzugt systematisch den kleinsten Speicher.
    /// </para>
    /// <para>
    /// <b>Fehlertoleranz.</b> Eine Variante, die nicht rechenbar ist (kein Geraet,
    /// Kapazitaet 0, Ausnahme in der Engine), bekommt eine Zeile mit dem Vermerk
    /// „nicht rechenbar" und einen Eintrag im Protokollfeld — der Vergleich der uebrigen
    /// laeuft weiter. Ein Abbruch waere hier das schlechtere Verhalten: Meist ist genau
    /// die unvollstaendige Variante der Grund, warum jemand den Vergleich oeffnet.
    /// </para>
    /// <para>
    /// <b>Aufbau.</b> Layout und Steuerelemente stehen in
    /// <c>Form_SpeicherVariantenVergleich.Designer.cs</c>; eine <c>.resx</c> gibt es
    /// bewusst nicht (<c>Localizable = false</c>). Alle Beschriftungen kommen aus
    /// <c>MyResource</c> (<c>VAR_*</c>, <c>OPT_*</c>) und werden in
    /// <see cref="TexteSetzen"/> gesetzt — die Designer-Datei traegt an ihrer Stelle nur
    /// Platzhalter, damit ein Designer-Speichern die zweisprachigen Texte nicht
    /// einfriert. Zahlen werden mit <c>CultureInfo.CurrentCulture</c> angezeigt, die
    /// CSV-Ausgabe folgt den Konventionen des Hauses (Semikolon, Dezimalkomma, UTF-8
    /// mit BOM).
    /// </para>
    /// </remarks>
    public partial class Form_SpeicherVariantenVergleich : Form
    {
        // ==================================================================
        // Zustand
        // ==================================================================

        private readonly SimulationControl m_Sim;
        private readonly int m_ID_Projekt;

        private readonly List<Vergleichszeile> m_Zeilen = new List<Vergleichszeile>();
        private long m_DauerMs;

        /// <summary>
        /// <c>true</c>, wenn der Anwender hier die aktive Variante umgestellt hat — dann
        /// muss die aufrufende Seite ihre Parameteranzeige auffrischen
        /// (<c>LeseSpeicherVariante</c>).
        /// </summary>
        public bool AktiveVarianteGeaendert { get; private set; }

        // --- Steuerelemente ---
        // Deklariert in Form_SpeicherVariantenVergleich.Designer.cs.

        /// <summary>
        /// Fettschrift der aktiven Zeile — EINMAL erzeugt. Ein <c>new Font(...)</c> je
        /// Zeile und je Auffrischung liesse GDI-Handles zurueck, die niemand freigibt.
        /// Freigegeben wird sie im <c>Dispose</c> der Designer-Datei: Steuerelemente
        /// raeumt die Basisklasse ab, eine lose <see cref="Font"/> nicht.
        /// </summary>
        private Font m_SchriftAktiv;

        // Spaltenindizes der Tabelle - einmal benannt statt zwoelfmal gezaehlt.
        private const int SP_AKTIV = 0;
        private const int SP_BEZEICHNUNG = 1;
        private const int SP_BETRIEBSART = 2;
        private const int SP_BERECHNUNGSART = 3;
        private const int SP_KAPAZITAET = 4;
        private const int SP_LEISTUNG = 5;
        private const int SP_INVESTITION = 6;
        private const int SP_ERTRAG = 7;
        private const int SP_DELTAJ = 8;
        private const int SP_AMORTISATION = 9;
        private const int SP_NPV = 10;
        private const int SP_VOLLZYKLEN = 11;

        /// <summary>Hinterlegung der besten Variante nach dJ — hell genug, dass die Schrift lesbar bleibt.</summary>
        private static readonly Color FARBE_BESTE = Color.FromArgb(222, 241, 222);

        /// <summary>Eine Zeile der Vergleichstabelle: ein Lauf einer Speichervariante.</summary>
        private sealed class Vergleichszeile
        {
            public int ID_Energieanlage;
            public int ID_Variante;
            public string Bezeichnung = "";
            public bool Aktiv;

            /// <summary><c>false</c> = nicht rechenbar; dann traegt nur <see cref="Hinweis"/> Inhalt.</summary>
            public bool Gerechnet;

            public string Betriebsart = "";
            public string Berechnungsart = "";

            public double KapazitaetKwh;
            public double LeistungKw;
            public double InvestitionEur;
            public double ErtragAequivalentEur;
            public double DeltaJEur;
            public double KapitalwertEur;
            public double Vollzyklen;
            public string AmortisationText = "";

            public string Hinweis = "";
        }

        // ==================================================================
        // Aufbau
        // ==================================================================

        /// <summary>
        /// Baut den Vergleich zu einer bereits gerechneten Simulation.
        /// </summary>
        /// <param name="sim">
        /// Simulationsobjekt der aufrufenden Seite. Ohne gerechneten Lauf bleibt die
        /// Tabelle leer und die Statuszeile sagt, warum.
        /// </param>
        /// <param name="idProjekt">Projekt-ID.</param>
        public Form_SpeicherVariantenVergleich(SimulationControl sim, int idProjekt)
        {
            m_Sim = sim;
            m_ID_Projekt = idProjekt;

            // Die Designer-Datei setzt AutoScaleMode BEWUSST auf None. Die Anwendung
            // laeuft DpiUnaware (app.manifest, Application.SetHighDpiMode in Program.cs),
            // und die handgebaute Fassung dieser Maske hatte AutoScaleMode.Font OHNE
            // AutoScaleDimensions — also faktisch keine Skalierung. None haelt genau
            // dieses Verhalten fest; Font mit einer vom Designer beim naechsten Speichern
            // ergaenzten Baseline wuerde die Skalierung nachtraeglich scharfschalten.
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteSetzen();

            // Erst nach InitializeComponent: die Fettschrift leitet sich aus der dort
            // gesetzten Listenschrift ab.
            m_SchriftAktiv = new Font(list_Varianten.Font, FontStyle.Bold);

            if (!LaufVorhanden())
            {
                lbl_Status.Text = MyResource.Resource.VAR_VGL_KEIN_LAUF;
                lbl_Status.ForeColor = Color.Firebrick;
                btn_Aktiv.Enabled = false;
                btn_Csv.Enabled = false;
                return;
            }

            VariantenRechnen();
            TabelleFuellen();
        }

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>.
        /// </summary>
        /// <remarks>
        /// Getrennt von <c>InitializeComponent</c>, weil die Designer-Datei nur
        /// Konstanten vertraegt: Ein Designer-Speichern wuerde jeden dort stehenden
        /// Ressourcenzugriff durch den zuletzt angezeigten Text ersetzen und die Maske
        /// damit einsprachig machen. In der Designer-Datei stehen an diesen Stellen
        /// Platzhalter (der jeweilige Feldname).
        /// </remarks>
        private void TexteSetzen()
        {
            Text = MyResource.Resource.VAR_VGL_TITEL;

            col_Aktiv.Text = MyResource.Resource.VAR_VGL_SP_AKTIV;
            col_Bezeichnung.Text = MyResource.Resource.VAR_VGL_SP_BEZEICHNUNG;
            col_Betriebsart.Text = MyResource.Resource.VAR_VGL_SP_BETRIEBSART;
            col_Berechnungsart.Text = MyResource.Resource.VAR_VGL_SP_BERECHNUNGSART;
            col_Kapazitaet.Text = MyResource.Resource.VAR_VGL_SP_KAPAZITAET;
            col_Leistung.Text = MyResource.Resource.VAR_VGL_SP_LEISTUNG;
            col_Investition.Text = MyResource.Resource.VAR_VGL_SP_INVESTITION;
            col_Ertrag.Text = MyResource.Resource.VAR_VGL_SP_ERTRAG;
            col_DeltaJ.Text = MyResource.Resource.VAR_VGL_SP_DELTAJ;
            col_Amortisation.Text = MyResource.Resource.VAR_VGL_SP_AMORTISATION;
            col_Npv.Text = MyResource.Resource.VAR_VGL_SP_NPV;
            col_Vollzyklen.Text = MyResource.Resource.VAR_VGL_SP_VOLLZYKLEN;

            lbl_Legende.Text = MyResource.Resource.VAR_VGL_LEGENDE;

            // AP9b: Der frühere Dauerhinweis („der Gesamtlauf summiert alle Anlagen")
            // beschrieb einen Befund, der behoben ist — die Gesamtsimulation rechnet die
            // aktive Variante. Stehen bleibt die Warnung für den EINEN Fall, in dem der
            // alte Satz noch zutrifft: Ist keine Variante aktiv, fällt der Gesamtlauf auf
            // die Aggregation zurück. Ein roter Dauerhinweis wäre jetzt irreführend, ein
            // ersatzloses Streichen ließe genau diesen Fall unkommentiert. Deshalb steht
            // das Feld in der Designer-Datei auf Visible = false und wird erst in
            // TabelleFuellen eingeblendet, wenn keine Variante aktiv ist.
            lbl_Hinweis.Text = MyResource.Resource.VAR_VGL_HINWEIS_KEINE_AKTIVE;

            lbl_Protokollkopf.Text = MyResource.Resource.VAR_VGL_PROTOKOLL;

            btn_Aktiv.Text = MyResource.Resource.VAR_VGL_BTN_AKTIV;
            btn_Csv.Text = MyResource.Resource.OPT_BTN_CSV;
            btn_Schliessen.Text = MyResource.Resource.OPT_BTN_SCHLIESSEN;
        }

        /// <summary>
        /// Ob ueberhaupt gerechnet werden kann: Der Vergleich liest Lastgang und
        /// Erzeugungsreihen aus einem <b>gelaufenen</b> Simulationsdurchgang — dieselbe
        /// Bedingung wie in <c>Form_SpeicherOptimierung.LaufMoeglich</c>.
        /// </summary>
        private bool LaufVorhanden()
        {
            return m_Sim != null && m_Sim.simulation_Strombedarf != null;
        }

        // ==================================================================
        // Rechnen
        // ==================================================================

        /// <summary>
        /// Rechnet jede Speichervariante des Projekts einmal auf dem vorliegenden
        /// Simulationslauf.
        /// </summary>
        /// <remarks>
        /// Die Reihenfolge ist die der Anlagenliste (<c>ORDER BY ID</c>) — dieselbe, die
        /// die Uebersicht im Hauptformular und <c>ReadAllByProjekt</c> benutzen. Damit
        /// steht eine Variante in allen drei Ansichten an derselben Stelle.
        /// </remarks>
        private void VariantenRechnen()
        {
            m_Zeilen.Clear();
            List<string> protokoll = new List<string>();

            Dictionary<int, StromspeicherVarianteModel> varianten = VariantenLesen();
            List<WErzeugerCtrl.AnlagenZeile> anlagen = AnlagenLesen();

            if (anlagen == null || anlagen.Count == 0)
            {
                lbl_Status.Text = MyResource.Resource.VAR_VGL_STATUS_LEER;
                lbl_Status.ForeColor = Color.Firebrick;
                return;
            }

            Stopwatch uhr = Stopwatch.StartNew();
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                foreach (WErzeugerCtrl.AnlagenZeile r in anlagen)
                {
                    Vergleichszeile z = ZeileRechnen(r, varianten, protokoll);
                    m_Zeilen.Add(z);
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                uhr.Stop();
            }

            m_DauerMs = uhr.ElapsedMilliseconds;
            tb_Protokoll.Text = string.Join(Environment.NewLine, protokoll.ToArray());
        }

        /// <summary>Ein Lauf. Wirft nicht — ein Fehlschlag wird zur Fehlerzeile mit Protokolleintrag.</summary>
        private Vergleichszeile ZeileRechnen(WErzeugerCtrl.AnlagenZeile r,
                                             Dictionary<int, StromspeicherVarianteModel> varianten,
                                             List<string> protokoll)
        {
            int idAnlage = r.Id;
            string name = r.Bezeichner ?? "";

            Vergleichszeile z = new Vergleichszeile
            {
                ID_Energieanlage = idAnlage,
                Bezeichnung = name
            };

            // Betriebsart und Berechnungsart stehen in der Variantenzeile und sind damit
            // auch dann bekannt, wenn der Lauf scheitert - die Fehlerzeile bleibt
            // aussagekraeftig.
            StromspeicherVarianteModel v;
            if (varianten.TryGetValue(idAnlage, out v) && v != null)
            {
                z.ID_Variante = v.ID;
                z.Aktiv = v.Aktiv;
                z.Betriebsart = BetriebsartText(v.Betriebsart);
                z.Berechnungsart = BerechnungsartText(v.Berechnungsart);
            }

            StromspeicherSimCtrl ctrl = new StromspeicherSimCtrl();
            SpeicherErgebnis ergebnis;

            try
            {
                ergebnis = ctrl.RechneVariante(m_Sim, m_ID_Projekt, idAnlage);
            }
            catch (Exception ex)
            {
                z.Hinweis = ex.Message;
                protokoll.Add(string.Format(MyResource.Resource.VAR_VGL_PROTOKOLL_ZEILE, name, ex.Message));
                return z;
            }

            if (ergebnis == null)
            {
                z.Hinweis = string.IsNullOrEmpty(ctrl.LetzterHinweis)
                    ? MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER
                    : ctrl.LetzterHinweis;
                protokoll.Add(string.Format(MyResource.Resource.VAR_VGL_PROTOKOLL_ZEILE, name, z.Hinweis));
                return z;
            }

            StromspeicherLaufKontext kontext = ctrl.LetzterKontext;
            ErgebnisStromspeicherModel k = StromspeicherSimCtrl.AlsErgebnismodell(ergebnis, kontext);

            z.Gerechnet = true;
            z.KapazitaetKwh = kontext != null ? kontext.Parameter.CNomKwh : 0.0;
            z.LeistungKw = kontext != null ? kontext.Parameter.PKw : 0.0;
            z.InvestitionEur = k.Investition;
            z.ErtragAequivalentEur = k.Ertrag_Aequivalent;
            z.DeltaJEur = k.Jahresueberschuss;          // dJ = E_a,aeq − Annuitaet
            z.KapitalwertEur = k.Kapitalwert;
            z.Vollzyklen = k.Vollzyklen;
            z.AmortisationText = AmortisationText(ergebnis.Wirtschaftlichkeit.StatischeAmortisation);

            // Die Betriebsfuehrung aus dem LAUF ueberschreibt die aus der Datenbank -
            // sie ist dieselbe, aber so bleibt die Anzeige an dem haengen, was wirklich
            // gerechnet wurde.
            if (!string.IsNullOrEmpty(k.Betriebsart)) z.Betriebsart = BetriebsartText(k.Betriebsart);
            if (!string.IsNullOrEmpty(k.Berechnungsart)) z.Berechnungsart = BerechnungsartText(k.Berechnungsart);

            // Hinweise eines GELUNGENEN Laufs (fehlende Leistungsangabe, SoC-Band,
            // Preisrueckfall) gehoeren ebenfalls ins Protokoll - sie erklaeren
            // Unterschiede zwischen zwei Zeilen.
            if (!string.IsNullOrEmpty(ctrl.LetzterHinweis))
                protokoll.Add(string.Format(MyResource.Resource.VAR_VGL_PROTOKOLL_ZEILE,
                                            name, ctrl.LetzterHinweis.Replace(Environment.NewLine, "  ")));

            return z;
        }

        /// <summary>
        /// Alle <c>SP_TYP</c>-Anlagenzeilen des Projekts in Anlagenreihenfolge.
        /// Seit iU9-W11a.2 im Kern (<c>WErzeugerCtrl.AnlagenJeTyp</c>) — dieselbe
        /// Abfrage bediente auch <c>Form_Simulation_Detail.SpVariantenzahl</c>.
        /// </summary>
        private List<WErzeugerCtrl.AnlagenZeile> AnlagenLesen()
        {
            return WErzeugerCtrl.AnlagenJeTyp(m_ID_Projekt, WizardItemClass.SP_TYP);
        }

        private Dictionary<int, StromspeicherVarianteModel> VariantenLesen()
        {
            Dictionary<int, StromspeicherVarianteModel> treffer =
                new Dictionary<int, StromspeicherVarianteModel>();

            try
            {
                foreach (StromspeicherVarianteModel v in
                         new StromspeicherVarianteCtrl().ReadAllByProjekt(m_ID_Projekt))
                    if (v.ID_Energieanlage > 0 && !treffer.ContainsKey(v.ID_Energieanlage))
                        treffer.Add(v.ID_Energieanlage, v);
            }
            catch (Exception ex)
            {
                // Datenbank vor Migrationsschritt 11b: Der Vergleich laeuft dann mit den
                // Vorgabewerten der Engine weiter, nur ohne Aktiv-Kennzeichnung.
                Console.WriteLine("Die Speichervarianten konnten nicht gelesen werden: " + ex.Message);
            }

            return treffer;
        }

        // ==================================================================
        // Anzeige
        // ==================================================================

        private void TabelleFuellen()
        {
            CultureInfo k = CultureInfo.CurrentCulture;

            list_Varianten.BeginUpdate();
            try
            {
                list_Varianten.Items.Clear();

                int besteZeile = BesteZeileNachDeltaJ();

                for (int i = 0; i < m_Zeilen.Count; i++)
                {
                    Vergleichszeile z = m_Zeilen[i];

                    ListViewItem eintrag = new ListViewItem(
                        z.Aktiv ? MyResource.Resource.VAR_VGL_MARKER_AKTIV : "");
                    eintrag.SubItems.Add(z.Bezeichnung);
                    eintrag.SubItems.Add(z.Betriebsart);
                    eintrag.SubItems.Add(z.Berechnungsart);

                    if (z.Gerechnet)
                    {
                        eintrag.SubItems.Add(z.KapazitaetKwh.ToString("N1", k));
                        eintrag.SubItems.Add(z.LeistungKw.ToString("N1", k));
                        eintrag.SubItems.Add(z.InvestitionEur.ToString("N0", k));
                        eintrag.SubItems.Add(z.ErtragAequivalentEur.ToString("N0", k));
                        eintrag.SubItems.Add(z.DeltaJEur.ToString("N0", k));
                        eintrag.SubItems.Add(z.AmortisationText);
                        eintrag.SubItems.Add(z.KapitalwertEur.ToString("N0", k));
                        eintrag.SubItems.Add(z.Vollzyklen.ToString("N1", k));
                    }
                    else
                    {
                        // Fehlerzeile: leere Zahlenspalten und der Grund an der Stelle,
                        // an der sonst die Vergleichsgroesse steht.
                        for (int s = SP_KAPAZITAET; s <= SP_VOLLZYKLEN; s++)
                            eintrag.SubItems.Add(s == SP_DELTAJ ? MyResource.Resource.VAR_VGL_FEHLER_ZEILE : "");
                        eintrag.ForeColor = Color.Firebrick;
                        eintrag.ToolTipText = z.Hinweis;
                    }

                    // Die aktive Variante fett, die beste nach dJ hinterlegt. Zwei
                    // getrennte Merkmale, weil es zwei getrennte Aussagen sind: was
                    // gerechnet WIRD und was am besten waere.
                    if (z.Aktiv) eintrag.Font = m_SchriftAktiv;
                    if (i == besteZeile) eintrag.BackColor = FARBE_BESTE;

                    eintrag.Tag = z;
                    list_Varianten.Items.Add(eintrag);
                }
            }
            finally
            {
                list_Varianten.EndUpdate();
            }

            // Die Warnung gilt genau dann, wenn es Varianten gibt, aber keine davon aktiv
            // ist - dann und nur dann rechnet der Gesamtlauf noch die Aggregation.
            lbl_Hinweis.Visible = m_Zeilen.Count > 0 && !EineVarianteIstAktiv();

            StatuszeileSetzen();
        }

        /// <summary>
        /// Ist eine der angezeigten Varianten als aktiv markiert? Nur dann rechnet die
        /// Gesamtsimulation eine bestimmte Variante (AP9b, Fachkonzept 7.3); sonst faellt
        /// sie auf die Summe aller Speicheranlagen zurueck.
        /// </summary>
        private bool EineVarianteIstAktiv()
        {
            foreach (Vergleichszeile z in m_Zeilen)
                if (z.Aktiv) return true;

            return false;
        }

        /// <summary>
        /// Index der besten Variante nach dJ, oder -1. Bei Gleichstand gewinnt die
        /// erste — die Reihenfolge ist die der Anlagenliste und damit stabil.
        /// </summary>
        private int BesteZeileNachDeltaJ()
        {
            int treffer = -1;
            double bestwert = 0.0;

            for (int i = 0; i < m_Zeilen.Count; i++)
            {
                if (!m_Zeilen[i].Gerechnet) continue;
                if (treffer < 0 || m_Zeilen[i].DeltaJEur > bestwert)
                {
                    treffer = i;
                    bestwert = m_Zeilen[i].DeltaJEur;
                }
            }

            return treffer;
        }

        private void StatuszeileSetzen()
        {
            int beste = BesteZeileNachDeltaJ();

            if (m_Zeilen.Count == 0)
            {
                lbl_Status.Text = MyResource.Resource.VAR_VGL_STATUS_LEER;
                lbl_Status.ForeColor = Color.Firebrick;
                return;
            }

            if (beste < 0)
            {
                lbl_Status.Text = MyResource.Resource.VAR_VGL_STATUS_OHNE_ERGEBNIS;
                lbl_Status.ForeColor = Color.Firebrick;
                return;
            }

            lbl_Status.Text = string.Format(CultureInfo.CurrentCulture,
                                            MyResource.Resource.VAR_VGL_STATUS,
                                            m_Zeilen.Count, m_DauerMs, m_Zeilen[beste].Bezeichnung);
            lbl_Status.ForeColor = Color.Black;
        }

        private static string BetriebsartText(string wert)
        {
            if (wert == DbWerte.SP_BETRIEBSART_GRAUSTROM)
                return MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRAUSTROM;
            if (wert == DbWerte.SP_BETRIEBSART_GRUENSTROM)
                return MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRUENSTROM;
            return wert ?? "";
        }

        private static string BerechnungsartText(string wert)
        {
            if (wert == DbWerte.SP_BERECHNUNG_NACHTNUTZUNG)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG;
            if (wert == DbWerte.SP_BERECHNUNG_DAUERNUTZUNG)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG;
            return wert ?? "";
        }

        /// <summary>
        /// Amortisation als Text — die beiden Sonderfaelle der Engine im Klartext,
        /// wortgleich mit Ergebnisseite und Auslegungsoptimierung.
        /// </summary>
        private static string AmortisationText(Amortisation a)
        {
            switch (a.Status)
            {
                case AmortisationStatus.NichtAmortisierbar:
                    return MyResource.Resource.OPT_AMORT_NIE;
                case AmortisationStatus.UeberNutzungsdauer:
                    return MyResource.Resource.OPT_AMORT_UEBER;
                default:
                    return a.Jahre.ToString("0.0", CultureInfo.CurrentCulture);
            }
        }

        // ==================================================================
        // Aktive Variante umstellen
        // ==================================================================

        /// <summary>Doppelklick auf eine Zeile stellt die aktive Variante um.</summary>
        private void Varianten_DoubleClick(object sender, EventArgs e)
        {
            AktivSetzen();
        }

        private void Aktiv_Click(object sender, EventArgs e)
        {
            AktivSetzen();
        }

        /// <summary>
        /// Macht die markierte Variante zur aktiven Variante des Projekts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Geschrieben wird ausschliesslich ueber
        /// <c>StromspeicherVarianteCtrl.SetzeAktiv</c> — die eine Schreibstelle fuer
        /// <c>Aktiv</c>, die die Zusage „genau eine aktive Variante je Projekt" traegt.
        /// </para>
        /// <para>
        /// <b>Kein Neurechnen.</b> Die Zahlen der Tabelle gehoeren zu den Varianten
        /// selbst und aendern sich nicht dadurch, welche als aktiv gilt; es wandert nur
        /// die Markierung. Der Simulationslauf im Hintergrund bleibt ebenfalls stehen —
        /// er beschreibt, was gerechnet WURDE, und das sagt die Rueckfrage auch.
        /// </para>
        /// </remarks>
        private void AktivSetzen()
        {
            Vergleichszeile z = MarkierteZeile();
            if (z == null)
            {
                Melden(MyResource.Resource.VAR_VGL_MSG_KEINE_AUSWAHL);
                return;
            }

            if (z.Aktiv) return;

            if (MessageBox.Show(this,
                                string.Format(MyResource.Resource.VAR_MSG_AKTIV_FRAGE, z.Bezeichnung),
                                MyResource.Resource.VAR_VGL_TITEL,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            StromspeicherVarianteCtrl ctrl = new StromspeicherVarianteCtrl();
            StromspeicherVarianteModel variante = ctrl.ReadByEnergieanlage(z.ID_Energieanlage);

            if (variante == null)
            {
                // Anlagenzeile ohne Variantenzeile (Datenbank vor Migrationsschritt 11b):
                // Sie entsteht hier mit den Vorgabewerten des Modells.
                variante = new StromspeicherVarianteModel { ID_Energieanlage = z.ID_Energieanlage };
                if (ctrl.Insert(variante) <= 0)
                {
                    Melden(MyResource.Resource.VAR_MSG_AKTIV_FEHLER);
                    return;
                }
            }

            if (!ctrl.SetzeAktiv(m_ID_Projekt, variante.ID))
            {
                Melden(MyResource.Resource.VAR_MSG_AKTIV_FEHLER);
                return;
            }

            z.ID_Variante = variante.ID;
            foreach (Vergleichszeile andere in m_Zeilen) andere.Aktiv = (andere == z);

            AktiveVarianteGeaendert = true;
            UebersichtAuffrischen();
            TabelleFuellen();
        }

        private Vergleichszeile MarkierteZeile()
        {
            if (list_Varianten.SelectedItems.Count == 0) return null;
            return list_Varianten.SelectedItems[0].Tag as Vergleichszeile;
        }

        /// <summary>
        /// Aktualisierungsmuster nach einer Aenderung (Fachkonzept 5.5):
        /// Aenderungsdatum fortschreiben und die Uebersicht des Hauptformulars neu
        /// aufbauen.
        /// </summary>
        private void UebersichtAuffrischen()
        {
            try
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle(m_ID_Projekt);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                if (Program.mainfrm != null) Program.mainfrm.SetSPControl(projctrl.m_szProjektname);
            }
            catch (Exception ex)
            {
                // Die Auffrischung ist Beiwerk - die Umstellung selbst ist bereits
                // geschrieben und darf an einer geschlossenen Uebersicht nicht scheitern.
                Console.WriteLine("Die Uebersicht konnte nicht aufgefrischt werden: " + ex.Message);
            }
        }

        // ==================================================================
        // CSV-Export
        // ==================================================================

        /// <summary>
        /// Schreibt die Vergleichstabelle als CSV.
        /// </summary>
        /// <remarks>
        /// Eigener Schreiber aus demselben Grund wie in
        /// <c>Form_SpeicherOptimierung</c>: <c>CsvExportClass</c> ist auf Zeitreihen
        /// zugeschnitten und stellte jeder Zeile einen erfundenen Zeitstempel voran.
        /// Uebernommen sind ihre Konventionen — Semikolon als Feldtrenner, Dezimalkomma
        /// der aktuellen Kultur, UTF-8 mit BOM.
        /// </remarks>
        private void Csv_Click(object sender, EventArgs e)
        {
            if (m_Zeilen.Count == 0)
            {
                Melden(MyResource.Resource.VAR_VGL_STATUS_LEER);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = MyResource.Resource.OPT_CSV_TITEL;
            dlg.Filter = "CSV (*.csv)|*.csv|" + MyResource.Resource.OPT_CSV_TITEL + " (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;
            dlg.FileName = string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.VAR_VGL_DATEI, m_ID_Projekt);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                TabelleSchreiben(dlg.FileName);
                MessageBox.Show(this,
                    string.Format(MyResource.Resource.OPT_CSV_GESCHRIEBEN, dlg.FileName),
                    MyResource.Resource.OPT_CSV_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    string.Format(MyResource.Resource.OPT_CSV_FEHLER, ex.Message),
                    MyResource.Resource.OPT_CSV_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TabelleSchreiben(string dateiname)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            StringBuilder text = new StringBuilder();

            string[] kopf =
            {
                MyResource.Resource.VAR_VGL_SP_AKTIV,
                MyResource.Resource.VAR_VGL_SP_BEZEICHNUNG,
                MyResource.Resource.VAR_VGL_SP_BETRIEBSART,
                MyResource.Resource.VAR_VGL_SP_BERECHNUNGSART,
                MyResource.Resource.VAR_VGL_SP_KAPAZITAET,
                MyResource.Resource.VAR_VGL_SP_LEISTUNG,
                MyResource.Resource.VAR_VGL_SP_INVESTITION,
                MyResource.Resource.VAR_VGL_SP_ERTRAG,
                MyResource.Resource.VAR_VGL_SP_DELTAJ,
                MyResource.Resource.VAR_VGL_SP_AMORTISATION,
                MyResource.Resource.VAR_VGL_SP_NPV,
                MyResource.Resource.VAR_VGL_SP_VOLLZYKLEN,
                MyResource.Resource.VAR_VGL_PROTOKOLL
            };
            text.AppendLine(string.Join(";", kopf));

            foreach (Vergleichszeile z in m_Zeilen)
            {
                string[] felder =
                {
                    z.Aktiv ? MyResource.Resource.VAR_VGL_MARKER_AKTIV : "",
                    Feld(z.Bezeichnung),
                    Feld(z.Betriebsart),
                    Feld(z.Berechnungsart),
                    z.Gerechnet ? z.KapazitaetKwh.ToString("0.###", k) : "",
                    z.Gerechnet ? z.LeistungKw.ToString("0.###", k) : "",
                    z.Gerechnet ? z.InvestitionEur.ToString("0.###", k) : "",
                    z.Gerechnet ? z.ErtragAequivalentEur.ToString("0.###", k) : "",
                    z.Gerechnet ? z.DeltaJEur.ToString("0.###", k) : MyResource.Resource.VAR_VGL_FEHLER_ZEILE,
                    z.Gerechnet ? z.AmortisationText : "",
                    z.Gerechnet ? z.KapitalwertEur.ToString("0.###", k) : "",
                    z.Gerechnet ? z.Vollzyklen.ToString("0.###", k) : "",
                    Feld(z.Hinweis)
                };
                text.AppendLine(string.Join(";", felder));
            }

            File.WriteAllText(dateiname, text.ToString(), new UTF8Encoding(true));
        }

        /// <summary>
        /// Textfeld fuer die CSV-Zeile: Semikolon und Zeilenumbruch wuerden die
        /// Spaltenaufteilung zerstoeren. Variantennamen und Protokolltexte sind frei
        /// eingegeben beziehungsweise mehrzeilig — anders als die reinen Zahlenspalten
        /// der Auslegungsoptimierung.
        /// </summary>
        private static string Feld(string wert)
        {
            if (string.IsNullOrEmpty(wert)) return "";
            return wert.Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
        }

        private void Schliessen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Melden(string text)
        {
            MessageBox.Show(this, text, MyResource.Resource.VAR_VGL_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ==================================================================
        // Oberfläche — Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen in Form_SpeicherVariantenVergleich.Designer.cs.
        // Designer-Code trägt keine Kommentare; die Pixelentscheidungen stehen
        // deshalb hier. Gemessen wurde mit TextRenderer.MeasureText in der jeweils
        // gesetzten Schrift (Liste und Statuszeile Segoe UI 9,75 pt, Legende und
        // Hinweis 8,25 pt, Knöpfe 9 pt) — die Faustformel „7 px je Zeichen" liegt bei
        // diesen Texten durchweg 20 bis 30 Prozent zu hoch, weil Ziffern, Klammern
        // und Einheitenzeichen schmal sind.
        //
        // --- Design-Politur 21.08.2026 -----------------------------------
        //
        // * Echttexte statt Feldnamen. Im Designer standen als Platzhalter die
        //   Feldnamen („col_Kapazitaet" usw.), lbl_Status hatte überhaupt keinen
        //   Text. Jetzt steht dort der deutsche Text aus MyResource — als reine
        //   VORSCHAU, damit im VS-Designer zu sehen ist, ob die Beschriftungen in
        //   ihre Felder passen. Gesetzt werden sie weiterhin ausschließlich in
        //   TexteSetzen(); die Maske bleibt zweisprachig. lbl_Status zeigt als
        //   Vorschau die Formatvorlage VAR_VGL_STATUS wörtlich mit ihren
        //   Platzhaltern {0}/{1}/{2} — das ist der Normalfall der Maske (die
        //   Fehlerfassungen VAR_VGL_KEIN_LAUF und VAR_VGL_STATUS_LEER setzt der
        //   Konstruktor).
        //
        // * Sechs Spaltenüberschriften waren mit den Echttexten zu schmal und wurden
        //   abgeschnitten. Gemessen (Kopftext + 16 px Kopfpolster) gegenüber alt:
        //     Berechnungsart    114 → 115 (alt 110)
        //     Kapazität [kWh]   115 → 115 (alt 100)
        //     Leistung [kW]     102 → 102 (alt  90)
        //     Ertrag E_a,äq …   132 → 132 (alt 120)
        //     Amortisation [a]  117 → 118 (alt 110)
        //     Vollzyklen [1/a]  111 → 112 (alt 100)
        //   Aktiv (54), Bezeichnung (210), Betriebsart (100), ΔJ (100),
        //   Investition (105) und Kapitalwert (110) reichten bereits und bleiben.
        //   Die Spaltensumme wächst damit von 1309 auf 1373 px. Sie lag also schon
        //   vorher über der Listenbreite von 1216 px: Die Tabelle hat in der
        //   Grundgröße eine waagerechte Bildlaufleiste. Das bleibt so, und zwar mit
        //   Absicht — die Alternative wäre eine ClientSize jenseits von 1400 px,
        //   und damit ein Fenster, das auf einem 1366 x 768-Notebook nicht mehr auf
        //   den Schirm passt (dieselbe Abwägung wie in Form_QuelleErdreich). Die
        //   Liste ist an allen vier Seiten verankert; wer die Maske aufzieht oder
        //   maximiert, sieht alle zwölf Spalten ohne Bildlauf.
        //
        // * btn_Schliessen auf 110 x 30 (vorher 94 x 30) — Mindestmaß für Fußknöpfe,
        //   einheitlich mit den beiden anderen Knopfhöhen der Maske. Die rechte
        //   Kante bleibt bei x = 1228 (Rand 12), der Knopf beginnt also bei 1118.
        //
        // * btn_Csv von x = 230 auf 232 gerückt: Der Abstand zu btn_Aktiv
        //   (Endkante 222) wächst von 8 auf 10 px. Beide Knöpfe sind Bottom|Left
        //   verankert, btn_Schliessen Bottom|Right — die Fußzeile hält damit in
        //   jeder Fensterbreite links und rechts ihren Rand.
        //
        // * Keine Größenänderung nötig bei:
        //     lbl_Status     (328 px Text in 1216 px, AutoEllipsis),
        //     lbl_Legende    (723 px Text in 1216 px, eine Zeile in 18 px Höhe; auch
        //                     bei MinimumSize 900 bleibt es eine Zeile),
        //     lbl_Hinweis    (1236 px Text, bricht damit auf zwei Zeilen um und
        //                     braucht 26 px — die 32 px Feldhöhe reichen bis hinunter
        //                     zur MinimumSize; das Feld steht im Designer weiter auf
        //                     Visible = false, siehe TexteSetzen),
        //     lbl_Protokollkopf (55 px Text in 200 px),
        //     btn_Aktiv      (87 px Text in 210 px),
        //     btn_Csv        (92 px Text in 190 px).
        //
        // * ClientSize bleibt 1240 x 640, MinimumSize 900 x 480.
    }
}
