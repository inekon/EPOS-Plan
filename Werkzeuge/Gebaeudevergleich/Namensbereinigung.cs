using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b>Die Namensbereinigung</b>: ersetzt in jedem Ausgabetext jeden Wert aus
    /// <c>Projektname</c>, <c>Kunde</c>, <c>Bearbeiter</c>, <c>Beschreibung</c> (Projekt; dazu der
    /// Variantenbezeichner) und <c>Gebaeudename</c>, <c>Beschreibung</c> (Gebäude) durch
    /// <c>Projekt &lt;ID&gt;</c> bzw. <c>Gebäude &lt;ID&gt;</c>.
    ///
    /// <para><b>Warum.</b> Die Arbeitsdatenbank enthält Kundendaten; die Ausgabe des Werkzeugs
    /// wird gelesen, besprochen und in Teilen in Papiere übernommen. Die Kennzahlen brauchen
    /// keinen Namen — die ID genügt, und wer die Namen braucht, ruft das Werkzeug selbst mit
    /// <c>--mit-namen</c>. Die Meldungen des Kerns tragen dagegen den Gebäudenamen
    /// (<c>SimulationWaermebedarf</c>, <c>Vdi6007Rechenweg</c>); deshalb läuft JEDE Zeile, die das
    /// Werkzeug schreibt oder die der Kern auf die Konsole legt, hier hindurch.</para>
    ///
    /// <para><b>Regeln.</b> Längere Werte zuerst, in EINEM Durchgang (ein Ersatztext wird nicht
    /// noch einmal durchsucht); Groß- und Kleinschreibung zählt. Leere und einstellige Werte
    /// bleiben stehen. Ein mehrzeiliger Wert wird auch zeilenweise und mit Leerzeichen statt
    /// Zeilenumbruch gesucht — so schreibt ihn <c>SimulationProtokoll</c>. Gehört derselbe
    /// Wert zu mehreren IDs, nennt der Ersatz alle (<c>Gebäude 10630/10631</c>).</para>
    /// </summary>
    internal sealed class Namensbereinigung
    {
        /// <summary>Werte bis zu dieser Länge bleiben stehen (Auftrag: „leere und einstellige").</summary>
        internal const int MINDESTLAENGE = 2;

        private readonly Dictionary<string, string> _ersatz;
        private readonly Regex _muster;

        /// <summary>Die Bereinigung, die nichts ersetzt (<c>--mit-namen</c> oder noch keine Namen).</summary>
        internal static Namensbereinigung Leer { get; } = new Namensbereinigung(new Dictionary<string, List<string>>());

        private Namensbereinigung(Dictionary<string, List<string>> werte)
        {
            _ersatz = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<string>> w in werte)
                _ersatz[w.Key] = Ersatztext(w.Value);

            if (_ersatz.Count > 0)
            {
                string alternativen = string.Join("|", _ersatz.Keys
                    .OrderByDescending(k => k.Length)
                    .ThenBy(k => k, StringComparer.Ordinal)
                    .Select(Regex.Escape));
                _muster = new Regex(alternativen, RegexOptions.CultureInvariant);
            }
        }

        /// <summary>Die gesuchten Werte (für die Probe T12 und die Prüfung vor einem Commit).</summary>
        internal IReadOnlyCollection<string> Werte => _ersatz.Keys;

        /// <summary>Die Anzahl der gesuchten Werte.</summary>
        internal int Anzahl => _ersatz.Count;

        internal string Bereinigen(string text)
        {
            if (_muster == null || string.IsNullOrEmpty(text)) return text;
            return _muster.Replace(text, m => _ersatz[m.Value]);
        }

        /// <summary>
        /// Liest die Namen der Datenbank, auf die <c>DataRepository</c> gerade zeigt — über die
        /// Controller des Kerns, ohne eigene SQL: <see cref="ProjektCtrl.NamenListe"/> (Name,
        /// Kunde, Beschreibung, Variantenbezeichner), <c>ProjektCtrl.ReadSingle</c> (Bearbeiter),
        /// <see cref="Z_ProjGebCtrl.LiesProjekt"/> und <see cref="GebaeudeBedarfCtrl.Projektgebaeude"/>
        /// (Gebäudename und Beschreibung, Schlüssel <c>Tab_Gebaeude.ID</c>).
        /// </summary>
        internal static Namensbereinigung AusDatenbank()
        {
            var werte = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (ProjektKopfZeile p in ProjektCtrl.NamenListe())
            {
                string wer = "Projekt " + Zahl(p.Id);
                Aufnehmen(werte, p.Name, wer);
                Aufnehmen(werte, p.Kunde, wer);
                Aufnehmen(werte, p.Beschreibung, wer);
                Aufnehmen(werte, p.Bezeichner, wer);

                var projekt = new ProjektCtrl();
                projekt.ReadSingle(p.Id);
                Aufnehmen(werte, projekt.m_szBearbeiter, wer);
                Aufnehmen(werte, projekt.m_szKunde, wer);
                Aufnehmen(werte, projekt.m_szBeschreibung, wer);

                foreach (Z_ProjGebModel z in Z_ProjGebCtrl.LiesProjekt(p.Id))
                {
                    // Die Sicht kann auf einem älteren Schemastand (vor der Migration) scheitern;
                    // dann bleibt der Name über die Zuordnung, unter der Projekt-ID.
                    ProjektGebaeudeModel g;
                    try { g = GebaeudeBedarfCtrl.Projektgebaeude(p.Id, z.ID_Z); }
                    catch (Exception) { g = null; }
                    string gebaeude = g != null ? "Gebäude " + Zahl(g.ID_Gebaeude) : wer;
                    Aufnehmen(werte, z.Gebaeudename, gebaeude);
                    Aufnehmen(werte, z.Beschreibung, gebaeude);
                    if (g != null)
                    {
                        Aufnehmen(werte, g.Gebaeudename, gebaeude);
                        Aufnehmen(werte, g.Beschreibung, gebaeude);
                    }
                }
            }
            return new Namensbereinigung(werte);
        }

        /// <summary>Für die Proben: eine Bereinigung aus festen Werten.</summary>
        internal static Namensbereinigung Aus(IEnumerable<(string Wert, string Wer)> eintraege)
        {
            var werte = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach ((string wert, string wer) in eintraege) Aufnehmen(werte, wert, wer);
            return new Namensbereinigung(werte);
        }

        private static void Aufnehmen(Dictionary<string, List<string>> werte, string wert, string wer)
        {
            if (string.IsNullOrEmpty(wert)) return;

            var fassungen = new List<string> { wert, wert.Trim() };
            if (wert.IndexOf('\n') >= 0 || wert.IndexOf('\r') >= 0)
            {
                // So schreibt SimulationProtokoll eine Meldung: Umbrüche werden Leerzeichen.
                fassungen.Add(wert.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim());
                fassungen.AddRange(wert.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(z => z.Trim()));
            }

            foreach (string f in fassungen)
            {
                if (f.Length < MINDESTLAENGE) continue;
                if (!werte.TryGetValue(f, out List<string> liste)) werte[f] = liste = new List<string>();
                if (!liste.Contains(wer)) liste.Add(wer);
            }
        }

        /// <summary>
        /// <c>Projekt 1007</c>, bei mehreren IDs derselben Art <c>Gebäude 10630/10631</c>, bei
        /// gemischten Arten <c>Projekt 1007/Gebäude 10614</c>.
        /// </summary>
        private static string Ersatztext(List<string> wer)
        {
            var sortiert = wer.Distinct().OrderBy(w => w, StringComparer.Ordinal).ToList();
            var arten = sortiert.Select(w => w.Substring(0, w.IndexOf(' '))).Distinct().ToList();
            if (arten.Count == 1)
                return arten[0] + " " + string.Join("/", sortiert.Select(w => w.Substring(w.IndexOf(' ') + 1)));
            return string.Join("/", sortiert);
        }

        private static string Zahl(int id) => id.ToString(CultureInfo.InvariantCulture);
    }
}
