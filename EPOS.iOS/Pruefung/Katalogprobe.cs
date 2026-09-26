using System.IO.Compression;
using System.Text;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die KATALOGPROBE des Pruefmodus (Anwenderentscheid ZU26, N23): der Katalogimport der
/// Brauchwasser-Nutzungsarten im Simulator - Paket lesen, Prueflauf, echter Import - samt der
/// Typkennungen, die der Dokumentenwaehler fuer ein ZIP-Paket bekommt.
///
/// <para><b>Warum die iOS-Schale das nachweist.</b> Der Katalog der Brauchwasser-Nutzungsarten
/// ist der EINZIGE Katalog, der auf iOS aufgeht (ZU26); die acht uebrigen Katalogverwaltungen
/// lehnt die Wurzel dort benannt ab (KI-D-Q10). Sein Import nimmt auf iOS allein ein ZIP-Archiv,
/// weil es hier keinen Ordnerdialog gibt. Ob das Archiv auf dem Geraet zu lesen und
/// einzuspielen ist, sagt keine Windows-Probe - nur diese.</para>
///
/// <para><b>Nur auf Zuruf.</b> Sie laeuft erst, wenn neben <c>EPOS_PRUEFLAUF</c> auch
/// <see cref="SCHALTER"/> gesetzt ist. Die Probedateien liegen als <c>MauiAsset</c> im
/// App-Paket (<c>EPOS.iOS.csproj</c>, Praefix <see cref="PRAEFIX"/>); sie sind erfunden und
/// zusammen wenige Kilobyte gross.</para>
///
/// <para><b>Hier steht nur, was die Schale beisteuert:</b> die Probedateien aus dem Paket in
/// die Sandbox schreiben, daraus EIN ZIP-Archiv packen - genau das, was der Anwender auf dem
/// iPad waehlt - und die Uebersetzung des Dateifilters. Gelesen, eingespielt und gezaehlt wird
/// im Kern (<see cref="TwwKatalogprobe.Probelauf"/>), also auf demselben Weg, den die Tests
/// unter Windows fahren. Die Probe wirft nicht: Jede Ausnahme wird eine Protokollzeile.</para>
/// </summary>
internal static class Katalogprobe
{
    /// <summary>Die Umgebungsvariable, die die Probe einschaltet (im Simulator mit <c>SIMCTL_CHILD_</c>).</summary>
    internal const string SCHALTER = "EPOS_PRUEFLAUF_KATALOGIMPORT";

    /// <summary>Praefix der Probedateien im App-Paket (LogicalName in <c>EPOS.iOS.csproj</c>).</summary>
    internal const string PRAEFIX = "katalogprobe_";

    /// <summary>Der Name des Archivs, das die Probe packt und dem Kern vorlegt.</summary>
    internal const string ARCHIV = "Katalogpaket.zip";

    /// <summary>
    /// Die Dateien des Probepakets aus <c>EPOS.Kern.Tests/Proben/Zapfprofil/Katalogpaket/</c> -
    /// in der Reihenfolge, in der das Umsetzungskonzept sie nennt (Format N2).
    /// </summary>
    internal static readonly string[] DATEIEN =
    {
        "Tab_TwwTagesgangsatz_STAMM.csv", "Tab_TwwTagesgang_STAMM.csv",
        "Tab_TwwNutzungsart_STAMM.csv", "Tab_TwwZapfkategorie_STAMM.csv",
        "Tab_TwwBedarfstag_STAMM.csv", "Tab_TwwBedarfstagEreignis_STAMM.csv",
        "Tab_TwwParameter_STAMM.csv",
    };

    /// <summary><c>true</c>, wenn die Probe angefordert wurde.</summary>
    internal static bool Angefordert
    {
        get
        {
            try { return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SCHALTER)); }
            catch { return false; }
        }
    }

    /// <summary>
    /// Faehrt die Probe und schreibt ihre Zeilen (<c>KATALOGPROBE</c>) ueber
    /// <paramref name="zeile"/>.
    /// </summary>
    /// <param name="zeile">Wohin die Zeilen gehen (Protokoll des Pruefmodus).</param>
    /// <param name="paketdatei">Oeffnet eine Datei des App-Pakets; <c>null</c>, wenn sie fehlt.</param>
    /// <param name="arbeitsordner">Ordner, in dem das Archiv entsteht (wird angelegt).</param>
    internal static void Ausfuehren(Action<string> zeile, Func<string, Stream?> paketdatei, string arbeitsordner)
    {
        try
        {
            zeile(DateifilterZeile());

            string ordner = Path.Combine(arbeitsordner, "katalogprobe");
            Directory.CreateDirectory(ordner);

            int gelegt = 0, fehlend = 0;
            foreach (string name in DATEIEN)
            {
                using Stream? quelle = paketdatei(PRAEFIX + name);
                if (quelle is null) { fehlend++; continue; }
                using FileStream ziel = File.Create(Path.Combine(ordner, name));
                quelle.CopyTo(ziel);
                gelegt++;
            }

            zeile(TwwKatalogprobe.PROBE + " paketdateien gelegt=" + gelegt + " fehlend=" + fehlend);
            if (gelegt == 0)
            {
                zeile(TwwKatalogprobe.PROBE + " ende ergebnis=BEFUNDE befunde=1 grund=\"keine Probedateien im App-Paket\"");
                return;
            }

            // DAS ARCHIV ist der Weg des Anwenders: Auf iOS waehlt er ein ZIP-Paket, keinen
            // Ordner. Deshalb wird gepackt und der Ordner nicht unmittelbar vorgelegt.
            string archiv = Path.Combine(arbeitsordner, ARCHIV);
            if (File.Exists(archiv)) File.Delete(archiv);
            ZipFile.CreateFromDirectory(ordner, archiv);
            zeile(TwwKatalogprobe.PROBE + " archiv datei=\"" + ARCHIV + "\" bytes="
                  + new FileInfo(archiv).Length);

            TwwKatalogprobe.Probelauf(archiv, zeile);
        }
        catch (Exception ex)
        {
            try
            {
                zeile(TwwKatalogprobe.PROBE + " abgebrochen ausnahme=\""
                      + (ex.GetType().Name + ": " + ex.Message).Replace('"', '\'')
                          .Replace('\r', ' ').Replace('\n', ' ') + "\"");
            }
            catch { }
        }
    }

    /// <summary>
    /// <c>KATALOGPROBE dateifilter .zip=… dialog=…</c> - die Typkennungen, die der
    /// Dokumentenwaehler fuer ein ZIP-Paket und fuer den Filter des Katalogimports bekommt.
    /// Sie sind der Grund, aus dem der Waehler das Archiv ueberhaupt anbietet.
    /// </summary>
    internal static string DateifilterZeile()
    {
        var sb = new StringBuilder(TwwKatalogprobe.PROBE + " dateifilter");
        sb.Append(" .zip=").Append(string.Join(",", Dateifilter.Kennungen_Zu("(*.zip)|*.zip")));
        sb.Append(" dialog=").Append(string.Join(",",
            Dateifilter.Kennungen_Zu(ZapfprofilHuelle.KatalogTexte().ImportDateifilterZip)));
        sb.Append(" ordnerwahl=").Append(new IosDateiDienst().OrdnerwahlMoeglich ? "JA" : "NEIN");
        return sb.ToString();
    }
}
