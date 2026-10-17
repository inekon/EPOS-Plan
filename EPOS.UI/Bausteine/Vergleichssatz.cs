namespace EPOS.UI.Bausteine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WindowsFormsApplication1;
    using WindowsFormsApplication1.MyResource;

    /// <summary>
    /// <b>Ein Satz im Vergleich</b> (KA-E-24): seine Herkunft (Projektzeile oder Katalogsatz), sein Name und seine
    /// Kennwerte als (Beschriftung, Wert). Projektzeile und Katalogsatz ziehen ihre Werte aus DERSELBEN
    /// Parameterliste (<c>Vergleichsparameter</c>); die Zeilen der Tabelle richten sich nach der Beschriftung.
    /// </summary>
    public sealed record Vergleichssatz(Satzmarke Herkunft, string Name,
                                        IReadOnlyList<(string Beschriftung, string Wert)> Werte)
    {
        /// <summary>Ein Satz aus der Parameterübersicht (<see cref="Parameterwert"/>), Einheit in eckigen Klammern.</summary>
        public static Vergleichssatz AusParametern(Satzmarke herkunft, string name, IReadOnlyList<Parameterwert>? werte)
            => new(herkunft, name, (werte ?? Array.Empty<Parameterwert>())
                .Select(p => (Satzvergleich.Beschriftung(p.Eintrag), p.Wert)).ToList());

        /// <summary>Ein Satz aus den Spalten eines Katalogprofils — der Rückfall ohne Parameterübersicht.</summary>
        public static Vergleichssatz AusSpalten(Satzmarke herkunft, Katalogfilterprofil? profil, Katalogfilterzeile zeile)
            => new(herkunft, zeile.Bezeichner, profil is null
                ? Array.Empty<(string, string)>()
                : profil.Spalten.Select(sp => (sp.Kopftext, zeile.Text(sp.Schluessel))).ToList());

        /// <summary>Das Wort der Herkunft im Spaltenkopf: „Projekt" oder „Katalog".</summary>
        public string Herkunftstext => Herkunft switch
        {
            Satzmarke.Projektsatz => Resource.AUSWAHL_VERGLEICH_HERKUNFT_PROJEKT,
            Satzmarke.Katalogsatz => Resource.AUSWAHL_VERGLEICH_HERKUNFT_KATALOG,
            _ => ""
        };
    }

    /// <summary>Ein Spaltenkopf des Vergleichs: Herkunft (leer im reinen Katalogvergleich) und Name.</summary>
    public sealed record Vergleichskopf(string Herkunft, string Name);

    /// <summary>
    /// <b>Der Bau der Vergleichstabelle</b> — eine Zeile je Beschriftung, eine Spalte je Satz; gemeinsam für
    /// die <see cref="Katalogliste"/> (reiner Katalogvergleich) und die <see cref="Zweispaltenauswahl"/>
    /// (Projektzeilen und Katalogsätze zusammen).
    /// </summary>
    public static class Satzvergleich
    {
        /// <summary>Höchstens DREI Sätze (5.5): vier Spalten plus Parameterspalte passen nicht.</summary>
        public const int HOECHSTENS = Katalogliste.HOECHSTENS;

        /// <summary>
        /// Die Zeilen über alle Sätze: die Reihenfolge des ersten Satzes, danach Beschriftungen, die nur ein
        /// späterer Satz führt; ein Wert, den ein Satz nicht führt, bleibt <see cref="ParameterVerwendung.LEER"/>.
        /// </summary>
        public static List<Katalogliste.Vergleichzeile> Zeilen(IReadOnlyList<Vergleichssatz> saetze)
        {
            ArgumentNullException.ThrowIfNull(saetze);
            var beschriftungen = new List<string>();
            var bekannt = new HashSet<string>(StringComparer.Ordinal);
            foreach (Vergleichssatz s in saetze)
                foreach (var (b, _) in s.Werte)
                    if (bekannt.Add(b)) beschriftungen.Add(b);

            var zeilen = new List<Katalogliste.Vergleichzeile>(beschriftungen.Count);
            foreach (string b in beschriftungen)
            {
                var werte = new List<string>(saetze.Count);
                foreach (Vergleichssatz s in saetze)
                {
                    string w = ParameterVerwendung.LEER;
                    foreach (var (sb, wert) in s.Werte)
                        if (sb == b) { w = wert; break; }
                    werte.Add(w);
                }
                zeilen.Add(new Katalogliste.Vergleichzeile(b, werte, Vergleichsbau.Abweichend(werte)));
            }
            return zeilen;
        }

        /// <summary>Die Beschriftung eines Parameters, die Einheit in eckigen Klammern.</summary>
        public static string Beschriftung(ParameterEintrag eintrag)
            => eintrag.Einheit.Length > 0 ? eintrag.Anzeigetext + " [" + eintrag.Einheit + "]" : eintrag.Anzeigetext;
    }
}
