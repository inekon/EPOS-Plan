using System.Text;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die IMPORTPROBE des Pruefmodus (Umsetzungskonzept Gebaeudesimulation 3.6 und 3.8, G4-8): der
/// Gebaeudeimport (gbXML und IFC) im Simulator, samt Speichermessung und Dateifilter-Tabelle.
///
/// <para><b>Nur auf Zuruf.</b> Sie laeuft erst, wenn neben <c>EPOS_PRUEFLAUF</c> auch
/// <see cref="SCHALTER"/> gesetzt ist, und ihre Proben liegen nur im App-Paket, wenn es mit
/// <c>-p:Importproben=true</c> gebaut ist (<c>EPOS.iOS.csproj</c>). Der gewoehnliche Pruefmodus
/// (Projekt 1030 gegen die Basis) bleibt damit, wie er ist.</para>
///
/// <para><b>Hier steht nur, was die Schale beisteuert:</b> der Weg zu den Proben im Paket, der
/// Speicherstand aus <see cref="Prozessspeicher"/> und die Uebersetzung der Dateifilter. Gemessen,
/// erzeugt und geschrieben wird im Kern (<c>Importmessung.Probelauf</c>) — derselbe Weg, den die
/// Tests unter Windows fahren. Die Probe wirft nicht: Jede Ausnahme wird eine Protokollzeile, und
/// der Pruefmodus schreibt danach wie gewohnt Protokoll und Fertigmarke.</para>
/// </summary>
internal static class Importprobe
{
    /// <summary>Die Umgebungsvariable, die die Probe einschaltet (im Simulator mit <c>SIMCTL_CHILD_</c>).</summary>
    internal const string SCHALTER = "EPOS_PRUEFLAUF_IMPORT";

    /// <summary>Praefix der Proben im App-Paket (LogicalName in <c>EPOS.iOS.csproj</c>).</summary>
    internal const string PRAEFIX = "importprobe_";

    /// <summary>Die Proben aus <c>Referenzlaeufe/Importproben/</c>, klein vor gross.</summary>
    internal static readonly string[] PROBEN =
    {
        "gbxml_haus_si.xml", "ifc4_haus.ifc", "ifc4_haus.ifczip", "ifc2x3_haus.ifc",
    };

    /// <summary>Die Endungen, deren Typkennung die Dateifilter-Zeile ausweist.</summary>
    internal static readonly string[] ENDUNGEN = { ".ifc", ".ifcxml", ".ifczip", ".gbxml", ".xml" };

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
    /// Faehrt die Probe und schreibt ihre Zeilen (<c>IMPORTPROBE</c>, <c>IMPORTMESSUNG</c>,
    /// <c>IMPORTDIAGNOSE</c>) ueber <paramref name="zeile"/>.
    /// </summary>
    /// <param name="zeile">Wohin die Zeilen gehen (Protokoll des Pruefmodus).</param>
    /// <param name="paketdatei">Oeffnet eine Datei des App-Pakets; <c>null</c>, wenn sie fehlt.</param>
    internal static void Ausfuehren(Action<string> zeile, Func<string, Stream?> paketdatei)
    {
        try
        {
            zeile(DateifilterZeile());
            var dateien = new List<(string Name, Func<Stream> Oeffnen)>();
            foreach (string name in PROBEN)
            {
                string paketname = PRAEFIX + name;
                dateien.Add((name, () => paketdatei(paketname)!));
            }
            Importmessung.Probelauf(dateien, zeile, Prozessspeicher.Lesen);
        }
        catch (Exception ex)
        {
            try
            {
                zeile(Importmessung.PROBE + " abgebrochen ausnahme=\"" +
                      Importmessung.Einzeilig(Importmessung.Ausnahmetext(ex)).Replace('"', '\'') + "\"");
            }
            catch { }
        }
    }

    /// <summary>
    /// <c>IMPORTPROBE dateifilter .ifc=… .ifcxml=… … dialog=…</c> — die Typkennungen, die der
    /// Dokumentenwaehler je Endung und fuer den Filter des Gebaeudeimports bekommt.
    /// </summary>
    internal static string DateifilterZeile()
    {
        var sb = new StringBuilder(Importmessung.PROBE + " dateifilter");
        foreach (string endung in ENDUNGEN)
            sb.Append(' ').Append(endung).Append('=')
              .Append(string.Join(",", Dateifilter.Kennungen_Zu("(*" + endung + ")|*" + endung)));
        sb.Append(" dialog=").Append(string.Join(",", Dateifilter.Kennungen_Zu(GebaeudeImportProfil.DATEIFILTER_ALLE)));
        return sb.ToString();
    }
}
