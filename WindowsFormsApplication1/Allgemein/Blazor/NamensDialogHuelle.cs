using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Allgemein;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-HELFER fuer die Namensabfrage (iU9-W1.2).
    ///
    /// <para><b>Wozu.</b> Fuenf Masken des Bestands fragten zeichengleich nach
    /// einem Namen — <c>Form_VariantenName</c>, <c>Form_KostenItemNeu</c>,
    /// <c>Form_StromspeicherItemNeu</c> (28 Aufrufer), <c>Form_GebaeudetypNeu</c>
    /// und <c>Form_AlsVariante</c>. Sie sind jetzt EINE Razor-Komponente
    /// (<see cref="NamensDialog"/>); dieser Helfer ist alles, was ein
    /// WinForms-Aufrufer davon sehen muss: ein Aufruf, ein Rueckgabewert.</para>
    ///
    /// <para><b>Warum kein eigenes Ergebnisobjekt.</b> Die Antwort ist ein
    /// einziger Text. <c>null</c> heisst „abgebrochen", genau wie
    /// <c>DialogResult.Cancel</c> vorher — nur dass der Aufrufer nicht mehr
    /// zwei Dinge (Ergebnis und Dialogausgang) auseinanderhalten muss.</para>
    /// </summary>
    internal static class NamensDialogHuelle
    {
        /// <summary>Innenmass des Fensters. Ein Feld, eine Frage, zwei Knoepfe —
        /// die WinForms-Fassungen massen 354 x 157 bzw. 331 x 137; die Huelle
        /// haelt das Mindestmass der Blazor-Hülle ein.</summary>
        private static readonly Size FENSTER = new Size(520, 360);

        /// <summary>
        /// Fragt nach einem Namen. Liefert den getrimmten Namen oder
        /// <c>null</c>, wenn der Anwender abgebrochen hat.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (fuer die mittige Lage); <c>null</c> erlaubt.</param>
        /// <param name="titel">Fenstertitel und Kopfzeile.</param>
        /// <param name="frage">Die Frage ueber dem Feld.</param>
        /// <param name="vorbelegung">Vorschlag im Feld; <c>null</c> = leeres Feld.</param>
        /// <param name="meldungLeer">Meldung bei leerer Eingabe; <c>null</c> = nur nicht
        /// schliessen (das Verhalten von <c>Form_VariantenName</c>).</param>
        internal static string Fragen(IWin32Window besitzer, string titel, string frage,
                                      string vorbelegung, string meldungLeer = null)
        {
            string ergebnis = null;
            BlazorDialogForm<NamensDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["TitelText"] = titel ?? "",
                ["FrageText"] = frage ?? "",
                ["Vorbelegung"] = vorbelegung ?? "",
                ["MeldungLeer"] = meldungLeer ?? "",
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,

                ["Geschlossen"] = EventCallback.Factory.Create<string>(new object(), name =>
                {
                    ergebnis = name;
                    if (dlg != null) dlg.Schliessen(name != null);
                })
            };

            dlg = new BlazorDialogForm<NamensDialog>(titel ?? "", FENSTER, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }
    }
}
