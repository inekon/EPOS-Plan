using System;
using System.Globalization;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Sicherungspunkt einer Chatsitzung (Fachkonzept 4.4, Punkt 1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Einmal je Sitzung, nicht je Aktion.</b> Vor der ERSTEN Schreibaktion einer
    /// Sitzung entsteht eine datierte Kopie der Projektdatenbank; jede weitere
    /// Schreibaktion derselben Sitzung verweist auf dieselbe Kopie. Bei rund 90 MB
    /// Dateigroesse waere eine Kopie je Aktion weder zumutbar noch hilfreich - der
    /// Anwender will den Stand VOR dem Eingriff des Assistenten, nicht zwanzig
    /// Zwischenstaende.
    /// </para>
    /// <para>
    /// <b>Fehlschlag sperrt, statt zu warnen.</b> Laesst sich die Kopie nicht anlegen,
    /// werden Schreibaktionen abgelehnt (Fachkonzept 4.4, Punkt 1 woertlich). Eine
    /// Aenderung ohne Rueckweg ist genau das, was der Assistent nicht anrichten darf.
    /// </para>
    /// <para>
    /// <b>Die Kopie zieht <see cref="Datenbanksicherung.KopieAnlegen"/></b> - EINE
    /// Sicherungswahrheit im Kern (Auftrag #158), gemeinsam mit
    /// <c>MenueCtrl.DatenbankKopieAnlegen</c>. Sie zieht die Kopie ueber eine geoeffnete
    /// SQLite-Verbindung (<c>VACUUM INTO</c>, BETRIEB_SQLITE.md § 3.2) statt ueber eine
    /// byteweise Dateikopie: Unter SQLite im WAL-Modus ist der aktuelle Datenstand die Summe aus
    /// Hauptdatei und <c>-wal</c>, und nur eine Verbindung, die die Datei OEFFNET statt sie
    /// byteweise zu kopieren, liest zuverlaessig durch die <c>-wal</c> hindurch - auch waehrend
    /// die Anwendung sie geoeffnet haelt. Die fruehere <c>.laccdb</c>-Pruefung (Vorbild
    /// <c>Referenzlauf\DbUmgebung.ArbeitskopieAnlegen</c> aus der Access-Zeit) ist damit
    /// GEFALLEN: SQLite kennt keine solche Sperrdatei, der Hinweis konnte seit der
    /// SQLite-Umstellung nie mehr erscheinen, und <c>VACUUM INTO</c> braucht ihn nicht mehr -
    /// die Kopie ist so oder so vollstaendig und in sich konsistent. <see cref="Hinweis"/>
    /// bleibt als leere Eigenschaft stehen: <c>KiAusfuehrer.SicherungHinweis</c> (Windows-
    /// Oberflaeche, ausserhalb dieses Auftrags) greift weiter darauf zu.
    /// Ablage ist der Ordner <c>DB-Backup</c> NEBEN der Datenbank - derselbe Ort, an dem
    /// im Bestand die manuellen Staende liegen.
    /// </para>
    /// <para>
    /// <b>Namensschema.</b> <c>Kenndaten_KI_JJJJ-MM-TT_hhmmss.sqlite</c>. Bewusst ISO-nah
    /// und damit sortierbar - die vorhandenen Handstaende in <c>DB-Backup\</c> tragen
    /// uneinheitliche Datumsformen (<c>-10.06.2026</c>, <c>_13.05.2026</c>, <c>-alt1</c>)
    /// und liessen sich weder ordnen noch kollisionsfrei fortschreiben. Der Bestandteil
    /// <c>_KI_</c> macht auf einen Blick sichtbar, wer die Kopie angelegt hat.
    /// </para>
    /// </remarks>
    internal static class KiSicherungspunkt
    {
        /// <summary>Unterordner neben der Datenbank.</summary>
        internal const string ORDNER = "DB-Backup";

        /// <summary>Namensbestandteil, der die Kopie dem Assistenten zuordnet.</summary>
        internal const string KENNUNG = "_KI_";

        private static readonly object _sperre = new object();

        private static string _pfad = "";
        private static string _quelle = "";
        private static string _hinweis = "";

        /// <summary>
        /// Pfad des Sicherungspunkts dieser Sitzung; leer, solange keiner angelegt wurde.
        /// </summary>
        internal static string Pfad
        {
            get { lock (_sperre) { return _pfad; } }
        }

        /// <summary>
        /// Zusatzhinweis zum Sicherungspunkt; heute IMMER leer. <see cref="Datenbanksicherung"/>
        /// zieht die Kopie ueber eine geoeffnete SQLite-Verbindung (<c>VACUUM INTO</c>) - sie
        /// ist damit so oder so vollstaendig und in sich konsistent, es gibt keinen
        /// Zwischenstand mehr zu melden. Die Eigenschaft bleibt fuer
        /// <c>KiAusfuehrer.SicherungHinweis</c> (Windows-Oberflaeche) erhalten.
        /// </summary>
        internal static string Hinweis
        {
            get { lock (_sperre) { return _hinweis; } }
        }

        /// <summary>
        /// Stellt den Sicherungspunkt der Sitzung sicher.
        /// </summary>
        /// <param name="pfad">Der Pfad der Kopie; leer bei Fehlschlag.</param>
        /// <returns>
        /// <c>null</c>, wenn eine Kopie vorliegt - sonst der Klartextgrund, warum keine
        /// entstehen konnte. Ein Grund bedeutet: Schreibaktionen sind gesperrt.
        /// </returns>
        internal static string Sicherstellen(out string pfad)
        {
            lock (_sperre)
            {
                pfad = "";
                string quelle;
                try
                {
                    quelle = DataRepository.GetDBPath();
                }
                catch (Exception ex)
                {
                    return Grund(ex.Message);
                }

                if (string.IsNullOrEmpty(quelle) || !File.Exists(quelle))
                    return Grund(string.Format(CultureInfo.CurrentCulture,
                                               KiAktionsTexte.SicherungQuelleFehlt, quelle ?? ""));

                // Schon eine Kopie DIESER Datenbank in dieser Sitzung? Dann bleibt es dabei.
                if (_pfad.Length > 0 && File.Exists(_pfad) &&
                    string.Equals(_quelle, quelle, StringComparison.OrdinalIgnoreCase))
                {
                    pfad = _pfad;
                    return null;
                }

                try
                {
                    string ordner = Path.Combine(Path.GetDirectoryName(quelle) ?? "", ORDNER);

                    // KENNUNG ("_KI_") sitzt in der alten Zusammensetzung ZWISCHEN Stamm und
                    // Zeitstempel; Datenbanksicherung.KopieAnlegen setzt sein eigenes "_" vor
                    // den Zeitstempel, deshalb hier nur die fuehrende Haelfte ("_KI"). Ergebnis
                    // unveraendert: "Kenndaten_KI_2026-09-09_153000.sqlite".
                    string praefix = Path.GetFileNameWithoutExtension(quelle) + KENNUNG.TrimEnd('_');
                    string ziel = Datenbanksicherung.KopieAnlegen(quelle, ordner, praefix);

                    // Keine ".laccdb"-Pruefung mehr (SQLite kennt sie nicht) und damit kein
                    // Zwischenstands-Hinweis: VACUUM INTO liest ueber eine geoeffnete
                    // Verbindung durch die "-wal" hindurch und liefert immer den vollstaendigen,
                    // committeten Stand - _hinweis bleibt "" (siehe Hinweis oben).
                    _pfad = ziel;
                    _quelle = quelle;
                    pfad = ziel;
                    return null;
                }
                catch (Exception ex)
                {
                    return Grund(ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        /// <summary>Vergisst den Sicherungspunkt (Sitzungswechsel, Pruefläufe).</summary>
        internal static void Zuruecksetzen()
        {
            lock (_sperre)
            {
                _pfad = "";
                _quelle = "";
                _hinweis = "";
            }
        }

        private static string Grund(string ursache)
        {
            return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SicherungFehlgeschlagen,
                                 ursache ?? "");
        }
    }
}
