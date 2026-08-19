using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KiKern
{
    /// <summary>Eine gelesene Protokollzeile.</summary>
    public sealed class KiProtokollEintrag
    {
        /// <summary>Zeitpunkt der Ausfuehrung (lokale Zeit, sekundengenau).</summary>
        public DateTime Zeitpunkt { get; set; }

        /// <summary>Name der Aktion.</summary>
        public string Aktion { get; set; } = "";

        /// <summary>Schutzstufe.</summary>
        public Schutzstufe Stufe { get; set; }

        /// <summary>Parameter als kompaktes JSON-Objekt.</summary>
        public string Parameter { get; set; } = "{}";

        /// <summary>Betroffene Projekt-ID; 0 = keine.</summary>
        public int ProjektId { get; set; }

        /// <summary>Ausgang des Versuchs.</summary>
        public KiStatus Status { get; set; }

        /// <summary>Kurzfassung des Ergebnisses.</summary>
        public string Ergebnis { get; set; } = "";

        /// <summary>Laufzeit in Millisekunden.</summary>
        public long DauerMs { get; set; }
    }

    /// <summary>
    /// Das Protokollformat: GENAU EINE Zeile je Ausfuehrungsversuch, maschinenlesbar und
    /// lesbar zugleich (Fachkonzept 3.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Aufbau, durch <c> | </c> getrennt:
    /// <code>
    /// Zeitstempel | Aktion | Stufe | Parameter | Projekt | Entscheidung | Ergebnis | Dauer
    /// 2026-08-19 14:22:31 | varianten_auflisten | lesen | {"projekt_id":1007} | 1007 | ausgefuehrt | 4x; 4 Varianten | 128 ms
    /// </code>
    /// Acht feste Felder, keine Kopfzeile je Eintrag - <c>split(" | ")</c> genuegt zum
    /// Auswerten, das Auge liest dieselbe Zeile ohne Werkzeug.
    /// </para>
    /// <para>
    /// <b>Maskierung.</b> Trennzeichen und Zeilenumbrueche in Werten werden mit einem
    /// Rueckstrich maskiert (<c>\\</c>, <c>\|</c>, <c>\n</c>, <c>\r</c>). Damit kann keine
    /// Meldung des Bestands die Zeilenstruktur zerbrechen - die Zusage „eine Zeile je
    /// Ausfuehrung" haelt auch bei mehrzeiligen Fehlertexten.
    /// </para>
    /// <para>
    /// Geschrieben wird die Datei im Anwendungsprojekt (<c>KiAusfuehrer</c>); der Kern
    /// liefert nur Format und Leser, damit beide zusammen pruefbar sind.
    /// </para>
    /// </remarks>
    public static class KiProtokoll
    {
        /// <summary>Feldtrenner.</summary>
        public const string Trenner = " | ";

        /// <summary>Zahl der Felder je Zeile.</summary>
        public const int Feldzahl = 8;

        /// <summary>Vorgeschlagener Dateiname neben der Datenbank (Fachkonzept 3.6).</summary>
        public const string Dateiname = "ki_aktionen.txt";

        /// <summary>Zeitformat - sortierbar, invariant, sekundengenau.</summary>
        public const string Zeitformat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>Platzhalter fuer „keine Projekt-ID".</summary>
        public const string KeinProjekt = "-";

        /// <summary>
        /// Vorspann einer neu angelegten Protokolldatei - nach dem Vorbild des
        /// Migrationsprotokolls (<c>Allgemein\Update\SchemaMigration.cs</c>).
        /// </summary>
        public static string Vorspann()
        {
            var sb = new StringBuilder();
            sb.Append("# Protokoll der KI-Aktionen (EPOS-Plan)").Append('\n');
            sb.Append("# Eine Zeile je Ausführungsversuch, Felder durch \" | \" getrennt:").Append('\n');
            sb.Append("# ").Append(Kopfzeile()).Append('\n');
            sb.Append("# Maskierung in Werten: \\\\ \\| \\n \\r").Append('\n');
            return sb.ToString();
        }

        /// <summary>Die Feldnamen in Reihenfolge - Kopf des Vorspanns.</summary>
        public static string Kopfzeile()
            => string.Join(Trenner, "Zeitstempel", "Aktion", "Stufe", "Parameter",
                                    "Projekt", "Entscheidung", "Ergebnis", "Dauer");

        // ================================================================== Schreiben

        /// <summary>Baut die Protokollzeile aus einem gepruefen Aufruf und seinem Ergebnis.</summary>
        public static string Zeile(DateTime zeitpunkt, KiAufruf aufruf, KiErgebnis ergebnis, int projektId = 0)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));
            if (ergebnis == null) throw new ArgumentNullException(nameof(ergebnis));

            return Zeile(zeitpunkt, aufruf.Name, aufruf.Aktion.Stufe, aufruf.AlsJson(), projektId,
                         ergebnis.Status, ergebnis.Kurzfassung(), ergebnis.Dauer);
        }

        /// <summary>
        /// Baut die Protokollzeile aus Einzelteilen - der Weg fuer abgewiesene Aufrufe, bei
        /// denen es noch keinen <see cref="KiAufruf"/> gibt (Fachkonzept 3.6: protokolliert
        /// wird JEDER Versuch, auch der abgelehnte).
        /// </summary>
        public static string Zeile(DateTime zeitpunkt, string aktion, Schutzstufe stufe, string parameterJson,
                                   int projektId, KiStatus status, string ergebnis, TimeSpan dauer)
        {
            var felder = new[]
            {
                zeitpunkt.ToString(Zeitformat, CultureInfo.InvariantCulture),
                Maskiere(aktion),
                SchutzstufeText.Schluessel(stufe),
                Maskiere(string.IsNullOrWhiteSpace(parameterJson) ? "{}" : parameterJson),
                projektId > 0 ? projektId.ToString(CultureInfo.InvariantCulture) : KeinProjekt,
                SchutzstufeText.Schluessel(status),
                Maskiere(ergebnis),
                ((long)Math.Round(dauer.TotalMilliseconds)).ToString(CultureInfo.InvariantCulture) + " ms"
            };
            return string.Join(Trenner, felder);
        }

        // ==================================================================== Lesen

        /// <summary>
        /// Liest eine Protokollzeile zurueck. Liefert <c>null</c> bei Kommentar-, Leer- oder
        /// unvollstaendigen Zeilen - eine kaputte Zeile darf die Auswertung nicht sprengen.
        /// </summary>
        public static KiProtokollEintrag? Lies(string? zeile)
        {
            if (string.IsNullOrWhiteSpace(zeile)) return null;
            if (zeile!.TrimStart().StartsWith("#", StringComparison.Ordinal)) return null;

            string[] f = zeile.Split(new[] { Trenner }, StringSplitOptions.None);
            if (f.Length != Feldzahl) return null;

            try
            {
                var e = new KiProtokollEintrag
                {
                    Zeitpunkt = DateTime.ParseExact(f[0], Zeitformat, CultureInfo.InvariantCulture),
                    Aktion = Demaskiere(f[1]),
                    Stufe = SchutzstufeText.StufeAusSchluessel(f[2]),
                    Parameter = Demaskiere(f[3]),
                    ProjektId = f[4] == KeinProjekt ? 0 : int.Parse(f[4], CultureInfo.InvariantCulture),
                    Status = SchutzstufeText.StatusAusSchluessel(f[5]),
                    Ergebnis = Demaskiere(f[6])
                };

                string dauer = f[7].EndsWith(" ms", StringComparison.Ordinal)
                    ? f[7].Substring(0, f[7].Length - 3) : f[7];
                e.DauerMs = long.Parse(dauer, CultureInfo.InvariantCulture);
                return e;
            }
            catch (FormatException) { return null; }
            catch (ArgumentException) { return null; }
            catch (OverflowException) { return null; }
        }

        /// <summary>Liest eine ganze Protokolldatei; kaputte Zeilen werden uebergangen.</summary>
        public static IReadOnlyList<KiProtokollEintrag> LiesAlle(IEnumerable<string> zeilen)
        {
            if (zeilen == null) throw new ArgumentNullException(nameof(zeilen));

            var liste = new List<KiProtokollEintrag>();
            foreach (string z in zeilen)
            {
                KiProtokollEintrag? e = Lies(z);
                if (e != null) liste.Add(e);
            }
            return liste;
        }

        // ================================================================ Maskierung

        /// <summary>Maskiert Trennzeichen und Zeilenumbrueche.</summary>
        public static string Maskiere(string? wert)
        {
            if (string.IsNullOrEmpty(wert)) return "";

            var sb = new StringBuilder(wert!.Length + 8);
            foreach (char c in wert)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '|': sb.Append("\\|"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Umkehrung von <see cref="Maskiere"/>.</summary>
        public static string Demaskiere(string? wert)
        {
            if (string.IsNullOrEmpty(wert)) return "";

            var sb = new StringBuilder(wert!.Length);
            for (int i = 0; i < wert.Length; i++)
            {
                if (wert[i] != '\\' || i == wert.Length - 1) { sb.Append(wert[i]); continue; }

                char n = wert[++i];
                switch (n)
                {
                    case '\\': sb.Append('\\'); break;
                    case '|': sb.Append('|'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    default: sb.Append('\\').Append(n); break;
                }
            }
            return sb.ToString();
        }
    }
}
