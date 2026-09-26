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
/// <c>2026-09-05_R2_Zeitbasis</c> (bis 05.09.2026 <c>2026-08-30_B3-Kaskade</c>).</para>
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

    /// <summary>Das Referenzprojekt - Projekt 1030 („B3-Kaskade") der eingefrorenen Basis.</summary>
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

        // DIE WERKZEUG-FREIGABE DER SCHREIBNAHT (Welle iF30) - EINE benannte Zeile,
        // ausdruecklich und nicht durch Auslassen. Der Pruefmodus rechnet das
        // Referenzprojekt im Simulator und SPEICHERT sein Ergebnis; ein Geraet in der CI
        // hat keine Lizenz. Ohne diese Zeile faellt der iOS-Job rot aus - aus einem Grund,
        // der mit dem Rechenweg nichts zu tun hat.
        Schreibnaht.WerkzeugFreigabe("EPOS.iOS-Pruefmodus (Rechennachweis ohne Lizenz)");

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

            log.Leerzeile();
            AssistentProbe(log);
        }
        catch (Exception ex)
        {
            log.FehlerZeile("Pruefmodus abgebrochen: " + ex.Message);
            log.Roh(ex.StackTrace ?? "");
        }

        // DIE IMPORTPROBE (G4-8) - nur auf Zuruf (EPOS_PRUEFLAUF_IMPORT) und nur mit den
        // Proben im Paket (-p:Importproben=true). Ausserhalb des Blocks oben, damit sie auch
        // dann laeuft, wenn der Rechennachweis abbricht; sie wirft nicht, und ihre Zeilen
        // stehen VOR der Fertigmarke im Protokoll. Im Bau ohne xBIM (-p:OhneXbim=true, nur fuer
        // den Groessenvergleich des Geraetebaus) gibt es sie nicht.
#if !OHNE_XBIM
        if (Importprobe.Angefordert)
        {
            log.Leerzeile();
            Importprobe.Ausfuehren(log.Zeile, Paketdatei);
        }
#endif

        // DIE KATALOGPROBE (ZU26, N23) - nur auf Zuruf (EPOS_PRUEFLAUF_KATALOGIMPORT). Sie
        // spielt das Probepaket der Brauchwasser-Nutzungsarten als ZIP-Archiv ein: erst als
        // Prueflauf, dann wirklich. Sie braucht kein xBIM und steht deshalb AUSSERHALB des
        // Blocks darueber; wie die Importprobe wirft sie nicht, und ihre Zeilen stehen VOR der
        // Fertigmarke. Der Katalog ist nach dem Lauf veraendert - das ist unbedenklich, weil
        // der Rechennachweis des Projekts 1030 vorher gerechnet hat und die Datenbank des
        // Simulators mit dem Lauf endet.
        if (Katalogprobe.Angefordert)
        {
            log.Leerzeile();
            Katalogprobe.Ausfuehren(log.Zeile, Paketdatei, wurzel);
        }

        TimeSpan dauer = DateTime.Now - start;
        log.Leerzeile();
        log.Zeile("Fertig. " + dateien + " Dateien in " + dauer.ToString(@"hh\:mm\:ss"));

        Schreiben(log, wurzel, start, dauer, dateien);
        return dateien > 0;
    }

    /// <summary>
    /// Die PROBE AUF DEN PROJEKTASSISTENTEN (Befund <b>W16a-O-4</b>): Sie baut auf
    /// dem Geraet den Parametersatz, den <c>AssistentSeite</c> erwartet, und schreibt
    /// EINE Zeile mit dem Befund.
    ///
    /// <para><b>Warum sie hierher gehoert.</b> Der iOS-Job kann ohne sie nur
    /// UEBERSETZEN - dass <c>IosProjektQuelle.AssistentGaben</c> auf der echten
    /// Datenbank in der Sandbox wirklich einen Satz liefert, saehe niemand. Die Probe
    /// liest ausschliesslich; sie schreibt nichts und laesst den Rechennachweis
    /// unberuehrt. Faellt sie aus, bleibt es bei einer Protokollzeile: Der
    /// Rechennachweis ist der Zweck dieses Modus, nicht diese Probe.</para>
    ///
    /// <para>Gemessen wird, was der Befund verlangt: dass es den Satz gibt, dass die
    /// zwei plattformfreien Schritte (Komponentenauswahl, Projektkopf) einen Inhalt
    /// haben, dass die elf uebrigen BENANNT abgelehnt werden statt still leer zu
    /// bleiben, und dass der Delegat <c>HatAenderungen</c> aus 62b-E-1 dabei
    /// ist.</para>
    /// </summary>
    private static void AssistentProbe(Protokoll log)
    {
        try
        {
            var quelle = new IosProjektQuelle();
            IReadOnlyDictionary<string, object>? gaben =
                quelle.AssistentGaben(AssistentCtrl.BETRIEBSART_BEARBEITEN, PROJEKT);

            if (gaben == null)
            {
                log.FehlerZeile("Assistent: kein Parametersatz.");
                return;
            }

            // Das Projekt markieren - denselben Weg, den das linke Band der
            // Assistentenseite geht. Danach steht der Komponentenbestand DIESES
            // Projekts, und die dreizehn Seitenschalter sind danach gestellt.
            (gaben["ProjektMarkiert"] as Action<int, string>)?.Invoke(PROJEKT, Projektname(PROJEKT));

            var seiteGaben = gaben["SeiteGaben"] as Func<int, IReadOnlyDictionary<string, object>>;
            var aenderungen = gaben["HatAenderungen"] as Func<bool>;
            string sperrgrund = gaben["SeiteSperrgrundText"] as string ?? "";

            bool seite0 = seiteGaben != null && seiteGaben(WizardItemClass.KOMPONENTEN_ITEM) != null;
            bool seite1 = seiteGaben != null && seiteGaben(WizardItemClass.PROJEKT_ITEM) != null;
            bool seite2 = seiteGaben != null && seiteGaben(WizardItemClass.GEBAEUDE_ITEM) != null;

            log.Zeile("Assistent: Schluessel=" +
                      gaben.Count.ToString(CultureInfo.InvariantCulture) +
                      " Komponenten=" + (seite0 ? "ja" : "nein") +
                      " Projektkopf=" + (seite1 ? "ja" : "nein") +
                      " Gebaeude=" + (seite2 ? "ja" : "abgelehnt") +
                      " Sperrgrund=" + (sperrgrund.Length > 0 ? "ja" : "nein") +
                      " HatAenderungen=" + (aenderungen == null
                                                ? "fehlt"
                                                : aenderungen() ? "ja" : "nein"));
        }
        catch (Exception ex)
        {
            log.FehlerZeile("Assistent: " + ex.Message);
        }
    }

    /// <summary>
    /// Oeffnet eine Datei des App-Pakets (MauiAsset) - derselbe Weg wie
    /// <c>MauiProgram.Paketdatei</c> fuer die Seed-Datenbank; <c>null</c>, wenn sie fehlt.
    /// </summary>
    private static Stream? Paketdatei(string name)
    {
        try { return Microsoft.Maui.Storage.FileSystem.OpenAppPackageFileAsync(name).GetAwaiter().GetResult(); }
        catch { return null; }
    }

    /// <summary>Der Projektname zur Nummer; leer, wenn unbekannt.</summary>
    private static string Projektname(int idProjekt)
    {
        try
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            return projekt.rows > 0 ? (projekt.m_szProjektname ?? "") : "";
        }
        catch { return ""; }
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
