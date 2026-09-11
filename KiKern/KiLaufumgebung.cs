using System;
using System.Threading;

namespace KiKern
{
    /// <summary>
    /// Ein Fortschrittsschritt einer lang laufenden Aktion (Fachkonzept 5.3,
    /// Auftrag #201 / Etappe S3).
    /// </summary>
    /// <remarks>
    /// <b>Zwei Angaben und mehr nicht.</b> Ein Anteil zwischen 0 und 1 - oder
    /// <c>null</c>, wo sich keiner angeben laesst (eine Bisektion weiss nicht, wie viele
    /// Schritte sie noch braucht) - und eine Zeile Klartext. Genau diese zwei Angaben
    /// fuehrt auch der Baustein <c>Fortschritt</c> der Oberflaeche; ein dritter waere
    /// eine zweite Wahrheit.
    /// </remarks>
    public sealed class KiFortschritt
    {
        /// <summary>Baut einen Schritt.</summary>
        /// <param name="anteil">0…1, oder <c>null</c> fuer „unbestimmt".</param>
        /// <param name="text">Eine Zeile Klartext; darf leer sein.</param>
        public KiFortschritt(double? anteil, string? text)
        {
            Anteil = anteil.HasValue
                ? (double?)(anteil.Value < 0 ? 0 : anteil.Value > 1 ? 1 : anteil.Value)
                : null;
            Text = text ?? "";
        }

        /// <summary>Der Anteil 0…1; <c>null</c> = unbestimmt.</summary>
        public double? Anteil { get; }

        /// <summary>Die Zeile Klartext.</summary>
        public string Text { get; }

        /// <inheritdoc/>
        public override string ToString()
            => (Anteil.HasValue ? (int)(Anteil.Value * 100) + " % " : "") + Text;
    }

    /// <summary>
    /// Was eine LANG LAUFENDE Aktion ueber ihren Lauf hinaus braucht: einen Weg, den
    /// Fortschritt zu melden, und einen, den Abbruch zu bemerken (Fachkonzept 3.4,
    /// Pflicht 4; Etappe S3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein eigener Parameter und nicht der <see cref="KiAufruf"/>.</b> Der
    /// Aufruf ist das, was aus der Modellantwort entsteht - reine Daten, vom Anwender
    /// mittelbar bestimmt. Fortschritt und Abbruchmarke gehoeren dem AUSFUEHRER; sie im
    /// Aufruf mitzufuehren hiesse, dem Modell einen Weg an die Laufsteuerung zu geben.
    /// </para>
    /// <para>
    /// <b>Nie <c>null</c>.</b> <see cref="Melde"/> schluckt einen fehlenden Empfaenger,
    /// und <see cref="Abbruch"/> ist im Zweifel <see cref="CancellationToken.None"/> -
    /// eine Aktion muss sich nicht darum kuemmern, ob gerade jemand zuhoert.
    /// </para>
    /// </remarks>
    public sealed class KiLaufumgebung
    {
        /// <summary>Die leere Umgebung: niemand hoert zu, nichts bricht ab.</summary>
        public static readonly KiLaufumgebung Leer = new KiLaufumgebung(null, CancellationToken.None);

        /// <summary>Baut eine Umgebung.</summary>
        public KiLaufumgebung(IProgress<KiFortschritt>? fortschritt, CancellationToken abbruch)
        {
            Fortschritt = fortschritt;
            Abbruch = abbruch;
        }

        /// <summary>Der Empfaenger der Fortschrittsschritte; <c>null</c> = keiner.</summary>
        public IProgress<KiFortschritt>? Fortschritt { get; }

        /// <summary>Die Abbruchmarke.</summary>
        public CancellationToken Abbruch { get; }

        /// <summary>Meldet einen Schritt. Ohne Empfaenger geschieht nichts.</summary>
        public void Melde(double? anteil, string? text)
        {
            IProgress<KiFortschritt>? ziel = Fortschritt;
            if (ziel == null) return;

            try { ziel.Report(new KiFortschritt(anteil, text)); }
            catch (Exception) { /* Ein Fortschrittsfehler darf keinen Lauf kippen. */ }
        }

        /// <summary>Wirft, wenn abgebrochen wurde - der uebliche Punkt zwischen zwei Phasen.</summary>
        public void AbbruchPruefen() => Abbruch.ThrowIfCancellationRequested();
    }
}
