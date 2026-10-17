using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Waermepumpe;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Sperrfenster zwischen Kern und Feldsatz</b> (Welle V14): liest die Fenster einer
    /// Anlage samt aktivem Altfenster in die Liste des Wärmepumpen-Dialogs und bildet sie beim
    /// Speichern zurück. Plattformfrei — beide Schalen nehmen dieselbe Abbildung.
    /// </summary>
    public static class SperrfensterAbbildung
    {
        /// <summary>
        /// Die Liste für den Dialog: das aktive Altfenster als erste Zeile, dann die Zeilen von
        /// <c>Tab_Sperrfenster</c>; <c>null</c>, solange die Tabelle fehlt (dann zeigt der Dialog
        /// das Altfenster wie zuvor).
        /// </summary>
        public static List<SperrfensterZeile>? Lesen(int idAnlage, bool sperrung, int von, int bis)
        {
            if (!SperrfensterCtrl.TabelleVorhanden()) return null;
            return SperrfensterCtrl.MitAltfenster(idAnlage, sperrung, von, bis).Select(Zeile).ToList();
        }

        /// <summary>Ein Fenster des Kerns als Zeile des Feldsatzes.</summary>
        public static SperrfensterZeile Zeile(Sperrfenster f) => new()
        {
            VonH = f.VonH, DauerH = f.DauerH, Wochentage = f.Wochentage, HeizstabGesperrt = f.HeizstabGesperrt
        };

        /// <summary>Die Zeilen des Feldsatzes als Fenster des Kerns; unvollständige Zeilen fallen weg.</summary>
        public static List<Sperrfenster> Fenster(IEnumerable<SperrfensterZeile>? zeilen)
        {
            var l = new List<Sperrfenster>();
            if (zeilen is null) return l;
            int rang = 0;
            foreach (SperrfensterZeile z in zeilen)
            {
                if (z?.VonH is not double von || z.DauerH is not double dauer || dauer <= 0) continue;
                l.Add(new Sperrfenster
                {
                    VonH = von, DauerH = dauer, Wochentage = z.Wochentage, HeizstabGesperrt = z.HeizstabGesperrt,
                    Reihenfolge = ++rang
                });
            }
            return l;
        }

        /// <summary>
        /// Überträgt die Liste ans Modell: gesetzt (auch leer) schreibt sie der Speicherweg der Anlagen
        /// nach <c>Tab_Sperrfenster</c>, und das Altfenster der Zeile geht aus — EINE Quelle. Ohne Liste
        /// bleibt alles, wie es ist.
        /// </summary>
        public static void NachModell(WaermepumpeAnlageDaten d, WErzeugerModel m)
        {
            if (d?.Sperrfenster is null || m is null) return;
            m.WP_Sperrfenster = Fenster(d.Sperrfenster);
            m.Sperrung = false;
        }
    }
}
