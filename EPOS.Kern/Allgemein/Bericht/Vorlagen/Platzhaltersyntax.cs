using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DIE SYNTAX DER BERICHTSVORLAGEN (Konzept Berichtsvorlagen mit Platzhaltern,
    // Abschnitte 4.2, 4.5 und 4.8; Etappe BV-E1).
    //
    // Hier wird nur ERKANNT, nicht gefüllt: Was zwischen doppelten geschweiften
    // Klammern steht, wird zu einem Platzhalter mit Art, normiertem Schlüssel und
    // typisierten Formatangaben. Ob es den Schlüssel gibt, weiß der
    // Vorlagenfeldkatalog; ob er an der Stelle passt, entscheidet der Prüfer. Blöcke
    // ({{#je …}}, {{#wenn …}}) werden erkannt, damit der Prüfer sie benennen kann —
    // gefüllt werden sie erst ab BV-E4.
    // ---------------------------------------------------------------------------

    /// <summary>Art einer Marke in doppelten geschweiften Klammern (Konzept 4.2).</summary>
    public enum Platzhalterart
    {
        /// <summary>Ein Einzel- oder Absatzplatzhalter: <c>{{schluessel}}</c>, <c>{{schluessel|angabe}}</c>.</summary>
        Feld,

        /// <summary>Anfang eines Wiederholblocks: <c>{{#je stand}}</c>, <c>{{#je variante|block 3}}</c>.</summary>
        BlockAnfang,

        /// <summary>Ende eines Wiederholblocks: <c>{{/je}}</c>.</summary>
        BlockEnde,

        /// <summary>Anfang einer Bedingung: <c>{{#wenn schalter}}</c> oder <c>{{#wenn nicht schalter}}</c>.</summary>
        WennAnfang,

        /// <summary>Ende einer Bedingung: <c>{{/wenn}}</c>.</summary>
        WennEnde,

        /// <summary>
        /// Doppelte Klammern, deren Inhalt keine der Formen ist: leer (<c>{{}}</c>, <c>{{|stellen 1}}</c>)
        /// oder ein Blockwort, das es nicht gibt (<c>{{#gruppe x}}</c>, <c>{{/ende}}</c>). Der Prüfer meldet sie.
        /// </summary>
        Unbekannt,
    }

    /// <summary>Die Formatangaben nach dem senkrechten Strich (Konzept 4.8).</summary>
    public enum Formatangabeart
    {
        /// <summary>Keine der bekannten Angaben — der Prüfer meldet sie; die Engine nimmt das Katalogformat.</summary>
        Unbekannt,

        /// <summary><c>|stellen n</c>: Dezimalstellen einer Zahl (0 bis <see cref="Platzhaltersyntax.STELLEN_MAX"/>).</summary>
        Stellen,

        /// <summary><c>|ohne einheit</c>: die Einheit einer Zahl weglassen.</summary>
        OhneEinheit,

        /// <summary><c>|mit einheit</c>: die Einheit einer Zahl anhängen (die Vorgabe).</summary>
        MitEinheit,

        /// <summary><c>|datum</c>: kurzes Datum nach Kultur.</summary>
        Datum,

        /// <summary><c>|datum lang</c>: langes Datum nach Kultur.</summary>
        DatumLang,

        /// <summary><c>|datum mit zeit</c>: kurzes Datum mit Uhrzeit nach Kultur.</summary>
        DatumMitZeit,

        /// <summary><c>|leer statt strich</c>: leerer Text statt „—“, wenn kein Wert vorliegt.</summary>
        LeerStattStrich,

        /// <summary><c>|mit grund</c>: Leerwert mit Grund, etwa „— (Lauf fehlgeschlagen)“.</summary>
        MitGrund,

        /// <summary><c>|block n</c>: Gruppen zu höchstens n Varianten (Tabelle, <c>{{#je variante}}</c>).</summary>
        Block,

        /// <summary><c>|ohne titel</c>: ein Kapitel ohne eigene Überschrift.</summary>
        OhneTitel,

        /// <summary><c>|ebene n</c>: die Überschriften eines Kapitels beginnen auf Ebene n.</summary>
        Ebene,
    }

    /// <summary>
    /// Eine Formatangabe hinter dem senkrechten Strich, typisiert (Konzept 4.8). Eine Angabe,
    /// die keine der bekannten ist oder deren Zahl außerhalb des Bereichs liegt, trägt die Art
    /// <see cref="Formatangabeart.Unbekannt"/> — sie wird nicht verworfen, damit der Prüfer sie
    /// mit ihrem Wortlaut nennen kann.
    /// </summary>
    public sealed class Formatangabe
    {
        internal Formatangabe(Formatangabeart art, int? zahl, string roh, string normalform)
        {
            Art = art;
            Zahl = zahl;
            Roh = roh ?? "";
            Normalform = normalform ?? "";
        }

        /// <summary>Die erkannte Angabe.</summary>
        public Formatangabeart Art { get; }

        /// <summary>Die Zahl von <c>stellen n</c>, <c>block n</c> und <c>ebene n</c>; sonst <c>null</c>.</summary>
        public int? Zahl { get; }

        /// <summary>Die Angabe, wie sie in der Vorlage steht (ohne den Strich).</summary>
        public string Roh { get; }

        /// <summary>Die kanonische Schreibweise, etwa <c>stellen 1</c> oder <c>ohne einheit</c>; bei
        /// einer unbekannten Angabe ihr normierter Wortlaut.</summary>
        public string Normalform { get; }

        /// <summary>Ist die Angabe eine der bekannten?</summary>
        public bool IstBekannt { get { return Art != Formatangabeart.Unbekannt; } }

        /// <summary>
        /// Gilt die Angabe für ein Feld dieser Art (Tabelle in Konzept 4.8)? Eine unpassende
        /// Angabe ist ein Prüferfehler; die Engine übergeht sie und nimmt das Katalogformat.
        /// </summary>
        public bool PasstZu(Vorlagenfeldart art)
        {
            switch (Art)
            {
                case Formatangabeart.Stellen:
                case Formatangabeart.OhneEinheit:
                case Formatangabeart.MitEinheit:
                case Formatangabeart.MitGrund:
                    return art == Vorlagenfeldart.Zahl;
                case Formatangabeart.Datum:
                case Formatangabeart.DatumLang:
                case Formatangabeart.DatumMitZeit:
                    return art == Vorlagenfeldart.Datum;
                case Formatangabeart.LeerStattStrich:
                    return art == Vorlagenfeldart.Zahl || art == Vorlagenfeldart.Text || art == Vorlagenfeldart.Datum;
                case Formatangabeart.Block:
                    return art == Vorlagenfeldart.Tabelle;
                case Formatangabeart.OhneTitel:
                case Formatangabeart.Ebene:
                    return art == Vorlagenfeldart.Kapitel;
                default:
                    return false;
            }
        }

        /// <summary>Gilt die Angabe für einen Wiederholblock (<c>{{#je variante|block 3}}</c>)?</summary>
        public bool PasstZuBlock { get { return Art == Formatangabeart.Block; } }

        /// <inheritdoc/>
        public override string ToString() { return Normalform; }
    }

    /// <summary>
    /// Eine erkannte Marke in doppelten geschweiften Klammern — Einzelplatzhalter, Blockanfang
    /// oder -ende. Entsteht über <see cref="Platzhaltersyntax.Finde"/> (mit Fundort im Text) oder
    /// <see cref="Platzhaltersyntax.Lies"/> (ein einzelner Platzhalter, etwa aus dem Tag eines
    /// Inhaltssteuerelements).
    /// </summary>
    public sealed class Platzhalter
    {
        internal Platzhalter(string roh, Platzhalterart art, string schluessel, bool verneint,
                             IReadOnlyList<Formatangabe> angaben, int position, string unbekannterInhalt)
        {
            Roh = roh ?? "";
            Art = art;
            Schluessel = schluessel ?? "";
            Verneint = verneint;
            Angaben = angaben ?? Array.Empty<Formatangabe>();
            Position = position;
            Normalform = BildeNormalform(art, Schluessel, verneint, Angaben, unbekannterInhalt);
        }

        /// <summary>Die Marke, wie sie im Text steht — bei <see cref="Platzhaltersyntax.Finde"/> samt
        /// Klammern, bei <see cref="Platzhaltersyntax.Lies"/> genau die übergebene Zeichenkette.</summary>
        public string Roh { get; }

        /// <summary>Feld, Blockanfang, Blockende, Bedingung oder unbekannt.</summary>
        public Platzhalterart Art { get; }

        /// <summary>
        /// Der normierte Schlüssel (klein, Umlaute gefaltet, ohne Leerraum): beim Feld der
        /// Katalogschlüssel, bei <c>{{#je …}}</c> der Wiederholbereich (<c>stand</c>, <c>variante</c>,
        /// <c>gebaeude</c>), bei <c>{{#wenn …}}</c> der Schalter; bei einem Blockende leer, es sei
        /// denn, hinter dem Blockwort steht noch etwas (<c>{{/je stand}}</c>).
        /// </summary>
        public string Schluessel { get; }

        /// <summary><c>true</c> bei <c>{{#wenn nicht …}}</c>.</summary>
        public bool Verneint { get; }

        /// <summary>Die Formatangaben in der Reihenfolge der Vorlage; leere Angaben (<c>{{x|}}</c>) entfallen.</summary>
        public IReadOnlyList<Formatangabe> Angaben { get; }

        /// <summary>
        /// Die kanonische Schreibweise für Prüfermeldungen, etwa <c>{{projekt.kunde|stellen 1}}</c>,
        /// <c>{{#wenn nicht hat.kaelte}}</c> oder <c>{{/je}}</c> — immer mit Klammern.
        /// </summary>
        public string Normalform { get; }

        /// <summary>Anfang der Marke im durchsuchten Text (Index der ersten Klammer); <c>-1</c> bei
        /// <see cref="Platzhaltersyntax.Lies"/>.</summary>
        public int Position { get; }

        /// <summary>Länge der Marke im Text (<see cref="Roh"/>).</summary>
        public int Laenge { get { return Roh.Length; } }

        /// <summary>Steht die Marke schon in ihrer Normalform?</summary>
        public bool IstNormalform { get { return string.Equals(Roh, Normalform, StringComparison.Ordinal); } }

        /// <summary>Ist die Marke ein Blockanfang oder -ende (Wiederholung oder Bedingung)?</summary>
        public bool IstBlockmarke
        {
            get
            {
                return Art == Platzhalterart.BlockAnfang || Art == Platzhalterart.BlockEnde ||
                       Art == Platzhalterart.WennAnfang || Art == Platzhalterart.WennEnde;
            }
        }

        /// <summary>Genügt <see cref="Schluessel"/> dem Schlüsselmuster (Konzept 4.5)? Ob es ihn im
        /// Katalog gibt, sagt der <see cref="Vorlagenfeldkatalog"/>.</summary>
        public bool SchluesselGueltig { get { return Platzhaltersyntax.IstGueltigerSchluessel(Schluessel); } }

        /// <summary>Trägt die Marke eine Angabe, die keine der bekannten ist?</summary>
        public bool HatUnbekannteAngabe { get { return Angaben.Any(a => !a.IstBekannt); } }

        /// <inheritdoc/>
        public override string ToString() { return Normalform; }

        private static string BildeNormalform(Platzhalterart art, string schluessel, bool verneint,
                                              IReadOnlyList<Formatangabe> angaben, string unbekannterInhalt)
        {
            string anhang = string.Concat(angaben.Where(a => a.Normalform.Length > 0).Select(a => "|" + a.Normalform));
            switch (art)
            {
                case Platzhalterart.Feld:
                    return "{{" + schluessel + anhang + "}}";
                case Platzhalterart.BlockAnfang:
                    return "{{#" + Platzhaltersyntax.BLOCK_JE + (schluessel.Length > 0 ? " " + schluessel : "") + anhang + "}}";
                case Platzhalterart.WennAnfang:
                    return "{{#" + Platzhaltersyntax.BLOCK_WENN + (verneint ? " " + Platzhaltersyntax.WORT_NICHT : "") +
                           (schluessel.Length > 0 ? " " + schluessel : "") + anhang + "}}";
                case Platzhalterart.BlockEnde:
                    return "{{/" + Platzhaltersyntax.BLOCK_JE + "}}";
                case Platzhalterart.WennEnde:
                    return "{{/" + Platzhaltersyntax.BLOCK_WENN + "}}";
                default:
                    return "{{" + (unbekannterInhalt ?? "") + "}}";
            }
        }
    }

    /// <summary>
    /// <b>Erkennen der Platzhalter einer Berichtsvorlage</b> (Konzept 4.2, 4.5, 4.8).
    ///
    /// <para><b>Normierung.</b> Groß- und Kleinschreibung, Leerraum in den Klammern und
    /// Umlaute spielen keine Rolle: Umlaute werden dokumentiert gefaltet (ä → ae, ö → oe,
    /// ü → ue, ß → ss), gleich für Schlüssel, Block- und Formatwörter. Als Leerraum gilt auch,
    /// was Word gern einstreut — geschütztes Leerzeichen, schmales geschütztes Leerzeichen —,
    /// und unsichtbare Zeichen (Nullbreite, bedingter Trennstrich) fallen weg. Im Schlüssel
    /// verschwindet aller Leerraum, in Block- und Formatwörtern trennt er die Wörter
    /// (<c>{{#je stand}}</c>, <c>|stellen 1</c>); eine Formatangabe wird auch ohne ihn erkannt
    /// (<c>|ohneeinheit</c>). Weicht die Schreibweise ab, nennt der Prüfer die
    /// <see cref="Platzhalter.Normalform"/>.</para>
    ///
    /// <para><b>Grenzen.</b> Ein Platzhalter reicht nicht über einen Zeilenumbruch und enthält
    /// keine geschweifte Klammer; zerlegte Word-Runs setzt die Engine vorher im Speicher
    /// zusammen. Eine öffnende Doppelklammer ohne Gegenstück meldet
    /// <see cref="OffeneKlammern"/>.</para>
    /// </summary>
    public static class Platzhaltersyntax
    {
        /// <summary>Muster der Katalogschlüssel (Konzept 4.5): ASCII, klein, punktgegliedert,
        /// Unterstrich nur einzeln im Glied.</summary>
        public const string SCHLUESSELMUSTER = @"^[a-z][a-z0-9]*(_[a-z0-9]+)*(\.[a-z0-9]+(_[a-z0-9]+)*)*$";

        /// <summary>Das Blockwort der Wiederholung (<c>{{#je …}}</c>, <c>{{/je}}</c>).</summary>
        public const string BLOCK_JE = "je";

        /// <summary>Das Blockwort der Bedingung (<c>{{#wenn …}}</c>, <c>{{/wenn}}</c>).</summary>
        public const string BLOCK_WENN = "wenn";

        /// <summary>Die Verneinung der Bedingung (<c>{{#wenn nicht …}}</c>).</summary>
        public const string WORT_NICHT = "nicht";

        /// <summary>Höchste Zahl von <c>|stellen n</c>.</summary>
        public const int STELLEN_MAX = 15;

        /// <summary>Höchste Zahl von <c>|block n</c> (die Engine begrenzt zusätzlich nach Satzspiegel).</summary>
        public const int BLOCK_MAX = 99;

        /// <summary>Höchste Zahl von <c>|ebene n</c> (Word kennt neun Überschriftenebenen).</summary>
        public const int EBENE_MAX = 9;

        /// <summary>Die Wiederholbereiche von <c>{{#je …}}</c> (Konzept 4.2); gefüllt ab BV-E4.</summary>
        public static readonly IReadOnlyList<string> JeBereiche = new[] { "stand", "variante", "gebaeude" };

        /// <summary>Die bekannten Formatangaben in Normalform, <c>n</c> für die Zahl — die Liste für
        /// Vorschläge des Prüfers.</summary>
        public static readonly IReadOnlyList<string> AngabenMuster = new[]
        {
            "stellen n", "ohne einheit", "mit einheit", "datum", "datum lang", "datum mit zeit",
            "leer statt strich", "mit grund", "block n", "ohne titel", "ebene n",
        };

        /// <summary>Ein Platzhalter: zwei öffnende Klammern, Inhalt ohne Klammer und Zeilenumbruch,
        /// zwei schließende Klammern.</summary>
        private static readonly Regex Marke =
            new Regex(@"\{\{([^{}\r\n]*)\}\}", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex Schluesselmuster =
            new Regex(SCHLUESSELMUSTER, RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>Formatangabe mit Zahl in kompakter Schreibweise (ohne Leerraum).</summary>
        private static readonly Regex AngabeMitZahl =
            new Regex(@"^(stellen|block|ebene)([0-9]{1,3})$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // ------------------------------------------------------------------ Suchen

        /// <summary>
        /// Alle Marken des Texts in Textreihenfolge, jede mit <see cref="Platzhalter.Position"/>
        /// im übergebenen Text. <c>null</c> oder leer liefert nichts.
        /// </summary>
        public static IEnumerable<Platzhalter> Finde(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            foreach (Match m in Marke.Matches(text))
                yield return Zerlege(m.Value, m.Groups[1].Value, m.Index);
        }

        /// <summary>
        /// Liest einen einzelnen Platzhalter — mit Klammern (<c>{{projekt.kunde|stellen 1}}</c>) oder
        /// ohne (<c>projekt.kunde</c>, so steht er im Tag eines Inhaltssteuerelements oder im
        /// Alternativtext eines Bildes). Die Position ist <c>-1</c>. <c>null</c> oder leer liefert
        /// einen Platzhalter der Art <see cref="Platzhalterart.Unbekannt"/>.
        /// </summary>
        public static Platzhalter Lies(string roh)
        {
            string inneres = (roh ?? "").Trim();
            if (inneres.StartsWith("{{", StringComparison.Ordinal) && inneres.EndsWith("}}", StringComparison.Ordinal)
                && inneres.Length >= 4)
                inneres = inneres.Substring(2, inneres.Length - 4);
            return Zerlege(roh ?? "", inneres, -1);
        }

        /// <summary>Enthält der Text mindestens einen Platzhalter?</summary>
        public static bool EnthaeltPlatzhalter(string text)
        {
            return !string.IsNullOrEmpty(text) && Marke.IsMatch(text);
        }

        /// <summary>
        /// Die Stellen, an denen eine öffnende Doppelklammer steht, die zu keinem Platzhalter
        /// gehört — etwa <c>{{projekt.kunde}</c> oder ein Platzhalter über einen Zeilenumbruch
        /// hinweg. Der Prüfer meldet sie; die Engine lässt den Text stehen.
        /// </summary>
        public static IReadOnlyList<int> OffeneKlammern(string text)
        {
            var offen = new List<int>();
            if (string.IsNullOrEmpty(text)) return offen;

            List<Match> treffer = Marke.Matches(text).Cast<Match>().ToList();
            int i = text.IndexOf("{{", StringComparison.Ordinal);
            while (i >= 0)
            {
                Match drin = treffer.FirstOrDefault(m => i >= m.Index && i < m.Index + m.Length);
                if (drin != null)
                {
                    i = drin.Index + drin.Length;
                }
                else
                {
                    offen.Add(i);
                    i += 2;
                }
                i = i < text.Length ? text.IndexOf("{{", i, StringComparison.Ordinal) : -1;
            }
            return offen;
        }

        // ------------------------------------------------------------------ Normieren

        /// <summary>
        /// Normiert einen Text der Platzhaltersyntax: Unicode-Normalform C, klein (invariant),
        /// Umlaute gefaltet (ä → ae, ö → oe, ü → ue, ß → ss), unsichtbare Zeichen entfernt, jeder
        /// Leerraum zu einem Leerzeichen zusammengezogen, Ränder gekürzt.
        /// </summary>
        public static string Normiere(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string s = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
            var sb = new StringBuilder(s.Length + 8);
            bool leerraum = false;
            foreach (char c in s)
            {
                if (IstUnsichtbar(c)) continue;
                if (char.IsWhiteSpace(c)) { leerraum = true; continue; }
                if (leerraum && sb.Length > 0) sb.Append(' ');
                leerraum = false;
                switch (c)
                {
                    case 'ä': sb.Append("ae"); break;
                    case 'ö': sb.Append("oe"); break;
                    case 'ü': sb.Append("ue"); break;
                    case 'ß': sb.Append("ss"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Normiert einen Schlüssel: wie <see cref="Normiere"/>, dazu ohne jeden Leerraum.</summary>
        public static string NormiereSchluessel(string schluessel)
        {
            return Normiere(schluessel).Replace(" ", "");
        }

        /// <summary>Genügt der (schon normierte) Schlüssel dem <see cref="SCHLUESSELMUSTER"/>?</summary>
        public static bool IstGueltigerSchluessel(string schluessel)
        {
            return !string.IsNullOrEmpty(schluessel) && Schluesselmuster.IsMatch(schluessel);
        }

        /// <summary>
        /// Liest eine einzelne Formatangabe (ohne den Strich). Leerraum und Groß-/Kleinschreibung
        /// sind gleichgültig; eine unbekannte Angabe oder eine Zahl außerhalb des Bereichs
        /// (<see cref="STELLEN_MAX"/>, <see cref="BLOCK_MAX"/>, <see cref="EBENE_MAX"/>) ergibt
        /// <see cref="Formatangabeart.Unbekannt"/>.
        /// </summary>
        public static Formatangabe LiesAngabe(string roh)
        {
            string normiert = Normiere(roh);
            string kompakt = normiert.Replace(" ", "");
            switch (kompakt)
            {
                case "ohneeinheit": return new Formatangabe(Formatangabeart.OhneEinheit, null, roh, "ohne einheit");
                case "miteinheit": return new Formatangabe(Formatangabeart.MitEinheit, null, roh, "mit einheit");
                case "datum": return new Formatangabe(Formatangabeart.Datum, null, roh, "datum");
                case "datumlang": return new Formatangabe(Formatangabeart.DatumLang, null, roh, "datum lang");
                case "datummitzeit": return new Formatangabe(Formatangabeart.DatumMitZeit, null, roh, "datum mit zeit");
                case "leerstattstrich": return new Formatangabe(Formatangabeart.LeerStattStrich, null, roh, "leer statt strich");
                case "mitgrund": return new Formatangabe(Formatangabeart.MitGrund, null, roh, "mit grund");
                case "ohnetitel": return new Formatangabe(Formatangabeart.OhneTitel, null, roh, "ohne titel");
            }

            Match m = AngabeMitZahl.Match(kompakt);
            if (m.Success)
            {
                string wort = m.Groups[1].Value;
                int n = int.Parse(m.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture);
                switch (wort)
                {
                    case "stellen":
                        if (n <= STELLEN_MAX) return new Formatangabe(Formatangabeart.Stellen, n, roh, "stellen " + n.ToString(CultureInfo.InvariantCulture));
                        break;
                    case "block":
                        if (n >= 1 && n <= BLOCK_MAX) return new Formatangabe(Formatangabeart.Block, n, roh, "block " + n.ToString(CultureInfo.InvariantCulture));
                        break;
                    case "ebene":
                        if (n >= 1 && n <= EBENE_MAX) return new Formatangabe(Formatangabeart.Ebene, n, roh, "ebene " + n.ToString(CultureInfo.InvariantCulture));
                        break;
                }
            }
            return new Formatangabe(Formatangabeart.Unbekannt, null, roh, normiert);
        }

        // ------------------------------------------------------------------ Zerlegen

        /// <summary>Zerlegt den Inhalt zwischen den Klammern in Art, Schlüssel und Angaben.</summary>
        private static Platzhalter Zerlege(string roh, string inneres, int position)
        {
            string[] teile = (inneres ?? "").Split('|');
            string kopf = Normiere(teile[0]);
            List<Formatangabe> angaben = teile.Skip(1)
                .Where(t => Normiere(t).Length > 0)
                .Select(LiesAngabe)
                .ToList();

            if (kopf.Length == 0)
                return new Platzhalter(roh, Platzhalterart.Unbekannt, "", false, angaben, position, Normiere(inneres));

            if (kopf[0] == '#')
            {
                string wort = ErstesWort(kopf.Substring(1).Trim(), out string rest);
                if (wort == BLOCK_JE)
                    return new Platzhalter(roh, Platzhalterart.BlockAnfang, rest.Replace(" ", ""), false, angaben, position, null);
                if (wort == BLOCK_WENN)
                {
                    string zweites = ErstesWort(rest, out string nachNicht);
                    if (zweites == WORT_NICHT && nachNicht.Length > 0)
                        return new Platzhalter(roh, Platzhalterart.WennAnfang, nachNicht.Replace(" ", ""), true, angaben, position, null);
                    return new Platzhalter(roh, Platzhalterart.WennAnfang, rest.Replace(" ", ""), false, angaben, position, null);
                }
                return new Platzhalter(roh, Platzhalterart.Unbekannt, "", false, angaben, position, Normiere(inneres));
            }

            if (kopf[0] == '/')
            {
                string wort = ErstesWort(kopf.Substring(1).Trim(), out string rest);
                if (wort == BLOCK_JE)
                    return new Platzhalter(roh, Platzhalterart.BlockEnde, rest.Replace(" ", ""), false, angaben, position, null);
                if (wort == BLOCK_WENN)
                    return new Platzhalter(roh, Platzhalterart.WennEnde, rest.Replace(" ", ""), false, angaben, position, null);
                return new Platzhalter(roh, Platzhalterart.Unbekannt, "", false, angaben, position, Normiere(inneres));
            }

            return new Platzhalter(roh, Platzhalterart.Feld, kopf.Replace(" ", ""), false, angaben, position, null);
        }

        /// <summary>Das erste Wort eines normierten Texts; <paramref name="rest"/> ist der Rest ohne
        /// den trennenden Leerraum.</summary>
        private static string ErstesWort(string normiert, out string rest)
        {
            int i = normiert.IndexOf(' ');
            if (i < 0) { rest = ""; return normiert; }
            rest = normiert.Substring(i + 1).Trim();
            return normiert.Substring(0, i);
        }

        /// <summary>Zeichen, die Word und Zwischenablage unsichtbar einstreuen: Nullbreite
        /// (Leerzeichen, Nichtverbinder, Verbinder, Wortverbinder), Bytereihenfolgemarke,
        /// bedingter Trennstrich.</summary>
        private static bool IstUnsichtbar(char c)
        {
            return c == (char)0x200B || c == (char)0x200C || c == (char)0x200D || c == (char)0x2060 || c == (char)0xFEFF || c == (char)0x00AD;
        }
    }
}
