using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Allgemein;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-HELFER fuer die Namensabfrage (iU9-W1.2, ausgerollt iU9-W2.1).
    ///
    /// <para><b>Wozu.</b> Fuenf Masken des Bestands fragten zeichengleich nach
    /// einem Namen — <c>Form_VariantenName</c>, <c>Form_KostenItemNeu</c>,
    /// <c>Form_StromspeicherItemNeu</c> (28 Aufrufer), <c>Form_GebaeudetypNeu</c>
    /// und <c>Form_AlsVariante</c>. Sie sind jetzt EINE Razor-Komponente
    /// (<see cref="NamensDialog"/>); dieser Helfer ist alles, was ein
    /// WinForms-Aufrufer davon sehen muss: ein Aufruf, ein Rueckgabewert. Mit
    /// iU9-W2.1 sind alle fuenf WinForms-Fassungen geloescht (Regel M1).</para>
    ///
    /// <para><b>Warum kein eigenes Ergebnisobjekt.</b> Die Antwort ist ein
    /// einziger Text. <c>null</c> heisst „abgebrochen", genau wie
    /// <c>DialogResult.Cancel</c> vorher — nur dass der Aufrufer nicht mehr
    /// zwei Dinge (Ergebnis und Dialogausgang) auseinanderhalten muss. Die eine
    /// Maske mit einem ZWEITEN Feld (<c>Form_GebaeudetypNeu</c>: Beschreibung)
    /// bekommt es ueber <see cref="FragenMitBeschreibung"/> als
    /// <c>out</c>-Parameter — ein Ergebnisobjekt fuer einen einzigen Aufrufer
    /// waere mehr Bauwerk als Nutzen.</para>
    ///
    /// <para><b>Drei Abweichungen gegenueber den Vorlaeufern</b> sind gewollt und
    /// im Protokoll <c>iU9_W2_Blazor_Port_Protokoll.md</c> begruendet: Der Name
    /// kommt GETRIMMT zurueck, der Dialog erscheint MITTIG statt an der
    /// Knopfposition (<c>PointToScreen</c> entfaellt), und eine leere Eingabe
    /// meldet sich im Dialog statt in einer MessageBox.</para>
    /// </summary>
    internal static class NamensDialogHuelle
    {
        /// <summary>Innenmass des Fensters. Ein Feld, eine Frage, zwei Knoepfe —
        /// die WinForms-Fassungen massen 354 x 157 bzw. 331 x 137; die Huelle
        /// haelt das Mindestmass der Blazor-Hülle ein.</summary>
        private static readonly Size FENSTER = new Size(520, 360);

        /// <summary>Zuschlag je zusaetzlicher Zeile (Hinweis bzw. zweites Feld).</summary>
        private const int ZEILE = 60;

        /// <summary>
        /// Die Hausabfrage „Bezeichner eingeben" — der Dialog, den
        /// <c>Form_StromspeicherItemNeu</c> 28 Aufrufern gestellt hat. Titel,
        /// Beschriftung und Leermeldung sind seine Texte, wortgleich.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster; <c>null</c> erlaubt.</param>
        /// <param name="vorbelegung">Vorschlag im Feld (die Aufrufer setzten dafuer
        /// <c>m_szName</c> vor <c>SetControl()</c>); <c>null</c> = leeres Feld.</param>
        /// <returns>Der getrimmte Bezeichner oder <c>null</c> bei Abbruch.</returns>
        internal static string Bezeichner(IWin32Window besitzer, string vorbelegung = null)
        {
            return Fragen(besitzer,
                          T("NAMD_TITEL_BEZEICHNER", "Bezeichner eingeben"),
                          T("NAMD_LBL_BEZEICHNER", "Bezeichner"),
                          vorbelegung,
                          T("NAMD_MSG_BEZEICHNUNG", "Bezeichnung eingeben!"));
        }

        /// <summary>
        /// Wie <see cref="Bezeichner"/>, zusaetzlich mit dem Beschreibungsfeld von
        /// <c>Form_GebaeudetypNeu</c>.
        /// </summary>
        internal static string BezeichnerUndBeschreibung(IWin32Window besitzer,
                                                         out string beschreibung)
        {
            return FragenMitBeschreibung(besitzer,
                          T("NAMD_TITEL_BEZEICHNER", "Bezeichner eingeben"),
                          T("NAMD_LBL_BEZEICHNER", "Bezeichner"),
                          vorbelegung: null,
                          meldungLeer: T("NAMD_MSG_BEZEICHNUNG", "Bezeichnung eingeben!"),
                          frageBeschreibung: T("NAMD_LBL_BESCHREIBUNG", "Beschreibung:"),
                          vorbelegungBeschreibung: null,
                          beschreibung: out beschreibung);
        }

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
            string zusatz;
            return Zeigen(besitzer, titel, frage, vorbelegung, meldungLeer,
                          hinweis: null, zusatzFrage: null, zusatzVorbelegung: null,
                          okText: null, abbrechenText: null, okNurMitText: false,
                          zusatz: out zusatz);
        }

        /// <summary>
        /// Fragt nach Bezeichner UND Beschreibung — der Sonderfall
        /// <c>Form_GebaeudetypNeu</c>. Rueckgabe wie <see cref="Fragen"/>;
        /// <paramref name="beschreibung"/> traegt bei OK das zweite Feld und ist
        /// bei Abbruch leer.
        /// </summary>
        internal static string FragenMitBeschreibung(IWin32Window besitzer, string titel,
                                                     string frage, string vorbelegung,
                                                     string meldungLeer,
                                                     string frageBeschreibung,
                                                     string vorbelegungBeschreibung,
                                                     out string beschreibung)
        {
            return Zeigen(besitzer, titel, frage, vorbelegung, meldungLeer,
                          hinweis: null, zusatzFrage: frageBeschreibung,
                          zusatzVorbelegung: vorbelegungBeschreibung,
                          okText: null, abbrechenText: null, okNurMitText: false,
                          zusatz: out beschreibung);
        }

        /// <summary>
        /// Fragt nach einem Namen und stellt einen erklaerenden Satz darueber —
        /// der Sonderfall <c>Form_AlsVariante</c>. Dort ist der OK-Knopf
        /// gesperrt, solange das Feld leer ist; die Knopftexte sind eigene
        /// („Variante anlegen").
        /// </summary>
        internal static string FragenMitHinweis(IWin32Window besitzer, string titel,
                                                string hinweis, string frage,
                                                string okText, string abbrechenText)
        {
            string zusatz;
            return Zeigen(besitzer, titel, frage, vorbelegung: null, meldungLeer: null,
                          hinweis: hinweis, zusatzFrage: null, zusatzVorbelegung: null,
                          okText: okText, abbrechenText: abbrechenText, okNurMitText: true,
                          zusatz: out zusatz);
        }

        /// <summary>Der eine Weg, den alle drei Einstiege nehmen.</summary>
        private static string Zeigen(IWin32Window besitzer, string titel, string frage,
                                     string vorbelegung, string meldungLeer, string hinweis,
                                     string zusatzFrage, string zusatzVorbelegung,
                                     string okText, string abbrechenText, bool okNurMitText,
                                     out string zusatz)
        {
            string ergebnis = null;
            string zweitesFeld = "";
            BlazorDialogForm<NamensDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["TitelText"] = titel ?? "",
                ["FrageText"] = frage ?? "",
                ["Vorbelegung"] = vorbelegung ?? "",
                ["MeldungLeer"] = meldungLeer ?? "",
                ["HinweisText"] = hinweis ?? "",
                ["ZusatzFrageText"] = zusatzFrage ?? "",
                ["ZusatzVorbelegung"] = zusatzVorbelegung ?? "",
                ["OkNurMitText"] = okNurMitText,
                ["OkText"] = string.IsNullOrEmpty(okText)
                             ? MyResource.Resource.ALLG_BTN_OK : okText,
                ["AbbrechenText"] = string.IsNullOrEmpty(abbrechenText)
                             ? MyResource.Resource.ALLG_BTN_ABBRECHEN : abbrechenText,

                ["ZusatzGeschlossen"] = EventCallback.Factory.Create<string>(new object(),
                    text => zweitesFeld = text ?? ""),

                ["Geschlossen"] = EventCallback.Factory.Create<string>(new object(), name =>
                {
                    ergebnis = name;
                    if (dlg != null) dlg.Schliessen(name != null);
                })
            };

            Size groesse = FENSTER;
            if (!string.IsNullOrEmpty(hinweis)) groesse.Height += ZEILE;
            if (!string.IsNullOrEmpty(zusatzFrage)) groesse.Height += ZEILE;

            dlg = new BlazorDialogForm<NamensDialog>(titel ?? "", groesse, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            zusatz = ergebnis == null ? "" : zweitesFeld;
            return ergebnis;
        }

        /// <summary>
        /// Anzeigetext mit deutschem Rueckfall (Drei-Schichten-Regel). Die
        /// <c>NAMD_*</c>-Schluessel kommen mit dem Sammelnachtrag iU9-W2.6 in den
        /// Katalog; bis dahin — und wenn ein Schluessel fehlt — steht der
        /// deutsche Wortlaut der geloeschten Maske.
        /// </summary>
        private static string T(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { /* ein fehlender Katalog darf keinen Dialog mitreissen */ }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
