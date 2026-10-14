using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Diagnoseblock für Support und Dialog „Über EPOS-Plan" (Konzept Administration und
    /// Support, Abschnitt 3.6; Entwicklerhandbuch Abschnitt 3.3): Programmversion, Plattform,
    /// Sprache, Datenbankpfad, Schemastand, Katalogfassung, Lizenz, Gerätekennung-Kurzform,
    /// Protokollordner und Entwicklermodus.
    ///
    /// <para><b>Ein Supportwerkzeug wirft nie.</b> Jeder Wert steht in eigener Fehlerschranke;
    /// was sich nicht ermitteln lässt, heißt so (<c>DIAG_NICHT_ERMITTELBAR</c>). Der Block hat
    /// keine Nebenwirkung: Die Lizenz wird bewertet, ohne den Zeitanker fortzuschreiben, und
    /// eine fehlende Datenbank wird nicht angelegt.</para>
    /// </summary>
    public static class Diagnosedaten
    {
        /// <summary>Präfix des Abdrucks aus <see cref="GeraeteId.Ermitteln"/>.</summary>
        private const string ABDRUCK_PRAEFIX = "SHA256:";

        /// <summary>Länge der Kurzform der Gerätekennung.</summary>
        private const int KURZFORM_LAENGE = 8;

        /// <summary>Die Zeilen des Blocks in fester Reihenfolge.</summary>
        /// <param name="programmversion">Die Programmversion, wie die Schale sie kennt.</param>
        public static IReadOnlyList<(string Bezeichnung, string Wert)> Zeilen(string programmversion)
        {
            var zeilen = new List<(string, string)>();

            // Lizenz einmal lesen, die fünf Zeilen teilen sich Token und Status.
            LizenzToken token = Sicher(() => LizenzManager.Token);

            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_PROGRAMMVERSION, "Programmversion",
                             () => Leer(programmversion)));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_PLATTFORM, "Plattform",
                             () => RuntimeInformation.OSDescription.Trim() + " (" + RuntimeInformation.OSArchitecture + ")"));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_SPRACHE, "Sprache",
                             () => Leer(CultureInfo.CurrentUICulture.Name)));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_DATENBANKPFAD, "Datenbankpfad",
                             () => Leer(DataRepository.GetDBPath())));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_SCHEMASTAND, "Schemastand", Schemastand));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_KATALOGFASSUNG, "Katalogfassung", Katalogstand));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_LIZENZTYP, "Lizenztyp",
                             () => token == null ? MyResource.Resource.DIAG_KEINE_LIZENZ : Leer(token.TypText())));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_LIZENZNUMMER, "Lizenznummer",
                             () => token == null ? MyResource.Resource.DIAG_KEINE_LIZENZ
                                 : !string.IsNullOrWhiteSpace(token.LizenzId) ? token.LizenzId
                                 : token.Nummer.ToString(CultureInfo.InvariantCulture)));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_FIRMA, "Firma",
                             () => token == null ? MyResource.Resource.DIAG_KEINE_LIZENZ : Leer(token.Firma)));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_LIZENZSTATUS, "Lizenzstatus", () => Lizenzstatus(token)));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_GUELTIG_BIS, "Gültig bis",
                             () => token == null ? MyResource.Resource.DIAG_KEINE_LIZENZ
                                 : token.GueltigBis.HasValue ? token.GueltigBis.Value.ToString("d", CultureInfo.CurrentCulture)
                                 : MyResource.Resource.DIAG_UNBEFRISTET));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_GERAETEKENNUNG, "Gerätekennung", Geraetekurzform));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_PROTOKOLLORDNER, "Protokollordner", Protokollordner));
            zeilen.Add(Zeile(() => MyResource.Resource.DIAG_ENTWICKLERMODUS, "Entwicklermodus",
                             () => (Entwicklermodus.Aktiv ? MyResource.Resource.ENTW_STUFE_ENTWICKLER
                                                          : MyResource.Resource.ENTW_STUFE_ANWENDER)
                                   + " (" + Entwicklermodus.QuelleText + ")"));
            return zeilen;
        }

        /// <summary>Der Block als Text, je Zeile „Bezeichnung: Wert".</summary>
        public static string Textblock(string programmversion)
        {
            var sb = new StringBuilder();
            try
            {
                foreach ((string bezeichnung, string wert) in Zeilen(programmversion))
                    sb.Append(bezeichnung).Append(": ").Append(wert).Append(Environment.NewLine);
            }
            catch
            {
                // Zeilen fängt jeden Wert einzeln; das hier ist nur die letzte Schranke.
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        //  Einzelwerte
        // ------------------------------------------------------------------

        private static string Schemastand()
        {
            if (!DataRepository.DatenbankVorhanden()) return MyResource.Resource.DIAG_KEINE_DATENBANK;
            int stand = ApplikationCtrl.GetSchemaVersion();
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.DIAG_SCHEMA_FORMAT,
                                 stand, SchemaStand.Zielversion);
        }

        private static string Katalogstand()
        {
            if (!DataRepository.DatenbankVorhanden()) return MyResource.Resource.DIAG_KEINE_DATENBANK;
            DataTable dt = StilleDb.Tabelle("SELECT * FROM Tab_Applikation LIMIT 1");
            if (dt == null || dt.Rows.Count == 0 || !dt.Columns.Contains(Katalogfassung.SPALTE_FASSUNG))
                return MyResource.Resource.DIAG_NICHT_ERMITTELBAR;
            string wert = StilleDb.Text(dt.Rows[0][Katalogfassung.SPALTE_FASSUNG]);
            return string.IsNullOrWhiteSpace(wert) ? MyResource.Resource.DIAG_KATALOG_NIE : wert.Trim();
        }

        /// <summary>
        /// Status ohne Nebenwirkung: <see cref="LizenzManager.Bewerten"/> statt
        /// <see cref="LizenzManager.Pruefe"/>, ohne Zeitanker (die Uhrprüfung entfällt) und
        /// ohne ihn fortzuschreiben.
        /// </summary>
        private static string Lizenzstatus(LizenzToken token)
        {
            string geraet = token == null ? "" : GeraeteId.Ermitteln();
            LizenzStatus status = LizenzManager.Bewerten(token, geraet, DateTime.UtcNow.Date, DateTime.MinValue);
            return LizenzManager.StatusText(status, token);
        }

        private static string Geraetekurzform()
        {
            string abdruck = GeraeteId.Ermitteln() ?? "";
            if (abdruck.StartsWith(ABDRUCK_PRAEFIX, StringComparison.Ordinal))
                abdruck = abdruck.Substring(ABDRUCK_PRAEFIX.Length);
            if (abdruck.Length == 0) return MyResource.Resource.DIAG_NICHT_ERMITTELBAR;
            return abdruck.Length <= KURZFORM_LAENGE ? abdruck : abdruck.Substring(0, KURZFORM_LAENGE);
        }

        /// <summary>
        /// Der PROTOKOLLORDNER: <c>Logs</c> neben der Datenbank (wie <c>Program.Main</c>); leer,
        /// wenn kein Datenbankpfad bekannt ist. Der Weg der Schalen zu „Protokollordner öffnen".
        /// </summary>
        public static string ProtokollordnerPfad()
        {
            string db = DataRepository.GetDBPath();
            string ordner = string.IsNullOrWhiteSpace(db) ? null : Path.GetDirectoryName(db);
            return string.IsNullOrWhiteSpace(ordner) ? "" : Path.Combine(ordner, "Logs");
        }

        private static string Protokollordner()
        {
            string ordner = ProtokollordnerPfad();
            return ordner.Length == 0 ? MyResource.Resource.DIAG_NICHT_ERMITTELBAR : ordner;
        }

        // ------------------------------------------------------------------
        //  Fehlerschranken
        // ------------------------------------------------------------------

        /// <summary>Eine Zeile; Bezeichnung und Wert je in eigener Schranke.</summary>
        private static (string, string) Zeile(Func<string> bezeichnung, string ersatzbezeichnung, Func<string> wert)
        {
            string b = Sicher(bezeichnung);
            if (string.IsNullOrEmpty(b)) b = ersatzbezeichnung;
            string w = Sicher(wert);
            if (string.IsNullOrEmpty(w)) w = NichtErmittelbar();
            return (b, w);
        }

        private static T Sicher<T>(Func<T> f) where T : class
        {
            try { return f(); }
            catch { return null; }
        }

        private static string Leer(string wert)
        {
            return string.IsNullOrWhiteSpace(wert) ? null : wert.Trim();
        }

        private static string NichtErmittelbar()
        {
            try { return MyResource.Resource.DIAG_NICHT_ERMITTELBAR; }
            catch { return "?"; }
        }
    }
}
