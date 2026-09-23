using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Woher der Wert einer Wertgruppe stammt — die Wertemenge der Spalten
    /// <c>…Herkunftsart</c> der <c>Tab_Tww*_STAMM</c> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.1, Provenienzgruppe).
    /// </summary>
    internal enum Herkunftsart
    {
        /// <summary><c>VERFAHREN</c> — aus einem Verfahren gerechnet.</summary>
        Verfahren = 1,

        /// <summary><c>EIGENKONSTRUKTION</c>.</summary>
        Eigenkonstruktion = 2,

        /// <summary><c>FREI</c> — frei verfügbare Quelle.</summary>
        Frei = 3,

        /// <summary><c>IMPORT</c> — vom Anwender eingespielt.</summary>
        Import = 4,

        /// <summary><c>FIKTIV</c> — erfundener Wert (Testkatalog).</summary>
        Fiktiv = 5
    }

    /// <summary>
    /// Der Stand einer Katalogzeile — die Wertemenge der Spalte <c>Status</c> der
    /// <c>Tab_Tww*_STAMM</c> (Konzept 3.1).
    /// </summary>
    internal enum ZapfKatalogstatus
    {
        /// <summary><c>AUSLIEFERUNG</c> — kommt nur aus dem Katalogpaket, nie aus dem Repositorium.</summary>
        Auslieferung = 1,

        /// <summary><c>EIGEN</c> — vom Anwender angelegt oder kopiert; auch der fiktive Testkatalog.</summary>
        Eigen = 2,

        /// <summary><c>IMPORT</c> — aus einer Datei eingespielt; die Auslieferungsvorlage entfernt sie.</summary>
        Import = 3
    }

    /// <summary>
    /// Die Provenienz einer Wertgruppe (Konzept 2.1, 3.1): Quelle (nur Norm, Verfahren oder
    /// Eigenkonstruktion, nie ein Hersteller), Ausgabe, Katalogversion, in der die Gruppe
    /// zuletzt gesetzt wurde, und Herkunftsart. Die interne Spalte <c>Beleg</c> gehört
    /// ausdrücklich NICHT hierher — Oberfläche, Bericht und KiSicht zeigen sie nie
    /// (Kapitel 6 (e)).
    /// </summary>
    internal sealed record Provenienz(string Quelle, string Ausgabe, string Version, Herkunftsart Art);

    /// <summary>
    /// Die Bandbreite der Bedarfswerte je Niveau (<c>Bedarf_Niedrig_Min</c> …
    /// <c>Bedarf_Hoch_Max</c>) — je drei Einträge in der Reihenfolge niedrig, mittel,
    /// hoch; <c>null</c> heißt „keine Angabe".
    /// </summary>
    internal sealed record Bedarfsbandbreite(double?[] Min, double?[] Max);

    /// <summary>
    /// Die Provenienz einer Nutzungsart, gebündelt je Wertgruppe (Konzept 2.1): Bedarf
    /// samt Bandbreite, Jahresgang, Wochengang und — aus dem Tagesgangsatz — Tagesgang
    /// je Tagtyp (Index 0 … 3 für Tagtyp 1 … 4; <c>null</c>, wo dem Satz der Tagtyp fehlt).
    /// </summary>
    internal sealed record Katalogherkunft(
        Provenienz Bedarf,
        Bedarfsbandbreite Bandbreite,
        Provenienz Jahresgang,
        Provenienz Wochengang,
        IReadOnlyList<Provenienz> Tagesgang);

    /// <summary>
    /// Die Übersetzung zwischen den Textwerten der CHECK-Spalten und den Aufzählungen —
    /// an EINER Stelle, damit Lesen und Schreiben dieselben Wörter benutzen. Die Wörter
    /// selbst stehen als Konstanten in <see cref="TwwSchema"/>.
    /// </summary>
    internal static class TwwWertemengen
    {
        /// <summary>Die Herkunftsart zu einem Spaltenwert; ein fremdes Wort wird benannt abgelehnt.</summary>
        internal static Herkunftsart Herkunft(string text)
        {
            switch (text)
            {
                case TwwSchema.HERKUNFT_VERFAHREN: return Herkunftsart.Verfahren;
                case TwwSchema.HERKUNFT_EIGENKONSTRUKTION: return Herkunftsart.Eigenkonstruktion;
                case TwwSchema.HERKUNFT_FREI: return Herkunftsart.Frei;
                case TwwSchema.HERKUNFT_IMPORT: return Herkunftsart.Import;
                case TwwSchema.HERKUNFT_FIKTIV: return Herkunftsart.Fiktiv;
            }
            throw new ArgumentException("Unbekannte Herkunftsart „" + text + "“ in einer Tww-Katalogzeile.", nameof(text));
        }

        /// <summary>Der Spaltenwert zu einer Herkunftsart.</summary>
        internal static string Text(Herkunftsart art)
        {
            switch (art)
            {
                case Herkunftsart.Verfahren: return TwwSchema.HERKUNFT_VERFAHREN;
                case Herkunftsart.Eigenkonstruktion: return TwwSchema.HERKUNFT_EIGENKONSTRUKTION;
                case Herkunftsart.Frei: return TwwSchema.HERKUNFT_FREI;
                case Herkunftsart.Import: return TwwSchema.HERKUNFT_IMPORT;
                case Herkunftsart.Fiktiv: return TwwSchema.HERKUNFT_FIKTIV;
            }
            throw new ArgumentOutOfRangeException(nameof(art));
        }

        /// <summary>Der Katalogstatus zu einem Spaltenwert; ein fremdes Wort wird benannt abgelehnt.</summary>
        internal static ZapfKatalogstatus Status(string text)
        {
            switch (text)
            {
                case TwwSchema.STATUS_AUSLIEFERUNG: return ZapfKatalogstatus.Auslieferung;
                case TwwSchema.STATUS_EIGEN: return ZapfKatalogstatus.Eigen;
                case TwwSchema.STATUS_IMPORT: return ZapfKatalogstatus.Import;
            }
            throw new ArgumentException("Unbekannter Katalogstatus „" + text + "“ in einer Tww-Katalogzeile.", nameof(text));
        }

        /// <summary>Der Spaltenwert zu einem Katalogstatus.</summary>
        internal static string Text(ZapfKatalogstatus status)
        {
            switch (status)
            {
                case ZapfKatalogstatus.Auslieferung: return TwwSchema.STATUS_AUSLIEFERUNG;
                case ZapfKatalogstatus.Eigen: return TwwSchema.STATUS_EIGEN;
                case ZapfKatalogstatus.Import: return TwwSchema.STATUS_IMPORT;
            }
            throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
