using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE des Erststart-Assistenten (iU9-W15c.7) — die Klammer zwischen
    /// <see cref="ErststartMigration"/> und der Razor-Komponente
    /// <c>EPOS.UI.Dialoge.Lizenz.ErststartDialog</c>.
    ///
    /// <para><b>Warum dieser Controller NICHT im Kern liegt.</b> Er ist der einzige der
    /// Welle, der in <c>WindowsFormsApplication1/</c> bleibt, und das hat einen
    /// zwingenden Grund: <see cref="ErststartMigration"/> hebt eine <c>.accdb</c> über
    /// die ACE-Engine und bringt damit <c>System.Data.OleDb</c> und
    /// <c>EposSqliteMigrator.Kern</c> mit. Beides bricht den Plattform-Wächter des
    /// Kerns (<c>EPOS.Kern/CLAUDE.md</c>) — und auf iOS gibt es ohnehin keinen
    /// Access-Altbestand: Dort ist der „Erststart" eine Dateikopie aus dem
    /// Anwendungspaket (<c>EPOS.iOS/Datenbankbereitstellung.cs</c>, Befund W15c-B9).
    /// <b>Die Komponente sieht diesen Controller deshalb nie</b>; sie bekommt ihre
    /// Gaben wie überall als <c>[Parameter]</c>, und die Windows-Hülle füllt sie.</para>
    ///
    /// <para><b>Der Ablauf selbst bleibt, wo er ist.</b> <c>ErststartMigration</c> ist
    /// laut eigenem Klassenkopf „bewusst OHNE Oberfläche" gebaut: Fortschritt über
    /// <c>IProgress&lt;string&gt;</c>, Ergebnis über den Rückgabewert und
    /// <c>LetzteMeldung</c>. Dieser Controller fügt nichts hinzu — er beantwortet drei
    /// Fragen, die die Anzeige stellt.</para>
    /// </summary>
    internal static class ErststartCtrl
    {
        /// <summary>Der Ordner, in dem die Datenbank erwartet wird.</summary>
        internal static string StandardOrdner() => ErststartMigration.StandardOrdner();

        /// <summary>
        /// Das Lagebild des Ordners: Steht dort ein Access-Altbestand ohne
        /// SQLite-Datei, ist eine Umstellung fällig.
        /// </summary>
        internal static bool UmstellungFaellig(string dbOrdner)
            => ErststartMigration.Pruefe(dbOrdner) == ErststartLage.NurAccdbVorhanden;

        /// <summary>
        /// Der neunzeilige Kopftext des Assistenten — Ordner und die DREI Dateinamen
        /// kommen aus <see cref="ErststartMigration"/>, damit sie nirgends ein zweites
        /// Mal stehen.
        /// </summary>
        internal static string Kopftext(string dbOrdner)
        {
            return string.Format(MyResource.Resource.ERST_KOPF,
                                 dbOrdner,
                                 ErststartMigration.ACCDB_DATEI,
                                 ErststartMigration.SQLITE_DATEI,
                                 ErststartMigration.ACCDB_UMBENANNT);
        }

        /// <summary>
        /// Führt die Umstellung durch. <b>Läuft auf dem aufrufenden Faden</b> — die
        /// Hülle schickt ihn über einen eigenen Strang, wie der Vorläufer
        /// (<c>Form_Erststart.Starten</c>, <c>:222-242</c>).
        /// </summary>
        /// <param name="dbOrdner">Ordner mit <c>Kenndaten.accdb</c>.</param>
        /// <param name="fortschritt">Meldeweg je Zeile; auf dem Oberflächenfaden erzeugt.</param>
        /// <param name="berichtPfad">Pfad des Migrationsberichts, sofern einer entstand.</param>
        /// <returns><c>true</c> = die SQLite-Datei steht.</returns>
        internal static bool Starten(string dbOrdner, IProgress<string> fortschritt,
                                     out string berichtPfad)
        {
            // settingsFixup: true - im Programmbetrieb soll der gespeicherte DBName
            // nach der Umstellung auf Kenndaten.sqlite zeigen (N7). Unveraendert der
            // Wert, den Program.ErststartAnbieten seit S8 uebergibt.
            return ErststartMigration.Fuehredurch(dbOrdner, fortschritt, true, out berichtPfad);
        }

        /// <summary>Die letzte Meldung des Ablaufs — Erfolgs- wie Fehlertext.</summary>
        internal static string LetzteMeldung => ErststartMigration.LetzteMeldung;
    }
}
