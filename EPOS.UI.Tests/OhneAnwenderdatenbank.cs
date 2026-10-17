using System;
using System.IO;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Kein Fall dieses Testprojekts sieht die Anwenderdatenbank des Rechners.</b> Beim Laden der Testassembly, vor dem
/// ersten Fall, zeigt <see cref="DataRepository.PfadUeberschreibung"/> auf eine Datei in einem Temp-Ordner, den es
/// nicht gibt. Gegenstück zu <c>EPOS.Kern.Tests/OhneAnwenderdatenbank.cs</c>; dort steht die Begründung ausführlich.
///
/// <para><b>Kurz.</b> Ohne Überschreibung bildet <see cref="DataRepository.GetDBPath"/> den Pfad aus den
/// Einstellungen, sonst aus <c>CommonApplicationData</c> — unter Windows <c>%ProgramData%\EPOS_PLAN\Kenndaten.sqlite</c>,
/// die produktive Datenbank des Arbeitsrechners; auf ubuntu liegt dort nichts. Die Dialoge des Gebäudeimports
/// (<c>GebaeudeImportZonenDialogTests</c>, <c>GebaeudeImportProjektdateiDialogTests</c>) lesen über die Hülle den
/// Nutzungskatalog und zeigten auf dem Arbeitsrechner dessen Profile (<c>#1</c>, <c>#2</c>) statt der Vorgabe im Code
/// (WOHNEN, BUERO). Mit der Überschreibung gilt auf jedem Rechner dasselbe: keine Datenbank.</para>
///
/// <para><b>Regel für die Fälle.</b> Wer eine Datenbank einlegt, stellt den Pfad danach auf den vorherigen Wert
/// zurück — nie auf <c>null</c>, das gäbe den Standardpfad des Rechners frei. Der Wächter
/// <see cref="OhneAnwenderdatenbankTests"/> hält Pfad, Datei und Ordner.</para>
/// </summary>
internal static class OhneAnwenderdatenbank
{
    /// <summary>Die Datei, die es nie gibt: der Pfad der Zugriffsschicht ohne eingelegte Datenbank.</summary>
    internal static readonly string Pfad = Path.Combine(
        Path.GetTempPath(), "EPOS-ohne-Anwenderdatenbank-" + Guid.NewGuid().ToString("N"), "Kenndaten.sqlite");

#pragma warning disable CA2255 // Modulinitialisierer: gewollt, nur der Testlauf lädt diese Assembly.
    /// <summary>Legt den Pfad prozessweit ein.</summary>
    [ModuleInitializer]
    internal static void Setzen() => DataRepository.PfadUeberschreibung = Pfad;
#pragma warning restore CA2255
}
