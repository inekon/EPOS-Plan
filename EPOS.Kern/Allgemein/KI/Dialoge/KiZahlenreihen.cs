// DIE FORMEN DER ZAHLENREIHEN des Dialogkatalogs (Welle #458 Stufe 3b).
//
// Eine Zahlenreihe (KiKern.KiZahlenreihe) nennt ihre Stellen beim Namen: „Januar" bis
// „Dezember", „Stunde 1" bis „Stunde 24", „Montag, Stunde 1" bis „Sonntag, Stunde 24".
// Die Namen kommen aus den Ressourcen der Maskenbeschriftungen (ALLG_MONAT_n,
// ALLG_WOCHENTAG_n) und entstehen HIER an einer Stelle - der Katalog wird je Kultur
// gebaut (KiDialoge.Katalog), und damit auch diese Formen.
//
// DIE STUNDEN ZAEHLEN AB 1 - so beschriften die Masken ihre Stundenfelder („1." bis
// „24."; TypProfilDialog, GebaeudetypDialog, KostenprofilDialog). Der Assistent nennt
// die Stelle, die der Anwender auf der Maske liest.

using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Formen der Zahlenreihen, die der Dialogkatalog deklariert (Welle #458 Stufe 3b).
    /// </summary>
    internal static class KiZahlenreihen
    {
        /// <summary>Stunden je Tag.</summary>
        internal const int STUNDEN = 24;

        /// <summary>Tage je Woche.</summary>
        internal const int TAGE = 7;

        /// <summary>Zwoelf Monatswerte, Januar bis Dezember.</summary>
        internal static KiZahlenreihe Monate()
        {
            var stellen = new string[12];
            for (int m = 0; m < 12; m++) stellen[m] = Monatsname(m);
            return new KiZahlenreihe(stellen);
        }

        /// <summary>24 Stundenwerte eines Tages, Stunde 1 bis Stunde 24.</summary>
        internal static KiZahlenreihe Stunden()
        {
            var stellen = new string[STUNDEN];
            for (int s = 0; s < STUNDEN; s++) stellen[s] = Stundenname(s);
            return new KiZahlenreihe(stellen);
        }

        /// <summary>
        /// 168 Wochenwerte, Montag Stunde 1 bis Sonntag Stunde 24 - Tag fuer Tag, je Tag
        /// die 24 Stunden. Die Stelle des Tages t (Montag = 1) und der Stunde s ist
        /// (t − 1) · 24 + s.
        /// </summary>
        internal static KiZahlenreihe Wochenstunden()
        {
            var stellen = new string[TAGE * STUNDEN];
            for (int t = 0; t < TAGE; t++)
                for (int s = 0; s < STUNDEN; s++)
                    stellen[t * STUNDEN + s] = string.Format(
                        CultureInfo.CurrentCulture, MyResource.Resource.KI_DLG_REIHE_WOCHENSTUNDE,
                        Wochentagname(t), (s + 1).ToString(CultureInfo.CurrentCulture));
            return new KiZahlenreihe(stellen);
        }

        /// <summary>
        /// Der Name eines Monats (0 = Januar) - derselbe, den die Monatsfelder der
        /// Bedarfsverwaltungen tragen (<see cref="KiDialogTexte.Monat"/>).
        /// </summary>
        private static string Monatsname(int m) => KiDialogTexte.Monat(m + 1);

        /// <summary>Der Name eines Wochentags (0 = Montag) aus den Ressourcen.</summary>
        private static string Wochentagname(int t)
            => Text("ALLG_WOCHENTAG_" + (t + 1).ToString(CultureInfo.InvariantCulture),
                    (t + 1).ToString(CultureInfo.CurrentCulture));

        /// <summary>„Stunde 7" - gezaehlt ab 1 wie auf der Maske.</summary>
        private static string Stundenname(int s)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_DLG_REIHE_STUNDE,
                             (s + 1).ToString(CultureInfo.CurrentCulture));

        /// <summary>Ein Ressourcentext unter einem gebauten Schluessel; fehlt er, die Vorgabe.</summary>
        private static string Text(string schluessel, string vorgabe)
        {
            string text = MyResource.Resource.ResourceManager.GetString(
                schluessel, MyResource.Resource.Culture ?? CultureInfo.CurrentUICulture);
            return string.IsNullOrWhiteSpace(text) ? vorgabe : text;
        }
    }
}
