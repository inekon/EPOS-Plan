using System;
using System.IO;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests;

/// <summary>
/// Stellt für die Dauer eines Falls den Wirt der Rasterprobe nach: KEINE Datenbank.
/// <see cref="DataRepository.PfadUeberschreibung"/> zeigt auf eine Datei in einem Temp-Ordner,
/// den es nicht gibt (wie <c>Proben/Rasterprobe/Wirt/Program.cs</c>).
/// </summary>
/// <remarks>
/// <para><b>Wozu.</b> Ohne Überschreibung sieht die Zugriffsschicht die Anwenderdatenbank unter
/// <c>%ProgramData%\EPOS_PLAN</c>. Auf einem Arbeitsrechner liegt sie, auf dem Linux-Läufer
/// nicht — ein Fall „ohne Datenbank" hinge sonst am Rechner. Mit der Probe gilt er auf jeder
/// Plattform gleich.</para>
/// <para><b>Wer sie einlegt, legt sie auch zurück</b> — <c>using</c>; der Fall gehört in die
/// serielle Sammlung „Testdatenbank", weil der Pfad prozessweit ist.</para>
/// </remarks>
internal sealed class OhneDatenbankprobe : IDisposable
{
    private readonly string _vorher;

    /// <summary>Biegt den Pfad der Zugriffsschicht auf eine nicht vorhandene Datei um.</summary>
    public OhneDatenbankprobe()
    {
        _vorher = DataRepository.PfadUeberschreibung;
        DataRepository.PfadUeberschreibung = Path.Combine(
            Path.GetTempPath(), "EPOS-ohne-Datenbank-" + Guid.NewGuid().ToString("N"), "Kenndaten.sqlite");
    }

    /// <summary>Stellt den Pfad zurück.</summary>
    public void Dispose() => DataRepository.PfadUeberschreibung = _vorher;
}
