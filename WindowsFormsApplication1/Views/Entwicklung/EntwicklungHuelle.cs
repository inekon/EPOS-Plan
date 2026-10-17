using System;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Wege des Menuekopfs „Entwicklung" (Administration und Support, Stufe 1; Konzept
    /// Abschnitte 3.3 bis 3.6, Entwicklerhandbuch Abschnitt 5) und die Geste
    /// Strg+Umschalt+F12 im Dialog „Über EPOS-Plan". Eine Huelle ohne Fachwissen: Sie oeffnet Dateien und Ordner,
    /// kopiert den Diagnoseblock des Kerns und schaltet den Modus ueber
    /// <see cref="Entwicklermodus"/>. Gerufen wird sie aus <c>HauptfensterHuelle.Ablauf</c>
    /// (schon eine Nachricht spaeter, also auf dem UI-Faden) und aus
    /// <c>UeberFenster</c>.
    ///
    /// <para><b>Ein Supportwerkzeug wirft nie.</b> Was nicht geht, meldet die Huelle benannt.</para>
    /// </summary>
    internal static class EntwicklungHuelle
    {
        /// <summary>Der Protokollordner: <c>Logs</c> neben der Datenbank (wie <c>Program.Main</c>).</summary>
        internal static string Protokollordner() => Diagnosedaten.ProtokollordnerPfad();

        /// <summary>„Ausnahmeprotokoll oeffnen": <c>Logs\Ausnahmen.txt</c> mit dem System oeffnen.</summary>
        internal static void AusnahmeprotokollOeffnen()
        {
            string ordner = Protokollordner();
            string pfad = string.IsNullOrEmpty(ordner) ? Ausnahmeprotokoll.DATEI
                                                       : Path.Combine(ordner, Ausnahmeprotokoll.DATEI);
            if (!Vorhanden(() => File.Exists(pfad)))
            {
                Melden(string.Format(MyResource.Resource.ENTW_AUSNAHMEN_FEHLT, pfad));
                return;
            }
            if (!Dienste.Datei.MitSystemOeffnen(pfad))
                Fehler(string.Format(MyResource.Resource.ENTW_OEFFNEN_FEHLER, pfad));
        }

        /// <summary>„Protokollordner oeffnen" im Explorer.</summary>
        internal static void ProtokollordnerOeffnen()
        {
            string ordner = Protokollordner();
            if (string.IsNullOrEmpty(ordner) || !Vorhanden(() => Directory.Exists(ordner)))
            {
                Melden(string.Format(MyResource.Resource.ENTW_ORDNER_FEHLT, ordner));
                return;
            }
            if (!WindowsBerichtsvorlagenwege.ImOrdnerZeigen(ordner))
                Fehler(string.Format(MyResource.Resource.ENTW_OEFFNEN_FEHLER, ordner));
        }

        /// <summary>„Datenbankordner oeffnen": der Explorer mit der Datenbank markiert.</summary>
        internal static void DatenbankordnerOeffnen()
        {
            string db = "";
            try { db = DataRepository.GetDBPath() ?? ""; } catch { /* benannt unten */ }
            string ordner = string.IsNullOrWhiteSpace(db) ? "" : Path.GetDirectoryName(db) ?? "";
            if (string.IsNullOrEmpty(ordner) || !Vorhanden(() => Directory.Exists(ordner)))
            {
                Melden(string.Format(MyResource.Resource.ENTW_ORDNER_FEHLT, ordner));
                return;
            }
            if (!WindowsBerichtsvorlagenwege.ImOrdnerZeigen(Vorhanden(() => File.Exists(db)) ? db : ordner))
                Fehler(string.Format(MyResource.Resource.ENTW_OEFFNEN_FEHLER, ordner));
        }

        /// <summary>
        /// „Diagnosedaten kopieren": der Block des Kerns in die Zwischenablage. Die Zwischenablage
        /// verlangt den STA-Faden der Oberflaeche; der Aufruf kommt ueber <c>Blazorsprung</c>
        /// eine Nachricht spaeter auf genau diesem Faden an.
        /// </summary>
        internal static void DiagnosedatenKopieren()
        {
            try
            {
                Clipboard.SetText(Diagnosedaten.Textblock(Application.ProductVersion));
            }
            catch (Exception ex)
            {
                Fehler(string.Format(MyResource.Resource.ENTW_OEFFNEN_FEHLER, ex.Message));
                return;
            }
            Melden(MyResource.Resource.ENTW_DIAGNOSE_KOPIERT);
        }

        /// <summary>
        /// „Entwicklerwerkzeuge der Oberflaeche": die DevTools der WebView des Hauptfensters.
        /// Den Zugang reicht der Rahmen herein; <c>false</c> = keine WebView bereit.
        /// </summary>
        internal static void WebWerkzeugeOeffnen(Func<bool> zugang)
        {
            bool offen = false;
            try { offen = zugang != null && zugang(); }
            catch { offen = false; }
            if (!offen) Melden(MyResource.Resource.ENTW_WEBWERKZEUGE_NICHT);
        }

        /// <summary>
        /// „Entwicklermodus ausschalten": Laeuft er ueber Umgebung oder Startargument, nennt die
        /// Meldung den Grund; sonst versorgt <paramref name="neuVersorgen"/> Menue und Marke sofort.
        /// </summary>
        internal static void ModusAusschalten(Action neuVersorgen)
        {
            if (!Entwicklermodus.Ausschalten(out string grund))
            {
                Melden(grund);
                return;
            }
            neuVersorgen?.Invoke();
        }

        /// <summary>
        /// Die Geste Strg+Umschalt+F12 im Dialog „Über EPOS-Plan" (<c>UeberFenster</c>; Konzept
        /// 3.2): Ist der Modus aus, fragt sie nach und schaltet ihn dauerhaft ein — liegt die Sperre
        /// des Administrators, meldet sie das; ist er an, schaltet sie ihn aus — läuft er über
        /// Umgebung oder Startargument, nennt die Meldung den Grund. Nach einem Wechsel versorgt
        /// <paramref name="neuVersorgen"/> Menü, Marke und Titel sofort.
        /// </summary>
        /// <returns><c>true</c>, wenn der Modus gewechselt hat.</returns>
        internal static bool GesteUmschalten(Action neuVersorgen)
        {
            if (Entwicklermodus.Aktiv)
            {
                if (!Entwicklermodus.Ausschalten(out string grund))
                {
                    Melden(grund);
                    return false;
                }
                neuVersorgen?.Invoke();
                return true;
            }

            if (Entwicklermodus.Gesperrt)
            {
                Melden(MyResource.Resource.ENTW_EIN_GESPERRT);
                return false;
            }
            if (!Dienste.Dialog.Frage(MyResource.Resource.ENTW_EIN_FRAGE, MyResource.Resource.ENTW_TITEL)) return false;

            if (!Entwicklermodus.Einschalten())
            {
                Melden(MyResource.Resource.ENTW_EIN_GESPERRT);
                return false;
            }
            neuVersorgen?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------

        private static bool Vorhanden(Func<bool> pruefung)
        {
            try { return pruefung(); }
            catch { return false; }
        }

        private static void Melden(string text) => Dienste.Dialog.Meldung(text, MyResource.Resource.ENTW_TITEL);

        private static void Fehler(string text) => Dienste.Dialog.Fehler(text, MyResource.Resource.ENTW_TITEL);
    }
}
