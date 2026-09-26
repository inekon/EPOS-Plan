using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Die Stufe eines Hinweises des Berichtslaufs.</summary>
    public enum Berichtshinweisstufe
    {
        /// <summary>Beiläufige Erläuterung (Ersatzannahme, Zeitbasis, Rechenweg) — die Seite klappt sie ein.</summary>
        Hinweis,

        /// <summary>Etwas fehlt oder ist unvollständig — die Seite zeigt es sichtbar als Warnband.</summary>
        Warnung
    }

    /// <summary>
    /// Ein Hinweis des Berichtslaufs, gegliedert: der Stand, zu dem er gehört (leer = der Lauf
    /// als Ganzes), die Stufe und der Text OHNE Standvorsatz.
    /// </summary>
    public sealed class Berichtshinweis
    {
        public Berichtshinweis(string stand, bool istStamm, Berichtshinweisstufe stufe, string text)
        {
            Stand = stand ?? "";
            IstStamm = istStamm;
            Stufe = stufe;
            Text = text ?? "";
        }

        /// <summary>Anzeigename des Standes (<see cref="VariantenDaten.Anzeige"/>); leer = der ganze Lauf.</summary>
        public string Stand { get; }

        /// <summary>Der Stand ist der Stamm.</summary>
        public bool IstStamm { get; }

        /// <summary>Hinweis oder Warnung.</summary>
        public Berichtshinweisstufe Stufe { get; }

        /// <summary>Der Text ohne Standvorsatz.</summary>
        public string Text { get; }

        /// <summary>Der Vorsatz des Fließtextes: „Stamm 'X'" bzw. „Variante 'Y'".</summary>
        public static string Vorsatz(VariantenDaten v)
            => (v.IstStamm ? "Stamm" : "Variante") + " '" + v.Anzeige + "'";
    }

    /// <summary>Welche Art Gruppe die Hinweisliste bildet.</summary>
    public enum Berichtshinweisgruppenart
    {
        /// <summary>Hinweise des Laufs ohne Standbezug.</summary>
        Lauf,

        /// <summary>Hinweise, die für ALLE Stände des Laufs gleich lauten — einmal statt je Stand.</summary>
        AlleStaende,

        /// <summary>Die Hinweise eines Standes.</summary>
        Stand
    }

    /// <summary>Eine Gruppe der Hinweisliste: Art, Stand (bei <see cref="Berichtshinweisgruppenart.Stand"/>) und die Texte.</summary>
    public sealed class Berichtshinweisgruppe
    {
        public Berichtshinweisgruppe(Berichtshinweisgruppenart art, string stand, bool istStamm, IReadOnlyList<string> texte)
        {
            Art = art;
            Stand = stand ?? "";
            IstStamm = istStamm;
            Texte = texte ?? Array.Empty<string>();
        }

        public Berichtshinweisgruppenart Art { get; }
        public string Stand { get; }
        public bool IstStamm { get; }
        public IReadOnlyList<string> Texte { get; }
    }

    /// <summary>
    /// Gliedert die Hinweise eines Berichtslaufs für die Anzeige: zuerst die des Laufs, dann die,
    /// die für alle Stände gleich lauten (einmal statt je Stand), dann je Stand in der Folge des
    /// Laufs der Rest. Doppelte Texte eines Standes erscheinen einmal.
    /// </summary>
    public static class Berichtshinweise
    {
        /// <param name="hinweise">Die Hinweise (eine Stufe — der Aufrufer filtert).</param>
        /// <param name="staende">Alle Stände des Laufs in ihrer Folge (Stamm zuerst). Nur wenn es
        /// mindestens zwei sind, fasst die Gliederung gleichlautende Hinweise zusammen.</param>
        public static IReadOnlyList<Berichtshinweisgruppe> Gruppiere(IEnumerable<Berichtshinweis> hinweise,
                                                                    IReadOnlyList<string> staende)
        {
            var liste = (hinweise ?? Enumerable.Empty<Berichtshinweis>()).Where(h => h != null).ToList();
            var ergebnis = new List<Berichtshinweisgruppe>();
            if (liste.Count == 0) return ergebnis;

            // 1. Der Lauf ohne Standbezug.
            var lauf = liste.Where(h => h.Stand.Length == 0).Select(h => h.Text).Distinct(StringComparer.Ordinal).ToList();
            if (lauf.Count > 0) ergebnis.Add(new Berichtshinweisgruppe(Berichtshinweisgruppenart.Lauf, "", false, lauf));

            // Die Folge der Stände: die übergebene, dahinter jeder weitere in der Folge des Auftretens.
            var folge = new List<string>();
            foreach (string s in staende ?? Array.Empty<string>())
                if (!string.IsNullOrEmpty(s) && !folge.Contains(s)) folge.Add(s);
            foreach (Berichtshinweis h in liste)
                if (h.Stand.Length > 0 && !folge.Contains(h.Stand)) folge.Add(h.Stand);

            // 2. Gleich für alle Stände — nur bei mindestens zwei Ständen.
            var gemeinsam = new List<string>();
            if (folge.Count >= 2)
            {
                foreach (Berichtshinweis h in liste)
                {
                    if (h.Stand.Length == 0 || gemeinsam.Contains(h.Text)) continue;
                    bool ueberall = folge.All(s => liste.Any(x => x.Stand == s &&
                                                               string.Equals(x.Text, h.Text, StringComparison.Ordinal)));
                    if (ueberall) gemeinsam.Add(h.Text);
                }
                if (gemeinsam.Count > 0)
                    ergebnis.Add(new Berichtshinweisgruppe(Berichtshinweisgruppenart.AlleStaende, "", false, gemeinsam));
            }

            // 3. Je Stand der Rest.
            foreach (string s in folge)
            {
                var eigene = liste.Where(h => h.Stand == s && !gemeinsam.Contains(h.Text))
                                  .Select(h => h.Text).Distinct(StringComparer.Ordinal).ToList();
                if (eigene.Count == 0) continue;
                bool istStamm = liste.Any(h => h.Stand == s && h.IstStamm);
                ergebnis.Add(new Berichtshinweisgruppe(Berichtshinweisgruppenart.Stand, s, istStamm, eigene));
            }
            return ergebnis;
        }
    }
}
