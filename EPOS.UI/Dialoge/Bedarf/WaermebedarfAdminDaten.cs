#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf
{
    /// <summary>
    /// Was die verlustfreie Originalablage ergeben hat (iU9-W13.2).
    ///
    /// <para>Der Vorlaeufer kopierte die gewaehlte Datei in den Anwenderordner und
    /// fing jeden Fehler mit <c>catch { }</c> ab (Befund W13-B9). Hier kommt der
    /// Fehlschlag als <see cref="Meldung"/> zurueck: Wer glaubt, sein Original sei
    /// gesichert, und es ist nicht so, merkt es sonst erst, wenn er es braucht.</para>
    /// </summary>
    public sealed class AblageErgebnis
    {
        public AblageErgebnis(string pfad, string meldung = "")
        {
            Pfad = pfad ?? "";
            Meldung = meldung ?? "";
        }

        /// <summary>Der Pfad, unter dem die Datei jetzt liegt — leer bei Fehlschlag.</summary>
        public string Pfad { get; }

        /// <summary>Was schiefging; leer heisst: nichts.</summary>
        public string Meldung { get; }
    }

    // WaermebedarfImportRueckrufe und WaermebedarfImportErgebnis standen bis
    // iU9-W9-E-3 hier. Sie waren die Oberflaechenseite einer ZWEITEN, engeren
    // Importkette fuer den Waermebedarf; seit dem Anwenderwunsch W9-E-3 laeuft
    // auch der Waermebedarf durch GanglinienImportAblauf (Auspraegung
    // GanglinienZiel.Waermebedarf) und damit ueber GanglinienImportRueckrufe
    // und GanglinienImportErgebnis aus dem Kern. Zwei Ergebnistypen fuer
    // dieselbe Kette waeren beim ersten Fachwechsel auseinandergelaufen.
}
