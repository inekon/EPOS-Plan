using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b>Der Einstieg aller Unterbefehle</b> — in fester Reihenfolge, vor dem ersten Zugriff
    /// auf den Kern:
    /// <list type="number">
    /// <item>alle vier Kulturwerte auf de-DE (wie <c>EPOS.Referenzlauf</c>);</item>
    /// <item><see cref="Console.Out"/> und <see cref="Console.Error"/> ins Protokoll
    /// (<see cref="Ausgabe"/>);</item>
    /// <item><b>die Schreibsperre</b>: <c>Schreibnaht.Schreibrecht = () =&gt; false</c> — nie die
    /// Werkzeugfreigabe. Das Werkzeug liest; scheitert ein Lesezugriff an der Sperre, ist das
    /// ein Befund, und die Sperre wird nicht gelockert;</item>
    /// <item><see cref="DatenbankBinden"/>: <c>PfadUeberschreibung</c> setzen und hart
    /// nachprüfen.</item>
    /// </list>
    /// </summary>
    internal static class Einstieg
    {
        internal static int Ausfuehren(Argumente arg)
        {
            KulturSetzen();

            using var ausgabe = new Ausgabe(arg.Protokolldatei);
            int code = Program.ABBRUCH;
            try
            {
                SchreibsperreSetzen();
                ausgabe.ProtokollPruefsumme("Gebaeudevergleich " + arg.Befehl + " — ", Werkzeugversion);
                ausgabe.Protokoll("Beginn " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                // Klein geschrieben: Die Bereinigung ersetzt jeden Namenswert ab zwei Zeichen,
                // auch ein Kürzel wie „DE" im Feld Bearbeiter.
                ausgabe.Protokoll("Kultur (Rechnen und Anzeige): " + CultureInfo.CurrentCulture.Name.ToLowerInvariant());
                ausgabe.Protokoll("Schreibnaht: gesperrt (Schreibrecht = nein)");

                code = arg.Befehl switch
                {
                    Argumente.AUFNAHME => Aufnahme.Ausfuehren(arg, ausgabe),
                    Argumente.VERGLEICH => Vergleichslauf.Ausfuehren(arg, ausgabe),
                    _ => Variante.Ausfuehren(arg, ausgabe),
                };
            }
            catch (Exception ex)
            {
                ausgabe.Fehler("Abbruch: " + ex.GetType().Name + " — " + ex.Message);
                ausgabe.Protokoll(ex.StackTrace ?? "");
                code = Program.ABBRUCH;
            }
            finally
            {
                DataRepository.PfadUeberschreibung = null;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
                ausgabe.Protokoll("Ende " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                ausgabe.Konsole("Exitcode " + code.ToString(CultureInfo.InvariantCulture));
            }
            return code;
        }

        /// <summary>Rechen- UND Anzeigekultur fest auf de-DE (Muster <c>EPOS.Referenzlauf/Program.cs</c>).</summary>
        internal static void KulturSetzen()
        {
            var kultur = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = kultur;
            CultureInfo.DefaultThreadCurrentUICulture = kultur;
            Thread.CurrentThread.CurrentCulture = kultur;
            Thread.CurrentThread.CurrentUICulture = kultur;
        }

        /// <summary>Die Schreibsperre: Der Kern darf in diesem Prozess nicht schreiben.</summary>
        internal static void SchreibsperreSetzen() => Schreibnaht.Schreibrecht = () => false;

        /// <summary>
        /// Richtet die Zugriffsschicht auf <paramref name="db"/> und prüft hart nach (Muster
        /// <c>Referenzlauf/DbUmgebung.AufArbeitskopieUmschaltenUndPruefen</c>):
        /// <c>GetDBPath()</c> muss genau <paramref name="db"/> sein und darf nicht unter
        /// <c>%ProgramData%\EPOS_PLAN</c> liegen. Rückgabe <c>null</c> = gebunden, sonst der
        /// Abbruchgrund — dann ist die Überschreibung wieder gelöscht.
        /// </summary>
        internal static string DatenbankBinden(string db)
        {
            string erwartet = Path.GetFullPath(db);
            DataRepository.PfadUeberschreibung = erwartet;
            string tatsaechlich = Path.GetFullPath(DataRepository.GetDBPath());

            StringComparison vergleich = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            string grund = null;
            if (!string.Equals(erwartet, tatsaechlich, vergleich))
                grund = "DataRepository.GetDBPath() liefert '" + tatsaechlich + "', erwartet war '" + erwartet +
                        "'. Das Werkzeug würde auf einer fremden Datenbank lesen.";
            else if (Schreibort.IstUnter(tatsaechlich, Schreibort.Produktivordner))
                grund = "Der Datenbankpfad zeigt unter " + Schreibort.Produktivordner + " ('" + tatsaechlich +
                        "'). Das Werkzeug liest nur eine Momentaufnahme.";

            if (grund != null) DataRepository.PfadUeberschreibung = null;
            return grund;
        }

        /// <summary>Die Fassung des Werkzeugs samt Commit (InformationalVersion).</summary>
        internal static string Werkzeugversion
        {
            get
            {
                Assembly a = typeof(Einstieg).Assembly;
                string info = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                return info ?? a.GetName().Version?.ToString() ?? "?";
            }
        }
    }
}
