using System.Globalization;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Der Weg zur Datenbank auf dem Geraet: Seed-Kopie beim Erststart und das
/// Umbiegen der Zugriffsschicht auf die Kopie.
///
/// <para><b>Warum ueberhaupt eine Kopie.</b> Das Anwendungspaket einer iOS-App
/// ist SCHREIBGESCHUETZT und wird bei jeder Aktualisierung ersetzt. Eine
/// Datenbank, in die der Anwender schreibt, kann dort nicht liegen. Beim ersten
/// Start wird die mitgelieferte <c>Kenndaten.sqlite</c> deshalb einmal in den
/// beschreibbaren Bereich kopiert - und danach NIE wieder ueberschrieben.
/// Das ist derselbe Gedanke wie in <c>ErststartMigration.Pruefe</c> unter
/// Windows: Liegt die Datei schon da, ist nichts zu tun.</para>
///
/// <para><b>Warum diese Datei keine iOS-API kennt.</b> Sie bekommt den Zugang
/// zum Anwendungspaket als Rueckruf herein
/// (<c>FileSystem.OpenAppPackageFileAsync</c> reicht ihn in
/// <see cref="MauiProgram"/>) und den Ablageort ueber <c>Dienste.Pfade</c>.
/// Damit laesst sie sich ohne Mac uebersetzen und pruefen - genau das verlangt
/// die Arbeitsregel des Pakets.</para>
///
/// <para><b>Der Ordnername ist derselbe wie unter Windows.</b>
/// <c>&lt;Gemeinsam&gt;/EPOS_PLAN/Kenndaten.sqlite</c> - zeichengleich zu
/// <c>%ProgramData%\EPOS_PLAN\Kenndaten.sqlite</c>, dem Rueckfall in
/// <c>DataRepository.GetDBPath</c>. Wer spaeter eine Sicherung von einem
/// Windows-Rechner auf das iPad bringt, findet dieselbe Struktur vor.</para>
/// </summary>
internal static class Datenbankbereitstellung
{
    /// <summary>Dateiname der Datenbank - gleichlautend mit dem Kern.</summary>
    internal const string DATEI = "Kenndaten.sqlite";

    /// <summary>Der Unterordner unter <c>Dienste.Pfade.Gemeinsam</c>.</summary>
    internal const string ORDNER = "EPOS_PLAN";

    /// <summary>
    /// Der Ablageort der Arbeitsdatenbank. Legt den Ordner an, wenn er fehlt
    /// (<c>Unterordner</c>), die Datei aber nicht.
    /// </summary>
    internal static string Zielpfad()
    {
        string ordner = Dienste.Pfade.Unterordner(Dienste.Pfade.Gemeinsam, ORDNER);
        return Dienste.Pfade.Verbinde(ordner, DATEI);
    }

    /// <summary>
    /// Stellt sicher, dass es eine beschreibbare Datenbank gibt, und richtet die
    /// Zugriffsschicht darauf aus.
    ///
    /// <para><b>Die Reihenfolge ist wichtig.</b> Erst kopieren, dann
    /// <c>DataRepository.PfadUeberschreibung</c> setzen - sonst zeigte der erste
    /// Zugriff auf einen Pfad, unter dem noch nichts liegt.
    /// <c>PfadUeberschreibung</c> ist der Haken, der alles andere schlaegt
    /// (derselbe, den die Referenzlauf-Suite benutzt); damit braucht die
    /// iOS-Huelle weder <c>Properties.Settings</c> noch einen
    /// <c>ConfigurationManager</c>.</para>
    /// </summary>
    /// <param name="paketDatei">
    /// Oeffnet die mitgelieferte Datenbank im Anwendungspaket. Liefert
    /// <c>null</c>, wenn es keine gibt - dann bleibt es beim Zustand „keine
    /// Datenbank", den der Kern kennt und meldet.
    /// </param>
    /// <param name="protokoll">Nimmt die Startzeilen auf; <c>null</c> = still.</param>
    /// <returns>Der Pfad der Arbeitsdatenbank.</returns>
    internal static string Sicherstellen(Func<Stream?> paketDatei, Action<string>? protokoll = null)
    {
        string ziel = Zielpfad();

        if (File.Exists(ziel))
        {
            Melde(protokoll, "Datenbank vorhanden: " + ziel +
                             " (" + Groesse(ziel) + " MB)");
        }
        else
        {
            Melde(protokoll, "Erststart - Datenbank wird aus dem Anwendungspaket kopiert.");
            if (!Kopiere(paketDatei, ziel, protokoll)) return ziel;
            Melde(protokoll, "Datenbank angelegt: " + ziel + " (" + Groesse(ziel) + " MB)");
        }

        DataRepository.PfadUeberschreibung = ziel;
        return ziel;
    }

