using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ZapfprofilValidierung.Tests
{
    /// <summary>
    /// <b>Die Vorrichtung der Proben</b>: Sie findet das mitgelieferte Beispiel, legt Arbeitskopien
    /// in einem eigenen Ordner unter <c>Path.GetTempPath()</c> an und ruft den Einstieg des Werkzeugs
    /// <b>in diesem Prozess</b> (kein <c>dotnet run</c>) — Rückgabecode und Ausgaben kommen als Werte
    /// zurück.
    ///
    /// <para><b>Nichts wird im Repositorium geschrieben.</b> Jede Probe arbeitet auf einer Kopie;
    /// das Beispiel selbst bleibt unberührt.</para>
    /// </summary>
    internal sealed class Vorrichtung : IDisposable
    {
        internal Vorrichtung()
        {
            Wurzel = RepoWurzel();
            Beispiel = Path.Combine(Wurzel, "Werkzeuge", "ZapfprofilValidierung", "Beispiel");
            Katalog = Path.Combine(Beispiel, "katalog");
            Arbeitsordner = Path.Combine(Path.GetTempPath(), "ZapfprofilValidierung.Proben",
                                         Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Arbeitsordner);
        }

        /// <summary>Die Repowurzel (aufwärts gesucht, höchstens acht Ebenen).</summary>
        internal string Wurzel { get; }

        /// <summary>Der Ordner <c>Werkzeuge/ZapfprofilValidierung/Beispiel</c>.</summary>
        internal string Beispiel { get; }

        /// <summary>Der Katalogordner des Beispiels.</summary>
        internal string Katalog { get; }

        /// <summary>Der eigene Arbeitsordner dieser Probe; er wird am Ende gelöscht.</summary>
        internal string Arbeitsordner { get; }

        /// <summary>Liegt das Beispiel? Ohne es überspringen die Fälle still.</summary>
        internal bool BeispielDa => Directory.Exists(Beispiel)
                                    && File.Exists(Path.Combine(Beispiel, "BSP-WOHNEN-01", "messreihe.csv"));

        /// <summary>Ein neuer, leerer Unterordner.</summary>
        internal string Neu(string name)
        {
            string p = Path.Combine(Arbeitsordner, name);
            Directory.CreateDirectory(p);
            return p;
        }

        /// <summary>
        /// Kopiert ein Beispielobjekt in einen neuen Quellordner und liefert dessen Pfad; der
        /// Objektordner heißt wie im Beispiel.
        /// </summary>
        internal string ObjektKopie(string kennung, string nameQuelle)
        {
            string quelle = Neu(nameQuelle);
            string ziel = Path.Combine(quelle, kennung);
            Directory.CreateDirectory(ziel);
            foreach (string d in Directory.EnumerateFiles(Path.Combine(Beispiel, kennung)))
                File.Copy(d, Path.Combine(ziel, Path.GetFileName(d)), true);
            return quelle;
        }

        /// <summary>Der Pfad der <c>objekt.json</c> im kopierten Objektordner.</summary>
        internal static string Objektdatei(string quelle, string kennung)
            => Path.Combine(quelle, kennung, "objekt.json");

        /// <summary>Der Pfad der Messreihe im kopierten Objektordner.</summary>
        internal static string Messdatei(string quelle, string kennung)
            => Path.Combine(quelle, kennung, "messreihe.csv");

        /// <summary>Führt das Werkzeug aus und liefert Rückgabecode samt beiden Ausgaben.</summary>
        internal (int Code, string Aus, string Fehler) Lauf(params string[] args)
        {
            var aus = new StringWriter();
            var fehler = new StringWriter();
            int code = Einstieg.Lauf(args, aus, fehler);
            return (code, aus.ToString(), fehler.ToString());
        }

        /// <summary>
        /// Schreibt eine Messreihe aus Zeitstempeln und Werten — für die Fälle Teiljahr und
        /// Volumenreihe, die aus der Beispielreihe abgeleitet werden.
        /// </summary>
        internal static void ReiheSchreiben(string pfad, string kopf, IEnumerable<(DateTime Zeit, double Wert)> zeilen)
        {
            var s = new StringBuilder();
            s.Append(kopf).Append("\r\n");
            foreach ((DateTime z, double w) in zeilen)
                s.Append(z.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)).Append(';')
                 .Append(w.ToString("0.######", CultureInfo.InvariantCulture)).Append("\r\n");
            File.WriteAllText(pfad, s.ToString(), new UTF8Encoding(false));
        }

        /// <summary>Liest eine Messreihendatei des Beispiels als Zeitstempel und Wert.</summary>
        internal static List<(DateTime Zeit, double Wert)> ReiheLesen(string pfad)
        {
            var liste = new List<(DateTime, double)>();
            foreach (string zeile in File.ReadAllLines(pfad, Encoding.UTF8).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(zeile)) continue;
                string[] f = zeile.Split(';');
                liste.Add((DateTime.ParseExact(f[0], "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                           double.Parse(f[1], CultureInfo.InvariantCulture)));
            }
            return liste;
        }

        /// <summary>Ersetzt einen Textabschnitt in der <c>objekt.json</c> (kleine, gezielte Änderung).</summary>
        internal static void JsonErsetzen(string pfad, string alt, string neu)
        {
            string t = File.ReadAllText(pfad, Encoding.UTF8);
            if (!t.Contains(alt, StringComparison.Ordinal))
                throw new InvalidOperationException("In " + pfad + " steht \"" + alt + "\" nicht.");
            File.WriteAllText(pfad, t.Replace(alt, neu, StringComparison.Ordinal), new UTF8Encoding(false));
        }

        /// <summary>Sucht die Repowurzel am Kennzeichen <c>WP-Plan.sln</c>.</summary>
        internal static string RepoWurzel()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 10 && d != null; i++, d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return AppContext.BaseDirectory;
        }

        public void Dispose()
        {
            try { Directory.Delete(Arbeitsordner, true); } catch (Exception) { /* Aufräumen ist Kür */ }
        }
    }
}
