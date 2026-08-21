using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Auswahldialog des Knopfes „Planwert übernehmen…": legt <b>je Anlage</b> fest,
    /// welcher Technikwert als Investition gilt, und zeigt die Nebenkosten, die als
    /// eigene Zeilen entstehen.
    ///
    /// <para>
    /// Umsetzung der Nutzerentscheidungen 1 und 2 vom 18.08.2026. Der Anwender sieht zu
    /// jeder Zeile, WOHER die Zahl kommt (Spalte „Herkunft": Feldname bzw. die Rechnung
    /// „653,60 €/kWel × 250,00 kWel"), damit die Wahl nicht raten heißt.
    /// </para>
    ///
    /// <para>
    /// Der Aufbau der Maske steht in <c>Form_PlanwertUebernahme.Designer.cs</c>;
    /// <c>.resx</c>-Dateien gibt es bewusst keine, weil alle Anzeigetexte aus
    /// <c>MyResource</c> kommen (<see cref="TexteSetzen"/>) und im Designer nur als
    /// Entwurfsbild stehen. Die Steuerwerte der Auswahlspalte sind die sprachneutralen
    /// Schlüssel aus <see cref="TechnikPlanwertCtrl"/>.
    /// </para>
    /// </summary>
    internal partial class Form_PlanwertUebernahme : Form
    {
        private readonly List<TechnikPlanwertCtrl.Anlage> _anlagen;
        private readonly string _komponente;

        /// <summary>Gewählte Kostenbasis je GerätID (Schlüssel aus <see cref="TechnikPlanwertCtrl"/>).</summary>
        internal Dictionary<int, string> Wahl { get; private set; }

        /// <summary>Summe der Hauptposition nach der Auswahl.</summary>
        internal double Hauptsumme { get; private set; }

        /// <summary>Nebenkosten, je Bezeichnung zusammengefasst.</summary>
        internal List<TechnikPlanwertCtrl.Nebenposten> Nebenkosten { get; private set; }

        internal Form_PlanwertUebernahme(string komponente, List<TechnikPlanwertCtrl.Anlage> anlagen)
        {
            _komponente = komponente ?? "";
            _anlagen = anlagen ?? new List<TechnikPlanwertCtrl.Anlage>();
            Wahl = new Dictionary<int, string>();
            Nebenkosten = TechnikPlanwertCtrl.Nebensummen(_anlagen);

            // Der Designer setzt bewusst AutoScaleMode.None und KEIN AutoScaleDimensions:
            // die Anwendung läuft DpiUnaware (app.manifest, Application.SetHighDpiMode in
            // Program.cs), und der handgebaute Vorgänger dieser Maske hat mangels
            // AutoScaleDimensions ebenfalls nie skaliert — das Verhalten bleibt so wie bisher.
            InitializeComponent();
            TexteSetzen();

            Fuellen();
            SummeAktualisieren();
        }

        // --------------------------------------------------------------- Geometrie
        //
        // Die Steuerelemente stehen seit der Designer-Umstellung in
        // Form_PlanwertUebernahme.Designer.cs. Designer-Code trägt keine Kommentare;
        // die Pixelentscheidungen stehen deshalb hier.
        //
        // Design-Politur 21.08.2026 — Echttexte im Designer, geprüfte Abstände,
        // einheitliche Fußknöpfe. Alle Breiten mit TextRenderer gemessen (Segoe UI 9 pt,
        // deutsch und englisch); die Maske skaliert nicht (AutoScaleMode.None,
        // DpiUnaware), ist aber in der Breite veränderlich (SizableToolWindow).
        //
        // * Beschriftungen und die vier Spaltenköpfe tragen im Designer jetzt den
        //   deutschen Echttext aus MyResource statt des Feldnamens — der Anwender sieht
        //   im VS-Designer das Bild der laufenden Maske. Titelzeile und Summenzeile
        //   stehen als Formatvorlage inklusive {0} da, weil TexteSetzen() bzw.
        //   SummeAktualisieren() genau diese Zeichenkette füllen. Die Anzeige kommt
        //   unverändert ausschließlich aus MyResource.
        // * lblKopf 760 x 34 -> 760 x 44: Der Kopftext misst 522 px (englisch 527 px)
        //   und passt bei voller Breite in eine Zeile. Zieht der Anwender das Fenster
        //   auf MinimumSize 560, bleiben nach dem Innenabstand rund 530 px — dann bricht
        //   der Text auf zwei Zeilen (30 px) und lief mit den 8 px Innenabstand oben aus
        //   den bisherigen 34 px heraus. 44 px fassen beide Fälle.
        // * ClientSize 760 x 380 -> 760 x 390: gleicht die 10 px des Kopfes aus, damit
        //   die Liste nicht kleiner startet als bisher.
        // * Fußknöpfe einheitlich 120 x 30 (vorher 120 x 28), in panelFuss (42 px hoch)
        //   senkrecht mittig: btnOk (498/6), btnAbbruch (628/6). Die rechte Kante der
        //   Gruppe bleibt bei x = 748, also 12 px vom Rand; zwischen den Knöpfen liegen
        //   jetzt 10 px statt 8. Die Verankerung Top|Right bleibt unverändert.
        // * btnOk trägt SIM_BTN_OK („OK") statt KOSTEN_PLANWERT_BTN_OK („Übernehmen"),
        //   btnAbbruch SIM_BTN_ABBRECHEN: Der Dialog wählt nur aus, geschrieben wird
        //   erst in Form_Kosten nach DialogResult.OK — damit trägt die Maske den
        //   Standardsatz OK/Abbrechen. Die beiden KOSTEN_PLANWERT-Schlüssel bleiben in
        //   MyResource stehen.

        // ------------------------------------------------------------------- Texte

        /// <summary>
        /// Alle sichtbaren Texte aus <c>MyResource</c>; im Designer stehen dieselben
        /// Texte nur als Entwurfsbild (Drei-Schichten-Regel: Anzeige nur über MyResource).
        /// </summary>
        private void TexteSetzen()
        {
            Text = string.Format(MyResource.Resource.KOSTEN_PLANWERT_TITEL, _komponente);
            lblKopf.Text = MyResource.Resource.KOSTEN_PLANWERT_KOPF;

            spalteAnlage.HeaderText = MyResource.Resource.KOSTEN_PLANWERT_SP_ANLAGE;
            spalteBasis.HeaderText = MyResource.Resource.KOSTEN_PLANWERT_SP_BASIS;
            spalteBetrag.HeaderText = MyResource.Resource.KOSTEN_PLANWERT_SP_BETRAG;
            spalteHerkunft.HeaderText = MyResource.Resource.KOSTEN_PLANWERT_SP_HERLEITUNG;

            btnOk.Text = MyResource.Resource.SIM_BTN_OK;
            btnAbbruch.Text = MyResource.Resource.SIM_BTN_ABBRECHEN;
        }

        // -------------------------------------------------------------- Ereignisse

        /// <summary>Änderung der Auswahlspalte sofort festschreiben, nicht erst beim Zellwechsel.</summary>
        private void grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) ZeileNachziehen(e.RowIndex);
        }

        // -------------------------------------------------------------------- Daten

        private void Fuellen()
        {
            foreach (TechnikPlanwertCtrl.Anlage a in _anlagen)
            {
                int i = grid.Rows.Add();
                DataGridViewRow r = grid.Rows[i];
                r.Tag = a;

                r.Cells[0].Value = a.Bezeichner;

                var zelle = (DataGridViewComboBoxCell)r.Cells[1];
                foreach (TechnikPlanwertCtrl.Basiswert b in a.Basiswerte)
                    zelle.Items.Add(TechnikPlanwertCtrl.BasisName(b.Schluessel));

                if (a.Basiswerte.Count == 0)
                {
                    // Nichts gepflegt: keine Auswahl anbieten, Zeile trägt 0 bei.
                    zelle.Items.Add(TechnikPlanwertCtrl.BasisName(TechnikPlanwertCtrl.BASIS_KEINE));
                    zelle.Value = zelle.Items[0];
                    zelle.ReadOnly = true;
                }
                else
                {
                    if (a.Mehrdeutig)
                        zelle.Items.Add(TechnikPlanwertCtrl.BasisName(TechnikPlanwertCtrl.BASIS_KEINE));

                    // Vorauswahl: der einzige gepflegte Wert, sonst der ERSTE (Modulpreis) —
                    // eine Vorauswahl ist nötig, damit die Maske eine Summe zeigt; sie ist
                    // sichtbar und änderbar, also keine stille Festlegung.
                    zelle.Value = TechnikPlanwertCtrl.BasisName(a.Basiswerte[0].Schluessel);
                }

                ZeileNachziehen(i);
            }
        }

        /// <summary>Betrag/Herkunft der Zeile aus der gewählten Basis nachziehen.</summary>
        private void ZeileNachziehen(int index)
        {
            if (index < 0 || index >= grid.Rows.Count) return;
            DataGridViewRow r = grid.Rows[index];

            var a = r.Tag as TechnikPlanwertCtrl.Anlage;
            if (a == null) return;

            string angezeigt = Convert.ToString(r.Cells[1].Value);
            TechnikPlanwertCtrl.Basiswert treffer = null;
            foreach (TechnikPlanwertCtrl.Basiswert b in a.Basiswerte)
                if (string.Equals(TechnikPlanwertCtrl.BasisName(b.Schluessel), angezeigt, StringComparison.Ordinal))
                { treffer = b; break; }

            Wahl[a.GeraetID] = (treffer != null) ? treffer.Schluessel : TechnikPlanwertCtrl.BASIS_KEINE;

            r.Cells[2].Value = (treffer != null)
                ? treffer.Betrag.ToString("N2", BerichtTexte.Kultur) : "0,00";
            r.Cells[3].Value = (treffer != null) ? treffer.Herleitung : "";

            SummeAktualisieren();
        }

        private void SummeAktualisieren()
        {
            Hauptsumme = TechnikPlanwertCtrl.Hauptsumme(_anlagen, Wahl);

            if (lblSumme != null)
                lblSumme.Text = string.Format(MyResource.Resource.KOSTEN_PLANWERT_SUMME,
                                              Hauptsumme.ToString("N2", BerichtTexte.Kultur));

            if (lblNeben == null) return;

            if (Nebenkosten.Count == 0) { lblNeben.Text = ""; return; }

            var teile = new List<string>();
            foreach (TechnikPlanwertCtrl.Nebenposten n in Nebenkosten)
                teile.Add(n.Bezeichnung + " " + n.Betrag.ToString("N2", BerichtTexte.Kultur) + " €");

            lblNeben.Text = MyResource.Resource.KOSTEN_PLANWERT_NEBENKOSTEN + " " +
                            string.Join("  ·  ", teile.ToArray());
        }
    }
}
