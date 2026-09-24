using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KiKern
{
    /// <summary>
    /// Eine geplante Aenderung an EINEM Maskenfeld - der Baustein des Feldblocks
    /// (Fachkonzept 11.5).
    /// </summary>
    /// <remarks>
    /// Der alte Text wird VOR der Ausfuehrung auf dem UI-Thread gelesen und hier
    /// festgehalten. Er ist kein Schmuck: Erst „alt → neu" macht aus der Bestaetigung eine
    /// Entscheidung - der Anwender sieht, was er verliert, nicht nur, was er bekommt.
    /// </remarks>
    public sealed class KiFeldAenderung
    {
        /// <summary>Haelt eine geplante Feldaenderung fest.</summary>
        /// <param name="anzeigename">Klartextname des Feldes, wie er auf der Maske steht.</param>
        /// <param name="alterText">Der bisherige Inhalt; <c>null</c> gilt als leer.</param>
        /// <param name="neuerText">Der einzutragende Inhalt; <c>null</c> gilt als leer.</param>
        public KiFeldAenderung(string anzeigename, string? alterText, string? neuerText)
        {
            if (string.IsNullOrWhiteSpace(anzeigename))
                throw new ArgumentException(
                    "Eine Feldaenderung braucht den Anzeigenamen des Feldes.", nameof(anzeigename));

            Anzeigename = anzeigename;
            AlterText = alterText ?? "";
            NeuerText = neuerText ?? "";
        }

        /// <summary>Klartextname des Feldes.</summary>
        public string Anzeigename { get; }

        /// <summary>Der bisherige Inhalt; leer, wenn das Feld leer war.</summary>
        public string AlterText { get; }

        /// <summary>Der einzutragende Inhalt; leer, wenn das Feld geleert wird.</summary>
        public string NeuerText { get; }

        /// <summary>Aendert sich ueberhaupt etwas?</summary>
        public bool IstAenderung => !string.Equals(AlterText, NeuerText, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override string ToString() => KiFeldBlock.Zeile(this);
    }

    /// <summary>
    /// Baut den Klartext des Feldblocks, den der Anwender vor einer Formularaktion
    /// bestaetigt (Fachkonzept 11.5, Umsetzungskonzept 3b/F1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Reine Funktion, und der Text stammt NIE aus dem Modell.</b> Der Block entsteht
    /// ausschliesslich aus dem Katalog (Anzeigenamen) und den auf dem UI-Thread gelesenen
    /// Werten - dieselbe Regel wie bei <see cref="KiBestaetigung"/>. Waere er Modelltext,
    /// bestaetigte der Anwender eine Beschreibung, die mit dem tatsaechlichen Eingriff
    /// nichts zu tun haben muss.
    /// </para>
    /// <para>
    /// <b>Ohne eigene Aufzaehlungszeichen.</b> Der Block geht als Vorschautext in
    /// <see cref="KiBestaetigung.Erzeuge"/>, und DORT bekommt jede Zeile
    /// <see cref="KiBestaetigung.Punkt"/> vorangestellt. Wer hier noch einmal einrueckt,
    /// erzeugt doppelte Punkte im Bestaetigungsblock.
    /// </para>
    /// <para>
    /// <b>Felder und Knopf sind zwei Bloecke, nicht einer.</b> Das Setzen der Felder und
    /// das Ausloesen des Knopfes sind zwei Aufrufe mit je eigener Bestaetigung
    /// (Fachkonzept 11.4): Das Setzen bleibt in der Maske, erst der Knopf laesst die
    /// Pruefung des Bestands laufen und kann in die Datenbank fuehren. Ein gemeinsamer
    /// Block verwischte genau diesen Unterschied.
    /// </para>
    /// </remarks>
    public static class KiFeldBlock
    {
        /// <summary>Trennt Feldname und Werte innerhalb einer Zeile.</summary>
        public const string Trenner = " · ";

        /// <summary>Trennt alten von neuem Wert.</summary>
        public const string Pfeil = " → ";

        /// <summary>
        /// Der Block einer Feldsetzung: Maskenkopf und je Feld eine Zeile
        /// „Feld · alt → neu".
        /// </summary>
        /// <param name="maskenAnzeigename">Klartextname der Maske aus dem Katalog.</param>
        /// <param name="aenderungen">Die geplanten Aenderungen in Anzeigereihenfolge.</param>
        public static string Felder(string maskenAnzeigename, IReadOnlyList<KiFeldAenderung> aenderungen)
        {
            if (aenderungen == null) throw new ArgumentNullException(nameof(aenderungen));

            // Ein Block ohne Aenderung waere eine Bestaetigung ohne Gegenstand - der
            // Anwender klickte auf „Ausfuehren", ohne dass etwas geschieht.
            if (aenderungen.Count == 0)
                throw new ArgumentException(
                    "Ein Feldblock ohne Aenderung ist kein Block.", nameof(aenderungen));

            var sb = new StringBuilder();
            Kopf(sb, maskenAnzeigename);

            foreach (KiFeldAenderung a in aenderungen)
            {
                if (a == null)
                    throw new ArgumentException(
                        "Der Feldblock fuehrt einen leeren Eintrag.", nameof(aenderungen));
                sb.Append(Zeile(a)).Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Der Block einer Knopfausloesung: Maskenkopf und die Zeile
        /// „Knopf ‚Speichern' wird ausgelöst".
        /// </summary>
        /// <param name="maskenAnzeigename">Klartextname der Maske aus dem Katalog.</param>
        /// <param name="knopfAnzeigename">Beschriftung des Knopfes aus dem Katalog.</param>
        public static string Knopf(string maskenAnzeigename, string knopfAnzeigename)
        {
            if (string.IsNullOrWhiteSpace(knopfAnzeigename))
                throw new ArgumentException(
                    "Der Knopfblock braucht die Beschriftung des Knopfes.", nameof(knopfAnzeigename));

            var sb = new StringBuilder();
            Kopf(sb, maskenAnzeigename);
            sb.Append(string.Format(CultureInfo.CurrentCulture, KiTexte.KnopfWirdAusgeloest, knopfAnzeigename))
              .Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Der Block einer ZAHLENREIHE (Welle #458 Stufe 3b): Maskenkopf, eine Zeile
        /// „Reihe · n von m Werten ändern sich" und je GEAENDERTER Stelle
        /// „Reihe (Stelle) · alt → neu" - hoechstens <paramref name="hoechstens"/> solcher
        /// Zeilen, der Rest als Zahl.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Gekuerzt, aber nie verschwiegen.</b> 168 Wochenwerte in 168 Zeilen waeren
        /// eine Bestaetigung, die niemand liest. Gezeigt werden die ersten geaenderten
        /// Stellen samt ihren Namen, und die Kopfzeile sagt, wie viele es insgesamt sind -
        /// der Anwender weiss damit, was er freigibt. Unveraenderte Stellen stehen nicht
        /// im Block: Sie sind nicht Gegenstand der Entscheidung.
        /// </para>
        /// <para>
        /// <b>Reine Funktion wie der Feldblock</b>: Namen aus dem Katalog, Werte vom
        /// UI-Thread, nie Modelltext.
        /// </para>
        /// </remarks>
        /// <param name="maskenAnzeigename">Klartextname der Maske aus dem Katalog.</param>
        /// <param name="feldAnzeigename">Klartextname der Reihe aus dem Katalog.</param>
        /// <param name="reihe">Die Form der Reihe - sie nennt die Stellen.</param>
        /// <param name="alt">Die Werte vor dem Setzen; ein fehlendes Glied gilt als leer.</param>
        /// <param name="neu">Die Werte nach dem Setzen; ein fehlendes Glied gilt als leer.</param>
        /// <param name="kultur">Kultur der Zahlen; <c>null</c> = die laufende.</param>
        /// <param name="hoechstens">So viele geaenderte Stellen stehen einzeln da.</param>
        public static string Reihe(string maskenAnzeigename, string feldAnzeigename, KiZahlenreihe reihe,
                                   IReadOnlyList<double?> alt, IReadOnlyList<double?> neu,
                                   CultureInfo? kultur = null, int hoechstens = KiZahlenreihe.Kurzlaenge)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));
            if (string.IsNullOrWhiteSpace(feldAnzeigename))
                throw new ArgumentException("Der Reihenblock braucht den Anzeigenamen der Reihe.",
                                            nameof(feldAnzeigename));

            CultureInfo k = kultur ?? CultureInfo.CurrentCulture;

            var geaendert = new List<int>();
            for (int i = 0; i < reihe.Laenge; i++)
                if (Glied(alt, i) != Glied(neu, i)) geaendert.Add(i);

            // Dieselbe Regel wie beim Feldblock: ohne Aenderung kein Block.
            if (geaendert.Count == 0)
                throw new ArgumentException("Ein Reihenblock ohne Aenderung ist kein Block.", nameof(neu));

            var sb = new StringBuilder();
            Kopf(sb, maskenAnzeigename);
            sb.Append(string.Format(k, KiTexte.ReiheAenderung, feldAnzeigename, geaendert.Count, reihe.Laenge))
              .Append('\n');

            if (hoechstens < 1) hoechstens = 1;
            for (int n = 0; n < geaendert.Count && n < hoechstens; n++)
            {
                int i = geaendert[n];
                sb.Append(Zeile(new KiFeldAenderung(feldAnzeigename + " (" + reihe.Stellen[i] + ")",
                                                    KiZahlenreihe.Zahl(Glied(alt, i), k),
                                                    KiZahlenreihe.Zahl(Glied(neu, i), k))))
                  .Append('\n');
            }

            if (geaendert.Count > hoechstens)
                sb.Append(string.Format(k, KiTexte.ReiheWeitere, geaendert.Count - hoechstens)).Append('\n');

            return sb.ToString();
        }

        /// <summary>Das Glied an dieser Stelle; ausserhalb der Liste leer.</summary>
        private static double? Glied(IReadOnlyList<double?> werte, int i)
            => werte != null && i >= 0 && i < werte.Count ? werte[i] : null;

        /// <summary>Eine Zeile „Feld · alt → neu"; leere Werte werden benannt, nicht verschwiegen.</summary>
        public static string Zeile(KiFeldAenderung aenderung)
        {
            if (aenderung == null) throw new ArgumentNullException(nameof(aenderung));

            return aenderung.Anzeigename + Trenner +
                   Wert(aenderung.AlterText) + Pfeil + Wert(aenderung.NeuerText);
        }

        /// <summary>
        /// Der Wert fuer die Anzeige - ein leerer Wert wird ausgeschrieben.
        /// </summary>
        /// <remarks>
        /// „Wartungskosten ·  → 1200" saehe aus wie ein Anzeigefehler. Der Anwender soll
        /// sehen, dass das Feld vorher WIRKLICH leer war (oder nachher leer sein wird).
        /// </remarks>
        private static string Wert(string text) => text.Length == 0 ? KiTexte.WertLeer : text;

        private static void Kopf(StringBuilder sb, string maskenAnzeigename)
        {
            if (string.IsNullOrWhiteSpace(maskenAnzeigename))
                throw new ArgumentException(
                    "Der Block braucht den Anzeigenamen der Maske.", nameof(maskenAnzeigename));

            sb.Append(KiTexte.FeldMaske).Append(": ").Append(maskenAnzeigename).Append('\n');
        }
    }
}
