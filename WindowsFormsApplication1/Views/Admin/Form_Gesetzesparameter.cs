using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Pflegemaske „Gesetzliche Parameter" für <c>Tab_Gesetzesparameter</c>
    /// (Konzept_BHKW_Kosten_Erloese.md, Abschnitt 6, Etappe E1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Die Kernregel steht sichtbar auf der Maske.</b> Eine Gesetzesänderung ist
    /// eine NEUE Jahreszeile, kein Ändern der alten — sonst lässt sich eine 2026
    /// gerechnete Variante 2029 nicht mehr reproduzieren. Wer eine Zeile bearbeitet,
    /// deren <c>JahrVon</c> in der Vergangenheit liegt, wird deshalb gefragt, und die
    /// VORGABE der Rückfrage ist „neue Zeile anlegen". Das Ändern der alten Zeile
    /// bleibt möglich — für Tippfehler —, aber es ist die bewusste Ausnahme.
    /// </para>
    /// <para>
    /// <b>Ein leeres Wertfeld ist kein Nullwert.</b> Es bedeutet „der Satz ist
    /// entfallen" (Verdrängungsstrommix ab 2027) und wird als NULL gespeichert; die
    /// Lesefassade liefert dafür <c>null</c>, nicht 0.
    /// </para>
    /// <para>
    /// Die Oberfläche steht in <c>Form_Gesetzesparameter.Designer.cs</c>, weiterhin ohne
    /// eigene <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und werden
    /// in <see cref="TexteSetzen"/> gesetzt; im Designer stehen nur Platzhalter. Alle
    /// Datenbankwerte laufen über <c>DbWerte.GESETZ_*</c>.
    /// </para>
    /// </remarks>
    public partial class Form_Gesetzesparameter : Form
    {
        private readonly GesetzKatalog _katalog = new GesetzKatalog();

        /// <summary>
        /// Rückfrage „neue Jahreszeile anlegen?" — Rückgabe <c>Yes</c> = neue Zeile,
        /// <c>No</c> = bestehende Zeile ändern, <c>Cancel</c> = abbrechen.
        /// Im Test überschreibbar, damit der Reflection-Harness beide Antworten
        /// prüfen kann, ohne auf eine modale MessageBox angewiesen zu sein.
        /// </summary>
        internal Func<GesetzParameter, DialogResult> FrageNeueZeile { get; set; }

        /// <summary>Rückfrage vor dem Löschen; im Test überschreibbar.</summary>
        internal Func<GesetzParameter, DialogResult> FrageLoeschen { get; set; }

        /// <summary>Zeilendialog; im Test überschreibbar (liefert null = Abbruch).</summary>
        internal Func<GesetzParameter, bool, GesetzParameter> ZeileBearbeiten { get; set; }

        public Form_Gesetzesparameter()
        {
            // Der Katalog muss stehen, BEVOR die Oberfläche ihn liest.
            GesetzKatalog.StelleKatalogSicher();

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske arbeitet mit fest gerechneten
            // Pixelpositionen, und die Anwendung läuft DpiUnaware (app.manifest,
            // Program.SetHighDpiMode). Vor der Designer-Umstellung wurde
            // AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls keine
            // Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();
            TexteSetzen();

            KlassenFuellen();
            Aktualisieren();
        }

        // ==================================================================
        // Oberfläche — Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen in Form_Gesetzesparameter.Designer.cs.
        // Designer-Code trägt keine Kommentare; die Pixelentscheidungen stehen
        // deshalb hier (Muster Form_PufferSp_Projekt).
        //
        // DESIGN-POLITUR 21.08.2026
        // * Im Designer stehen jetzt die deutschen ECHTTEXTE statt der Feldnamen —
        //   auch in den sechs Spaltenköpfen. TexteSetzen() überschreibt sie beim
        //   Start unverändert.
        // * Mit den Echttexten geprüft und in Ordnung: „Bereich" (45 px) bei x = 12
        //   vor cbKlasse bei x = 90; der Hinweistext bleibt bei 916 px Breite
        //   einzeilig (865 px), die 34 px Höhe tragen auch eine zweite Zeile für
        //   längere Übersetzungen. Die sechs Spaltenbreiten (300/70/90/80/90/270 =
        //   900) passen mit 16 px Rest für den Rollbalken in die 916 px der Liste.
        // * Fußknöpfe auf das einheitliche Maß 110 x 30 (Breite war schon 110, die
        //   Höhe lag auf dem Standard 23). Die rechte Kante von btnSchliessen
        //   bleibt bei x = 928 (ClientSize 940 minus 12 Rand). Unterkante 550, also
        //   10 px Luft — die ClientSize 940 x 560 bleibt unverändert.
        // * Die drei linken Knöpfe rücken von 12/130/248 auf 12/132/252: Der
        //   Abstand wächst damit von 8 auf die geforderten 10 px, die linke Kante
        //   bleibt bei x = 12.
        // * Knopf-Semantik unverändert (Pflegemaske): Neu/Ändern/Löschen wirken
        //   sofort auf Tab_Gesetzesparameter, btnSchliessen ist CancelButton. Ein
        //   „Abbrechen" gäbe es hier nichts zurückzunehmen.

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.GESETZ_TITEL;
            this.lblHinweis.Text = MyResource.Resource.GESETZ_LBL_HINWEIS;
            this.lblKlasse.Text = MyResource.Resource.GESETZ_LBL_KLASSE;

            this.colSchluessel.Text = MyResource.Resource.GESETZ_SP_SCHLUESSEL;
            this.colJahrVon.Text = MyResource.Resource.GESETZ_SP_JAHRVON;
            this.colWert.Text = MyResource.Resource.GESETZ_SP_WERT;
            this.colEinheit.Text = MyResource.Resource.GESETZ_SP_EINHEIT;
            this.colStatus.Text = MyResource.Resource.GESETZ_SP_STATUS;
            this.colQuelle.Text = MyResource.Resource.GESETZ_SP_QUELLE;

            this.btnNeu.Text = MyResource.Resource.GESETZ_BTN_NEU;
            this.btnAendern.Text = MyResource.Resource.GESETZ_BTN_AENDERN;
            this.btnLoeschen.Text = MyResource.Resource.GESETZ_BTN_LOESCHEN;
            this.btnSchliessen.Text = MyResource.Resource.GESETZ_BTN_SCHLIESSEN;
        }

        // ==================================================================
        // Oberfläche
        // ==================================================================

        /// <summary>
        /// Trägt den DB-Wert und zeigt den lokalisierten Namen — kein Anzeigetext ist
        /// je Steuerwert (Drei-Schichten-Regel).
        /// </summary>
        internal sealed class KlasseItem
        {
            public KlasseItem(string wert, string anzeige) { Wert = wert; Anzeige = anzeige; }
            public string Wert { get; private set; }
            public string Anzeige { get; private set; }
            public override string ToString() { return Anzeige; }
        }

        private void KlassenFuellen()
        {
            cbKlasse.Items.Clear();
            foreach (string k in _katalog.Klassen())
                cbKlasse.Items.Add(new KlasseItem(k, KlasseAnzeige(k)));
            if (cbKlasse.Items.Count > 0) cbKlasse.SelectedIndex = 0;
        }

        /// <summary>Anzeigename einer Klasse; unbekannte Klassen zeigen ihren Rohwert.</summary>
        internal static string KlasseAnzeige(string klasse)
        {
            switch (klasse)
            {
                case DbWerte.GESETZ_KLASSE_KWKG: return MyResource.Resource.GESETZ_KLASSE_ANZ_KWKG;
                case DbWerte.GESETZ_KLASSE_STROMSTEUER: return MyResource.Resource.GESETZ_KLASSE_ANZ_STROMSTEUER;
                case DbWerte.GESETZ_KLASSE_ENERGIESTEUER: return MyResource.Resource.GESETZ_KLASSE_ANZ_ENERGIESTEUER;
                case DbWerte.GESETZ_KLASSE_CO2_PREIS: return MyResource.Resource.GESETZ_KLASSE_ANZ_CO2_PREIS;
                case DbWerte.GESETZ_KLASSE_EF_NACHWEIS: return MyResource.Resource.GESETZ_KLASSE_ANZ_EF_NACHWEIS;
                case DbWerte.GESETZ_KLASSE_EF_BILANZ: return MyResource.Resource.GESETZ_KLASSE_ANZ_EF_BILANZ;
                case DbWerte.GESETZ_KLASSE_PEF_NACHWEIS: return MyResource.Resource.GESETZ_KLASSE_ANZ_PEF_NACHWEIS;
                case DbWerte.GESETZ_KLASSE_UMSATZSTEUER: return MyResource.Resource.GESETZ_KLASSE_ANZ_UMSATZSTEUER;
                default: return klasse;
            }
        }

        /// <summary>Die gewählte Klasse als DB-Wert; leer, wenn nichts gewählt ist.</summary>
        internal string GewaehlteKlasse
        {
            get
            {
                KlasseItem i = cbKlasse.SelectedItem as KlasseItem;
                return i == null ? "" : i.Wert;
            }
            set
            {
                for (int i = 0; i < cbKlasse.Items.Count; i++)
                    if (((KlasseItem)cbKlasse.Items[i]).Wert == value) { cbKlasse.SelectedIndex = i; return; }
            }
        }

        // ==================================================================
        // Liste
        // ==================================================================

        /// <summary>Liest den Katalog neu und füllt die Liste der gewählten Klasse.</summary>
        internal void Aktualisieren()
        {
            _katalog.Neuladen();
            string klasse = GewaehlteKlasse;
            lvZeilen.BeginUpdate();
            lvZeilen.Items.Clear();
            if (klasse.Length > 0)
                foreach (GesetzParameter p in _katalog.AlleDerKlasse(klasse))
                {
                    ListViewItem it = new ListViewItem(p.Schluessel);
                    it.SubItems.Add(p.JahrVon.ToString(CultureInfo.CurrentCulture));
                    it.SubItems.Add(WertText(p.Wert));
                    it.SubItems.Add(p.Einheit);
                    it.SubItems.Add(p.Status);
                    it.SubItems.Add(p.Quelle);
                    it.Tag = p;
                    lvZeilen.Items.Add(it);
                }
            lvZeilen.EndUpdate();

            bool etwasDa = lvZeilen.Items.Count > 0;
            btnAendern.Enabled = etwasDa;
            btnLoeschen.Enabled = etwasDa;
        }

        /// <summary>Anzeige des Werts; ein leerer Text steht für „Satz entfallen".</summary>
        internal static string WertText(double? wert)
        {
            return wert.HasValue ? wert.Value.ToString("0.####", CultureInfo.CurrentCulture) : "";
        }

        /// <summary>Anzahl der gerade angezeigten Zeilen (Prüfhilfe des Harness).</summary>
        internal int ZeilenAnzahl { get { return lvZeilen.Items.Count; } }

        /// <summary>Die ausgewählte Zeile; null, wenn nichts markiert ist.</summary>
        internal GesetzParameter Auswahl
        {
            get
            {
                if (lvZeilen.SelectedItems.Count == 0) return null;
                return lvZeilen.SelectedItems[0].Tag as GesetzParameter;
            }
        }

        /// <summary>Markiert die Zeile mit Schlüssel und Jahr; liefert false, wenn es sie nicht gibt.</summary>
        internal bool Waehle(string schluessel, int jahrVon)
        {
            _ = lvZeilen.Handle;      // ohne Handle greift die ListView-Auswahl nicht
            foreach (ListViewItem it in lvZeilen.Items)
            {
                GesetzParameter p = it.Tag as GesetzParameter;
                if (p != null && p.Schluessel == schluessel && p.JahrVon == jahrVon)
                {
                    it.Selected = true;
                    it.Focused = true;
                    return true;
                }
            }
            return false;
        }

        private void cbKlasse_SelectedIndexChanged(object sender, EventArgs e)
        {
            Aktualisieren();
        }

        private void btnSchliessen_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ==================================================================
        // Anlegen, Ändern, Löschen
        // ==================================================================

        private void btnNeu_Click(object sender, EventArgs e)
        {
            GesetzParameter vorlage = new GesetzParameter(
                0, "", GewaehlteKlasse.Length > 0 ? GewaehlteKlasse : DbWerte.GESETZ_KLASSE_KWKG,
                DateTime.Today.Year, null, DbWerte.GESETZ_EINHEIT_OHNE,
                DbWerte.GESETZ_STATUS_GESICHERT, "");

            GesetzParameter neu = Dialog(vorlage, true);
            if (neu == null) return;
            if (!PruefeNeu(neu, 0)) return;

            if (GesetzKatalog.Anlegen(neu.Schluessel, neu.Klasse, neu.JahrVon, neu.Wert,
                                      neu.Einheit, neu.Status, neu.Quelle) == 0)
                Melden(MyResource.Resource.GESETZ_MSG_SPEICHERN_FEHLER);

            KlassenErgaenzen(neu.Klasse);
            GewaehlteKlasse = neu.Klasse;
            Aktualisieren();
            Waehle(neu.Schluessel, neu.JahrVon);
        }

        private void btnAendern_Click(object sender, EventArgs e)
        {
            GesetzParameter alt = Auswahl;
            if (alt == null) return;

            GesetzParameter bearbeitet = Dialog(alt, false);
            if (bearbeitet == null) return;

            // Kernregel: liegt das Gültig-ab-Jahr der BESTEHENDEN Zeile in der
            // Vergangenheit, ist eine Änderung im Regelfall eine Gesetzesänderung —
            // und die gehört in eine neue Jahreszeile.
            bool alsNeueZeile = false;
            if (alt.JahrVon < DateTime.Today.Year)
            {
                DialogResult antwort = FrageNeueZeile != null
                    ? FrageNeueZeile(alt)
                    : MessageBox.Show(
                        string.Format(MyResource.Resource.GESETZ_FRAGE_NEUE_ZEILE,
                                      alt.Schluessel, alt.JahrVon),
                        MyResource.Resource.GESETZ_FRAGE_TITEL,
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                if (antwort == DialogResult.Cancel) return;
                alsNeueZeile = antwort == DialogResult.Yes;
            }

            if (alsNeueZeile)
            {
                if (!PruefeNeu(bearbeitet, 0)) return;
                if (GesetzKatalog.Anlegen(bearbeitet.Schluessel, bearbeitet.Klasse, bearbeitet.JahrVon,
                                          bearbeitet.Wert, bearbeitet.Einheit, bearbeitet.Status,
                                          bearbeitet.Quelle) == 0)
                    Melden(MyResource.Resource.GESETZ_MSG_SPEICHERN_FEHLER);
            }
            else
            {
                if (!PruefeNeu(bearbeitet, alt.Id)) return;
                if (!GesetzKatalog.Aendern(alt.Id, bearbeitet.JahrVon, bearbeitet.Wert,
                                           bearbeitet.Einheit, bearbeitet.Status, bearbeitet.Quelle))
                    Melden(MyResource.Resource.GESETZ_MSG_SPEICHERN_FEHLER);
            }

            Aktualisieren();
            Waehle(bearbeitet.Schluessel, bearbeitet.JahrVon);
        }

        private void btnLoeschen_Click(object sender, EventArgs e)
        {
            GesetzParameter p = Auswahl;
            if (p == null) return;

            DialogResult antwort = FrageLoeschen != null
                ? FrageLoeschen(p)
                : MessageBox.Show(
                    string.Format(MyResource.Resource.GESETZ_FRAGE_LOESCHEN, p.Schluessel, p.JahrVon),
                    MyResource.Resource.GESETZ_LOESCHEN_TITEL,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
            if (antwort != DialogResult.Yes) return;

            if (!GesetzKatalog.Loeschen(p.Id))
                Melden(MyResource.Resource.GESETZ_MSG_SPEICHERN_FEHLER);
            Aktualisieren();
        }

        private GesetzParameter Dialog(GesetzParameter vorlage, bool istNeu)
        {
            if (ZeileBearbeiten != null) return ZeileBearbeiten(vorlage, istNeu);
            using (Form_GesetzparameterZeile dlg = new Form_GesetzparameterZeile(vorlage, istNeu))
                return dlg.ShowDialog(this) == DialogResult.OK ? dlg.Ergebnis : null;
        }

        /// <summary>
        /// Prüft Pflichtangaben und die Eindeutigkeit von Schlüssel plus Jahr.
        /// <paramref name="eigeneId"/> nimmt die gerade bearbeitete Zeile aus der
        /// Dublettenprüfung heraus.
        /// </summary>
        private bool PruefeNeu(GesetzParameter p, int eigeneId)
        {
            if (p.Schluessel.Length == 0)
            {
                Melden(MyResource.Resource.GESETZ_MSG_SCHLUESSEL_FEHLT);
                return false;
            }
            if (p.JahrVon < 1990 || p.JahrVon > 2100)
            {
                Melden(MyResource.Resource.GESETZ_MSG_JAHR_UNGUELTIG);
                return false;
            }

            GesetzKatalog frisch = new GesetzKatalog();
            foreach (GesetzParameter v in frisch.AlleDerKlasse(p.Klasse))
                if (v.Schluessel == p.Schluessel && v.JahrVon == p.JahrVon && v.Id != eigeneId)
                {
                    Melden(string.Format(MyResource.Resource.GESETZ_MSG_DOPPELT,
                                         p.Schluessel, p.JahrVon));
                    return false;
                }
            return true;
        }

        /// <summary>Nimmt eine noch nicht gelistete Klasse in die Auswahl auf.</summary>
        private void KlassenErgaenzen(string klasse)
        {
            if (klasse.Length == 0) return;
            foreach (object o in cbKlasse.Items)
                if (((KlasseItem)o).Wert == klasse) return;
            cbKlasse.Items.Add(new KlasseItem(klasse, KlasseAnzeige(klasse)));
        }

        /// <summary>Meldungskanal; im Test überschreibbar.</summary>
        internal Action<string> Meldung { get; set; }

        private void Melden(string text)
        {
            if (Meldung != null) { Meldung(text); return; }
            MessageBox.Show(text, MyResource.Resource.GESETZ_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
