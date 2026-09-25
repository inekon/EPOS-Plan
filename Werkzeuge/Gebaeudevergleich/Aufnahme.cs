using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b><c>aufnahme</c> — die Momentaufnahme einer Quelle</b>, ohne die Quelle anzufassen.
    ///
    /// <para><b>Warum <c>immutable=1</c> und kein <c>--laufend</c>.</b> Schon ein Öffnen mit
    /// <c>Mode=ReadOnly</c> legt im WAL-Modus <c>-wal</c> und <c>-shm</c> neben die Datei —
    /// neben der Produktivdatenbank soll aber keine Datei entstehen. <c>immutable=1</c> liest ohne
    /// Sperre und ohne Nebendatei; dafür sieht es den Inhalt einer <c>-wal</c> nicht. Deshalb
    /// bricht die Aufnahme ab, sobald eine nichtleere <c>-wal</c> oder eine <c>-journal</c>
    /// daneben liegt, und nimmt eine Produktivquelle nur auf, wenn EPOS_Plan nicht läuft. Ob
    /// sich die Quelle während der Kopie bewegt hat, zeigt der SHA-256 davor und danach.</para>
    ///
    /// <para><b>Ablauf.</b> Prozesssperre (nur Produktivquelle) → Nebendateien → SHA-256 →
    /// <c>VACUUM INTO</c> → SHA-256 (bis zu dreimal wiederholt) → Nebendateien gegenprüfen →
    /// Kopie nur lesend prüfen: <c>integrity_check</c>, Schemastand, SHA-256 der Kopie.</para>
    /// </summary>
    internal static class Aufnahme
    {
        /// <summary>Wie oft die Kopie wiederholt wird, wenn sich die Quelle bewegt hat.</summary>
        internal const int WIEDERHOLUNGEN = 3;

        /// <summary>Der Prozessname des Programms (Assembly <c>EPOS_Plan</c>).</summary>
        internal const string PROZESSNAME = "EPOS_Plan";

        /// <summary>
        /// <b>Die Naht der Prozesssperre</b>: Läuft EPOS_Plan? Austauschbar, damit die Probe T8
        /// die Sperre nachweisen kann, ohne auf dem Rechner des Anwenders rot zu werden, wo das
        /// Programm meist läuft.
        /// </summary>
        internal static Func<bool> EposPlanLaeuft = ProzessLaeuft;

        private static bool ProzessLaeuft()
        {
            Process[] p = Process.GetProcessesByName(PROZESSNAME);
            try { return p.Length > 0; }
            finally { foreach (Process x in p) x.Dispose(); }
        }

        /// <summary>
        /// Die Prozesssperre — nur für eine Produktivquelle (unter <c>%ProgramData%\EPOS_PLAN</c>).
        /// Rückgabe <c>null</c> = frei, sonst der Grund.
        /// </summary>
        internal static string Prozesssperre(string quelle, Func<bool> laeuft)
        {
            if (!Schreibort.IstUnter(quelle, Schreibort.Produktivordner)) return null;
            if (!(laeuft ?? EposPlanLaeuft)()) return null;
            return "EPOS_Plan läuft. Eine Quelle unter " + Schreibort.Produktivordner + " wird nur bei " +
                   "geschlossenem Programm aufgenommen (immutable sieht den Inhalt einer -wal nicht). " +
                   "EPOS-Plan schließen und die Aufnahme wiederholen.";
        }

        /// <summary>Eine Nebendatei der Quelle: Name, Größe, Zeitpunkt.</summary>
        internal sealed record Nebendatei(string Name, long Groesse, DateTime ZeitUtc)
        {
            public override string ToString()
                => Name + " " + Groesse.ToString(CultureInfo.InvariantCulture) + " B " +
                   ZeitUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
        }

        /// <summary>Der Bestand der Nebendateien <c>-wal</c>, <c>-shm</c>, <c>-journal</c>.</summary>
        internal static List<Nebendatei> Nebendateien(string quelle)
        {
            var liste = new List<Nebendatei>();
            foreach (string endung in new[] { "-wal", "-shm", "-journal" })
            {
                var f = new FileInfo(quelle + endung);
                if (f.Exists) liste.Add(new Nebendatei(f.Name, f.Length, f.LastWriteTimeUtc));
            }
            return liste;
        }

        /// <summary>
        /// Rückgabe <c>null</c> = die Nebendateien erlauben eine Aufnahme; sonst der Grund. Eine
        /// leere <c>-wal</c> oder eine <c>-shm</c> allein wird nur protokolliert.
        /// </summary>
        internal static string NebendateienPruefen(string quelle, IReadOnlyList<Nebendatei> bestand)
        {
            foreach (Nebendatei n in bestand)
            {
                if (n.Name.EndsWith("-wal", StringComparison.OrdinalIgnoreCase) && n.Groesse > 0)
                    return "Neben der Quelle liegt eine nichtleere " + n.Name + " (" +
                           n.Groesse.ToString(CultureInfo.InvariantCulture) + " B). immutable sähe deren Inhalt " +
                           "nicht; das Programm schließen, damit es den Stand zurückschreibt.";
                if (n.Name.EndsWith("-journal", StringComparison.OrdinalIgnoreCase))
                    return "Neben der Quelle liegt " + n.Name + " - eine unterbrochene Transaktion. Die Quelle " +
                           "zuerst mit EPOS-Plan öffnen und schließen.";
            }
            return null;
        }

        internal static int Ausfuehren(Argumente arg, Ausgabe aus)
        {
            string quelle = arg.Quelle;
            string kopie = Path.Combine(arg.Ziel, Argumente.AUFNAHMEDATEI);
            aus.Konsole("Gebaeudevergleich aufnahme");
            Stopwatch uhr = Stopwatch.StartNew();
            string T() => "   [" + uhr.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s]";
            aus.Protokoll("Quelle  " + quelle);
            aus.Protokoll("Ziel    " + kopie);

            // 1. Prozesssperre - nur für eine Produktivquelle.
            string grund = Prozesssperre(quelle, EposPlanLaeuft);
            if (grund != null) return Abbruch(aus, grund);
            aus.Protokoll("Produktivquelle: " + (Schreibort.IstUnter(quelle, Schreibort.Produktivordner) ? "ja (EPOS_Plan läuft nicht)" : "nein"));

            // 2./3. Nebendateien und Prüfsumme der Quelle.
            List<Nebendatei> vorher = Nebendateien(quelle);
            aus.Protokoll("Nebendateien vorher: " + (vorher.Count == 0 ? "keine" : string.Join("; ", vorher)));
            grund = NebendateienPruefen(quelle, vorher);
            if (grund != null) return Abbruch(aus, grund);

            if (File.Exists(kopie) || File.Exists(kopie + "-wal") || File.Exists(kopie + "-journal"))
                return Abbruch(aus, "Im Ziel liegt schon eine Momentaufnahme: " + kopie + ". Ein neues Ziel nehmen.");

            // 4./5. VACUUM INTO über eine immutable-Verbindung, Prüfsumme davor und danach.
            string hashVorher = Sqlitehilfe.Sha256(quelle);
            aus.Protokoll("SHA-256 Quelle vorher:  " + hashVorher + T());
            bool stabil = false;
            for (int versuch = 0; versuch <= WIEDERHOLUNGEN && !stabil; versuch++)
            {
                if (versuch > 0)
                {
                    aus.Protokoll("Die Quelle hat sich während der Kopie geändert - Versuch " +
                                  (versuch + 1).ToString(CultureInfo.InvariantCulture));
                    Loeschen(kopie);
                    hashVorher = Sqlitehilfe.Sha256(quelle);
                }

                using (SqliteConnection v = Sqlitehilfe.Unveraenderlich(quelle))
                    Sqlitehilfe.VacuumInto(v, kopie);

                string hashNachher = Sqlitehilfe.Sha256(quelle);
                aus.Protokoll("SHA-256 Quelle nachher: " + hashNachher + T());
                stabil = hashNachher == hashVorher;
            }
            if (!stabil)
            {
                Loeschen(kopie);
                return Abbruch(aus, "Quelle ändert sich: Die Prüfsumme weicht nach " +
                                    (WIEDERHOLUNGEN + 1).ToString(CultureInfo.InvariantCulture) + " Versuchen ab.");
            }

            // 6. Die Nebendateien dürfen sich nicht bewegt haben. Gelöscht wird dort nichts.
            List<Nebendatei> nachher = Nebendateien(quelle);
            aus.Protokoll("Nebendateien nachher: " + (nachher.Count == 0 ? "keine" : string.Join("; ", nachher)));
            if (!vorher.SequenceEqual(nachher))
                return Abbruch(aus, "Der Bestand der Nebendateien der Quelle hat sich während der Aufnahme geändert " +
                                    "(vorher: " + (vorher.Count == 0 ? "keine" : string.Join("; ", vorher)) +
                                    "; nachher: " + (nachher.Count == 0 ? "keine" : string.Join("; ", nachher)) +
                                    "). Neben der Quelle wird nichts gelöscht.");

            // 7. Die Kopie nur lesend prüfen.
            string integritaet = Sqlitehilfe.Integritaet(kopie);
            aus.Protokoll("integrity_check der Kopie: " + integritaet + T());
            if (integritaet != "ok")
                return Abbruch(aus, "Die Momentaufnahme besteht den integrity_check nicht: " + integritaet);

            string bindung = Einstieg.DatenbankBinden(kopie);
            if (bindung != null) return Abbruch(aus, bindung);
            int stand = Schemapruefung.Stand();
            aus.Scharfschalten(Namensbereinigung.AusDatenbank());
            DataRepository_Loesen();

            aus.Protokoll("Schemastand der Kopie: " + stand.ToString(CultureInfo.InvariantCulture) +
                          " (Zielstand " + WindowsFormsApplication1.SchemaStand.Zielversion.ToString(CultureInfo.InvariantCulture) + ")" + T());
            string schema = Schemapruefung.Pruefen(stand);
            if (schema != null) aus.Protokoll("Hinweis: " + schema);
            aus.Protokoll("SHA-256 Kopie: " + Sqlitehilfe.Sha256(kopie) + T());
            aus.Protokoll("Größe Kopie: " + new FileInfo(kopie).Length.ToString(CultureInfo.InvariantCulture) + " B");

            aus.Konsole("Momentaufnahme geschrieben, Schemastand " + stand.ToString(CultureInfo.InvariantCulture) +
                        (schema == null ? " (Zielstand)" : " (nicht Zielstand - siehe protokoll.txt)"));
            return Program.OHNE_ROT;
        }

        /// <summary>Gibt die Kopie frei: keine gepoolte Verbindung, keine Nebendatei bleibt liegen.</summary>
        private static void DataRepository_Loesen()
        {
            WindowsFormsApplication1.DataRepository.PfadUeberschreibung = null;
            SqliteConnection.ClearAllPools();
        }

        private static int Abbruch(Ausgabe aus, string grund)
        {
            aus.Fehler("Abbruch: " + grund);
            return Program.ABBRUCH;
        }

        private static void Loeschen(string kopie)
        {
            SqliteConnection.ClearAllPools();
            foreach (string d in new[] { kopie, kopie + "-wal", kopie + "-shm", kopie + "-journal" })
                if (File.Exists(d)) File.Delete(d);
        }
    }
}
