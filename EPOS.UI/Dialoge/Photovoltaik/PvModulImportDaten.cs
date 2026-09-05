#nullable enable

using System;
using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Photovoltaik
{
    /// <summary>
    /// Was das Laden einer PV-Modulquelle ergeben hat (iU9-W13.3) — die
    /// Modulliste und, im Fehlerfall, der Meldungsschluessel des Dienstes.
    /// </summary>
    public sealed class PvLeseErgebnis
    {
        public PvLeseErgebnis(bool erfolgreich, IReadOnlyList<UnifiedModule>? module,
                              CecFortschritt meldung)
        {
            Erfolgreich = erfolgreich;
            Module = module ?? Array.Empty<UnifiedModule>();
            Meldung = meldung;
        }

        /// <summary>Steht die Liste?</summary>
        public bool Erfolgreich { get; }

        /// <summary>Die Module der gewaehlten Quelle.</summary>
        public IReadOnlyList<UnifiedModule> Module { get; }

        /// <summary>Der Schluessel der Rueckmeldung samt Platzhalterwerten.</summary>
        public CecFortschritt Meldung { get; }
    }

    /// <summary>
    /// Das Ergebnis der Vorpruefung EINES Moduls. Der PV-Import ist der einzige
    /// der Welle ohne Mehrfachauswahl (der Vorlaeufer setzte
    /// <c>MultiSelect = false</c>) — deshalb ein Befund und nicht eine Liste.
    /// </summary>
    public sealed class PvVorpruefung
    {
        public PvVorpruefung(ImportBefund befund, IReadOnlyList<ImportPruefung>? pruefungen,
                             IReadOnlyCollection<string>? vergebeneNamen,
                             string plausibilitaet = "", bool gesperrt = false)
        {
            Befund = befund;
            Pruefungen = pruefungen ?? Array.Empty<ImportPruefung>();
            VergebeneNamen = vergebeneNamen ?? Array.Empty<string>();
            Plausibilitaet = plausibilitaet ?? "";
            Gesperrt = gesperrt;
        }

        /// <summary>Der Befund des einen Kandidaten.</summary>
        public ImportBefund Befund { get; }

        /// <summary>Die Pruefliste fuer den Konfliktdialog (genau ein Eintrag).</summary>
        public IReadOnlyList<ImportPruefung> Pruefungen { get; }

        /// <summary>Die normalisierten Bestandsnamen — fuer die Namensvalidierung.</summary>
        public IReadOnlyCollection<string> VergebeneNamen { get; }

        /// <summary>
        /// Befund der Plausibilitaetspruefung (<c>PvModulPlausibilitaet</c>, Merge 5): leer =
        /// nichts zu bemerken; sonst der fertige Meldungstext. Mit <see cref="Gesperrt"/>
        /// ist es ein Fehler, der die Uebernahme verhindert, sonst eine Warnung, die der
        /// Dialog zurueckfragt.
        /// </summary>
        public string Plausibilitaet { get; }
        public bool Gesperrt { get; }
    }
}
