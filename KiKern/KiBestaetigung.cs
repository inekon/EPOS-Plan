using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KiKern
{
    /// <summary>
    /// Erzeugt den Klartext fuer Bestaetigung, Chat und Werkzeugliste
    /// (Fachkonzept 3.2 Verwendung c, 3.5 Punkt 2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der Bestaetigungstext stammt NIE von einem Modell.</b> Er entsteht ausschliesslich
    /// aus der Deklaration (<see cref="KiAktion"/>, <see cref="KiParameter"/>) und den
    /// gepruefen Werten des Aufrufs. Waere er Modelltext, bestaetigte der Anwender eine
    /// Beschreibung, die mit dem tatsaechlichen Aufruf nichts zu tun haben muss - genau der
    /// Fehler, den die Bestaetigungsschicht verhindern soll.
    /// </para>
    /// <para>
    /// <b>Kultur.</b> Zahlen werden hier - und nur hier - in der Anwenderkultur formatiert
    /// (Fachkonzept 3.2). Die Tests uebergeben die Kultur ausdruecklich, damit sie nicht
    /// von der Maschine abhaengen.
    /// </para>
    /// </remarks>
    public static class KiBestaetigung
    {
        /// <summary>Aufzaehlungszeichen der Angabenliste.</summary>
        public const string Punkt = "  · ";

        /// <summary>
        /// Der vollstaendige Bestaetigungstext: was geschieht, womit, was danach anders ist.
        /// </summary>
        /// <param name="aufruf">Der gepruefte Aufruf.</param>
        /// <param name="vorschautext">Ergebnis des Trockenlaufs (Stufe 2/3); <c>null</c> = keiner.</param>
        /// <param name="kultur">Anzeigekultur; <c>null</c> = <see cref="CultureInfo.CurrentCulture"/>.</param>
        /// <param name="sicherung">
        /// Pfad des Sicherungspunkts dieser Sitzung (Fachkonzept 4.4, Punkt 1);
        /// <c>null</c> = keiner noetig oder noch keiner angelegt.
        /// </param>
        /// <param name="gueltigBis">
        /// Zeitpunkt, zu dem die Vorschau verfaellt (Fachkonzept 3.5, Punkt 5);
        /// <c>null</c> = ohne Frist (Stufe 1).
        /// </param>
        public static string Erzeuge(KiAufruf aufruf, string? vorschautext = null,
                                     CultureInfo? kultur = null, string? sicherung = null,
                                     DateTime? gueltigBis = null)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));
            CultureInfo k = kultur ?? CultureInfo.CurrentCulture;
            KiAktion a = aufruf.Aktion;

            var sb = new StringBuilder();
            sb.Append(KiTexte.FeldAktion).Append(": ").Append(a.Name)
              .Append(" (").Append(KiTexte.Stufe(a.Stufe)).Append(')').Append('\n');
            sb.Append(KiTexte.FeldZweck).Append(": ").Append(a.Zweck).Append('\n');

            IReadOnlyList<string> angaben = aufruf.AlsKlartext(k);
            if (angaben.Count == 0)
            {
                sb.Append(KiTexte.FeldAngaben).Append(": ").Append(KiTexte.KeineAngaben).Append('\n');
            }
            else
            {
                sb.Append(KiTexte.FeldAngaben).Append(':').Append('\n');
                foreach (string z in angaben) sb.Append(Punkt).Append(z).Append('\n');
            }

            if (!string.IsNullOrWhiteSpace(vorschautext))
                sb.Append(KiTexte.FeldVorschau).Append(':').Append('\n')
                  .Append(Einruecken(vorschautext!)).Append('\n');

            if (a.Wirkung.Length > 0)
                sb.Append(KiTexte.FeldWirkung).Append(": ").Append(a.Wirkung).Append('\n');

            // Umkehrbarkeit steht NUR bei Aktionen, die etwas veraendern - bei einer
            // Leseaktion waere die Zeile bestenfalls Rauschen (Fachkonzept 4.4, Punkt 3).
            if (a.Stufe != Schutzstufe.Lesen)
                sb.Append(KiTexte.FeldRueckholbar).Append(": ")
                  .Append(a.Umkehrbar ? KiTexte.RueckholbarJa : KiTexte.RueckholbarNein)
                  .Append('\n');

            // Der Sicherungspunkt gehoert in die Bestaetigung, nicht nur ins Protokoll:
            // Der Anwender soll VOR dem Klick sehen, wohin der Vorzustand gesichert ist.
            if (!string.IsNullOrWhiteSpace(sicherung))
                sb.Append(KiTexte.FeldSicherung).Append(": ").Append(sicherung).Append('\n');

            if (gueltigBis.HasValue)
                sb.Append(KiTexte.FeldGueltigBis).Append(": ")
                  .Append(gueltigBis.Value.ToString("HH:mm:ss", k)).Append('\n');

            return sb.ToString();
        }

        /// <summary>
        /// Eine Zeile fuer den Chat: „varianten_auflisten (Projekt: 1007)".
        /// </summary>
        public static string Kurzfassung(KiAufruf aufruf, CultureInfo? kultur = null)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));

            IReadOnlyList<string> angaben = aufruf.AlsKlartext(kultur ?? CultureInfo.CurrentCulture);
            return angaben.Count == 0
                ? aufruf.Name
                : aufruf.Name + " (" + string.Join("; ", angaben) + ")";
        }

        /// <summary>
        /// Die Werkzeugliste, aus der der Anwender in Etappe 1 von Hand eine Aktion waehlt
        /// (Fachkonzept 8, Etappe 1).
        /// </summary>
        public static string Werkzeugliste(KiRegister register)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));

            var sb = new StringBuilder();
            foreach (KiAktion a in register.Alle)
            {
                sb.Append(a.Name).Append(" — ").Append(a.Zweck).Append('\n');
                foreach (KiParameter p in a.Parameter)
                {
                    sb.Append(Punkt).Append(p.Name);
                    if (!p.Pflicht) sb.Append(" (optional)");
                    sb.Append(": ").Append(p.SchemaBeschreibung()).Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>Beschreibung einer einzelnen Aktion fuer die Werkzeugliste.</summary>
        public static string Beschreibe(KiAktion aktion)
        {
            if (aktion == null) throw new ArgumentNullException(nameof(aktion));

            var sb = new StringBuilder();
            sb.Append(KiTexte.FeldAktion).Append(": ").Append(aktion.Name)
              .Append(" (").Append(KiTexte.Stufe(aktion.Stufe)).Append(')').Append('\n');
            sb.Append(KiTexte.FeldZweck).Append(": ").Append(aktion.Zweck).Append('\n');

            if (aktion.Parameter.Count == 0)
            {
                sb.Append(KiTexte.FeldAngaben).Append(": ").Append(KiTexte.KeineAngaben).Append('\n');
            }
            else
            {
                sb.Append(KiTexte.FeldAngaben).Append(':').Append('\n');
                foreach (KiParameter p in aktion.Parameter)
                {
                    sb.Append(Punkt).Append(p.Anzeigename).Append(" (").Append(p.Name);
                    if (!p.Pflicht) sb.Append(", optional");
                    sb.Append("): ").Append(p.SchemaBeschreibung()).Append('\n');
                }
            }

            if (aktion.Andockpunkt.Length > 0)
                sb.Append(KiTexte.FeldAndockpunkt).Append(": ").Append(aktion.Andockpunkt).Append('\n');

            return sb.ToString();
        }

        private static string Einruecken(string text)
        {
            string[] zeilen = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var sb = new StringBuilder();
            for (int i = 0; i < zeilen.Length; i++)
            {
                if (i == zeilen.Length - 1 && zeilen[i].Length == 0) break;
                sb.Append(Punkt).Append(zeilen[i]);
                if (i < zeilen.Length - 1) sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
