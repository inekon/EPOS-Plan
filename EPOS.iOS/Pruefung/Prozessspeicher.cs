using System.Runtime.InteropServices;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Der Speicherstand des Prozesses, wie iOS ihn fuehrt — fuer die IMPORTPROBE (G4-8).
///
/// <para><b>Warum hier und nicht im Kern.</b> iOS beendet eine App, wenn ihr
/// <c>phys_footprint</c> das Geraetelimit ueberschreitet; genau diese Zahl liefert
/// <c>task_info(mach_task_self(), TASK_VM_INFO, …)</c> — eine Mach-Schnittstelle, die es nur auf
/// Apple-Plattformen gibt. Der Kern bekommt sie als Naht (<see cref="Prozessspeicherstand"/>) und
/// faellt ohne Schale auf <c>Environment.WorkingSet</c> zurueck.</para>
///
/// <para><b>Vorsichtig gebaut.</b> Der Puffer ist groesser als jede Fassung von
/// <c>task_vm_info</c> und genullt; der Kern schreibt hoechstens so viele Worte, wie
/// <c>anzahl</c> erlaubt, und meldet zurueck, wie viele es waren. Gelesen wird nur, was darin
/// liegt: <c>phys_footprint</c> (Byte 144, ab Fassung 1) und <c>ledger_phys_footprint_peak</c>
/// (Byte 168, ab Fassung 3). Jede Ausnahme — keine Bibliothek, kein Einsprung — ergibt den
/// Rueckfall des Kerns mit benannter Quelle, nie einen Abbruch.</para>
/// </summary>
internal static class Prozessspeicher
{
    private const string LIBSYSTEM = "/usr/lib/libSystem.dylib";

    /// <summary><c>TASK_VM_INFO</c> aus <c>mach/task_info.h</c>.</summary>
    private const int TASK_VM_INFO = 22;

    /// <summary>Puffergroesse in Bytes — reichlich ueber <c>sizeof(task_vm_info_data_t)</c>.</summary>
    private const int PUFFER = 1024;

    private const int VERSATZ_FOOTPRINT = 144;
    private const int VERSATZ_SPITZE = 168;

    private static uint? _task;

    /// <summary>Grund, aus dem <c>task_info</c> nicht erreichbar war — dann wird es nicht erneut versucht.</summary>
    private static string? _nichtErreichbar;

    [DllImport(LIBSYSTEM)]
    private static extern int task_info(uint zielTask, int art, IntPtr info, ref int anzahl);

    /// <summary>Liest den Speicherstand; faellt bei jedem Fehler auf den Kern zurueck.</summary>
    internal static Prozessspeicherstand Lesen()
    {
        if (_nichtErreichbar != null) return Rueckfall(_nichtErreichbar);

        IntPtr puffer = IntPtr.Zero;
        try
        {
            uint task = EigenerTask();
            puffer = Marshal.AllocHGlobal(PUFFER);
            for (int i = 0; i < PUFFER; i += 8) Marshal.WriteInt64(puffer, i, 0L);

            int anzahl = PUFFER / 4;   // in natural_t
            int ergebnis = task_info(task, TASK_VM_INFO, puffer, ref anzahl);
            if (ergebnis != 0) return Rueckfall("task_info-kr" + ergebnis);

            int bytes = anzahl * 4;
            long? footprint = bytes >= VERSATZ_FOOTPRINT + 8 ? Marshal.ReadInt64(puffer, VERSATZ_FOOTPRINT) : null;
            long? spitze = bytes >= VERSATZ_SPITZE + 8 ? Marshal.ReadInt64(puffer, VERSATZ_SPITZE) : null;
            if (!(footprint > 0)) return Rueckfall("task_info-leer");
            return new Prozessspeicherstand(footprint, spitze > 0 ? spitze : null, "phys_footprint");
        }
        catch (Exception ex)
        {
            _nichtErreichbar = "task_info-" + ex.GetType().Name;
            return Rueckfall(_nichtErreichbar);
        }
        finally
        {
            if (puffer != IntPtr.Zero) Marshal.FreeHGlobal(puffer);
        }
    }

    /// <summary>
    /// <c>mach_task_self()</c> ist in C ein Makro auf die Variable <c>mach_task_self_</c>; gelesen wird
    /// deshalb die Variable selbst, nicht eine Funktion.
    /// </summary>
    private static uint EigenerTask()
    {
        if (_task.HasValue) return _task.Value;
        IntPtr bibliothek = NativeLibrary.Load(LIBSYSTEM);
        IntPtr adresse = NativeLibrary.GetExport(bibliothek, "mach_task_self_");
        uint task = unchecked((uint)Marshal.ReadInt32(adresse));
        _task = task;
        return task;
    }

    private static Prozessspeicherstand Rueckfall(string grund)
    {
        Prozessspeicherstand standard = Prozessspeicherstand.Standard();
        return new Prozessspeicherstand(standard.Aktuell, null, standard.Quelle + "(" + grund + ")");
    }
}
