using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Ferien einer Zone für Testfälle</b> — die Liste <see cref="ZonenStand.Ferien"/> aus Paaren (Beginn, Ende)
    /// und zurück. Ein Paar ohne jede Angabe (beide <c>null</c>) entfällt; alles andere geht so in die Liste, wie es
    /// dasteht (auch 0 und 366 = keine Angabe), denn diese Deutung gehört dem Kern.
    /// </summary>
    internal static class Zonenferienprobe
    {
        /// <summary>Die Liste aus zwei gleich langen Reihen der Jahrestage (Beginn je Zeitraum, Ende je Zeitraum).</summary>
        internal static ZonenferienStand[] Liste(int?[] beginn, int?[] ende)
        {
            var liste = new List<ZonenferienStand>();
            int n = System.Math.Max(beginn?.Length ?? 0, ende?.Length ?? 0);
            for (int i = 0; i < n; i++)
            {
                int? b = beginn != null && i < beginn.Length ? beginn[i] : null;
                int? e = ende != null && i < ende.Length ? ende[i] : null;
                if (b.HasValue || e.HasValue) liste.Add(new ZonenferienStand { Beginn = b, Ende = e });
            }
            return liste.ToArray();
        }

        /// <summary>Die Liste aus Paaren (Beginn, Ende).</summary>
        internal static ZonenferienStand[] Liste(params (int? Beginn, int? Ende)[] paare)
            => paare.Select(p => new ZonenferienStand { Beginn = p.Beginn, Ende = p.Ende }).ToArray();

        /// <summary>Die Paare (Beginn, Ende) aus zwei Reihen — nach der Regel von <see cref="Liste(int?[], int?[])"/>.</summary>
        internal static (int? Beginn, int? Ende)[] Paare(int?[] beginn, int?[] ende)
            => Liste(beginn, ende).Select(f => (f.Beginn, f.Ende)).ToArray();

        /// <summary>Die Paare (Beginn, Ende) einer Zone in ihrer Reihenfolge.</summary>
        internal static (int? Beginn, int? Ende)[] Paare(ZonenStand z)
            => (z?.Ferien ?? new ZonenferienStand[0]).Select(f => (f.Beginn, f.Ende)).ToArray();
    }
}
