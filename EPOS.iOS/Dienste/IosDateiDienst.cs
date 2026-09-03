using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IDateiDienst"/>: Dokumentenwaehler und
/// Teilen-Blatt.
///
/// <para><b>Drei Fundstellen, drei verschiedene Antworten.</b></para>
/// <list type="bullet">
///   <item><description><see cref="DateiOeffnen"/> ist der Dokumentenwaehler.
///   Der Windows-Filter wird ueber <see cref="Dateifilter"/> in Typkennungen
///   uebersetzt.</description></item>
///   <item><description><see cref="DateiSpeichern"/> hat auf iOS KEIN
///   Gegenstueck: Es gibt keinen „Speichern unter"-Dialog, der einen Pfad
///   zurueckgibt. Geschrieben wird deshalb in den Dokumentenordner der Sandbox
///   (mit <c>UIFileSharingEnabled</c> in der App „Dateien" sichtbar); der
///   Aufrufer bekommt genau wie unter Windows einen Pfad und schreibt dorthin.
///   Weitergereicht wird die fertige Datei anschliessend ueber
///   <see cref="MitSystemOeffnen"/>, also ueber das Teilen-Blatt.</description></item>
///   <item><description><see cref="OrdnerWaehlen"/> liefert <c>""</c> - iOS
///   kennt keinen Ordnerdialog. Der Bestand prueft an jeder Fundstelle auf leer
///   und tut dann nichts; das ist derselbe Ausgang wie „abgebrochen".</description></item>
/// </list>
///
/// <para><b>Alle Aufrufe laufen ueber den Hauptfaden.</b> Ein Waehler und ein
/// Teilen-Blatt sind Oberflaeche. Die Schnittstelle ist synchron, deshalb
/// <c>InvokeOnMainThreadAsync(...).GetAwaiter().GetResult()</c> - das ist
/// unbedenklich, solange der Aufruf NICHT selbst vom Hauptfaden kommt. Wo der
/// Kern aus einem Blazor-Ereignis heraus fragt, faehrt die Wurzel den Aufruf in
/// einen <c>Task.Run</c> (iR-f).</para>
/// </summary>
public sealed class IosDateiDienst : IDateiDienst
{
    /// <inheritdoc/>
    public string DateiOeffnen(string titel, string filter, string startOrdner)
    {
        try
        {
            var typen = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.iOS] = Dateifilter.Kennungen_Zu(filter),
                [DevicePlatform.MacCatalyst] = Dateifilter.Kennungen_Zu(filter),
            });

            var wahl = new PickOptions { PickerTitle = titel ?? "", FileTypes = typen };

            FileResult? ergebnis = AufDemHauptfaden(() => FilePicker.Default.PickAsync(wahl));
            return ergebnis?.FullPath ?? "";
        }
        catch
        {
            // Abbruch und Fehler sind hier dasselbe: nichts gewaehlt.
            return "";
        }
    }

    /// <inheritdoc/>
    public string DateiSpeichern(string titel, string filter, string vorschlag)
    {
        try
        {
            string name = Path.GetFileName(vorschlag ?? "");
            if (string.IsNullOrWhiteSpace(name)) name = "EPOS-Plan";

            // Endung aus dem Filter ergaenzen, wenn der Vorschlag keine hat -
            // unter Windows tut das der Speichern-Dialog.
            if (!Path.HasExtension(name))
            {
                IReadOnlyList<string> endungen = Dateifilter.Endungen(filter);
                if (endungen.Count > 0) name += endungen[0];
            }

            return Path.Combine(Dienste.Pfade.Unterordner(Dienste.Pfade.Dokumente), name);
        }
        catch
        {
            return "";
        }
    }

    /// <inheritdoc/>
    /// <remarks>iOS kennt keinen Ordnerdialog - <c>""</c> heisst „abgebrochen".</remarks>
    public string OrdnerWaehlen(string titel, string startOrdner) => "";

    /// <inheritdoc/>
    /// <remarks>
    /// Das Teilen-Blatt ist auf iOS das, was unter Windows „mit der
    /// Standardanwendung oeffnen" ist: Der Anwender waehlt aus, was mit der
    /// Datei geschehen soll - ansehen, sichern, weiterschicken.
    /// </remarks>
    public bool MitSystemOeffnen(string pfad)
    {
        if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad)) return false;

        try
        {
            AufDemHauptfaden(async () =>
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = Path.GetFileName(pfad),
                    File = new ShareFile(pfad)
                });
                return true;
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Faehrt eine Oberflaechenarbeit auf dem Hauptfaden und wartet auf ihr
    /// Ergebnis. Kommt der Aufruf bereits VOM Hauptfaden, waere das Warten ein
    /// Selbstblock - dann wird nichts getan und der Vorgang gilt als
    /// abgebrochen (dasselbe Verhalten wie ein weggeklickter Dialog).
    /// </summary>
    private static T? AufDemHauptfaden<T>(Func<Task<T>> arbeit)
    {
        if (MainThread.IsMainThread) return default;
        return MainThread.InvokeOnMainThreadAsync(arbeit).GetAwaiter().GetResult();
    }
}
