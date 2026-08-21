using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Anzeige des Validierungsprotokolls (AP5). Ersetzt die frueheren
    /// Abbruch-MessageBoxen des Ganglinienimports: Fehler blockieren den Import,
    /// Warnungen und Eingriffe (Schaltjahr, Sommerzeit, Minutenmittelung) werden
    /// zur Bestaetigung vorgelegt, ein sauberer Lauf laeuft ohne Nachfrage durch.
    /// </summary>
    /// <remarks>
    /// Die Oberflaeche steht in <c>Form_GanglinieProtokoll.Designer.cs</c>, weiterhin
    /// ohne eigene <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und
    /// werden in <see cref="TexteSetzen"/> gesetzt; im Designer stehen nur Platzhalter.
    /// Was von den Konstruktorparametern abhaengt - Kopftext, Beschriftung des zweiten
    /// Knopfes, Freigabe von "Uebernehmen" und die Wahl des <c>AcceptButton</c> - sowie
    /// das Fuellen der Liste stehen im Konstruktor. Die Uebersetzung der Engine-
    /// Schluessel liegt in <see cref="GanglinienProtokollText"/>.
    /// </remarks>
    public partial class Form_GanglinieProtokoll : Form
    {
        /// <summary>
        /// Baut den Dialog auf.
        /// </summary>
        /// <param name="meldungen">Anzuzeigende Meldungen.</param>
        /// <param name="importMoeglich">Kein Fehler - die Schaltflaeche "Uebernehmen" ist aktiv.</param>
        /// <param name="bestaetigungNoetig">An der Reihe wurde etwas veraendert; der Anwender muss bestaetigen.</param>
        public Form_GanglinieProtokoll(IList<PruefMeldung> meldungen, bool importMoeglich, bool bestaetigungNoetig)
        {
            // Der Designer setzt AutoScaleMode bewusst auf None und laesst
            // AutoScaleDimensions weg: Die Anwendung laeuft DpiUnaware (app.manifest,
            // Program.SetHighDpiMode). Der bisherige Aufbau setzte zwar
            // AutoScaleMode.Font, aber nie AutoScaleDimensions - der Skalierungsfaktor
            // blieb damit immer 1:1, es fand also faktisch keine Skalierung statt.
            // None haelt genau dieses Verhalten fest.
            InitializeComponent();
            TexteSetzen();

            lbl_Kopf.Text = !importMoeglich
                ? MyResource.Resource.IMPORT_KOPF_FEHLER
                : (bestaetigungNoetig ? MyResource.Resource.IMPORT_KOPF_EINGRIFF
                                      : MyResource.Resource.IMPORT_KOPF_OK);

            if (meldungen != null)
            {
                foreach (PruefMeldung m in meldungen)
                {
                    ListViewItem item = new ListViewItem(GanglinienProtokollText.StufeText(m.Stufe));
                    item.SubItems.Add(GanglinienProtokollText.Text(m));
                    item.ForeColor = GanglinienProtokollText.StufeFarbe(m.Stufe);
                    listView_Protokoll.Items.Add(item);
                }
            }

            btn_OK.Enabled = importMoeglich;

            btn_Abbrechen.Text = importMoeglich
                ? MyResource.Resource.IMPORT_BTN_ABBRECHEN
                : MyResource.Resource.IMPORT_BTN_SCHLIESSEN;

            AcceptButton = importMoeglich ? btn_OK : btn_Abbrechen;
        }

        // ------------------------------------------------------------------- Texte

        /// <summary>
        /// Setzt die festen sichtbaren Texte aus <c>MyResource</c>. Laeuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.IMPORT_TITEL_PROTOKOLL;
            columnHeader_Stufe.Text = MyResource.Resource.IMPORT_SPALTE_STUFE;
            columnHeader_Meldung.Text = MyResource.Resource.IMPORT_SPALTE_MELDUNG;
            btn_OK.Text = MyResource.Resource.IMPORT_BTN_UEBERNEHMEN;
        }

        /// <summary>
        /// Zeigt das Protokoll und liefert <c>true</c>, wenn der Import fortgesetzt
        /// werden soll. Ein fehlerfreier Lauf ohne Eingriffe wird gar nicht erst
        /// angezeigt.
        /// </summary>
        /// <param name="eltern">Elternfenster.</param>
        /// <param name="meldungen">Meldungen.</param>
        /// <param name="importMoeglich">Kein Fehler im Protokoll.</param>
        /// <param name="bestaetigungNoetig">Warnung oder Eingriff an der Reihe.</param>
        public static bool Zeigen(IWin32Window eltern, IList<PruefMeldung> meldungen,
                                  bool importMoeglich, bool bestaetigungNoetig)
        {
            if (importMoeglich && !bestaetigungNoetig) return true;

            using (Form_GanglinieProtokoll dlg =
                   new Form_GanglinieProtokoll(meldungen, importMoeglich, bestaetigungNoetig))
            {
                return dlg.ShowDialog(eltern) == DialogResult.OK && importMoeglich;
            }
        }

        // ==================================================================
        // Oberflaeche - Begruendungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen in Form_GanglinieProtokoll.Designer.cs.
        // Designer-Code traegt keine Kommentare; die Pixelentscheidungen stehen
        // deshalb hier.
        //
        // --- Design-Politur 21.08.2026 -----------------------------------
        //
        // * Echttexte statt Feldnamen. Im Designer standen als Platzhalter die
        //   Feldnamen ("columnHeader_Stufe" usw.), lbl_Kopf und btn_Abbrechen hatten
        //   ueberhaupt keinen Text. Damit war im VS-Designer nicht zu sehen, ob die
        //   Beschriftungen ueberhaupt in ihre Felder passen. Jetzt steht dort der
        //   deutsche Text aus MyResource - als reine VORSCHAU. Gesetzt wird er
        //   weiterhin ausschliesslich in TexteSetzen() bzw. im Konstruktor; die
        //   Maske bleibt zweisprachig.
        //
        // * lbl_Kopf (12/10, 736 x 34) zeigt als Vorschau IMPORT_KOPF_EINGRIFF, den
        //   haeufigsten der drei Faelle: IMPORT_KOPF_OK ist ueber Zeigen() gar nicht
        //   erreichbar (ein sauberer Lauf ohne Eingriff oeffnet den Dialog nicht),
        //   und die Eingriffsmeldung ist mit 90 Zeichen zugleich die laengste.
        //   Gemessen (TextRenderer.MeasureText, Segoe UI 9 pt) braucht sie 477 px
        //   und passt damit einzeilig in die 736 px Feldbreite; die 34 px Hoehe
        //   fangen den Umbruch ab, wenn das Fenster auf MinimumSize geschoben wird
        //   (dort bleiben dem Feld noch 480 px, was gerade eben eine Zeile bleibt).
        //   Keine Groessenaenderung noetig.
        //
        // * btn_Abbrechen zeigt als Vorschau "Abbrechen". Die Beschriftung haengt am
        //   Konstruktorparameter (importMoeglich ? Abbrechen : Schliessen) und wird
        //   deshalb weiter im Konstruktor gesetzt. "Abbrechen" gehoert zum selben
        //   Fall wie der Kopftext der Vorschau - beides ist der Eingriffsfall.
        //
        // * Fussknoepfe auf einheitliche 110 x 30 (vorher btn_OK 90 x 26,
        //   btn_Abbrechen 94 x 26). Die Unterkante bleibt bei y = 408 (Rand 12),
        //   dafuer wandert der Fuss von y = 382 auf y = 378. Die rechte Kante bleibt
        //   bei x = 748 (Rand 12), btn_Abbrechen beginnt also bei 638, btn_OK bei
        //   516; der Abstand zwischen beiden waechst von 8 auf 12 px. Nach oben
        //   bleiben 8 px zur Liste (Unterkante 370). Beide Knoepfe sind
        //   Bottom|Right verankert - die rechte und die untere Kante bleiben damit
        //   auch beim Aufziehen des Fensters konstant.
        //
        // * Die Spaltenbreiten der Liste (90 / 620) bleiben: "Stufe" braucht mit
        //   dem laengsten Inhalt ("Warnung", 7 Zeichen) 57 px, die Summe 710 px
        //   bleibt unter der Listenbreite von 736 px abzueglich Bildlaufleiste.
    }
}
