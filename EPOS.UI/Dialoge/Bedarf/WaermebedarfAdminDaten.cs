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

    /// <summary>
    /// Die eine Entscheidung, die die Importkette des Waermebedarfs dem Anwender
    /// vorlegt. <c>null</c> als Rueckgabe = Abbruch, es wird nichts geschrieben.
    /// </summary>
    public sealed class WaermebedarfImportRueckrufe
    {
        /// <summary>
        /// Der Konfliktdialog (Namensdubletten im Katalog). <c>null</c> als
        /// RUECKGABE heisst Abbruch: Es wird nichts geschrieben.
        /// </summary>
        public Func<List<ImportPruefung>, HashSet<string>,
                    Task<List<KonfliktEntscheidung>?>>? Konflikte;
    }

    /// <summary>Wie der Import einer Waermebedarfsganglinie ausgegangen ist.</summary>
    public sealed class WaermebedarfImportErgebnis
    {
        /// <summary>Steht die Ganglinie im Katalog?</summary>
        public bool Erfolgreich;

        /// <summary>Der Bezeichner, unter dem sie steht.</summary>
        public string Bezeichner = "";

        /// <summary>Der fertige Text fuer das Banner; leer heisst: nichts zu melden.</summary>
        public string Meldung = "";
    }
}
