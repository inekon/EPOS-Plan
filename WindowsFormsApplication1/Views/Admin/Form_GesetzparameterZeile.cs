using System;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zeilendialog der Pflegemaske. Schlüssel und Klasse sind beim ÄNDERN gesperrt:
    /// Sie sind die Identität der Reihe und in der Datenbank eingefroren — wer sie
    /// ändern will, legt eine neue Zeile an und löscht die alte.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Oberfläche steht in <c>Form_GesetzparameterZeile.Designer.cs</c>, ohne eigene
    /// <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und werden in
    /// <see cref="TexteSetzen"/> gesetzt; im Designer stehen nur Platzhalter.
    /// </para>
    /// <para>
    /// Alles, was vom Konstruktorparameter <c>istNeu</c> abhängt — Fenstertitel,
    /// <c>ReadOnly</c> des Schlüsselfelds, <c>Enabled</c> der Klassenauswahl —, steht
    /// bewusst NICHT im Designer, sondern im Nachlauf des Konstruktors
    /// (<see cref="TexteSetzen"/>, <see cref="ModusSetzen"/>). Die Listeninhalte der
    /// Auswahlfelder ebenso: Sie kommen aus <c>DbWerte</c> und dürfen nie als Literal
    /// im Designer landen (Drei-Schichten-Regel).
    /// </para>
    /// </remarks>
    public partial class Form_GesetzparameterZeile : Form
    {
        private readonly bool _istNeu;
        private readonly int _id;

        /// <summary>Die eingegebene Zeile; erst nach <c>DialogResult.OK</c> gefüllt.</summary>
        public GesetzParameter Ergebnis { get; private set; }

        public Form_GesetzparameterZeile(GesetzParameter vorlage, bool istNeu)
        {
            _istNeu = istNeu;
            _id = vorlage == null ? 0 : vorlage.Id;

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske ist ein FixedDialog mit fest
            // gerechneten Pixelpositionen, und die Anwendung läuft DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). Vor der Designer-Umstellung
            // wurde AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls
            // keine Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();
            TexteSetzen();
            ListenFuellen();
            ModusSetzen();

            Uebernehmen(vorlage);
        }

        // ==================================================================
        // Oberfläche — Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen in Form_GesetzparameterZeile.Designer.cs.
        // Designer-Code trägt keine Kommentare; die Pixelentscheidungen stehen
        // deshalb hier (Muster Form_PufferSp_Projekt).
        //
        // DESIGN-POLITUR 21.08.2026
        // * Im Designer stehen jetzt die deutschen ECHTTEXTE statt der Feldnamen.
        //   TexteSetzen() überschreibt sie beim Start unverändert; der Designer
        //   zeigt damit dasselbe Bild wie die laufende Maske.
        // * Beschriftungsspalte geprüft: Die Beschriftungen stehen bei x = 12, die
        //   Eingaben beginnen bei x = 160 — 148 px Platz. Der breiteste deutsche
        //   Text ist „Schlüssel"/„Gültig ab" mit 54 px (Segoe UI 9 pt), der
        //   breiteste englische „Valid from" mit 61 px. Die Spalte reicht mit
        //   großem Abstand; die Eingaben mussten NICHT nach rechts wandern, die
        //   ClientSize bleibt 620 x 260.
        // * Fußknöpfe auf das einheitliche Maß 110 x 30 (vorher 96 x 23 —
        //   Standardhöhe). Die rechte Kante bleibt bei x = 600 (ClientSize 620
        //   minus 20 Rand), der Abstand zwischen den Knöpfen wächst von 8 auf
        //   10 px: btnAbbruch 490, btnOk 370. Unterkante 240, also 20 px Luft bis
        //   zum Fensterrand — die ClientSize musste dafür nicht wachsen.
        // * Knopf-Semantik unverändert: btnOk ist AcceptButton, btnAbbruch ist
        //   CancelButton mit DialogResult.Cancel. Beschriftet bleiben sie über
        //   GESETZ_BTN_UEBERNEHMEN/GESETZ_BTN_ABBRECHEN — einen Schlüssel mit dem
        //   Wert „OK" gibt es im GESETZ_-Vorrat nicht, und neue Schlüssel sind
        //   ausgeschlossen. „Übernehmen" ist keine „Speichern"-Beschriftung; die
        //   geforderte Fußzeilen-Ordnung (Zusage links, Abbruch rechts) steht.

        // ==================================================================
        // Texte und Listen
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = _istNeu
                ? MyResource.Resource.GESETZ_DLG_TITEL_NEU
                : MyResource.Resource.GESETZ_DLG_TITEL_AENDERN;

            this.lblSchluessel.Text = MyResource.Resource.GESETZ_SP_SCHLUESSEL;
            this.lblKlasse.Text = MyResource.Resource.GESETZ_LBL_KLASSE;
            this.lblJahr.Text = MyResource.Resource.GESETZ_SP_JAHRVON;
            this.lblWert.Text = MyResource.Resource.GESETZ_SP_WERT;
            this.lblWertLeer.Text = MyResource.Resource.GESETZ_LBL_WERT_LEER;
            this.lblEinheit.Text = MyResource.Resource.GESETZ_SP_EINHEIT;
            this.lblStatus.Text = MyResource.Resource.GESETZ_SP_STATUS;
            this.lblQuelle.Text = MyResource.Resource.GESETZ_SP_QUELLE;

            this.btnOk.Text = MyResource.Resource.GESETZ_BTN_UEBERNEHMEN;
            this.btnAbbruch.Text = MyResource.Resource.GESETZ_BTN_ABBRECHEN;
        }

        /// <summary>
        /// Füllt die drei Auswahlfelder. Die Einträge sind Datenbankwerte aus
        /// <c>DbWerte</c> — nur die Klassenauswahl zeigt über <c>KlasseItem</c> einen
        /// lokalisierten Namen an, der Steuerwert bleibt der deutsche DB-Wert.
        /// </summary>
        private void ListenFuellen()
        {
            foreach (string k in Klassen())
                this.cbKlasse.Items.Add(new Form_Gesetzesparameter.KlasseItem(
                    k, Form_Gesetzesparameter.KlasseAnzeige(k)));

            this.cbEinheit.Items.AddRange(Einheiten());

            this.cbStatus.Items.AddRange(new object[]
            {
                DbWerte.GESETZ_STATUS_GESICHERT,
                DbWerte.GESETZ_STATUS_VORLAEUFIG,
                DbWerte.GESETZ_STATUS_PROGNOSE
            });
        }

        /// <summary>
        /// Schaltet die Felder, die von <c>istNeu</c> abhängen: Schlüssel und Klasse
        /// sind beim Ändern gesperrt.
        /// </summary>
        private void ModusSetzen()
        {
            this.tbSchluessel.ReadOnly = !_istNeu;
            this.cbKlasse.Enabled = _istNeu;
        }

        private static string[] Klassen()
        {
            return new string[]
            {
                DbWerte.GESETZ_KLASSE_KWKG,
                DbWerte.GESETZ_KLASSE_STROMSTEUER,
                DbWerte.GESETZ_KLASSE_ENERGIESTEUER,
                DbWerte.GESETZ_KLASSE_CO2_PREIS,
                DbWerte.GESETZ_KLASSE_EF_NACHWEIS,
                DbWerte.GESETZ_KLASSE_EF_BILANZ,
                DbWerte.GESETZ_KLASSE_PEF_NACHWEIS,
                DbWerte.GESETZ_KLASSE_UMSATZSTEUER
            };
        }

        /// <summary>
        /// Die zulässigen Einheiten — feste Liste, damit niemand „EUR/MWh" einmal so
        /// und einmal anders schreibt (L3).
        /// </summary>
        internal static object[] Einheiten()
        {
            return new object[]
            {
                DbWerte.GESETZ_EINHEIT_EUR_MWH,
                DbWerte.GESETZ_EINHEIT_EUR_1000L,
                DbWerte.GESETZ_EINHEIT_EUR_1000KG,
                DbWerte.GESETZ_EINHEIT_EUR_GJ,
                DbWerte.GESETZ_EINHEIT_EUR_T,
                DbWerte.GESETZ_EINHEIT_EUR_A,
                DbWerte.GESETZ_EINHEIT_CT_KWH,
                DbWerte.GESETZ_EINHEIT_G_KWH,
                DbWerte.GESETZ_EINHEIT_GJ_MWH,
                DbWerte.GESETZ_EINHEIT_H,
                DbWerte.GESETZ_EINHEIT_KW,
                DbWerte.GESETZ_EINHEIT_KM,
                DbWerte.GESETZ_EINHEIT_PROZENT,
                DbWerte.GESETZ_EINHEIT_JAHR,
                DbWerte.GESETZ_EINHEIT_OHNE
            };
        }

        // ==================================================================
        // Übernahme in die Felder und zurück
        // ==================================================================

        private void Uebernehmen(GesetzParameter p)
        {
            if (p == null) return;
            tbSchluessel.Text = p.Schluessel;
            tbJahr.Text = p.JahrVon.ToString(CultureInfo.CurrentCulture);
            tbWert.Text = Form_Gesetzesparameter.WertText(p.Wert);
            tbQuelle.Text = p.Quelle;
            WaehleText(cbEinheit, p.Einheit, DbWerte.GESETZ_EINHEIT_OHNE);
            WaehleText(cbStatus, p.Status, DbWerte.GESETZ_STATUS_GESICHERT);
            for (int i = 0; i < cbKlasse.Items.Count; i++)
                if (((Form_Gesetzesparameter.KlasseItem)cbKlasse.Items[i]).Wert == p.Klasse)
                { cbKlasse.SelectedIndex = i; break; }
            if (cbKlasse.SelectedIndex < 0 && cbKlasse.Items.Count > 0) cbKlasse.SelectedIndex = 0;
        }

        private static void WaehleText(ComboBox cb, string wert, string ersatz)
        {
            int i = cb.Items.IndexOf(wert ?? "");
            if (i < 0) i = cb.Items.IndexOf(ersatz);
            cb.SelectedIndex = i < 0 ? (cb.Items.Count > 0 ? 0 : -1) : i;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            int jahr;
            if (!int.TryParse((tbJahr.Text ?? "").Trim(), NumberStyles.Integer,
                              CultureInfo.CurrentCulture, out jahr) || jahr < 1990 || jahr > 2100)
            {
                MessageBox.Show(MyResource.Resource.GESETZ_MSG_JAHR_UNGUELTIG,
                                this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Leeres Feld = der Satz ist entfallen; das ist etwas anderes als 0.
            double? wert = null;
            string roh = (tbWert.Text ?? "").Trim();
            if (roh.Length > 0)
            {
                double w;
                if (!double.TryParse(roh, NumberStyles.Float, CultureInfo.CurrentCulture, out w) &&
                    !double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out w))
                {
                    MessageBox.Show(MyResource.Resource.GESETZ_MSG_WERT_UNGUELTIG,
                                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                wert = w;
            }

            string schluessel = (tbSchluessel.Text ?? "").Trim();
            if (schluessel.Length == 0)
            {
                MessageBox.Show(MyResource.Resource.GESETZ_MSG_SCHLUESSEL_FEHLT,
                                this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form_Gesetzesparameter.KlasseItem ki =
                cbKlasse.SelectedItem as Form_Gesetzesparameter.KlasseItem;
            Ergebnis = new GesetzParameter(
                _id, schluessel,
                ki == null ? DbWerte.GESETZ_KLASSE_KWKG : ki.Wert,
                jahr, wert,
                cbEinheit.SelectedItem == null ? DbWerte.GESETZ_EINHEIT_OHNE : cbEinheit.SelectedItem.ToString(),
                cbStatus.SelectedItem == null ? DbWerte.GESETZ_STATUS_GESICHERT : cbStatus.SelectedItem.ToString(),
                (tbQuelle.Text ?? "").Trim());

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
