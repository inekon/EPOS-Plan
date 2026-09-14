using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der PARAMETERSATZ der Namensabfrage (<c>NamensDialog</c>) — die eine
    /// Stelle, an der ein Wirt die Frage „Wie soll es heißen?" als
    /// <c>Ueberlagerung</c> in seinem eigenen Fenster zeigt.
    ///
    /// <para><b>Warum hier und nicht in der Windows-Schale.</b> Ein
    /// Parametersatz ist sechs Texte; ein Fenster braucht er nicht. Er liegt
    /// deshalb plattformfrei, damit ihn auch eine Hülle in
    /// <c>EPOS.UI.Daten</c> bauen kann — <c>NamensDialogHuelle</c> in der
    /// Windows-Schale bleibt für die Aufrufer, die wirklich ein eigenes
    /// Fenster öffnen (WinForms-Masten ohne Wirt).</para>
    /// </summary>
    internal static class NamensabfrageGaben
    {
        /// <summary>Titel, Frage, Vorbelegung und die Leermeldung samt Knopftexten.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            string titel, string frage, string vorbelegung, string meldungLeer = null)
        {
            return new Dictionary<string, object>
            {
                ["TitelText"] = titel ?? "",
                ["FrageText"] = frage ?? "",
                ["Vorbelegung"] = vorbelegung ?? "",
                ["MeldungLeer"] = meldungLeer ?? "",
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }
    }
}
