using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog „KWK-Zuschlag je BHKW-Modul" (Etappe E6, Nutzerentscheidung 18.08.2026:
    /// „Je BHKW-Modul — erst damit sind die gesetzlichen Leistungsklassen abbildbar").
    ///
    /// <para>Er pflegt die acht Angaben aus Migrationsschritt 22 an
    /// <c>Tab_Energieanlagen</c> und zeigt zu jeder Anlage den <b>Katalogvorschlag</b>
    /// samt Herleitung. Der Vorschlag wird ausdrücklich <b>nicht</b> automatisch
    /// angesetzt: Er erscheint als Text, und erst „Vorschlag übernehmen" schreibt ihn in
    /// die Satzfelder. Ohne diesen Griff bleibt jede Anlage beim Projektsatz — genau
    /// daran hängt die Ergebnisneutralität für Bestandsprojekte.</para>
    ///
    /// <para>Alle Felder sind leer zulässig; leer heißt „kein eigener Wert, es gilt der
    /// Projektwert". Die Projektvorgaben stehen im Parameterdialog und sind dort als
    /// Vorgabe benannt.</para>
    ///
    /// <para>Die Oberfläche steht in <c>Form_KwkgModule.Designer.cs</c>, weiterhin ohne
    /// eigene <c>.resx</c>: Der Dialog ist — wie sein Muster
    /// <c>Form_WirtschaftlichkeitParameter</c> — nicht lokalisiert, im Designer stehen
    /// deshalb nur Platzhalter (der Feldname) und die echten deutschen Texte setzt
    /// <see cref="TexteSetzen"/> unmittelbar nach <c>InitializeComponent()</c>. Die
    /// Herleitungstexte kommen dagegen aus <c>MyResource</c>, weil dieselben Texte auch
    /// im Ergebnis erscheinen. Nicht serialisierbar und deshalb im Konstruktor-Nachlauf:
    /// die Auswahllisten (<see cref="AuswahlListenFuellen"/> — DB-Persistenzwerte aus
    /// <c>DbWerte</c>), die Anlagenliste (<see cref="ListeFuellen"/>), die Umbruchhöhe
    /// des Hinweises (<see cref="HinweisHoeheAnpassen"/>) und die Erstauswahl
    /// (<see cref="ErsteZeileWaehlen"/>).</para>
    /// </summary>
    public partial class Form_KwkgModule : Form
    {
        private readonly KwkgAnlagenCtrl _ctrl = new KwkgAnlagenCtrl();
        private readonly List<KwkgAnlagenAngabe> _anlagen;
        private readonly WirtschaftlichkeitParameter _projekt;
        private readonly GesetzKatalog _katalog = new GesetzKatalog();

        private int _aktuell = -1;
        private bool _stumm;   // true, während der Dialog die Felder selbst füllt

        /// <summary>true, wenn mindestens eine Zeile gespeichert wurde.</summary>
        public bool Gespeichert { get; private set; }

        public Form_KwkgModule(int idStamm, string stammName, WirtschaftlichkeitParameter projekt)
        {
            _projekt = projekt ?? new WirtschaftlichkeitParameter();
            _anlagen = _ctrl.LadeGruppe(idStamm, stammName);

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Bisher stand hier AutoScaleMode.Font OHNE
            // AutoScaleDimensions, der Skalierfaktor blieb damit (1,1) — es wurde also
            // faktisch nie skaliert. Die Anwendung läuft ohnehin DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). None hält genau dieses Verhalten
            // fest und verhindert, dass ein Designer-Speichern die Skalierung erstmals
            // scharf schaltet.
            InitializeComponent();
            TexteSetzen();
            AuswahlListenFuellen();
            ListeFuellen();
            HinweisHoeheAnpassen();   // NACH TexteSetzen — misst den echten Text
            ErsteZeileWaehlen();      // zuletzt, wie bisher am Ende von Aufbauen()
        }

        // -------------------------------------------------- Aufbau-Nachlauf

        /// <summary>
        /// Setzt alle sichtbaren Texte. Läuft direkt nach <c>InitializeComponent()</c> und
        /// ersetzt die dortigen Platzhalter. Die Texte sind (wie im Bestand) deutsche
        /// Literale — die Lokalisierung dieses Dialogs ist ein eigener Vorgang; hier steht
        /// nur, dass sie an genau einer Stelle liegen.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = "KWK-Zuschlag je BHKW-Modul";
            _lblListe.Text = "BHKW-Anlagen der Vergleichsgruppe:";
            _lblStichtag.Text = "Stichtag (Bestellung/Genehmigung):";
            _lblIbn.Text = "Inbetriebnahme:";
            _lblArt.Text = "Anlagenart:";
            _lblFall.Text = "Eigenstrom nach § 6 Abs. 3:";
            _lblEinsp.Text = "Satz Einspeisung [ct/kWh] (0 = Projektsatz):";
            _lblEigen.Text = "Satz Eigenstrom [ct/kWh] (0 = Projektsatz):";
            _lblKontingent.Text = "Vbh-Kontingent [h] (0 = Projektwert):";
            _lblDeckel.Text = "Vbh-Jahresdeckel [h/a] (0 = Staffel):";
            _lblKopf.Text = "Katalogvorschlag (§ 7 KWKG 2025)";
            _btnUebernehmen.Text = "Vorschlag in die Satzfelder übernehmen";
            _lblHinweis.Text =
                "Leere Felder heißen „kein eigener Wert“ — dann gilt die Projektvorgabe aus dem " +
                "Parameterdialog. Der Vorschlag wird NICHT automatisch angesetzt: Erst die " +
                "Schaltfläche schreibt ihn in die Satzfelder, und erst dann rechnet diese Anlage " +
                "mit einem eigenen Satz. Vollbenutzungsstunden, Jahresdeckel und Kontingent " +
                "gelten nach § 8 KWKG je Anlage.";
            // Standardpaar eines Eingabedialogs: „OK" (AcceptButton) und „Abbrechen"
            // (CancelButton). Der Knopf hieß bis zur Design-Politur 21.08.2026
            // „Speichern"; gespeichert wird weiterhin in Speichern_Klick — nur die
            // Beschriftung folgt jetzt dem Standard.
            _btnOk.Text = "OK";
            _btnAbbrechen.Text = "Abbrechen";
        }

        /// <summary>
        /// Füllt die beiden Auswahllisten. Steht bewusst NICHT im Designer: Die Steuerwerte
        /// sind DB-Persistenzwerte (<c>DbWerte.KWKG_*</c>) und dürfen nicht als Literale in
        /// Designer-Code geraten; die Einträge selbst sind Objekte einer privaten Klasse und
        /// wären ohnehin nicht serialisierbar.
        ///
        /// <para>Läuft unter dem <c>_stumm</c>-Wächter: Im Bestand wurde
        /// <c>SelectedIndexChanged</c> erst NACH <c>SelectedIndex = 0</c> angehängt, das
        /// Vorbelegen löste also kein Ereignis aus. Der Designer verdrahtet den Handler
        /// zwangsläufig vorher — der Wächter stellt denselben Zustand her.</para>
        /// </summary>
        private void AuswahlListenFuellen()
        {
            _stumm = true;
            try
            {
                _cbArt.Items.AddRange(new object[]
                {
                    new Steuerwahl("", "(nicht erfasst — gilt als Neuanlage)"),
                    new Steuerwahl(DbWerte.KWKG_ANLAGENART_NEU,           "neue Anlage (§ 8 Abs. 1)"),
                    new Steuerwahl(DbWerte.KWKG_ANLAGENART_MODERNISIERT,  "modernisiert (§ 8 Abs. 2)"),
                    new Steuerwahl(DbWerte.KWKG_ANLAGENART_NACHGERUESTET, "nachgerüstet (§ 8 Abs. 3)")
                });
                _cbArt.SelectedIndex = 0;

                _cbFall.Items.AddRange(new object[]
                {
                    new Steuerwahl(DbWerte.KWKG_EIGENFALL_KEINER, "kein Tatbestand (kein Eigenstromzuschlag)"),
                    new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR1,    "Nr. 1 — Anlage bis 100 kW"),
                    new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR2,    "Nr. 2 — Kundenanlage / geschl. Netz"),
                    new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR3,    "Nr. 3 — stromkostenintensiv")
                });
                _cbFall.SelectedIndex = 0;
            }
            finally { _stumm = false; }
        }

        /// <summary>
        /// Füllt die Anlagenliste. Steht nicht im Designer: Der Inhalt kommt aus
        /// <c>KwkgAnlagenCtrl.LadeGruppe()</c> und hängt an den Konstruktorargumenten.
        /// </summary>
        private void ListeFuellen()
        {
            foreach (KwkgAnlagenAngabe g in _anlagen)
                _liste.Items.Add(g.Projektname + " · " + g.Bezeichner +
                                 " (" + g.PelKW.ToString("N0") + " kW)");
        }

        /// <summary>
        /// Die Höhe des Hinweistextes hängt am Zeilenumbruch und ist deshalb nicht
        /// serialisierbar. Im Designer steht der auf diesem Stand gemessene Wert
        /// (780 × 51 px = 3 Zeilen Segoe UI 9 pt à 15 px + 6 px Luft), damit die
        /// Festkoordinaten von <c>_btnOk</c>/<c>_btnAbbrechen</c> und die
        /// <c>ClientSize</c> dazu passen; gemessen wird hier — nach
        /// <see cref="TexteSetzen"/>, also am ECHTEN Text und nicht am Platzhalter.
        ///
        /// <para>Die Breite 780 (= ClientSize 804 − 2 × 12 px Rand) muss mit der Größe
        /// von <c>_lblHinweis</c> im Designer übereinstimmen, sonst zeigt die
        /// Entwurfsansicht eine andere Zeilenzahl als der laufende Dialog. Sie ist mit
        /// der Verbreiterung des Dialogs am 21.08.2026 von 696 auf 780 gegangen; der
        /// Text bricht zwischen rund 640 und 900 px gleichbleibend auf 3 Zeilen, die
        /// Höhe 51 und damit die <c>ClientSize</c>-Höhe blieben deshalb unverändert.</para>
        /// </summary>
        private void HinweisHoeheAnpassen()
        {
            _lblHinweis.Size = new Size(780, TextRenderer.MeasureText(
                _lblHinweis.Text, this.Font, new Size(780, 0), TextFormatFlags.WordBreak).Height + 6);
        }

        /// <summary>
        /// Erstauswahl — läuft zuletzt, weil <see cref="Liste_Wechsel"/> die gefüllten
        /// Auswahllisten braucht (wie bisher am Ende von <c>Aufbauen()</c>).
        /// </summary>
        private void ErsteZeileWaehlen()
        {
            if (_liste.Items.Count > 0) _liste.SelectedIndex = 0;
            else FelderAktiv(false);
        }

        /// <summary>Ein Eintrag einer Auswahlliste: sprachneutraler Steuerwert für die
        /// Datenbank, deutscher Text für die Anzeige (Muster Form_WirtschaftlichkeitParameter).</summary>
        private class Steuerwahl
        {
            public readonly string Wert;
            private readonly string _text;
            public Steuerwahl(string wert, string text) { Wert = wert; _text = text; }
            public override string ToString() { return _text; }
        }

        // ------------------------------------------------------------- Bedienung

        private void FelderAktiv(bool an)
        {
            _dtStichtag.Enabled = an; _dtIbn.Enabled = an;
            _cbArt.Enabled = an; _cbFall.Enabled = an;
            _numEinsp.Enabled = an; _numEigen.Enabled = an;
            _numKontingent.Enabled = an; _numDeckel.Enabled = an;
            _btnUebernehmen.Enabled = an;
        }

        private void Liste_Wechsel(object sender, EventArgs e)
        {
            UebernimmFelder();                 // die zuvor gewählte Zeile sichern
            _aktuell = _liste.SelectedIndex;
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) { FelderAktiv(false); return; }

            KwkgAnlagenAngabe g = _anlagen[_aktuell];
            _stumm = true;
            try
            {
                FelderAktiv(true);
                Datum(_dtStichtag, g.Stichtag);
                Datum(_dtIbn, g.Inbetriebnahme);
                Waehle(_cbArt, g.Anlagenart);
                Waehle(_cbFall, g.Eigenfall);
                _numEinsp.Value = Geklemmt(_numEinsp, g.SatzEinspCt);
                _numEigen.Value = Geklemmt(_numEigen, g.SatzEigenCt);
                _numKontingent.Value = Geklemmt(_numKontingent, g.VbhKontingent);
                _numDeckel.Value = Geklemmt(_numDeckel, g.VbhDeckel);
            }
            finally { _stumm = false; }
            VorschlagZeigen();
        }

        /// <summary>Anlagenart und Eigenstromfall verändern den Vorschlag, deshalb wird er
        /// unmittelbar neu gebildet.</summary>
        private void Feld_Wechsel(object sender, EventArgs e)
        {
            if (_stumm) return;
            UebernimmFelder();
            VorschlagZeigen();
        }

        /// <summary>Die Bildschirmfelder in die Liste zurückschreiben (ohne Datenbank).</summary>
        private void UebernimmFelder()
        {
            if (_stumm || _aktuell < 0 || _aktuell >= _anlagen.Count) return;
            KwkgAnlagenAngabe g = _anlagen[_aktuell];
            g.Stichtag = _dtStichtag.Checked ? (DateTime?)_dtStichtag.Value.Date : null;
            g.Inbetriebnahme = _dtIbn.Checked ? (DateTime?)_dtIbn.Value.Date : null;
            g.Anlagenart = Gewaehlt(_cbArt);
            g.Eigenfall = Gewaehlt(_cbFall);
            // 0 heißt „kein eigener Wert": Ein Zuschlagssatz von null wäre fachlich kein
            // Satz, und ein Kontingent von null keine Laufzeit. Die Unterscheidung ist
            // wichtig, weil sonst niemand mehr zum Projektwert zurückkäme.
            g.SatzEinspCt = _numEinsp.Value > 0 ? (double?)_numEinsp.Value : null;
            g.SatzEigenCt = _numEigen.Value > 0 ? (double?)_numEigen.Value : null;
            g.VbhKontingent = _numKontingent.Value > 0 ? (double?)_numKontingent.Value : null;
            g.VbhDeckel = _numDeckel.Value > 0 ? (double?)_numDeckel.Value : null;
        }

        private void VorschlagZeigen()
        {
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) { _lblVorschlag.Text = ""; return; }
            KwkgAnlagenAngabe g = _anlagen[_aktuell];
            KwkgSatzVorschlag v = Vorschlag(g);
            _lblVorschlag.Text =
                "Einspeisung " + v.SatzEinspeisungCt.ToString("N2", BerichtTexte.Kultur) + " ct/kWh — " +
                v.HerleitungEinspeisung + Environment.NewLine + Environment.NewLine +
                "Eigenstrom " + v.SatzEigenCt.ToString("N2", BerichtTexte.Kultur) + " ct/kWh — " +
                v.HerleitungEigen;
        }

        /// <summary>Der Vorschlag für die gewählte Anlage — mit dem Inbetriebnahmejahr
        /// DIESER Anlage als Stichtag, ersatzweise dem des Projekts.</summary>
        private KwkgSatzVorschlag Vorschlag(KwkgAnlagenAngabe g)
        {
            int jahr = g.Inbetriebnahme.HasValue
                ? g.Inbetriebnahme.Value.Year
                : (_projekt.KwkgInbetriebnahme.HasValue
                    ? _projekt.KwkgInbetriebnahme.Value.Year
                    : DateTime.Now.Year + 1);
            return KwkgSatzRechner.Vorschlag(g.PelKW, jahr, g.Anlagenart, g.Eigenfall,
                                             _katalog.WertMitHerkunft, BerichtTexte.Kultur);
        }

        private void Uebernehmen_Klick(object sender, EventArgs e)
        {
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) return;
            UebernimmFelder();
            KwkgSatzVorschlag v = Vorschlag(_anlagen[_aktuell]);
            _stumm = true;
            try
            {
                _numEinsp.Value = Geklemmt(_numEinsp, v.SatzEinspeisungCt);
                _numEigen.Value = Geklemmt(_numEigen, v.SatzEigenCt);
            }
            finally { _stumm = false; }
            UebernimmFelder();
            VorschlagZeigen();
        }

        private void Speichern_Klick(object sender, EventArgs e)
        {
            UebernimmFelder();
            int fehler = 0;
            foreach (KwkgAnlagenAngabe g in _anlagen)
                if (!_ctrl.Speichere(g)) fehler++;

            if (fehler > 0)
            {
                MessageBox.Show(fehler + " von " + _anlagen.Count +
                                " Anlagen konnten nicht gespeichert werden.", "Fehler",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Gespeichert = _anlagen.Count > 0;
            this.DialogResult = DialogResult.OK;
            Close();
        }

        // ------------------------------------------------------------- Kleinkram

        private static void Datum(DateTimePicker dt, DateTime? wert)
        {
            if (wert.HasValue && wert.Value >= dt.MinDate && wert.Value <= dt.MaxDate)
            {
                dt.Value = wert.Value;
                dt.Checked = true;
            }
            else dt.Checked = false;
        }

        private static void Waehle(ComboBox cb, string wert)
        {
            for (int i = 0; i < cb.Items.Count; i++)
                if (string.Equals(((Steuerwahl)cb.Items[i]).Wert, wert ?? "", StringComparison.Ordinal))
                { cb.SelectedIndex = i; return; }
            cb.SelectedIndex = 0;
        }

        private static string Gewaehlt(ComboBox cb)
        {
            var w = cb.SelectedItem as Steuerwahl;
            return w != null ? w.Wert : "";
        }

        private static decimal Geklemmt(NumericUpDown num, double? wert)
        {
            if (!wert.HasValue) return num.Minimum;
            decimal d;
            try { d = Convert.ToDecimal(wert.Value); } catch { return num.Minimum; }
            return d < num.Minimum ? num.Minimum : (d > num.Maximum ? num.Maximum : d);
        }
    }
}
