using System.Globalization;
using System.Text;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Referenzlauf;

namespace EPOS.iOS;

/// <summary>
/// Der PRUEFMODUS der iOS-Huelle: Er rechnet ein Referenzprojekt auf dem Geraet
/// und schreibt dieselben CSV, die der Referenzlauf auf Windows und Linux
/// schreibt.
///
/// <para><b>Wofuer er da ist.</b> Das Abnahmeziel iZ6 lautet: „Ein Projekt
/// vollstaendig auf dem iPad durchgeplant; Ergebnis-CSV wertgleich zur
/// Windows-Basis." Von Hand ist das eine Sitzung vor dem Geraet - und nichts,
/// was eine CI wiederholen koennte. Dieser Modus macht daraus einen
/// maschinellen Nachweis: Die App rechnet Projekt 1030 beim Start, legt die CSV
/// im Dokumentenordner ab, und der Job holt sie aus dem Simulator und haelt sie
/// mit <c>EPOS.Referenzlauf vergleich</c> gegen die eingefrorene Basis
/// <c>2026-08-30_B3-Kaskade</c>.</para>
///
/// <para><b>Dieselben Bausteine, nicht nachgebaute.</b>
/// <see cref="Ergebnisexport"/> und <see cref="Protokoll"/> sind aus
/// <c>Referenzlauf\</c> VERLINKT (siehe EPOS.iOS.csproj) - genau wie
/// EPOS.Referenzlauf sie verlinkt. Damit gibt es weiterhin EINE Fassung des
/// CSV-Exports; ein iOS-Ergebnis, das anders aussieht, ist dann eine
/// Plattformabweichung und kein anderer Exportweg.</para>
///
/// <para><b>Die Kultur wird ausdruecklich gesetzt</b> - wortgleich zu
/// <c>EPOS.Referenzlauf.Program.KulturSetzen</c>. Auf einem Geraet mit
/// englischer Spracheinstellung liefe dieselbe Rechnung sonst mit anderen
/// Zahlenformaten, wo im Bestand ein <c>Convert.ToDouble</c> ohne Formatangabe
/// steht. Der Vergleich soll Plattformdrift messen, nicht Kulturdrift
/// (iR-d).</para>
///
/// <para><b>Geschaltet wird ueber eine Umgebungsvariable</b> und nicht ueber
/// einen Programmschalter: Eine iOS-App bekommt keine Befehlszeile. Der
/// Simulator reicht Variablen mit dem Praefix <c>SIMCTL_CHILD_</c> in die App
/// durch - <c>SIMCTL_CHILD_EPOS_PRUEFLAUF=1 xcrun simctl launch …</c>.</para>
/// </summary>
internal static class Prueflauf
{
    /// <summary>Name der Umgebungsvariablen, die den Pruefmodus einschaltet.</summary>
    internal const string SCHALTER = "EPOS_PRUEFLAUF";

    /// <summary>Das Referenzprojekt - die B3-Kaskade der eingefrorenen Basis.</summary>
    internal const int PROJEKT = 1030;

    /// <summary>Unterordner unter „Dokumente", in dem alles landet.</summary>
    internal const string ORDNER = "pruefung";

    /// <summary>Die Datei, an der die CI erkennt, dass der Lauf zu Ende ist.</summary>
    internal const string FERTIG = "fertig.txt";

    /// <summary><c>true</c>, wenn der Pruefmodus angefordert wurde.</summary>
    internal static bool Angefordert
    {
        get
        {
            try { return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SCHALTER)); }
            catch { return false; }
        }
    }

    /// <summary>
    /// Rechnet das Referenzprojekt und schreibt CSV, Protokoll und die
    /// Fertigmarke unter <paramref name="dokumente"/>/<see cref="ORDNER"/>.
    /// </summary>
    /// <returns><c>true</c>, wenn mindestens eine CSV entstanden ist.</returns>
    internal static bool Ausfuehren(string dokumente)
    {
        KulturSetzen();

        var log = new Protokoll();
        DateTime start = DateTime.Now;

        string wurzel = Path.Combine(dokumente, ORDNER);
        string ziel = Path.Combine(wurzel, "Projekt_" + PROJEKT.ToString(CultureInfo.InvariantCulture));

        int dateien = 0;
        try
        {
            Directory.CreateDirectory(wurzel);

            log.Zeile("Pruefmodus EPOS.iOS gestartet.");
            log.Zeile("Datenbank: " + DataRepository.GetDBPath());
            log.Zeile("Zielordner: " + ziel);

            (string fassung, int strict) = Datenbankbereitstellung.Auskunft();
            log.Zeile("SQLite " + fassung);
            log.Zeile("STRICT=" + strict.ToString(CultureInfo.InvariantCulture));
            log.Leerzeile();

            dateien = Ergebnisexport.ProjektAusfuehren(PROJEKT, ziel, log);
        }
        catch (Exception ex)
        {
            log.FehlerZeile("Pruefmodus abgebrochen: " + ex.Message);
            log.Roh(ex.StackTrace ?? "");
        }

        TimeSpan dauer = DateTime.Now - start;
        log.Leerzeile();
        log.Zeile("Fertig. " + dateien + " Dateien in " + dauer.ToString(@"hh\:mm\:ss"));

        Schreiben(log, wurzel, start, dauer, dateien);
        return dateien > 0;
    }

    /// <summary>
    /// Legt Protokoll und Fertigmarke ab. Die Fertigmarke entsteht ZULETZT -
    /// der CI-Job wartet auf sie und darf sie nicht sehen, solange noch
    /// geschrieben wird.
    /// </summary>
    private static void Schreiben(Protokoll log, string wurzel, DateTime start,
                                  TimeSpan dauer, int dateien)
    {
        try
        {
            log.Speichern(Path.Combine(wurzel, "protokoll.txt"),
                          "Pruefmodus EPOS.iOS vom " + start.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
                          new[]
                          {
                              "Projekt:   " + PROJEKT.ToString(CultureInfo.InvariantCulture),
                              "Datenbank: " + DataRepository.GetDBPath(),
                              "Kultur:    " + CultureInfo.CurrentCulture.Name,
                              "Dateien:   " + dateien.ToString(CultureInfo.InvariantCulture),
                              "Dauer:     " + dauer.ToString(@"hh\:mm\:ss")
                          });
        }
        catch { }

        try
        {
            File.WriteAllText(Path.Combine(wurzel, FERTIG),
                              "Dateien=" + dateien.ToString(CultureInfo.InvariantCulture) + Environment.NewLine,
                              new UTF8Encoding(false));
        }
        catch { }
    }

    /// <summary>
    /// Setzt Rechen- UND Anzeigekultur fest auf de-DE - wortgleich zu
    /// <c>EPOS.Referenzlauf.Program.KulturSetzen</c>.
    /// </summary>
    private static void KulturSetzen()
    {
        var kultur = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = kultur;
        CultureInfo.DefaultThreadCurrentUICulture = kultur;
        Thread.CurrentThread.CurrentCulture = kultur;
        Thread.CurrentThread.CurrentUICulture = kultur;
        Console.WriteLine("Kultur (Rechnen und Anzeige): " + kultur.Name);
    }
}
