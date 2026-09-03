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
    /// <b>Muster.</b> Vorbild ist <c>Referenzlauf\DbUmgebung.ArbeitskopieAnlegen</c>:
    /// vorhandene <c>.laccdb</c> melden (die Datenbank ist geoeffnet, die Kopie kann einen
    /// Zwischenstand zeigen), <c>File.Copy</c>, Schreibschutzattribut der Kopie loesen.
    /// Ablage ist der Ordner <c>DB-Backup</c> NEBEN der Datenbank - derselbe Ort, an dem
    /// im Bestand die manuellen Staende liegen.
    /// </para>
    /// <para>
    /// <b>Namensschema.</b> <c>Kenndaten_KI_JJJJ-MM-TT_hhmmss.accdb</c>. Bewusst ISO-nah
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
        /// Zusatzhinweis zum Sicherungspunkt (z. B. „Datenbank war geoeffnet"); leer,
        /// wenn es nichts zu melden gibt.
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
                    Directory.CreateDirectory(ordner);

                    // Die Datenbank ist geoeffnet - lesendes Kopieren bleibt zulaessig,
                    // die Kopie kann aber einen Zwischenstand zeigen. Das ist ein HINWEIS,
                    // kein Abbruchgrund (Muster DbUmgebung.ArbeitskopieAnlegen).
                    string sperrdatei = Path.ChangeExtension(quelle, ".laccdb");
                    _hinweis = File.Exists(sperrdatei)
                        ? string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SicherungGeoeffnet,
                                        Path.GetFileName(sperrdatei))
                        : "";

                    string name = Path.GetFileNameWithoutExtension(quelle) + KENNUNG +
                                  DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture) +
                                  Path.GetExtension(quelle);
                    string ziel = Path.Combine(ordner, name);

                    File.Copy(quelle, ziel, false);

                    // Der Installer legt die Datenbank schreibgeschuetzt ab; das Attribut
                    // wandert beim Kopieren mit. Eine schreibgeschuetzte Sicherung waere
                    // zwar lesbar, liesse sich aber nicht zurueckspielen.
                    var info = new FileInfo(ziel);
                    if (info.IsReadOnly) info.IsReadOnly = false;

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
