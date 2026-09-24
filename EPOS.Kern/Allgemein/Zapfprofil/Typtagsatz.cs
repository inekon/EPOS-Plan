using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Jahreszeit einer Typtagkategorie (Umsetzungskonzept Zapfprofilgenerator 4.2, Stufe Z4b;
    /// Grundlagen 5, Abschnitt 2.2). Sie entsteht aus der Tagesmitteltemperatur: über der
    /// Heizgrenze der Gebäudeart <see cref="Sommer"/>, unter der Wintergrenze
    /// <see cref="Winter"/>, dazwischen <see cref="Uebergang"/>. Die beiden Grenzen sind Werte des
    /// eingespielten Pakets, nie Zahlen des Quelltexts (Konzept Kapitel 6 (a)).
    /// </summary>
    internal enum Typtagjahreszeit
    {
        /// <summary>Übergangstag: Tagesmittel zwischen Wintergrenze und Heizgrenze.</summary>
        Uebergang = 1,

        /// <summary>Sommertag: Tagesmittel über der Heizgrenze der Gebäudeart.</summary>
        Sommer = 2,

        /// <summary>Wintertag: Tagesmittel unter der Wintergrenze.</summary>
        Winter = 3
    }

    /// <summary>
    /// Die Tagart einer Typtagkategorie: Werktag oder Sonntag. Ein Feiertag zählt als Sonntag
    /// (Grundlagen 5, Abschnitt 2.2, Anmerkung 2 zum Begriff „Sonntag"); der Samstag ist ein
    /// Werktag. Die Quelle ist der Zapfkalender (<see cref="Zapfkalender"/>), nicht der Tagtyp
    /// des Katalogs — die vier <see cref="ZapfTagtyp"/> fassen diese Systematik nicht.
    /// </summary>
    internal enum Typtagart
    {
        /// <summary>Werktag (Montag bis Samstag ohne Feiertag).</summary>
        Werktag = 1,

        /// <summary>Sonntag und jeder gesetzliche Feiertag.</summary>
        Sonntag = 2
    }

    /// <summary>
    /// Die Bewölkung einer Typtagkategorie, aus dem Tagesmittel des Bedeckungsgrads
    /// (<c>Tab_Solar.Bedeckungsgrad</c> in Achteln) gegen die Schwelle des eingespielten Pakets.
    /// <see cref="Ohne"/> heißt: Diese Jahreszeit unterscheidet nicht nach Bewölkung — dann gilt
    /// die Kategorie für jeden Tag ihrer Jahreszeit und Tagart.
    /// </summary>
    internal enum Typtagbewoelkung
    {
        /// <summary>Ohne Unterscheidung (eine Kategorie für heiter und bewölkt).</summary>
        Ohne = 0,

        /// <summary>Heiter: Tagesmittel des Bedeckungsgrads unter der Schwelle.</summary>
        Heiter = 1,

        /// <summary>Bewölkt: Tagesmittel des Bedeckungsgrads ab der Schwelle.</summary>
        Bewoelkt = 2
    }

    /// <summary>
    /// Eine Typtagkategorie des eingespielten Pakets: ihr <see cref="Code"/> (der Schlüssel, unter
    /// dem Anzahl, Faktor und Tagesgang stehen) und die drei Merkmale, nach denen ihr ein
    /// Kalendertag zufällt.
    /// </summary>
    internal sealed record Typtagkategorie(string Code, Typtagjahreszeit Jahreszeit, Typtagart Tagart,
                                           Typtagbewoelkung Bewoelkung);

    /// <summary>
    /// Der normierte Tagesgang einer Typtagkategorie (wahlfreier Teil des Pakets):
    /// <see cref="AufloesungMin"/> ist das Zeitraster, <see cref="Anteile"/> sind die
    /// 1440/<see cref="AufloesungMin"/> Anteile mit Summe 1. Ohne Tagesgang trägt der
    /// Tagesgangsatz der Zone die Tagesform.
    ///
    /// <para><b>Die Auflösung ist ein Teiler oder ein Vielfaches von 60 Minuten</b>
    /// (<c>Normformvektorleser.AufloesungTauglich</c>): Nur dann fasst
    /// <see cref="Stundenanteile"/> die Abschnitte verlustfrei zu Stunden zusammen. Der Leser und
    /// der Controller lassen keine andere herein.</para>
    /// </summary>
    internal sealed record Typtaggang(string Gebaeudeart, string Typtag, int AufloesungMin, double[] Anteile)
    {
        /// <summary>Die Anteile zu Stundenwerten zusammengefasst (24 Werte, Summe unverändert).</summary>
        internal double[] Stundenanteile()
        {
            var stunden = new double[Zapfkalender.STUNDEN_TAG];
            int jeStunde = Math.Max(1, 60 / Math.Max(1, AufloesungMin));
            if (AufloesungMin >= 60)
            {
                // Ein Abschnitt deckt mehrere Stunden: sein Anteil verteilt sich gleichmäßig.
                int stundenJeAbschnitt = AufloesungMin / 60;
                for (int i = 0; i < Anteile.Length; i++)
                    for (int k = 0; k < stundenJeAbschnitt; k++)
                    {
                        int h = i * stundenJeAbschnitt + k;
                        if (h < stunden.Length) stunden[h] += Anteile[i] / stundenJeAbschnitt;
                    }
                return stunden;
            }
            for (int i = 0; i < Anteile.Length; i++)
            {
                int h = i / jeStunde;
                if (h < stunden.Length) stunden[h] += Anteile[i];
            }
            return stunden;
        }
    }

    /// <summary>
    /// <b>Die Schlüssel der Kennwerte eines Normformvektorpakets</b> (Konzept Kapitel 6 (a),
    /// Stufe Z4b): Die Schlüssel stehen im Code, die Werte nie — sie kommen aus der Datei
    /// <c>kennwerte.csv</c> des Pakets, das der lizenzierte Anwender einspielt. Fehlt ein
    /// Pflichtschlüssel, ist das Paket benannt abgelehnt.
    /// </summary>
    internal static class Typtagkennwert
    {
        /// <summary>
        /// Vorsatz der Heizgrenze je Gebäudeart [°C]: <c>heizgrenze.&lt;gebaeudeart&gt;</c>. Über
        /// ihr ist ein Tag ein Sommertag. Pflicht je Gebäudeart des Pakets.
        /// </summary>
        internal const string HEIZGRENZE_VORSATZ = "heizgrenze.";

        /// <summary>Die Wintergrenze [°C]: darunter ist ein Tag ein Wintertag. Pflicht.</summary>
        internal const string WINTERGRENZE = "wintergrenze";

        /// <summary>
        /// Die Schwelle der Bewölkung [Achtel]: ab diesem Tagesmittel des Bedeckungsgrads gilt ein
        /// Tag als bewölkt. Pflicht, sobald eine Kategorie nach Bewölkung unterscheidet.
        /// </summary>
        internal const string BEWOELKUNG_SCHWELLE = "bewoelkung.schwelle";

        /// <summary>
        /// Die Toleranz der Prüfsumme Σ n_TT · F_TT je Zone und Gebäudeart [-] (wahlfrei); ohne
        /// sie prüft der Leser die Summe nicht und sagt das als Hinweis.
        /// </summary>
        internal const string PRUEFSUMME_TOLERANZ = "pruefsumme.toleranz";

        /// <summary>Die Heizgrenze einer Gebäudeart als Schlüssel.</summary>
        internal static string Heizgrenze(string gebaeudeart) => HEIZGRENZE_VORSATZ + (gebaeudeart ?? "");

        /// <summary>Textschlüssel: die Quelle des Pakets (Pflicht) — Richtlinie und Ausgabe.</summary>
        internal const string TEXT_QUELLE = "quelle";

        /// <summary>Textschlüssel: die Ausgabe der Richtlinie (wahlfrei).</summary>
        internal const string TEXT_AUSGABE = "ausgabe";
    }

    /// <summary>
    /// <b>Ein eingespieltes Normformvektorpaket im Speicher</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 2.5, 4.2; Stufe Z4b): die Typtagkategorien, die Klimazonen, je
    /// Klimazone und Gebäudeart die Zahl der Kalendertage und den Faktor der Tagesenergie je
    /// Kategorie, wahlfrei die normierten Tagesgänge, dazu die Kennwerte des Verfahrens.
    ///
    /// <para><b>Nur Daten.</b> Der Satz kennt keine Datenbank und keinen Dienst; er entsteht im
    /// <c>Normformvektorleser</c> aus einem Paket oder in <c>TwwTyptagCtrl</c> aus
    /// <c>Tab_TwwTyptag_IMPORT</c>. <b>Keine Werte im Quelltext</b> (Konzept Kapitel 6): Jeder
    /// Zahlenwert kommt aus dem Paket des lizenzierten Anwenders.</para>
    /// </summary>
    internal sealed class Normformvektorsatz
    {
        private readonly Dictionary<string, Typtagkategorie> _kategorien = new Dictionary<string, Typtagkategorie>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _anzahl = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _faktor = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly Dictionary<string, Typtaggang> _gaenge = new Dictionary<string, Typtaggang>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _kennwerte = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly SortedSet<int> _zonen = new SortedSet<int>();
        private readonly SortedSet<string> _gebaeudearten = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>Quelle des Pakets (Richtlinie samt Ausgabe) — nie ein Hersteller, nie ein Produkt.</summary>
        internal string Quelle { get; set; } = "";

        /// <summary>Die Ausgabe der Richtlinie; leer, wenn das Paket keine nennt.</summary>
        internal string Ausgabe { get; set; } = "";

        /// <summary>Der Tag des Einspielens (ISO, <c>yyyy-MM-dd</c>); leer, solange nichts geschrieben ist.</summary>
        internal string DatumImport { get; set; } = "";

        /// <summary>Die Typtagkategorien in der Reihenfolge des Pakets.</summary>
        internal IReadOnlyList<Typtagkategorie> Kategorien { get; private set; } = new Typtagkategorie[0];

        /// <summary>Die Klimazonen des Pakets, aufsteigend.</summary>
        internal IReadOnlyList<int> Klimazonen => _zonen.ToList();

        /// <summary>Die Gebäudearten des Pakets, alphabetisch.</summary>
        internal IReadOnlyList<string> Gebaeudearten => _gebaeudearten.ToList();

        /// <summary>Alle Kennwerte je Schlüssel (Zahlen; die Texte stehen in <see cref="Quelle"/> und <see cref="Ausgabe"/>).</summary>
        internal IReadOnlyDictionary<string, double> Kennwerte => _kennwerte;

        /// <summary>Alle Tagesgänge des Pakets; leer, wenn es keine führt.</summary>
        internal IReadOnlyList<Typtaggang> Gaenge => _gaenge.Values.ToList();

        /// <summary>Trägt der Satz überhaupt etwas (Kategorien und mindestens eine Zone)?</summary>
        internal bool Traegt => Kategorien.Count > 0 && _zonen.Count > 0;

        private static string Schluessel(int zone, string gebaeudeart, string typtag)
            => zone.ToString(CultureInfo.InvariantCulture) + "\u0001" + (gebaeudeart ?? "") + "\u0001" + (typtag ?? "");

        private static string Gangschluessel(string gebaeudeart, string typtag)
            => (gebaeudeart ?? "") + "\u0001" + (typtag ?? "");

        /// <summary>Setzt die Kategorien (der Leser und der Controller tun das einmal).</summary>
        internal void KategorienSetzen(IEnumerable<Typtagkategorie> kategorien)
        {
            Kategorien = (kategorien ?? new Typtagkategorie[0]).ToList();
            _kategorien.Clear();
            foreach (Typtagkategorie k in Kategorien) _kategorien[k.Code] = k;
        }

        /// <summary>Nimmt die Zahl der Kalendertage einer Kategorie auf.</summary>
        internal void AnzahlSetzen(int zone, string gebaeudeart, string typtag, int anzahl)
        {
            _anzahl[Schluessel(zone, gebaeudeart, typtag)] = anzahl;
            _zonen.Add(zone);
            _gebaeudearten.Add(gebaeudeart ?? "");
        }

        /// <summary>Nimmt den Faktor der Tagesenergie einer Kategorie auf.</summary>
        internal void FaktorSetzen(int zone, string gebaeudeart, string typtag, double faktor)
        {
            _faktor[Schluessel(zone, gebaeudeart, typtag)] = faktor;
            _zonen.Add(zone);
            _gebaeudearten.Add(gebaeudeart ?? "");
        }

        /// <summary>Nimmt einen Tagesgang auf.</summary>
        internal void GangSetzen(Typtaggang gang)
        {
            if (gang == null) return;
            _gaenge[Gangschluessel(gang.Gebaeudeart, gang.Typtag)] = gang;
        }

        /// <summary>Nimmt einen Kennwert auf.</summary>
        internal void KennwertSetzen(string schluessel, double wert) => _kennwerte[schluessel ?? ""] = wert;

        /// <summary>Die Kategorie zu einem Code; <c>null</c>, wenn das Paket ihn nicht führt.</summary>
        internal Typtagkategorie Kategorie(string code)
            => code != null && _kategorien.TryGetValue(code, out Typtagkategorie k) ? k : null;

        /// <summary>
        /// Die Kategorie zu Jahreszeit, Tagart und Bewölkung: zuerst die genaue, sonst die
        /// Kategorie derselben Jahreszeit und Tagart ohne Bewölkungsunterscheidung
        /// (<see cref="Typtagbewoelkung.Ohne"/>). <c>null</c>, wenn das Paket keine führt.
        /// </summary>
        internal Typtagkategorie Kategorie(Typtagjahreszeit jahreszeit, Typtagart tagart, Typtagbewoelkung bewoelkung)
            => Kategorien.FirstOrDefault(k => k.Jahreszeit == jahreszeit && k.Tagart == tagart && k.Bewoelkung == bewoelkung)
               ?? Kategorien.FirstOrDefault(k => k.Jahreszeit == jahreszeit && k.Tagart == tagart
                                                 && k.Bewoelkung == Typtagbewoelkung.Ohne);

        /// <summary>Die Zahl der Kalendertage einer Kategorie; <c>null</c>, wenn das Paket sie nicht führt.</summary>
        internal int? Anzahl(int zone, string gebaeudeart, string typtag)
            => _anzahl.TryGetValue(Schluessel(zone, gebaeudeart, typtag), out int a) ? a : (int?)null;

        /// <summary>Der Faktor der Tagesenergie; <c>null</c>, wenn das Paket ihn nicht führt.</summary>
        internal double? Faktor(int zone, string gebaeudeart, string typtag)
            => _faktor.TryGetValue(Schluessel(zone, gebaeudeart, typtag), out double f) ? f : (double?)null;

        /// <summary>Der Tagesgang einer Kategorie; <c>null</c>, wenn das Paket keinen führt.</summary>
        internal Typtaggang Gang(string gebaeudeart, string typtag)
            => _gaenge.TryGetValue(Gangschluessel(gebaeudeart, typtag), out Typtaggang g) ? g : null;

        /// <summary>Ein Kennwert; <c>null</c>, wenn das Paket ihn nicht führt.</summary>
        internal double? Kennwert(string schluessel)
            => schluessel != null && _kennwerte.TryGetValue(schluessel, out double w) ? w : (double?)null;

        /// <summary>
        /// Führt das Paket zu dieser Klimazone und Gebäudeart jede Kategorie — Anzahl UND Faktor?
        /// Die fehlenden Codes stehen in <paramref name="fehlend"/>.
        /// </summary>
        internal bool Vollstaendig(int zone, string gebaeudeart, out IReadOnlyList<string> fehlend)
        {
            var offen = new List<string>();
            foreach (Typtagkategorie k in Kategorien)
                if (!Anzahl(zone, gebaeudeart, k.Code).HasValue || !Faktor(zone, gebaeudeart, k.Code).HasValue)
                    offen.Add(k.Code);
            fehlend = offen;
            return offen.Count == 0;
        }

        /// <summary>Die Summe der Kalendertage einer Zone und Gebäudeart über alle Kategorien.</summary>
        internal int Tagesumme(int zone, string gebaeudeart)
            => Kategorien.Sum(k => Anzahl(zone, gebaeudeart, k.Code) ?? 0);
    }
}
