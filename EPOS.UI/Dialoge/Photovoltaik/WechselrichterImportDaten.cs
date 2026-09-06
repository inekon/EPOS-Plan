#nullable enable

using System;
using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Photovoltaik
{
    /// <summary>
    /// Was das Laden der CEC-Wechselrichterliste ergeben hat — die Geräteliste und,
    /// im Fehlerfall, der Meldungsschlüssel des Dienstes.
    /// </summary>
    /// <remarks>
    /// Zwilling zu <see cref="PvLeseErgebnis"/>, Feld für Feld; verschieden ist allein
    /// der Satztyp. Die VORPRÜFUNG teilen sich beide Importe:
    /// <see cref="PvVorpruefung"/> kennt keinen Modultyp und trägt hier unverändert.
    /// </remarks>
    public sealed class WrLeseErgebnis
    {
        public WrLeseErgebnis(bool erfolgreich, IReadOnlyList<CecWechselrichter>? geraete,
                              CecFortschritt meldung)
        {
            Erfolgreich = erfolgreich;
            Geraete = geraete ?? Array.Empty<CecWechselrichter>();
            Meldung = meldung;
        }

        /// <summary>Steht die Liste?</summary>
        public bool Erfolgreich { get; }

        /// <summary>Die Geräte der Quelle.</summary>
        public IReadOnlyList<CecWechselrichter> Geraete { get; }

        /// <summary>Der Schlüssel der Rückmeldung samt Platzhalterwerten.</summary>
        public CecFortschritt Meldung { get; }
    }
}