    /// <summary>
    /// Kopiert die Datenbank aus dem Anwendungspaket. <c>false</c>, wenn es
    /// nichts zu kopieren gab oder das Kopieren scheiterte; eine halb
    /// geschriebene Zieldatei wird dabei wieder entfernt.
    /// </summary>
    private static bool Kopiere(Func<Stream?> paketDatei, string ziel, Action<string>? protokoll)
    {
        // Reste eines abgebrochenen Vorlaufs. Ein liegengebliebenes -wal wuerde
        // beim ersten Oeffnen in die frisch kopierte Datei eingespielt - sie
        // waere danach weder der Auslieferungsstand noch ein gueltiger Stand.
        // Dieselbe Vorsorge trifft DbUmgebung.ArbeitskopieAnlegen.
        foreach (string anhang in new[] { "-wal", "-shm" })
        {
            try { if (File.Exists(ziel + anhang)) File.Delete(ziel + anhang); } catch { }
        }

        try
        {
            using Stream? quelle = paketDatei();
            if (quelle == null)
            {
                Melde(protokoll, "FEHLER: Im Anwendungspaket liegt keine " + DATEI + ".");
                return false;
            }

            using (FileStream neu = File.Create(ziel))
            {
                quelle.CopyTo(neu);
            }
            return true;
        }
        catch (Exception ex)
        {
            Melde(protokoll, "FEHLER beim Anlegen der Datenbank: " + ex.Message);
            try { if (File.Exists(ziel)) File.Delete(ziel); } catch { }
            return false;
        }
    }

    /// <summary>
    /// Die beiden Auskunftszeilen des Starts: Fassung der SQLite-Bibliothek und
    /// Zahl der STRICT-Tabellen.
    ///
    /// <para><b>Sie sind das Gate, nicht Schmuck.</b> Die Datenbank fuehrt 114
    /// STRICT-Tabellen; STRICT gibt es erst ab SQLite 3.37. Weil die Huelle
    /// <c>bundle_e_sqlite3</c> statisch mitlinkt, muss hier auf jedem Geraet
    /// dieselbe Fassung stehen wie auf Windows, Linux und im macOS-CI. Weicht
    /// sie ab, ist die Ursache im Paketgraphen zu suchen und nicht im
    /// Rechenweg - deshalb steht die Zeile im Startprotokoll und wird vom
    /// CI-Job geprueft.</para>
    /// </summary>
    internal static (string Fassung, int Strict) Auskunft()
    {
        string fassung = "?";
        int strict = -1;

        try
        {
            object? wert = DataRepository.ExecuteScalar("SELECT sqlite_version()", null);
            if (wert != null && wert != DBNull.Value) fassung = Convert.ToString(wert) ?? "?";
        }
        catch { }

        try
        {
            object? wert = DataRepository.ExecuteScalar(
                "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND sql LIKE '%STRICT%'", null);
            if (wert != null && wert != DBNull.Value)
                strict = Convert.ToInt32(wert, CultureInfo.InvariantCulture);
        }
        catch { }

        return (fassung, strict);
    }

    /// <summary>
    /// Legt eine konsistente Sicherung neben die Arbeitsdatenbank und liefert
    /// ihren Pfad; <c>""</c>, wenn es nicht geklappt hat.
    ///
    /// <para><b>VACUUM INTO statt Dateikopie.</b> Eine reine Kopie waere ohne
    /// die Begleitdateien <c>-wal</c> und <c>-shm</c> nur der eingecheckpointete
    /// Stand - dieselbe Falle, vor der <c>DbUmgebung.ArbeitskopieAnlegen</c>
    /// warnt. <c>VACUUM INTO</c> schreibt eine in sich stimmige, verdichtete
    /// Einzeldatei. Weitergereicht wird sie vom Aufrufer ueber das
    /// Teilen-Blatt (<c>Dienste.Datei.MitSystemOeffnen</c>).</para>
    /// </summary>
    /// <param name="ordner">Zielordner; leer = neben der Arbeitsdatenbank.</param>
    internal static string SicherungAnlegen(string ordner = "")
    {
        try
        {
            string quelle = Zielpfad();
            if (!File.Exists(quelle)) return "";

            string ziel = Path.Combine(
                string.IsNullOrEmpty(ordner) ? (Path.GetDirectoryName(quelle) ?? "") : ordner,
                "Kenndaten-" + DateTime.Now.ToString("yyyy-MM-dd_HHmm", CultureInfo.InvariantCulture) + ".sqlite");

            try { if (File.Exists(ziel)) File.Delete(ziel); } catch { }

            // Der Dateiname geht als Parameter hinein - VACUUM INTO nimmt einen
            // Ausdruck, und ein Anwenderpfad gehoert nie in eine SQL-Zeichenkette.
            //
            // AUSNAHME DER SCHREIBNAHT (Welle iF30): VACUUM INTO faengt die Naht als
            // schreibende Anweisung ab, obwohl es den Bestand gar nicht anfasst - es
            // schreibt eine ZWEITE Datei daneben. Eine Sicherung ist ein Export, und
            // der bleibt im Lesemodus ausdruecklich erlaubt (Konzept § 6).
            using (Schreibnaht.Freigabe(Schreibnaht.GRUND_SICHERUNG))
            {
                DataRepository.ExecuteNonQuery("VACUUM INTO ?", new[] { new DbParam("@ziel", ziel) });
            }
            return File.Exists(ziel) ? ziel : "";
        }
        catch
        {
            return "";
        }
    }

    private static string Groesse(string datei)
    {
        try { return (new FileInfo(datei).Length / 1024 / 1024).ToString(CultureInfo.InvariantCulture); }
        catch { return "?"; }
    }

    private static void Melde(Action<string>? protokoll, string zeile)
    {
        if (protokoll != null) protokoll(zeile);
    }
}
