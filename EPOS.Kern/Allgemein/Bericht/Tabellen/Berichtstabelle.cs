using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Rolle einer Zelle (Konzept Berichtsvorlagen 5.4, 6.4 Nr. 2): <b>Stamm</b> — die je Block
    /// wiederholte Spalte des Stammprojekts bzw. der Referenz; <b>Gruppe</b> und <b>Summe</b> — Zeilen mitten
    /// in der Tabelle; <b>Warnung</b> — Einzelzellen mit einem Vorbehalt. Eine Zelle kann mehrere Rollen tragen
    /// (die Summe in der Stammspalte). Die Rollen liest der Vorlagenweg aus der Mustertabelle
    /// <c>{{muster.tabelle}}</c>; ohne Muster gilt die <see cref="Tabellenhinterlegung"/> der Zelle.
    /// </summary>
    [Flags]
    public enum Tabellenrolle
    {
        /// <summary>Keine Rolle.</summary>
        Keine = 0,

        /// <summary>Spalte des Stammprojekts bzw. der Referenz.</summary>
        Stamm = 1,

        /// <summary>Gruppenzeile (Zwischenüberschrift in der Tabelle).</summary>
        Gruppe = 2,

        /// <summary>Summenzeile oder -spalte.</summary>
        Summe = 4,

        /// <summary>Zelle mit Vorbehalt (etwa ein mehrdeutiger Zinsfuß).</summary>
        Warnung = 8,
    }

    /// <summary>Die waagerechte Ausrichtung einer Zelle.</summary>
    public enum Tabellenausrichtung
    {
        /// <summary>Linksbündig (Text).</summary>
        Links,

        /// <summary>Mittig (Kopf, Strich „—“, Häkchen).</summary>
        Mitte,

        /// <summary>Rechtsbündig (Zahl).</summary>
        Rechts,
    }

    /// <summary>
    /// Die heutige Direktformatierung einer Zelle (WordBerichtGenerator: <c>HEAD_FILL</c>, <c>STAMM_FILL</c>) —
    /// das Aussehen des Bausteinwegs und des Vorlagenwegs ohne Tabellenformatvorlage und ohne Mustertabelle.
    /// </summary>
    public enum Tabellenhinterlegung
    {
        /// <summary>Keine Hinterlegung.</summary>
        Keine,

        /// <summary>Die Hinterlegung des Tabellenkopfs (<see cref="WordBerichtGenerator.HEAD_FILL"/>).</summary>
        Kopf,

        /// <summary>Die Hinterlegung der Stammspalte (<see cref="WordBerichtGenerator.STAMM_FILL"/>).</summary>
        Stamm,
    }

    /// <summary>Wie eine Spalte an der Blockteilung teilnimmt (Konzept 5.4, 6.4).</summary>
    public enum Spaltenart
    {
        /// <summary>Feste Spalte (Beschriftung) — steht in jedem Block.</summary>
        Fest,

        /// <summary>Die Spalte des Stammprojekts — steht in jedem Block.</summary>
        Stamm,

        /// <summary>Eine Spalte je Stand — die Stände werden auf die Blöcke verteilt.</summary>
        Stand,

        /// <summary>Eine feste Spalte hinter den Ständen (Δ-Spalte) — steht in jedem Block.</summary>
        Nach,
    }

    /// <summary>Eine Spalte der <see cref="Berichtstabelle"/>.</summary>
    public sealed class Tabellenspalte
    {
        /// <summary>Wie die Spalte an der Blockteilung teilnimmt.</summary>
        public Spaltenart Art { get; init; } = Spaltenart.Fest;

        /// <summary>
        /// Die Breite in DXA bei der Inhaltsbreite des Bausteinwegs; <c>0</c> = ein gleicher Teil dessen, was
        /// die festen Breiten übrig lassen (der Rechenweg des Bausteinwegs). Der Vorlagenweg rechnet daraus
        /// Prozent (<see cref="Berichtstabelle.Prozente"/>).
        /// </summary>
        public int Breite { get; init; }

        /// <summary>Der Stand der Spalte (Stamm- und Standspalten); sonst <c>null</c>.</summary>
        public int? IdProjekt { get; init; }
    }

    /// <summary>
    /// Eine Zelle der <see cref="Berichtstabelle"/>: fertiger Text in der Sprache des Berichts, bei Zahlen dazu der
    /// Rohwert samt Format und Einheit (für den Excel-Renderer späterer Etappen), Rolle, Ausrichtung und die
    /// Direktformatierung des Bausteinwegs.
    /// </summary>
    public sealed class Tabellenzelle
    {
        /// <summary>Der Leerwert einer Zelle: der Gedankenstrich, nie 0 (Konzept 4.10).</summary>
        public const string STRICH = "—";

        /// <summary>Der fertige Text; nie <c>null</c>.</summary>
        public string Text { get; init; } = "";

        /// <summary>Die Zahl hinter dem Text; <c>null</c> bei Text und Leerwert.</summary>
        public double? Zahl { get; init; }

        /// <summary>Das .NET-Format der Zahl (<c>N0</c>, <c>N1</c> …); <c>null</c> = keines.</summary>
        public string Format { get; init; }

        /// <summary>Die Einheit der Zahl, wenn sie nicht im Kopf steht; <c>null</c> = keine.</summary>
        public string Einheit { get; init; }

        /// <summary>Die Rolle der Zelle.</summary>
        public Tabellenrolle Rolle { get; init; }

        /// <summary>Die Ausrichtung.</summary>
        public Tabellenausrichtung Ausrichtung { get; init; } = Tabellenausrichtung.Links;

        /// <summary>Fett (Kopf, Gruppen- und Summenzeilen der Direktformatierung).</summary>
        public bool Fett { get; init; }

        /// <summary>Die Hinterlegung der Direktformatierung.</summary>
        public Tabellenhinterlegung Hinterlegung { get; init; }

        /// <summary>Der Vorbehalt einer Zelle mit der Rolle Warnung (Satz unter der Tabelle); sonst <c>null</c>.</summary>
        public string Warnung { get; init; }

        /// <summary>Steht der Leerwert da?</summary>
        public bool IstLeer { get { return Text == STRICH; } }

        /// <inheritdoc/>
        public override string ToString() { return Text; }
    }

    /// <summary>Eine Zeile der <see cref="Berichtstabelle"/>.</summary>
    public sealed class Tabellenzeile
    {
        /// <summary>Legt die Zeile an.</summary>
        public Tabellenzeile(IEnumerable<Tabellenzelle> zellen, Tabellenrolle rolle = Tabellenrolle.Keine)
        {
            Zellen = (zellen ?? Enumerable.Empty<Tabellenzelle>()).ToList();
            Rolle = rolle;
        }

        /// <summary>Die Zellen, eine je Spalte.</summary>
        public IReadOnlyList<Tabellenzelle> Zellen { get; }

        /// <summary>Die Rolle der ganzen Zeile (Gruppe, Summe); die Zellen tragen sie mit.</summary>
        public Tabellenrolle Rolle { get; }
    }

    /// <summary>
    /// <b>Die Strukturtabelle des Berichts</b> (Konzept Berichtsvorlagen 5.4, 6.4 Nr. 2; Etappe BV-E5) — Kopf,
    /// Spalten, Zeilen und Zellen mit Wert, Format und Rolle, die Spaltenblöcke und das Merkmal
    /// <see cref="Listentauglich"/>. Plattformfrei und ohne Datenbank; gebaut von <see cref="Berichtstabellen"/>
    /// aus denselben Zeilenquellen wie die Bausteine und gelesen vom Bausteinweg
    /// (<see cref="WordTabellenschreiber.Direkt"/>), vom Vorlagenweg (<c>{{tabelle.…}}</c>) und später von einem
    /// Excel-Renderer. <b>Baustein und Platzhalter zeigen dieselbe Tabelle.</b>
    /// </summary>
    public sealed class Berichtstabelle
    {
        /// <summary>Die Blockgröße ohne Angabe: höchstens drei Varianten je Block (<see cref="WordKontext.MAX_VARIANTEN_JE_BLOCK"/>).</summary>
        public const int BLOCKGROESSE = WordKontext.MAX_VARIANTEN_JE_BLOCK;

        private readonly List<Tabellenspalte> _spalten = new List<Tabellenspalte>();
        private readonly List<Tabellenzeile> _zeilen = new List<Tabellenzeile>();
        private readonly List<string> _hinweise = new List<string>();

        /// <summary>Die Spalten.</summary>
        public IReadOnlyList<Tabellenspalte> Spalten { get { return _spalten; } }

        /// <summary>Die Kopfzeile; <c>null</c> = keine (Eigenschaftstabelle Beschriftung · Wert).</summary>
        public Tabellenzeile Kopf { get; private set; }

        /// <summary>Die Zeilen unter dem Kopf.</summary>
        public IReadOnlyList<Tabellenzeile> Zeilen { get { return _zeilen; } }

        /// <summary>Sätze, die unter die Tabelle gehören (Warnungen einzelner Zellen) — in der Sprache des Berichts.</summary>
        public IReadOnlyList<string> Hinweise { get { return _hinweise; } }

        /// <summary>Schmale Schrift (7 pt) für breite Zahlentabellen — sonst 9 pt.</summary>
        public bool Schmal { get; set; }

        /// <summary>Dürfen die Standspalten auf Blöcke verteilt werden? In der Paarsicht nicht.</summary>
        public bool Teilbar { get; set; } = true;

        /// <summary>
        /// Warum die Tabelle keine Zeile hat (Konzept 4.10: Leertext mit Grund), in der Sprache des Berichts;
        /// <c>null</c> = kein Grund bekannt.
        /// </summary>
        public string Leergrund { get; set; }

        /// <summary>Hat die Tabelle keine Zeile?</summary>
        public bool IstLeer { get { return _zeilen.Count == 0; } }

        /// <summary>
        /// <b>Listentauglich</b> (Konzept 5.4, 7.3): feste Spaltenzahl (keine Spalte je Stand), eindeutige
        /// Textköpfe, keine Gruppenzeilen und damit keine verbundenen Zellen — nur solche Tabellen werden in
        /// Excel eine Excel-Tabelle; alle anderen ein erzeugter Bereich.
        /// </summary>
        public bool Listentauglich
        {
            get
            {
                if (Kopf == null || _spalten.Any(s => s.Art == Spaltenart.Stamm || s.Art == Spaltenart.Stand)) return false;
                if (_zeilen.Any(z => (z.Rolle & Tabellenrolle.Gruppe) != 0)) return false;
                List<string> koepfe = Kopf.Zellen.Select(z => (z.Text ?? "").Trim()).ToList();
                return koepfe.All(k => k.Length > 0) && koepfe.Distinct(StringComparer.OrdinalIgnoreCase).Count() == koepfe.Count;
            }
        }

        /// <summary>Hängt eine Spalte an.</summary>
        public Berichtstabelle Spalte(Spaltenart art, int breite, int? idProjekt = null)
        {
            _spalten.Add(new Tabellenspalte { Art = art, Breite = breite, IdProjekt = idProjekt });
            return this;
        }

        /// <summary>Hängt feste Spalten mit diesen Breiten an.</summary>
        public Berichtstabelle Feste(params int[] breiten)
        {
            foreach (int b in breiten) Spalte(Spaltenart.Fest, b);
            return this;
        }

        /// <summary>Setzt die Kopfzeile.</summary>
        public Berichtstabelle MitKopf(IEnumerable<Tabellenzelle> zellen)
        {
            Kopf = new Tabellenzeile(zellen);
            return this;
        }

        /// <summary>Hängt eine Zeile an.</summary>
        public Berichtstabelle Zeile(IEnumerable<Tabellenzelle> zellen, Tabellenrolle rolle = Tabellenrolle.Keine)
        {
            _zeilen.Add(new Tabellenzeile(zellen, rolle));
            return this;
        }

        /// <summary>Hängt einen Satz unter die Tabelle an.</summary>
        public Berichtstabelle Hinweis(string text)
        {
            if (!string.IsNullOrWhiteSpace(text)) _hinweise.Add(text);
            return this;
        }

        /// <summary>
        /// <b>Die Spaltenblöcke</b> (Konzept 5.4; <c>|block n</c>, 4.8): je Block die Spaltennummern — feste,
        /// Stamm- und Nachspalten in jedem Block, die Standspalten zu höchstens <paramref name="groesse"/> je
        /// Block. Ohne Standspalte genau ein Block (nur der Stamm), ebenso bei <see cref="Teilbar"/> falsch.
        /// <paramref name="groesse"/> kleiner 1 heißt <see cref="BLOCKGROESSE"/>.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<int>> Bloecke(int groesse = BLOCKGROESSE)
        {
            if (groesse < 1) groesse = BLOCKGROESSE;
            List<int> staende = Enumerable.Range(0, _spalten.Count).Where(i => _spalten[i].Art == Spaltenart.Stand).ToList();
            var bloecke = new List<IReadOnlyList<int>>();
            if (!Teilbar || staende.Count <= groesse)
            {
                bloecke.Add(Enumerable.Range(0, _spalten.Count).ToList());
                return bloecke;
            }
            for (int start = 0; start < staende.Count; start += groesse)
            {
                var drin = new HashSet<int>(staende.Skip(start).Take(groesse));
                bloecke.Add(Enumerable.Range(0, _spalten.Count)
                                      .Where(i => _spalten[i].Art != Spaltenart.Stand || drin.Contains(i))
                                      .ToList());
            }
            return bloecke;
        }

        /// <summary>
        /// Die Breiten der Spalten <paramref name="spalten"/> in DXA bei der Inhaltsbreite
        /// <paramref name="inhaltsbreite"/> — der Rechenweg des Bausteinwegs: feste Breiten bleiben, die übrigen
        /// Spalten teilen sich den Rest zu gleichen, ganzzahligen Teilen.
        /// </summary>
        public int[] Breiten(IReadOnlyList<int> spalten, int inhaltsbreite)
        {
            int fest = spalten.Sum(i => Math.Max(0, _spalten[i].Breite));
            int rest = spalten.Count(i => _spalten[i].Breite <= 0);
            int teil = rest > 0 ? (inhaltsbreite - fest) / rest : 0;
            return spalten.Select(i => _spalten[i].Breite > 0 ? _spalten[i].Breite : teil).ToArray();
        }

        /// <summary>
        /// Die Breiten in Fünfzigstel Prozent (<c>w:tblW w:type="pct"</c>, 5000 = 100 %) — aus den
        /// Breiten bei der Standardinhaltsbreite <see cref="WordBerichtGenerator.INHALT_B"/>; die Summe ist
        /// genau 5000, der Rundungsrest geht an die letzte Spalte.
        /// </summary>
        public int[] Prozente(IReadOnlyList<int> spalten)
        {
            int[] dxa = Breiten(spalten, WordBerichtGenerator.INHALT_B);
            double summe = Math.Max(1, dxa.Sum());
            int[] pct = dxa.Select(b => (int)Math.Round(Math.Max(0, b) * 5000.0 / summe, MidpointRounding.AwayFromZero)).ToArray();
            if (pct.Length > 0) pct[pct.Length - 1] += 5000 - pct.Sum();
            return pct;
        }

        /// <summary>Alle Zeilen als Text, Zellen mit „ | “ getrennt — für Proben und Laufmeldungen.</summary>
        public override string ToString()
        {
            var zeilen = new List<string>();
            if (Kopf != null) zeilen.Add(string.Join(" | ", Kopf.Zellen.Select(z => z.Text)));
            zeilen.AddRange(_zeilen.Select(z => string.Join(" | ", z.Zellen.Select(c => c.Text))));
            return string.Join("\n", zeilen);
        }
    }

    /// <summary>
    /// Die Zahlformen der Tabellen — dieselben Regeln wie <see cref="WordKontext.F"/>, <see cref="WordKontext.FW"/>,
    /// <see cref="WordKontext.Delta"/> und <see cref="WordKontext.DeltaProzent"/>, nur mit übergebener Kultur, damit
    /// Baustein und Platzhalter dieselbe Zeichenkette tragen.
    /// </summary>
    public static class Tabellenformat
    {
        /// <summary>Zahl mit <paramref name="dez"/> Stellen.</summary>
        public static string F(double v, int dez, CultureInfo kultur) { return v.ToString("N" + dez, kultur); }

        /// <summary>Kennzahlwert formatiert; <c>null</c> → „—“ (nie 0).</summary>
        public static string FW(double? v, string format, CultureInfo kultur)
        {
            return v.HasValue ? v.Value.ToString(format, kultur) : Tabellenzelle.STRICH;
        }

        /// <summary>Δ Variante − Stamm mit Vorzeichen.</summary>
        public static string Delta(double? stamm, double? variante, string format, CultureInfo kultur)
        {
            if (!stamm.HasValue || !variante.HasValue) return Tabellenzelle.STRICH;
            double d = variante.Value - stamm.Value;
            string betrag = Math.Abs(d).ToString(format, kultur);
            if (d > 0) return "+" + betrag;
            if (d < 0) return "−" + betrag;
            return "±" + 0.0.ToString(format, kultur);
        }

        /// <summary>Δ in Prozent zum Stammwert („+12,3 %“); „—“, wenn nicht berechenbar.</summary>
        public static string DeltaProzent(double? stamm, double? variante, CultureInfo kultur)
        {
            if (!stamm.HasValue || !variante.HasValue || Math.Abs(stamm.Value) < 1e-9) return Tabellenzelle.STRICH;
            double p = (variante.Value - stamm.Value) / Math.Abs(stamm.Value) * 100.0;
            string betrag = Math.Abs(p).ToString("N1", kultur) + " %";
            if (p > 0.05) return "+" + betrag;
            if (p < -0.05) return "−" + betrag;
            return "±0,0 %";
        }

        /// <summary>Der Prozentwert hinter <see cref="DeltaProzent"/>; <c>null</c>, wenn nicht berechenbar.</summary>
        public static double? DeltaProzentWert(double? stamm, double? variante)
        {
            if (!stamm.HasValue || !variante.HasValue || Math.Abs(stamm.Value) < 1e-9) return null;
            return (variante.Value - stamm.Value) / Math.Abs(stamm.Value) * 100.0;
        }
    }
}
