using System;
using System.IO;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kein Fall dieses Testprojekts sieht die Anwenderdatenbank des Rechners.</b> Beim Laden der Testassembly, vor
    /// dem ersten Fall, zeigt <see cref="DataRepository.PfadUeberschreibung"/> auf eine Datei in einem Temp-Ordner, den
    /// es nicht gibt. Gegenstück zur Standardkultur (<see cref="StandardkulturEnUs"/>).
    ///
    /// <para><b>Warum.</b> Ohne Überschreibung bildet <see cref="DataRepository.GetDBPath"/> den Pfad aus den
    /// Einstellungen, sonst aus <c>CommonApplicationData</c> — unter Windows
    /// <c>%ProgramData%\EPOS_PLAN\Kenndaten.sqlite</c>, die produktive Datenbank des Arbeitsrechners; auf ubuntu und auf
    /// den CI-Läufern liegt dort nichts. Ein Fall, der eine Lage „ohne Datenbank“ voraussetzt — der Zonenplan mit der
    /// Vorgabe im Code statt des Nutzungskatalogs —, war deshalb in der CI grün und auf dem Arbeitsrechner rot
    /// (<c>GebaeudeImportZonenplanHuelleTests</c>: erwartet WOHNEN, erhalten <c>#1</c>, die Id des Katalogprofils). Mit
    /// der Überschreibung gilt für jeden Fall auf jedem Rechner dasselbe: keine Datenbank, bis eine Vorrichtung eine
    /// einlegt.</para>
    ///
    /// <para><b>Regel für die Fälle.</b> Wer eine Datenbank braucht, legt sie ein — <see cref="TestDatenbank"/>,
    /// <see cref="TwwTestdatenbank"/> oder ein eigener Pfad — in der seriellen
    /// Sammlung „Testdatenbank“ und stellt den Pfad danach zurück; die Vorrichtungen tun das selbst und stellen damit
    /// diesen Pfad wieder her. Der Wächter <see cref="OhneAnwenderdatenbankTests"/> hält beides. Der Hinweis CA2255
    /// (Modulinitialisierer in einer Bibliothek) trifft hier nicht: Diese Assembly wird nur vom Testlauf geladen.</para>
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
}
