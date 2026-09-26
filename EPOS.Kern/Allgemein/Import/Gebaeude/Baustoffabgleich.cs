using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Stufe, auf der ein Materialname der Datei einen Baustoff trifft — die Kette N1…N7 des
    /// Mehrzonenkonzepts 3.5 und 6.3. N1 und N2 bereiten den Namen nur auf und treffen nichts; die
    /// Zahlen der Stufen sind die Nummern der Kette.
    /// </summary>
    internal enum Abgleichstufe
    {
        /// <summary>Kein Treffer (auch: mehrdeutig).</summary>
        Keine = 0,
        /// <summary>N3 — genauer Treffer gegen den normalisierten <c>Bezeichner</c> des Katalogs.</summary>
        Genau = 3,
        /// <summary>N4 — Synonym der Auslieferung, zweisprachig (auch als Wortanfang).</summary>
        Synonym = 4,
        /// <summary>N5 — Teilwort mit eindeutigem Treffer.</summary>
        Teilwort = 5,
        /// <summary>N6 — Sonderfall ohne Stoff (Luftschicht; Schraffur oder leer).</summary>
        Sonderfall = 6,
        /// <summary>N7 — gemerkte Zuordnung des Anwenders (je Projekt).</summary>
        Anwender = 7,
    }

    /// <summary>Die Sonderfälle der Stufe N6.</summary>
    internal enum Abgleichsonderfall
    {
        /// <summary>Kein Sonderfall.</summary>
        Keiner = 0,
        /// <summary>Eine ruhende Luftschicht — Widerstand nach DIN EN ISO 6946 Tabelle 8, keine Masse.</summary>
        Luftschicht = 1,
        /// <summary>Kein Stoff (Schraffur, leer) — die Schicht wird verworfen und gemeldet.</summary>
        Verwerfen = 2,
    }

    /// <summary>Ein Synonym der Auslieferung (<c>Tab_Baustoffsynonym_STAMM</c>): Materialname → Katalogbaustoff.</summary>
    /// <param name="Materialname">Der Name, wie ein Autorensystem ihn schreibt (normalisiert abgelegt; beim Vergleich erneut normalisiert).</param>
    /// <param name="Sprache">Sprache des Namens (<c>de</c>, <c>en</c>) — Auskunft, kein Schlüssel.</param>
    /// <param name="IdBaustoff">Der Baustoff des Katalogs (<c>Tab_Baustoff_STAMM.ID</c>).</param>
    /// <param name="Quelle">Woher der Name stammt.</param>
    internal sealed record BaustoffSynonym(string Materialname, string Sprache, int IdBaustoff, string Quelle);

    /// <summary>Eine gemerkte Zuordnung des Anwenders (N7, <c>Tab_Baustoffzuordnung</c>) — je Projekt.</summary>
    /// <param name="Materialname">Der normalisierte Materialname der Datei (Kern nach N1/N2).</param>
    /// <param name="IdBaustoff">Der Baustoff des Katalogs (<c>Tab_Baustoff_STAMM.ID</c>).</param>
    /// <param name="Zeitpunkt">Wann gemerkt (ISO 8601); <c>null</c> = unbekannt.</param>
    internal sealed record BaustoffNamenzuordnung(string Materialname, int IdBaustoff, string Zeitpunkt = null);

    /// <summary>
    /// <b>Die Lesenaht des Namensabgleichs</b>: der Katalog, die Synonyme und die gemerkten
    /// Zuordnungen eines Projekts. Die Datenbankseite ist <c>BaustoffabgleichCtrl</c>; ohne Datenbank
    /// trägt <see cref="BaustoffabgleichDaten"/> dieselben Listen (Tests, Saat).
    /// </summary>
    internal interface IBaustoffabgleichQuelle
    {
        /// <summary>Die Baustoffe des Katalogs (<c>Tab_Baustoff_STAMM</c>), herstellerneutral und mit Hersteller.</summary>
        IReadOnlyList<BaustoffModel> Baustoffe();

        /// <summary>Die Synonyme der Auslieferung.</summary>
        IReadOnlyList<BaustoffSynonym> Synonyme();

        /// <summary>Die gemerkten Zuordnungen des Projekts; leer ohne Projekt.</summary>
        IReadOnlyList<BaustoffNamenzuordnung> Anwenderzuordnungen();
    }

    /// <summary>
    /// Die Lesenaht aus Listen — für Tests und für den Abgleich gegen die Auslieferungssaat
    /// (<see cref="AusSaat"/>) ohne Datenbank.
    /// </summary>
    internal sealed class BaustoffabgleichDaten : IBaustoffabgleichQuelle
    {
        private readonly IReadOnlyList<BaustoffModel> _baustoffe;
        private readonly IReadOnlyList<BaustoffSynonym> _synonyme;
        private readonly IReadOnlyList<BaustoffNamenzuordnung> _zuordnungen;

        /// <summary>Die drei Listen; <c>null</c> = leer.</summary>
        internal BaustoffabgleichDaten(IEnumerable<BaustoffModel> baustoffe, IEnumerable<BaustoffSynonym> synonyme,
                                       IEnumerable<BaustoffNamenzuordnung> zuordnungen = null)
        {
            _baustoffe = (baustoffe ?? Enumerable.Empty<BaustoffModel>()).Where(b => b != null).ToList();
            _synonyme = (synonyme ?? Enumerable.Empty<BaustoffSynonym>()).Where(s => s != null).ToList();
            _zuordnungen = (zuordnungen ?? Enumerable.Empty<BaustoffNamenzuordnung>()).Where(z => z != null).ToList();
        }

        /// <summary>
        /// <b>Die Auslieferung ohne Datenbank</b>: die Baustoffsaat (<see cref="BaustoffSchema.Saat"/>) und die
        /// Synonymsaat (<see cref="BaustoffabgleichSchema.Saat"/>), dazu wahlweise gemerkte Zuordnungen.
        /// Dieselben Werte, die der Schemaschritt in eine neue Datenbank schreibt.
        /// </summary>
        internal static BaustoffabgleichDaten AusSaat(IEnumerable<BaustoffNamenzuordnung> zuordnungen = null)
            => new BaustoffabgleichDaten(
                BaustoffSchema.Saat.Select(s => new BaustoffModel
                {
                    ID = s.Id, Bezeichner = s.Bezeichner, Gruppe = s.Gruppe, Hersteller = s.Hersteller,
                    Lambda = s.Lambda, Rho = s.Rho, Cp = s.Cp, Quelle = s.Quelle, Herkunft = DbWerte.HERKUNFT_VORGABE, ReadOnly = true,
                }),
                BaustoffabgleichSchema.Saat.Select(s => new BaustoffSynonym(s.Materialname, s.Sprache, s.IdBaustoff, s.Quelle)),
                zuordnungen);

        /// <summary>
        /// <b>Ein Abzug einer Quelle</b>: liest Katalog, Synonyme und gemerkte Zuordnungen EINMAL und hält
        /// sie im Speicher — für einen Dialog, der den Abgleich nach jeder Änderung neu bildet, ohne die
        /// Datenbank erneut zu fragen.
        /// </summary>
        internal static BaustoffabgleichDaten Abzug(IBaustoffabgleichQuelle quelle)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            if (quelle is BaustoffabgleichDaten schon) return schon;
            return new BaustoffabgleichDaten(quelle.Baustoffe(), quelle.Synonyme(), quelle.Anwenderzuordnungen());
        }

        /// <summary>
        /// <b>Dieselben Listen, die gemerkten Zuordnungen überlagert</b> von noch nicht gespeicherten
        /// (Abschnitt „Baustoffe" des Importdialogs): Schlüssel → Id setzt bzw. ersetzt die Zuordnung
        /// dieses Namens, Schlüssel → <c>null</c> nimmt sie weg. Die Schlüssel werden wie beim Merken
        /// normalisiert (<see cref="Baustoffabgleich.Schluessel"/>); ein leerer Schlüssel zählt nicht.
        /// Die übrigen gemerkten Zuordnungen bleiben; der Abzug selbst bleibt unberührt.
        /// </summary>
        internal BaustoffabgleichDaten MitZuordnungen(IReadOnlyDictionary<string, int?> zuordnungen)
        {
            if (zuordnungen == null || zuordnungen.Count == 0) return this;
            var jeSchluessel = new Dictionary<string, BaustoffNamenzuordnung>(StringComparer.Ordinal);
            foreach (BaustoffNamenzuordnung z in _zuordnungen)
            {
                string k = Baustoffabgleich.Schluessel(z.Materialname);
                if (k.Length > 0) jeSchluessel[k] = z;
            }
            foreach (KeyValuePair<string, int?> paar in zuordnungen)
            {
                string k = Baustoffabgleich.Schluessel(paar.Key);
                if (k.Length == 0) continue;
                if (paar.Value is int id) jeSchluessel[k] = new BaustoffNamenzuordnung(k, id);
                else jeSchluessel.Remove(k);
            }
            return new BaustoffabgleichDaten(_baustoffe, _synonyme, jeSchluessel.Values);
        }

        /// <summary>Die Schlüssel der gemerkten Zuordnungen (normalisiert) — welche Namen das Projekt schon kennt.</summary>
        internal IReadOnlyCollection<string> GemerkteSchluessel()
            => new HashSet<string>(_zuordnungen.Select(z => Baustoffabgleich.Schluessel(z.Materialname)).Where(k => k.Length > 0),
                                   StringComparer.Ordinal);

        public IReadOnlyList<BaustoffModel> Baustoffe() => _baustoffe;

        public IReadOnlyList<BaustoffSynonym> Synonyme() => _synonyme;

        public IReadOnlyList<BaustoffNamenzuordnung> Anwenderzuordnungen() => _zuordnungen;
    }

    /// <summary>
    /// <b>Ein aufbereiteter Materialname</b> — N1 (Zahlenschwanz ab) und N2 (normalisiert, Marken
    /// abgetrennt). <see cref="Kern"/> ist der Vergleichsschlüssel aller Stufen und der Schlüssel, unter
    /// dem eine Anwenderzuordnung gemerkt wird.
    /// </summary>
    internal sealed class Baustoffname
    {
        internal Baustoffname(string roh, string ohneKennung, string voll, string kern, IReadOnlyList<string> marken)
        {
            Roh = roh;
            OhneKennung = ohneKennung;
            Voll = voll;
            Kern = kern;
            Marken = marken;
        }

        /// <summary>Der Name, wie die Datei ihn schreibt.</summary>
        internal string Roh { get; }

        /// <summary>Nach N1: ohne die angehängte Kennung des Autorensystems, sonst unverändert.</summary>
        internal string OhneKennung { get; }

        /// <summary>Nach N2, mit den Marken.</summary>
        internal string Voll { get; }

        /// <summary>Nach N2, ohne die Marken — der Vergleichsschlüssel.</summary>
        internal string Kern { get; }

        /// <summary>Die abgetrennten Marken (<c>verputzt</c>, <c>bewehrt</c>, eine Maßangabe …).</summary>
        internal IReadOnlyList<string> Marken { get; }

        /// <summary>Kurzfassung für Tests.</summary>
        public override string ToString() => Kern + (Marken.Count == 0 ? "" : " [" + string.Join(", ", Marken) + "]");
    }

    /// <summary>
    /// <b>Das Ergebnis des Abgleichs eines Materialnamens</b>: der Baustoff (Id, Bezeichner, λ, ρ, c),
    /// die Stufe und der Beleg — oder „ohne Treffer".
    /// </summary>
    internal sealed class Abgleichtreffer
    {
        internal Abgleichtreffer(Baustoffname name, Abgleichstufe stufe, Abgleichsonderfall sonderfall, BaustoffModel baustoff,
                                 GebaeudeBeleg beleg, int kandidaten = 0)
        {
            Name = name;
            Stufe = stufe;
            Sonderfall = sonderfall;
            Baustoff = baustoff;
            Beleg = beleg;
            Kandidaten = kandidaten;
        }

        /// <summary>Der aufbereitete Name.</summary>
        internal Baustoffname Name { get; }

        /// <summary>Die Stufe des Treffers; <see cref="Abgleichstufe.Keine"/> ohne.</summary>
        internal Abgleichstufe Stufe { get; }

        /// <summary>Der Sonderfall der Stufe N6; sonst <see cref="Abgleichsonderfall.Keiner"/>.</summary>
        internal Abgleichsonderfall Sonderfall { get; }

        /// <summary>Der getroffene Katalogbaustoff; <c>null</c> ohne Treffer und bei einem Sonderfall.</summary>
        internal BaustoffModel Baustoff { get; }

        /// <summary>Der Beleg (<c>GIMP_BELEG_BAUSTOFF_*</c>).</summary>
        internal GebaeudeBeleg Beleg { get; }

        /// <summary>Zahl der Kandidaten eines mehrdeutigen Teilworts (N5); 0 sonst.</summary>
        internal int Kandidaten { get; }

        /// <summary>Trägt der Treffer einen Baustoff?</summary>
        internal bool Getroffen => Baustoff != null;

        /// <summary>Kurzbezeichnung der Stufe (<c>N3</c> … <c>N7</c>); leer ohne Treffer.</summary>
        internal string StufeKurz => Stufe == Abgleichstufe.Keine ? "" : "N" + ((int)Stufe).ToString(CultureInfo.InvariantCulture);

        /// <summary>Kurzfassung für Tests.</summary>
        public override string ToString()
            => Name.Roh + " → " + (Stufe == Abgleichstufe.Keine ? "ohne Treffer"
                                   : StufeKurz + " " + (Baustoff?.Bezeichner ?? Sonderfall.ToString()));
    }

    /// <summary>
    /// <b>Der Namensabgleich N1…N7</b> (Mehrzonenkonzept 3.5 und 6.3, Befund P § 3.6; Datenaustauschkonzept
    /// 3.6) — ordnet einem Materialnamen der Datei einen Baustoff des Katalogs zu, ohne Oberfläche und
    /// ohne Datenbank (die Daten kommen über <see cref="IBaustoffabgleichQuelle"/>).
    ///
    /// <list type="number">
    /// <item><b>N1</b> Zahlenschwänze ab: <c>\s+\d{5,}$</c> — die Kennung, die Autorensysteme anhängen
    /// (<c>Leichtbeton 102890359</c> → <c>Leichtbeton</c>).</item>
    /// <item><b>N2</b> Normalisieren: Kleinschreibung, Umlaute aufgelöst (ä → ae, ß → ss, übrige Akzente
    /// ab), Bindestriche, Schrägstriche, Klammern und Kommata zu Leerzeichen (ein Komma oder Punkt zwischen
    /// zwei Ziffern bleibt), Leerzeichen einfach. Als <b>Marke</b> abgetrennt werden <c>verputzt</c>,
    /// <c>bewehrt</c>, <c>generisch</c>, <c>generic</c> und eine Maßangabe (<c>24 cm</c>, <c>200mm</c>) —
    /// sie beschreiben die Schicht, nicht den Stoff.</item>
    /// <item><b>N7</b> Die gemerkte Zuordnung des Anwenders gilt <b>vor</b> allen übrigen Stufen: Sie ist
    /// die Entscheidung, mit der der Anwender einen falschen Treffer der Kette berichtigt; stünde sie
    /// dahinter, ließe sich ein automatischer Treffer nie überstimmen. Die Tabelle 3.5 führt N7 zuletzt,
    /// weil dort die Namen landen, die die Kette nicht trifft — über den Vorrang sagt sie nichts.</item>
    /// <item><b>N6</b> Sonderfälle ohne Stoff, vor dem Katalog: Luftschicht (→ ruhende Luftschicht nach
    /// DIN EN ISO 6946), Schraffur und leer (→ Schicht verwerfen). Eine Schraffur ist nie ein Stoff, auch
    /// wenn ein Teilwort zufällig passte.</item>
    /// <item><b>N3</b> Genau gegen den normalisierten <c>Bezeichner</c>. <b>Herstellerzeilen (E39) nur
    /// hier</b> und nur, wenn keine herstellerneutrale Zeile genau trifft: Ein Hersteller steht nur
    /// dann fest, wenn die Datei sein Produkt beim Namen nennt; ein allgemeiner Name („Porenbeton")
    /// meint nie ein bestimmtes Produkt, und die neutrale Zeile trägt den Normwert, mit dem der Planer
    /// ohne Produktwahl rechnen darf.</item>
    /// <item><b>N4</b> Synonym der Auslieferung, genau; sonst ein Synonym als Wortanfang (das längste),
    /// wobei eine Zahl im Rest die Rohdichtestufe derselben Stoffreihe wählt, wenn es sie gibt
    /// (<c>KS 1400</c> → Kalksandstein 1400).</item>
    /// <item><b>N5</b> Teilwort mit eindeutigem Treffer, nur herstellerneutral: Ein Wort der Datei
    /// beginnt mit einem Katalogwort bzw. einem einwortigen Synonym oder umgekehrt (mindestens
    /// <see cref="TEILWORT_MINDESTLAENGE"/> Zeichen). Es gilt der längste Treffer; bleiben mehrere Stoffe,
    /// entscheidet der Grundeintrag (Bezeichner oder Synonym gleich dem Wort), sonst ist der Name
    /// mehrdeutig und bleibt ohne Treffer (<c>Fußbodenaufbau</c>).</item>
    /// </list>
    ///
    /// <para><b>Kein Anzeigetext ist Steuerwert:</b> Stufe und Sonderfall sind Aufzählungen, der Beleg ein
    /// Ressourcenschlüssel mit Werten (<see cref="GebaeudeBeleg"/>).</para>
    /// </summary>
    internal sealed class Baustoffabgleich
    {
        // ==================================================================
        //  Belegschlüssel
        // ==================================================================

        /// <summary>{0} Bezeichner: genauer Treffer (N3).</summary>
        internal const string BELEG_N3 = "GIMP_BELEG_BAUSTOFF_N3";
        /// <summary>{0} Bezeichner, {1} Hersteller: genauer Treffer auf eine Herstellerzeile (N3).</summary>
        internal const string BELEG_N3_HERSTELLER = "GIMP_BELEG_BAUSTOFF_N3_HERSTELLER";
        /// <summary>{0} Synonym, {1} Sprache, {2} Bezeichner (N4).</summary>
        internal const string BELEG_N4 = "GIMP_BELEG_BAUSTOFF_N4";
        /// <summary>{0} Synonym, {1} Sprache, {2} Bezeichner: Synonym als Wortanfang (N4).</summary>
        internal const string BELEG_N4_WORTANFANG = "GIMP_BELEG_BAUSTOFF_N4_WORTANFANG";
        /// <summary>{0} Wort, {1} Bezeichner (N5).</summary>
        internal const string BELEG_N5 = "GIMP_BELEG_BAUSTOFF_N5";
        /// <summary>Ruhende Luftschicht (N6).</summary>
        internal const string BELEG_N6_LUFTSCHICHT = "GIMP_BELEG_BAUSTOFF_N6_LUFTSCHICHT";
        /// <summary>Kein Stoff, Schicht verworfen (N6).</summary>
        internal const string BELEG_N6_VERWORFEN = "GIMP_BELEG_BAUSTOFF_N6_VERWORFEN";
        /// <summary>{0} Bezeichner: Zuordnung des Anwenders (N7).</summary>
        internal const string BELEG_N7 = "GIMP_BELEG_BAUSTOFF_N7";
        /// <summary>{0} Zahl der Kandidaten: mehrdeutig, ohne Treffer.</summary>
        internal const string BELEG_MEHRDEUTIG = "GIMP_BELEG_BAUSTOFF_MEHRDEUTIG";
        /// <summary>Ohne Treffer.</summary>
        internal const string BELEG_OHNE = "GIMP_BELEG_BAUSTOFF_OHNE";

        // ==================================================================
        //  Festwerte
        // ==================================================================

        /// <summary>
        /// Mindestlänge eines Worts im Teilwortabgleich (N5) — fünf Zeichen: <c>stahl</c> und <c>beton</c>
        /// tragen, <c>holz</c>, <c>putz</c> und <c>glas</c> nicht, weil sie als Wortanfang zu viele
        /// fremde Zusammensetzungen öffnen (<c>Holzwerkstoffplatte</c>, <c>Putzträgerplatte</c>,
        /// <c>Glasfaser</c>). Als ganzes Wort treffen sie über die Synonyme (N4).
        /// </summary>
        internal const int TEILWORT_MINDESTLAENGE = 5;

        /// <summary>Höchstlänge des Vergleichsschlüssels — dieselbe wie die der Spalten <c>Materialname</c>.</summary>
        internal const int LAENGE_SCHLUESSEL = BaustoffabgleichSchema.LAENGE_MATERIALNAME;

        /// <summary>Die Marken der Stufe N2 — sie beschreiben die Schicht, nicht den Stoff.</summary>
        internal static readonly IReadOnlyCollection<string> Markenwoerter =
            new HashSet<string>(StringComparer.Ordinal) { "verputzt", "bewehrt", "generisch", "generic" };

        /// <summary>Die Namen der ruhenden Luftschicht (N6), normalisiert.</summary>
        internal static readonly IReadOnlyCollection<string> Luftschichtnamen = new HashSet<string>(StringComparer.Ordinal)
        {
            "luftschicht", "luftspalt", "luft", "ruhende luftschicht", "luftschicht ruhend",
            "air", "air gap", "air layer", "air space", "airspace", "air cavity", "cavity",
        };

        /// <summary>Die Namen ohne Stoff (N6: Schraffur, leer), normalisiert — die Schicht wird verworfen.</summary>
        internal static readonly IReadOnlyCollection<string> Verwerfnamen = new HashSet<string>(StringComparer.Ordinal)
        {
            "leer", "empty", "empty fill", "solid", "solid fill", "radial gradient fill", "linear gradient fill",
            "gradient fill", "schraffur", "hatch",
        };

        private static readonly Regex Zahlenschwanz = new Regex(@"\s+\d{5,}$", RegexOptions.CultureInvariant);
        private static readonly Regex Massangabe = new Regex(@"^\d+([.,]\d+)?(mm|cm|m)$", RegexOptions.CultureInvariant);

        // ==================================================================
        //  Zustand
        // ==================================================================

        private readonly List<Katalogzeile> _neutral = new List<Katalogzeile>();
        private readonly List<Katalogzeile> _hersteller = new List<Katalogzeile>();
        private readonly Dictionary<int, Katalogzeile> _jeId = new Dictionary<int, Katalogzeile>();
        private readonly List<Synonymzeile> _synonyme = new List<Synonymzeile>();
        private readonly Dictionary<string, Synonymzeile> _synonymJeKern = new Dictionary<string, Synonymzeile>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _anwender = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, Abgleichtreffer> _ergebnisse = new Dictionary<string, Abgleichtreffer>(StringComparer.Ordinal);

        private sealed class Katalogzeile
        {
            internal BaustoffModel Stoff;
            internal string Kern;
            internal string[] Woerter;
        }

        private sealed class Synonymzeile
        {
            internal BaustoffSynonym Synonym;
            internal string Kern;
            internal Katalogzeile Ziel;
        }

        /// <summary>Liest Katalog, Synonyme und gemerkte Zuordnungen einmal aus der Quelle.</summary>
        internal Baustoffabgleich(IBaustoffabgleichQuelle quelle)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            foreach (BaustoffModel b in (quelle.Baustoffe() ?? Array.Empty<BaustoffModel>()).Where(b => b != null).OrderBy(b => b.ID))
            {
                if (_jeId.ContainsKey(b.ID)) continue;
                string kern = Normalisieren(b.Bezeichner).Kern;
                var z = new Katalogzeile { Stoff = b, Kern = kern, Woerter = Woerter(kern) };
                _jeId[b.ID] = z;
                (string.IsNullOrWhiteSpace(b.Hersteller) ? _neutral : _hersteller).Add(z);
            }
            foreach (BaustoffSynonym s in (quelle.Synonyme() ?? Array.Empty<BaustoffSynonym>()).Where(s => s != null))
            {
                if (!_jeId.TryGetValue(s.IdBaustoff, out Katalogzeile ziel)) continue;   // Verweis ins Leere: übergangen
                string kern = Normalisieren(s.Materialname).Kern;
                if (kern.Length == 0 || _synonymJeKern.ContainsKey(kern)) continue;       // der erste gilt
                var z = new Synonymzeile { Synonym = s, Kern = kern, Ziel = ziel };
                _synonyme.Add(z);
                _synonymJeKern[kern] = z;
            }
            foreach (BaustoffNamenzuordnung a in (quelle.Anwenderzuordnungen() ?? Array.Empty<BaustoffNamenzuordnung>()).Where(a => a != null))
            {
                string kern = Schluessel(a.Materialname);
                if (kern.Length > 0 && _jeId.ContainsKey(a.IdBaustoff)) _anwender[kern] = a.IdBaustoff;
            }
        }

        /// <summary>Zahl der Katalogbaustoffe, gegen die abgeglichen wird.</summary>
        internal int Katalogzahl => _jeId.Count;

        /// <summary>Zahl der wirksamen Synonyme.</summary>
        internal int Synonymzahl => _synonyme.Count;

        /// <summary>Der Katalogbaustoff zu einer Id; <c>null</c>, wenn es ihn nicht gibt.</summary>
        internal BaustoffModel Baustoff(int id) => _jeId.TryGetValue(id, out Katalogzeile z) ? z.Stoff : null;

        // ==================================================================
        //  N1 und N2
        // ==================================================================

        /// <summary>N1: der Name ohne die angehängte Kennung (<c>\s+\d{5,}$</c>), getrimmt; <c>""</c> für <c>null</c>.</summary>
        internal static string OhneZahlenschwanz(string name)
        {
            string s = (name ?? "").Trim();
            while (true)
            {
                string t = Zahlenschwanz.Replace(s, "").TrimEnd();
                if (t.Length == s.Length || t.Length == 0) return t.Length == 0 ? s : t;
                s = t;
            }
        }

        /// <summary>
        /// <b>N1 und N2</b>: Zahlenschwanz ab, normalisiert, Marken abgetrennt (Klassenkopf). Leerer Kern,
        /// wenn der Name nur aus Marken besteht? Dann ist der Kern der volle Name.
        /// </summary>
        internal static Baustoffname Normalisieren(string name)
        {
            string roh = name ?? "";
            string ohne = OhneZahlenschwanz(roh);
            string text = ohne.ToLowerInvariant()
                .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
            text = OhneAkzente(text);

            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool zwischenZiffern = (c == ',' || c == '.') && i > 0 && i < text.Length - 1
                                       && char.IsDigit(text[i - 1]) && char.IsDigit(text[i + 1]);
                sb.Append(char.IsLetterOrDigit(c) || c == '%' || zwischenZiffern ? c : ' ');
            }
            string[] woerter = sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var kern = new List<string>(woerter.Length);
            var marken = new List<string>();
            for (int i = 0; i < woerter.Length; i++)
            {
                string w = woerter[i];
                if (Markenwoerter.Contains(w) || Massangabe.IsMatch(w)) { marken.Add(w); continue; }
                if (IstZahl(w) && i + 1 < woerter.Length && (woerter[i + 1] == "mm" || woerter[i + 1] == "cm" || woerter[i + 1] == "m"))
                {
                    marken.Add(w + " " + woerter[i + 1]);
                    i++;
                    continue;
                }
                kern.Add(w);
            }
            string voll = string.Join(" ", woerter);
            string k = kern.Count == 0 ? voll : string.Join(" ", kern);
            return new Baustoffname(roh, ohne, voll, k, marken);
        }

        /// <summary>
        /// Der Schlüssel, unter dem ein Name gemerkt und gesucht wird (N7): der Kern nach N1/N2, auf
        /// <see cref="LAENGE_SCHLUESSEL"/> Zeichen gekürzt.
        /// </summary>
        internal static string Schluessel(string name)
        {
            string k = Normalisieren(name).Kern;
            return k.Length <= LAENGE_SCHLUESSEL ? k : k.Substring(0, LAENGE_SCHLUESSEL).TrimEnd();
        }

        private static string OhneAkzente(string text)
        {
            string zerlegt = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(zerlegt.Length);
            foreach (char c in zerlegt)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool IstZahl(string w) => w.Length > 0 && w.All(c => char.IsDigit(c) || c == ',' || c == '.') && char.IsDigit(w[0]);

        private static string[] Woerter(string kern) => kern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // ==================================================================
        //  Die Kette
        // ==================================================================

        /// <summary>
        /// <b>Gleicht einen Materialnamen ab</b> — Reihenfolge N7, N6, N3, N4, N5 (Klassenkopf). Wirft nie;
        /// das Ergebnis je Name wird behalten.
        /// </summary>
        internal Abgleichtreffer Abgleichen(string materialname)
        {
            string schluessel = materialname ?? "";
            if (_ergebnisse.TryGetValue(schluessel, out Abgleichtreffer bekannt)) return bekannt;
            Abgleichtreffer t = Kette(Normalisieren(materialname));
            _ergebnisse[schluessel] = t;
            return t;
        }

        private Abgleichtreffer Kette(Baustoffname n)
        {
            string kern = n.Kern;
            if (kern.Length == 0) return Ohne(n);

            // N7 — die Entscheidung des Anwenders zuerst.
            string schluessel = kern.Length <= LAENGE_SCHLUESSEL ? kern : kern.Substring(0, LAENGE_SCHLUESSEL).TrimEnd();
            if (_anwender.TryGetValue(schluessel, out int idAnwender) && _jeId.TryGetValue(idAnwender, out Katalogzeile anwender))
                return Treffer(n, Abgleichstufe.Anwender, anwender, new GebaeudeBeleg(BELEG_N7, anwender.Stoff.Bezeichner));

            // N6 — Sonderfälle ohne Stoff.
            if (Luftschichtnamen.Contains(kern))
                return new Abgleichtreffer(n, Abgleichstufe.Sonderfall, Abgleichsonderfall.Luftschicht, null,
                                           new GebaeudeBeleg(BELEG_N6_LUFTSCHICHT));
            if (Verwerfnamen.Contains(kern) || Verwerfnamen.Contains(n.Voll))
                return new Abgleichtreffer(n, Abgleichstufe.Sonderfall, Abgleichsonderfall.Verwerfen, null,
                                           new GebaeudeBeleg(BELEG_N6_VERWORFEN));

            // N3 — genau; herstellerneutral vor Hersteller.
            Katalogzeile genau = _neutral.FirstOrDefault(z => z.Kern == kern);
            if (genau != null)
                return Treffer(n, Abgleichstufe.Genau, genau, new GebaeudeBeleg(BELEG_N3, genau.Stoff.Bezeichner));
            List<Katalogzeile> hersteller = _hersteller.Where(z => z.Kern == kern).ToList();
            if (hersteller.Count == 1)
                return Treffer(n, Abgleichstufe.Genau, hersteller[0],
                               new GebaeudeBeleg(BELEG_N3_HERSTELLER, hersteller[0].Stoff.Bezeichner, hersteller[0].Stoff.Hersteller ?? ""));

            // N4 — Synonym genau, sonst als Wortanfang.
            if (_synonymJeKern.TryGetValue(kern, out Synonymzeile syn))
                return Treffer(n, Abgleichstufe.Synonym, syn.Ziel,
                               new GebaeudeBeleg(BELEG_N4, syn.Kern, syn.Synonym.Sprache ?? "", syn.Ziel.Stoff.Bezeichner));
            Synonymzeile anfang = _synonyme.Where(s => kern.StartsWith(s.Kern + " ", StringComparison.Ordinal))
                                           .OrderByDescending(s => s.Kern.Length).FirstOrDefault();
            if (anfang != null)
            {
                Katalogzeile ziel = Stufenwahl(anfang.Ziel, kern.Substring(anfang.Kern.Length + 1));
                return Treffer(n, Abgleichstufe.Synonym, ziel,
                               new GebaeudeBeleg(BELEG_N4_WORTANFANG, anfang.Kern, anfang.Synonym.Sprache ?? "", ziel.Stoff.Bezeichner));
            }

            // N5 — Teilwort mit eindeutigem Treffer, herstellerneutral.
            return Teilwort(n);
        }

        /// <summary>
        /// Die Rohdichtestufe derselben Stoffreihe, wenn der Rest nach dem Synonym eine Zahl nennt, die
        /// genau eine herstellerneutrale Zeile der Reihe (gleiches erstes Wort) trägt; sonst das Ziel des
        /// Synonyms.
        /// </summary>
        private Katalogzeile Stufenwahl(Katalogzeile ziel, string rest)
        {
            string[] zahlen = Woerter(rest).Where(w => w.All(char.IsDigit)).ToArray();
            if (zahlen.Length == 0 || ziel.Woerter.Length == 0) return ziel;
            string reihe = ziel.Woerter[0];
            List<Katalogzeile> stufe = _neutral.Where(z => z.Woerter.Length > 0 && z.Woerter[0] == reihe
                                                           && zahlen.All(x => z.Woerter.Contains(x))).ToList();
            return stufe.Count == 1 ? stufe[0] : ziel;
        }

        private Abgleichtreffer Teilwort(Baustoffname n)
        {
            // Die Wortliste: Katalogwörter (herstellerneutral) und einwortige Synonyme.
            var eintraege = new List<(string Wort, Katalogzeile Ziel, bool Grund)>();
            foreach (Katalogzeile z in _neutral)
                foreach (string w in z.Woerter)
                    if (Wortfaehig(w)) eintraege.Add((w, z, z.Kern == w));
            foreach (Synonymzeile s in _synonyme)
                if (s.Kern.IndexOf(' ') < 0 && Wortfaehig(s.Kern)) eintraege.Add((s.Kern, s.Ziel, true));

            var jeWort = new List<Katalogzeile>();
            string belegwort = null;
            foreach (string t in Woerter(n.Kern).Where(Wortfaehig))
            {
                var passend = eintraege
                    .Select(e => (e.Ziel, e.Grund, Laenge: t.StartsWith(e.Wort, StringComparison.Ordinal) ? e.Wort.Length
                                                          : e.Wort.StartsWith(t, StringComparison.Ordinal) ? t.Length : 0, e.Wort))
                    .Where(x => x.Laenge >= TEILWORT_MINDESTLAENGE)
                    .ToList();
                if (passend.Count == 0) continue;
                int max = passend.Max(x => x.Laenge);
                var beste = passend.Where(x => x.Laenge == max).ToList();
                List<Katalogzeile> stoffe = beste.Select(x => x.Ziel).Distinct().ToList();
                if (stoffe.Count == 1)
                {
                    jeWort.Add(stoffe[0]);
                    belegwort ??= t;
                    continue;
                }
                List<Katalogzeile> grund = beste.Where(x => x.Grund).Select(x => x.Ziel).Distinct().ToList();
                if (grund.Count == 1)
                {
                    jeWort.Add(grund[0]);
                    belegwort ??= t;
                    continue;
                }
                return new Abgleichtreffer(n, Abgleichstufe.Keine, Abgleichsonderfall.Keiner, null,
                                           new GebaeudeBeleg(BELEG_MEHRDEUTIG, stoffe.Count.ToString(CultureInfo.InvariantCulture)), stoffe.Count);
            }
            List<Katalogzeile> treffer = jeWort.Distinct().ToList();
            if (treffer.Count == 1)
                return Treffer(n, Abgleichstufe.Teilwort, treffer[0], new GebaeudeBeleg(BELEG_N5, belegwort, treffer[0].Stoff.Bezeichner));
            if (treffer.Count > 1)
                return new Abgleichtreffer(n, Abgleichstufe.Keine, Abgleichsonderfall.Keiner, null,
                                           new GebaeudeBeleg(BELEG_MEHRDEUTIG, treffer.Count.ToString(CultureInfo.InvariantCulture)), treffer.Count);
            return Ohne(n);
        }

        private static bool Wortfaehig(string w)
            => w.Length >= TEILWORT_MINDESTLAENGE && !w.Any(char.IsDigit) && w != "λd";

        private static Abgleichtreffer Treffer(Baustoffname n, Abgleichstufe stufe, Katalogzeile z, GebaeudeBeleg beleg)
            => new Abgleichtreffer(n, stufe, Abgleichsonderfall.Keiner, z.Stoff, beleg);

        private static Abgleichtreffer Ohne(Baustoffname n)
            => new Abgleichtreffer(n, Abgleichstufe.Keine, Abgleichsonderfall.Keiner, null, new GebaeudeBeleg(BELEG_OHNE));

        // ==================================================================
        //  Plausibilitätsband (Mehrzonenkonzept 3.5)
        // ==================================================================

        /// <summary>λ im Band [0,005; 500] W/(mK)?</summary>
        internal static bool LambdaImBand(double? l)
            => l is double v && v >= GebaeudeFestwerte.LAMBDA_MIN_WMK && v <= GebaeudeFestwerte.LAMBDA_MAX_WMK;

        /// <summary>ρ im Band [5; 8 000] kg/m³?</summary>
        internal static bool RhoImBand(double? r)
            => r is double v && v >= GebaeudeFestwerte.ROHDICHTE_MIN_KGM3 && v <= GebaeudeFestwerte.ROHDICHTE_MAX_KGM3;

        /// <summary>c im Band [100; 5 000] J/(kgK)?</summary>
        internal static bool CpImBand(double? c)
            => c is double v && v >= GebaeudeFestwerte.CP_MIN_JKGK && v <= GebaeudeFestwerte.CP_MAX_JKGK;

        /// <summary>
        /// Ein Stoffwert der Datei, der nicht im Band liegt — positiv, aber außerhalb (≤ 0 kommt aus den
        /// Lesern schon als <c>null</c>).
        /// </summary>
        internal static bool AusserhalbDesBands(double? wert, Func<double?, bool> band) => wert.HasValue && !band(wert);
    }
}
