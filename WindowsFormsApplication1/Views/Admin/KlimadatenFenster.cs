using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Klimadaten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das WINDOWS-FENSTER der Klimadaten (iU9-W14c.7) — seit Auftrag KI‑F8 nur noch
    /// der Adapter.
    ///
    /// <para><b>Die Datenseite liegt in <c>EPOS.UI.Daten</c></b>
    /// (<see cref="KlimadatenHuelle"/>, Regel Auftrag #208): Regionsliste,
    /// Zeichenmodelle, Import, Bereichsabruf und Löschen. Sie führte keine einzige
    /// Windows-Zeile und war trotzdem auf dem iPad unerreichbar; seit KI‑D‑Q8 zeigt
    /// die <c>AppWurzel</c> dieselbe Komponente als Ansicht. Hier bleibt, was nur
    /// Windows kann — das modale Fenster und das Nachziehen der Startseite.</para>
    /// </summary>
    internal static class KlimadatenFenster
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 757 × 641).</summary>
        private static readonly Size MASS = new Size(1180, 780);

        /// <summary>
        /// Zeigt die Klimadaten als eigenes Fenster — der Weg von
        /// <c>HauptfensterHuelle.Ablauf</c> (Menüpunkt „Administration → Klimadaten")
        /// und seit KI‑F8 auch der des Hilfe-Assistenten.
        ///
        /// <para><b>Mit Besitzer und in einem <c>using</c></b> (Befund W14c-B34).</para>
        ///
        /// <para><b>Nach dem Schließen frischt die Startseite ihre Klimaregionen
        /// auf</b> (Welle GM‑1): Sie liest die Liste nur in <c>Startseite.Laden</c>,
        /// also beim Aufbau und auf Meldung des <c>SeitenZustand</c>. Ohne diesen
        /// Rückweg fehlte eine hier importierte Region in ihrem Auswahlfeld bis zum
        /// Projektwechsel, und eine gelöschte stünde weiter darin. Aufgefrischt wird
        /// IMMER — ob importiert, gelöscht oder nichts geändert wurde, sagt der
        /// Rückgabewert des Fensters nicht, und die paar Dutzend Regionen neu zu
        /// lesen kostet nichts.</para>
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<KlimadatenDialog> dlg = null;

            var werte = new Dictionary<string, object>(KlimadatenHuelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<KlimadatenDialog>(
                KlimadatenHuelle.Titel(), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            // Die Startseite hinter dem Fenster liest ihre Regionsliste neu. Steht
            // keine (das Fenster ging aus einer anderen Maske auf), laeuft der
            // Aufruf leer - derselbe Weg wie VariantenAnzeigeAktualisieren.
            StartseiteHuelle.Aktuelle?.KlimaregionenAktualisieren();

            return ok;
        }
    }
}
