using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Plan eines Gebäudeexports</b> (Stufe G7a) — das Ergebnis von
    /// <see cref="GebaeudeExportAblauf.Vorbereiten"/>: das fertige Abbild, alle Meldungen, die der
    /// Exportdialog <b>vor</b> dem Schreiben zeigt (was fehlt, ersetzt oder abgelehnt wird), und die
    /// Ablehnung, wenn es eine gibt. Ein abgelehnter Plan trägt kein Abbild und wird nicht geschrieben.
    /// </summary>
    internal sealed class GebaeudeExportPlan
    {
        /// <summary>Legt den Plan an.</summary>
        internal GebaeudeExportPlan(GbxmlAbbild abbild, IReadOnlyList<PruefMeldung> meldungen, PruefMeldung ablehnung)
        {
            Ablehnung = ablehnung;
            Abbild = ablehnung == null ? abbild : null;
            Meldungen = meldungen ?? new List<PruefMeldung>();
        }

        /// <summary>Das Abbild, das der Schreiber schreibt; <c>null</c> bei einer Ablehnung.</summary>
        internal GbxmlAbbild Abbild { get; }

        /// <summary>Die Meldungen vor dem Schreiben, die Ablehnung eingeschlossen.</summary>
        internal IReadOnlyList<PruefMeldung> Meldungen { get; }

        /// <summary>Die Ablehnung (Stufe Fehler); <c>null</c> = der Plan ist schreibbar.</summary>
        internal PruefMeldung Ablehnung { get; }

        /// <summary>Ist der Export abgelehnt?</summary>
        internal bool Abgelehnt => Ablehnung != null;

        /// <summary>Die Schlüssel der Meldungen einer Stufe (für Tests und das Protokoll).</summary>
        internal IReadOnlyList<string> Schluessel(PruefStufe stufe)
            => Meldungen.Where(m => m.Stufe == stufe).Select(m => m.Schluessel).ToList();
    }
}
