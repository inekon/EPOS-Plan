using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Feldsetzweg: die EINE Stelle, an der eine Assistentenaktion ein Bedienelement
    /// einer Maske anfasst (Umsetzungskonzept Etappe 3b, Paket F3; Fachkonzept 11.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum eine eigene Klasse und nicht ein Anbau am <see cref="KiAusfuehrer"/>.</b>
    /// Der Ausfuehrer traegt die Hausfallen, die JEDE Aktion angehen: UI-Thread,
    /// Einlaeufigkeit, dialogfreier Modus, Riegel, Protokoll. Der Zugriff auf Masken und
    /// Controls geht nur die fuenf Dialogaktionen etwas an. Er steht deshalb neben dem
    /// Ausfuehrer und nicht in ihm - genauso, wie <see cref="KiSchreibschutz"/> und
    /// <see cref="KiSicherungspunkt"/> ihre eigenen Dateien haben. Der Ausfuehrer behaelt
    /// von der Formularsteuerung genau EINE Zeile: die Modalitaetsweiche.
    /// </para>
    /// <para>
    /// <b>Alles hier laeuft schon auf dem UI-Thread.</b> Gerufen wird ausschliesslich aus
    /// <c>Vorbedingung</c>, <c>Vorschau</c> und <c>Ausfuehren</c> einer
    /// <see cref="KiAktion"/>, und die drei marshallt der Ausfuehrer ueber
    /// <c>AufUiThread</c> (<c>KiAusfuehrer.cs:614</c>). Diese Klasse marshallt deshalb
    /// NICHT noch einmal - sie PRUEFT nur (<see cref="Fremder"/>) und lehnt im Klartext ab,
    /// wenn ein Control doch zu einem anderen Bedienfaden gehoert. Ein eigener
    /// <c>Invoke</c> waere hier ein zweiter Wechsel mitten in einer Aktion, die bereits
    /// richtig steht - und er verdeckte den Fehler, statt ihn zu zeigen.
    /// </para>
    /// <para>
    /// <b>Kein <c>SendKeys</c>, keine Fensternachrichten.</b> Gesetzt werden
    /// Eigenschaften: <c>TextBox.Text</c>, <c>CheckBox.Checked</c> und die Auswahl einer
    /// <c>ComboBox</c>. Tastatursimulation traefe das Fenster, das gerade den Fokus hat,
    /// und liesse sich nicht darauf festnageln, WAS sie veraendert hat; sie ist damit weder
    /// vorschaufaehig noch protokollierbar.
    /// </para>
    /// </remarks>
    internal static class KiDialogZugriff
    {
        /// <summary>
        /// Das Ergebnis einer Maskenaufloesung: entweder Katalogeintrag samt offener
        /// Maske - oder ein Grund im Klartext.
        /// </summary>
        internal sealed class Bezug
        {
            /// <summary>Der Katalogeintrag; <c>null</c>, wenn abgelehnt wurde.</summary>
            internal KiDialog Eintrag;

            /// <summary>Die offene Maske; <c>null</c>, wenn abgelehnt wurde.</summary>
            internal Form Maske;

            /// <summary>Der Klartextgrund; <c>null</c>, wenn alles stimmt.</summary>
            internal string Grund;

            /// <summary>Liegt eine brauchbare Maske vor?</summary>
            internal bool Ok
            {
                get { return Grund == null; }
            }

            internal static Bezug Nein(string grund)
            {
                return new Bezug { Grund = grund };
            }
        }

        // =====================================================================
        // Maske finden
        // =====================================================================

        /// <summary>
        /// Loest den Katalogeintrag und die dazugehoerige offene Maske auf.
        /// </summary>
        /// <param name="maskenname">
        /// Typname der Maske. Leer heisst „die gerade offene Katalogmaske" - dann muss es
        /// GENAU EINE geben (Fachkonzept 11.4: der Parameter ist optional).
        /// </param>
        /// <param name="mussAktivSein">
        /// Muss die Maske auch das aktive Fenster sein? Fuer die Formularaktionen ja
        /// (Umsetzungskonzept 3b, Bestandsanker B4), fuer das reine Lesen nicht.
        /// </param>
        /// <remarks>
        /// <b>Genau eine Instanz, sonst Klartext.</b> Zwei gleichartige Masken nebeneinander
        /// waeren fuer den Assistenten nicht auseinanderzuhalten; er wuerde in irgendeine
        /// von beiden schreiben. Das ist der eine Fall, in dem eine Rueckfrage nichts
        /// nuetzt - der Anwender kann die gemeinte Maske nicht benennen, weil beide
        /// denselben Typnamen tragen. Also: Ablehnung.
        /// </remarks>
        internal static Bezug Aufloesen(string maskenname, bool mussAktivSein)
        {
            KiDialogKatalog katalog = KiDialoge.Katalog;
            string gesucht = (maskenname ?? "").Trim();

            KiDialog eintrag;
            Form maske;

            if (gesucht.Length == 0)
            {
                var offene = new List<KiDialog>();
                var fenster = new List<Form>();
                OffeneKatalogmasken(katalog, offene, fenster);

                if (offene.Count == 0)
                    return Bezug.Nein(string.Format(CultureInfo.CurrentCulture,
                                                    KiDialogTexte.KeineOffen,
                                                    Aufzaehlen(katalog.Maskennamen())));
                if (offene.Count > 1)
                    return Bezug.Nein(string.Format(CultureInfo.CurrentCulture,
                                                    KiDialogTexte.MehrereOffen,
                                                    Anzeigenamen(offene)));

                eintrag = offene[0];
                maske = fenster[0];
            }
            else
            {
                eintrag = katalog.Finde(gesucht);
                if (eintrag == null)
                    return Bezug.Nein(string.Format(CultureInfo.CurrentCulture,
                                                    KiDialogTexte.MaskeUnbekannt, gesucht,
                                                    Aufzaehlen(katalog.Maskennamen())));

                var treffer = new List<Form>();
                Offene(eintrag.Maskenname, treffer);

                if (treffer.Count == 0)
                    return Bezug.Nein(string.Format(CultureInfo.CurrentCulture,
                                                    KiDialogTexte.NichtOffen, eintrag.Anzeigename));
                if (treffer.Count > 1)
                    return Bezug.Nein(string.Format(CultureInfo.CurrentCulture,
                                                    KiDialogTexte.MehrfachOffen, eintrag.Anzeigename));

                maske = treffer[0];
            }

            if (mussAktivSein && !IstAktiv(maske))
                return Bezug.Nein(string.Format(CultureInfo.CurrentCulture,
                                                KiDialogTexte.NichtAktiv, eintrag.Anzeigename));

            return new Bezug { Eintrag = eintrag, Maske = maske };
        }

        /// <summary>
        /// Ist <paramref name="maske"/> das aktive Fenster - oder Besitzer des aktiven?
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Besitzerkette gehoert zur Frage.</b> Ein blosser Vergleich mit
        /// <c>Form.ActiveForm</c> ginge im Betrieb nie auf: Der Anwender ruft den
        /// Assistenten ueber <see cref="KiAufrufKnopf"/> aus der Maske heraus, und
        /// <c>Form_KiChat.Oeffnen(besitzer)</c> zeigt das Chatfenster als BESESSENES
        /// Fenster der Maske (<c>Views\Help\Form_KiChat.cs:1491</c>). Im Augenblick des
        /// Klicks auf „Ausfuehren" ist deshalb der Chat aktiv und nicht die Maske. Genau
        /// diese Kette laeuft die Schleife hoch - und nur sie: Ein Fenster, das die Maske
        /// nicht besitzt, macht die Maske nicht zum Arbeitsplatz des Anwenders.
        /// </para>
        /// <para>
        /// Erfasst ist damit auch ein Unterdialog der Maske (etwa die Namensabfrage von
        /// „Speichern unter"). Das ist gewollt: Er gehoert zur Maske, und der Anwender
        /// sitzt weiterhin vor ihr.
        /// </para>
        /// </remarks>
        private static bool IstAktiv(Form maske)
        {
            Form aktiv;
            try { aktiv = Form.ActiveForm; }
            catch { return false; }

            if (aktiv == null || maske == null) return false;
            if (ReferenceEquals(aktiv, maske)) return true;

            for (Form besitzer = aktiv.Owner; besitzer != null; besitzer = besitzer.Owner)
                if (ReferenceEquals(besitzer, maske)) return true;

            return false;
        }

        /// <summary>Alle offenen Fenster dieses Typnamens.</summary>
        private static void Offene(string maskenname, List<Form> ziel)
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f == null || f.IsDisposed) continue;
                    if (string.Equals(f.GetType().Name, maskenname, StringComparison.Ordinal))
                        ziel.Add(f);
                }
            }
            catch (InvalidOperationException)
            {
                // OpenForms kann sich waehrend des Durchlaufs aendern - dasselbe Muster
                // wie in KiAusfuehrer.UiAnker. Dann eben mit dem, was gefunden wurde.
            }
        }

        /// <summary>Alle offenen Masken, die im Katalog stehen.</summary>
        private static void OffeneKatalogmasken(KiDialogKatalog katalog,
                                                List<KiDialog> eintraege, List<Form> fenster)
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f == null || f.IsDisposed) continue;
                    KiDialog d = katalog.Finde(f.GetType().Name);
                    if (d == null) continue;
                    eintraege.Add(d);
                    fenster.Add(f);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        // =====================================================================
        // Control finden
        // =====================================================================

        /// <summary>
        /// Loest einen Controlpfad in der Maske auf; <c>null</c>, wenn es ihn nicht gibt.
        /// </summary>
        /// <remarks>
        /// Bewusst dieselbe Bauart wie <c>HelpExtender.FindControlRecursive</c>
        /// (<c>Allgemein\Hilfe\HelpCatalog.cs:306</c>): Punkte trennen Zwischenbehaelter,
        /// jede Stufe wird in der Tiefe gesucht, verglichen wird ohne Ruecksicht auf
        /// Gross-/Kleinschreibung. Nachgebaut statt mitbenutzt, weil jene Methode
        /// <c>private</c> in einer Komponente steckt, die einen Katalog im Konstruktor
        /// verlangt - eine oeffentliche Fassade nur fuer diesen Zweck waere mehr Aenderung
        /// am Bestand als der Nachbau. Die Regel selbst darf nicht auseinanderlaufen; sie
        /// steht deshalb hier vollstaendig und mit Verweis auf ihr Vorbild.
        /// </remarks>
        internal static Control Aufloesen(Control behaelter, string pfad)
        {
            if (behaelter == null || string.IsNullOrEmpty(pfad)) return null;

            int punkt = pfad.IndexOf(KiControlpfad.Trenner);
            if (punkt < 0) return Tiefensuche(behaelter, pfad.Trim());

            string stufe = pfad.Substring(0, punkt).Trim();
            string rest = pfad.Substring(punkt + 1).Trim();

            Control naechster = Tiefensuche(behaelter, stufe);
            return naechster == null ? null : Aufloesen(naechster, rest);
        }

        /// <summary>Sucht ein Control ueber alle Ebenen hinweg.</summary>
        private static Control Tiefensuche(Control wurzel, string name)
        {
            if (wurzel == null || string.IsNullOrEmpty(name)) return null;
            if (string.Equals(wurzel.Name, name, StringComparison.OrdinalIgnoreCase)) return wurzel;

            foreach (Control kind in wurzel.Controls)
            {
                Control treffer = Tiefensuche(kind, name);
                if (treffer != null) return treffer;
            }
            return null;
        }

        /// <summary>
        /// Gehoert das Control zu einem anderen Bedienfaden? Dann wird nichts angefasst.
        /// </summary>
        private static bool Fremder(Control steuerelement)
        {
            try { return steuerelement.InvokeRequired; }
            catch { return true; }
        }

        // =====================================================================
        // Felder lesen und setzen
        // =====================================================================

        /// <summary>
        /// Liest den aktuellen Inhalt eines Feldes als Klartext - genau den Text, den der
        /// Anwender sieht.
        /// </summary>
        /// <returns>Der Inhalt; leerer Text, wenn das Feld leer ist oder nicht lesbar.</returns>
        internal static string LiesText(Control steuerelement)
        {
            if (steuerelement == null || Fremder(steuerelement)) return "";

            CheckBox haekchen = steuerelement as CheckBox;
            if (haekchen != null)
                return KiSchema.WertAlsText(haekchen.Checked, CultureInfo.CurrentCulture);

            ComboBox auswahl = steuerelement as ComboBox;
            if (auswahl != null)
                return auswahl.SelectedItem != null
                    ? auswahl.GetItemText(auswahl.SelectedItem)
                    : (auswahl.Text ?? "");

            return steuerelement.Text ?? "";
        }

        /// <summary>
        /// Prueft, ob sich in dieses Feld ueberhaupt schreiben laesst - OHNE zu schreiben.
        /// </summary>
        /// <returns>Der Klartextgrund, oder <c>null</c>, wenn es geht.</returns>
        /// <remarks>
        /// Getrennt von <see cref="Setze"/>, weil die Bestaetigungsschicht beides zu
        /// verschiedenen Zeitpunkten braucht: die Pruefung schon in der Vorbedingung (vor
        /// der Vorschau, Fachkonzept 3.5) und das Setzen erst nach dem Klick. Ein
        /// Vorschaublock fuer ein Feld, das sich gar nicht setzen laesst, waere eine
        /// Bestaetigung ohne Gegenstand.
        /// </remarks>
        internal static string PruefeSetzbar(KiDialogFeld feld, Control steuerelement)
        {
            if (steuerelement == null)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ControlFehlt,
                                     feld.Anzeigename, feld.Controlpfad);

            if (Fremder(steuerelement))
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FalscherThread,
                                     feld.Anzeigename);

            bool bekannt = steuerelement is TextBox || steuerelement is CheckBox ||
                           steuerelement is ComboBox;
            if (!bekannt)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ControlArt,
                                     feld.Anzeigename, steuerelement.GetType().Name);

            // Enabled liefert in WinForms den WIRKSAMEN Zustand: ist die Rubrik oder die
            // Maske gesperrt, ist es auch das Feld. Eine eigene Schleife ueber die Eltern
            // waere die zweite Regel fuer dieselbe Frage.
            if (!steuerelement.Enabled)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ControlGesperrt,
                                     feld.Anzeigename);

            TextBox textfeld = steuerelement as TextBox;
            if (textfeld != null && textfeld.ReadOnly)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ControlReadOnly,
                                     feld.Anzeigename);

            return null;
        }

        /// <summary>
        /// Prueft, ob der Wert in dieses Feld passt - OHNE zu schreiben.
        /// </summary>
        /// <returns>Der Klartextgrund, oder <c>null</c>.</returns>
        /// <remarks>
        /// <b>Nur die Auswahlliste hat hier ueberhaupt etwas zu pruefen.</b> Ein Textfeld
        /// nimmt jeden Text an - und genau so soll es sein: Ob „abc" in einem Zahlenfeld
        /// zulaessig ist, entscheidet die Knopfpruefung der Maske
        /// (<c>Program.ZahlPruefen</c>), nicht der Assistent (Fachkonzept 11.2). Wer hier
        /// eine zweite Zahlenpruefung einbaute, haette eine zweite Wahrheit ueber gueltige
        /// Eingaben - und die erste, die auseinanderlaufen kann.
        /// </remarks>
        internal static string PruefeWert(KiDialogFeld feld, Control steuerelement, string wert)
        {
            ComboBox auswahl = steuerelement as ComboBox;
            if (auswahl == null) return null;

            int treffer;
            return AuswahlSuchen(feld, auswahl, wert, out treffer);
        }

        /// <summary>
        /// Setzt das Feld. Vorbedingungen sind <see cref="PruefeSetzbar"/> und
        /// <see cref="PruefeWert"/>; sie werden hier ein zweites Mal gefragt, weil
        /// zwischen Vorschau und Klick eine Minute liegen kann - in der die Maske ein Feld
        /// gesperrt haben kann.
        /// </summary>
        /// <returns>Der Klartextgrund, oder <c>null</c>, wenn gesetzt wurde.</returns>
        internal static string Setze(KiDialogFeld feld, Control steuerelement, string wert)
        {
            string grund = PruefeSetzbar(feld, steuerelement);
            if (grund != null) return grund;

            string text = wert ?? "";

            CheckBox haekchen = steuerelement as CheckBox;
            if (haekchen != null)
            {
                bool gesetzt;
                if (!WahrheitLesen(text, out gesetzt))
                    return string.Format(CultureInfo.CurrentCulture, KiTexte.KeinWahrheitswert,
                                         feld.Anzeigename, text);
                haekchen.Checked = gesetzt;
                return null;
            }

            ComboBox auswahl = steuerelement as ComboBox;
            if (auswahl != null)
            {
                int treffer;
                grund = AuswahlSuchen(feld, auswahl, text, out treffer);
                if (grund != null) return grund;

                // Gesetzt wird der INDEX des per Anzeigetext gefundenen Eintrags - nicht
                // ein Index aus dem Modellaufruf (Umsetzungskonzept 3b, Abschnitt 8:
                // „nur per Anzeigetext, kein Setzen per Index"). Ueber SelectedIndex statt
                // ueber Text, weil eine DropDownList den Text sonst gar nicht uebernimmt
                // und SelectedIndexChanged der Maske ausbliebe.
                auswahl.SelectedIndex = treffer;
                return null;
            }

            steuerelement.Text = text;
            return null;
        }

        /// <summary>
        /// Sucht einen Auswahleintrag anhand seines ANZEIGETEXTES.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ueber den Anzeigetext und nicht ueber den Steuerwert.</b> Der Anwender
        /// sagt, was er auf der Maske liest; der dahinterliegende Persistenzwert steht
        /// nirgends, wo er ihn sehen koennte (Beispiel <c>cb_WartungEinheit</c>:
        /// <c>GetItemText</c> liefert den uebersetzten Namen, der Steuerwert ist ein
        /// sprachneutraler Schluessel). <c>GetItemText</c> liefert genau den Text, den die
        /// Liste anzeigt - einschliesslich <c>DisplayMember</c> und eigener
        /// <c>ToString</c>-Fassungen.
        /// </para>
        /// <para>
        /// <b>Zwei gleichlautende Eintraege sind eine Ablehnung, keine Wahl.</b> Der
        /// Assistent koennte nur raten, welchen der Anwender meint - und der Anwender
        /// saehe an der Bestaetigung nicht, welcher es geworden ist, weil beide gleich
        /// heissen (Umsetzungskonzept 3b, Abschnitt 8).
        /// </para>
        /// </remarks>
        private static string AuswahlSuchen(KiDialogFeld feld, ComboBox auswahl, string wert,
                                            out int treffer)
        {
            treffer = -1;
            string gesucht = (wert ?? "").Trim();
            int anzahl = 0;
            var eintraege = new List<string>();

            for (int i = 0; i < auswahl.Items.Count; i++)
            {
                string anzeige = (auswahl.GetItemText(auswahl.Items[i]) ?? "").Trim();
                eintraege.Add(anzeige);

                if (string.Equals(anzeige, gesucht, StringComparison.CurrentCultureIgnoreCase))
                {
                    anzahl++;
                    if (treffer < 0) treffer = i;
                }
            }

            if (anzahl > 1)
            {
                treffer = -1;
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.AuswahlMehrdeutig,
                                     feld.Anzeigename, gesucht);
            }
            if (anzahl == 0)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.AuswahlUnbekannt,
                                     feld.Anzeigename, gesucht, Aufzaehlen(eintraege));

            return null;
        }

        /// <summary>
        /// Deutet einen Text als Haekchenzustand. Angenommen werden die Woerter beider
        /// Oberflaechensprachen und die Ziffern - der Anwender diktiert „ja", nicht „true".
        /// </summary>
        private static bool WahrheitLesen(string text, out bool wert)
        {
            wert = false;
            string t = (text ?? "").Trim().ToLowerInvariant();

            if (t == "ja" || t == "yes" || t == "true" || t == "1" || t == "x")
            {
                wert = true;
                return true;
            }
            if (t == "nein" || t == "no" || t == "false" || t == "0" || t.Length == 0)
                return true;

            return false;
        }

        // =====================================================================
        // Knoepfe
        // =====================================================================

        /// <summary>
        /// Prueft, ob sich der Knopf ausloesen laesst - OHNE ihn auszuloesen.
        /// </summary>
        /// <returns>Der Klartextgrund, oder <c>null</c>.</returns>
        internal static string PruefeKnopf(KiDialogKnopf knopf, Control steuerelement)
        {
            if (steuerelement == null)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ControlFehlt,
                                     knopf.Anzeigename, knopf.Controlpfad);

            if (Fremder(steuerelement))
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FalscherThread,
                                     knopf.Anzeigename);

            if (!(steuerelement is Button))
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ControlArt,
                                     knopf.Anzeigename, steuerelement.GetType().Name);

            if (!steuerelement.Enabled)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.KnopfGesperrt,
                                     knopf.Anzeigename);

            return null;
        }

        /// <summary>
        /// Loest den Knopf aus - wie ein Klick von Hand.
        /// </summary>
        /// <returns>Der Klartextgrund, oder <c>null</c>, wenn ausgeloest wurde.</returns>
        /// <remarks>
        /// <c>PerformClick</c> ruft dieselbe Ereigniskette wie der Mausklick, also auch die
        /// Eingabepruefung der Maske (<c>EingabenPruefen</c>, <c>VolumenPruefen</c>). Genau
        /// das ist der Zweck: Der Assistent ersetzt die Bestandspruefung nicht, er loest
        /// sie aus (Fachkonzept 11.2, Abnahmepunkt 4).
        /// </remarks>
        internal static string Ausloesen(KiDialogKnopf knopf, Control steuerelement)
        {
            string grund = PruefeKnopf(knopf, steuerelement);
            if (grund != null) return grund;

            ((Button)steuerelement).PerformClick();
            return null;
        }

        // =====================================================================
        // Klartexthilfen
        // =====================================================================

        /// <summary>Zaehlt Namen lesbar auf; lange Listen werden gekuerzt.</summary>
        internal static string Aufzaehlen(IReadOnlyList<string> namen)
        {
            const int HOECHSTENS = 20;

            if (namen == null || namen.Count == 0) return "";

            var teile = new List<string>();
            for (int i = 0; i < namen.Count && i < HOECHSTENS; i++) teile.Add(namen[i]);

            string text = string.Join(", ", teile);
            if (namen.Count > HOECHSTENS) text += ", ... (" + (namen.Count - HOECHSTENS) + ")";
            return text;
        }

        private static string Anzeigenamen(List<KiDialog> dialoge)
        {
            var namen = new List<string>();
            foreach (KiDialog d in dialoge) namen.Add(d.Anzeigename);
            return Aufzaehlen(namen);
        }
    }
}
