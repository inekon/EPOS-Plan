using System;
using System.Windows.Forms;
using EPOS.UI.Dienste;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-FASSUNG des Hilfedienstes (Umsetzungskonzept iOS, Paket iU8).
    ///
    /// <para><b>Wozu.</b> Eine Razor-Komponente kennt vom Hilfesystem nur
    /// <see cref="IHilfeDienst"/> und einen Schluessel - dieselbe Zeichenkette, die
    /// links in <c>help_mapping.txt</c> steht. Alles andere - der Wiki-Katalog, die
    /// Zuordnungsdatei, das angeheftete Popup - bleibt hier in der Huelle und zieht
    /// nie nach iOS mit.</para>
    ///
    /// <para><b>Ein Weg, nicht zwei.</b> Aufgeloest wird ueber
    /// <c>HelpExtender.ZielFuer</c>, also ueber dieselbe Kette wie beim Klick auf
    /// einen WinForms-Infobutton. Ein zweiter Aufloesungsweg waere die Stelle, an der
    /// WinForms-Maske und Blazor-Dialog irgendwann verschiedene Hilfeseiten
    /// oeffnen.</para>
    ///
    /// <para><b>Vor dem Programmstart gibt es nichts.</b> Den einen
    /// <c>HelpExtender</c> legt <c>HilfeAutomatik.Starten</c> an. Solange der nicht
    /// gelaufen ist - Referenzlauf, Pruefstand, Konsolenwerkzeug -, liefert
    /// <see cref="Aufloesen"/> <c>null</c> und <see cref="Oeffnen"/> tut nichts. Das
    /// ist dasselbe Verhalten wie bei einem Infobutton ohne Zuordnung: sichtbar,
    /// aber folgenlos.</para>
    /// </summary>
    public sealed class WindowsHilfeDienst : IHilfeDienst
    {
        /// <summary>
        /// Das angeheftete Hilfefenster. Es gehoert diesem Dienst, nicht dem
        /// Dialog: Der Dienst ist ein Singleton der Huelle und ueberlebt jeden
        /// einzelnen Blazor-Dialog - genau wie das Popup des
        /// <c>HelpExtender</c> jedes Formular ueberlebt.
        /// </summary>
        private Form_HelpPopup _popup;

        /// <inheritdoc />
        public HilfeEintrag Aufloesen(string schluessel)
        {
            HelpExtender extender = HilfeAutomatik.Extender;
            if (extender == null || string.IsNullOrWhiteSpace(schluessel)) return null;

            try
            {
                return extender.ZielFuer(schluessel);
            }
            catch (Exception ex)
            {
                // Eine Ausnahme aus dem Hilfesystem darf keinen Dialog mitreissen.
                System.Diagnostics.Debug.WriteLine("[Help] FEHLER beim Aufloesen von '" + schluessel + "': " + ex);
                return null;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Angezeigt wird dasselbe angeheftete Popup wie bei einem Infobutton:
        /// Kapitelzeile, Einleitungssatz, Verweis auf die Wikiseite. Nur wenn das
        /// nicht geht - kein Oberflaechenfaden, Popup nicht erzeugbar -, wird die
        /// Adresse ersatzweise im Browser geoeffnet
        /// (<c>Dienste.Datei.MitSystemOeffnen</c>).
        /// </remarks>
        public void Oeffnen(string schluessel)
        {
            HilfeEintrag eintrag = Aufloesen(schluessel);
            if (eintrag == null) return;

            try
            {
                if (_popup == null || _popup.IsDisposed) _popup = new Form_HelpPopup();

                _popup.ShowHelpAngeheftet(eintrag.Tooltip, eintrag.Beschreibung, eintrag.Url ?? "", Cursor.Position);
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Help] Popup nicht moeglich, Rueckfall auf den Browser: " + ex);
            }

            if (!string.IsNullOrEmpty(eintrag.Url)) Dienste.Datei.MitSystemOeffnen(eintrag.Url);
        }
    }
}
