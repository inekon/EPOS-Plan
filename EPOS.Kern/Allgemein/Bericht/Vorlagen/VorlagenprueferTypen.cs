using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Die beiden Stufen des Vorlagenprüfers (Konzept Berichtsvorlagen 6.8).</summary>
    public enum Pruefstufe
    {
        /// <summary>
        /// Nur die Platzhalter: Schlüssel, Orte, Kontexte, Formatangaben, Blöcke, dazu Felder,
        /// Kommentare, Sprache und Katalogfassung — Millisekunden; beim Öffnen der Seite, beim
        /// Vorlagenwechsel und vor jedem Start.
        /// </summary>
        Schnell,

        /// <summary>
        /// Zusätzlich Paketprobe, Dateiformat, Makros, nachverfolgte Änderungen, externe Beziehungen
        /// und Überschriftenstile — beim Hinzufügen und auf Knopfdruck.
        /// </summary>
        Voll,
    }

    /// <summary>Das Gewicht einer Prüfmeldung.</summary>
    public enum Befundstufe
    {
        /// <summary>Die Stelle wird so nicht gefüllt; vor dem Start fragt die Rückfrage nach.</summary>
        Fehler,

        /// <summary>Der Bericht entsteht, aber anders als die Vorlage es vermuten lässt.</summary>
        Warnung,

        /// <summary>Zur Kenntnis; nichts muss geändert werden.</summary>
        Hinweis,
    }

    /// <summary>Wo ein Platzhalter gefunden wurde.</summary>
    public enum Fundquelle
    {
        /// <summary>Getippt im Text (<c>{{…}}</c>).</summary>
        Text,

        /// <summary>Als Tag eines Inhaltssteuerelements.</summary>
        Steuerelement,

        /// <summary>Im Alternativtext eines Bildes.</summary>
        Bild,
    }

    /// <summary>
    /// <b>Die Umstände der Prüfung</b> — was der Lauf vorhat, gegen den die Vorlage geprüft wird.
    /// In BV-E1 wirken <see cref="Englisch"/> (Sprache der Meldungen, Sprachregel) und
    /// <see cref="Dateiname"/> (Endung in der vollen Prüfung); <see cref="AnzahlVarianten"/>,
    /// <see cref="Sicht"/> und <see cref="Ausgabe"/> tragen die Lage für die Regeln des
    /// Paarvergleichs und der Excel-Vorlage (Konzept 4.7, BV-E4, BV-E7).
    /// </summary>
    public sealed class Pruefkontext
    {
        /// <summary>Die Zahl der gewählten Varianten ohne das Stammprojekt.</summary>
        public int AnzahlVarianten { get; init; }

        /// <summary>Die Vergleichssicht der Ergebnisansicht: 1 (Stamm gegen jede Variante) oder 2 (Paarvergleich).</summary>
        public int Sicht { get; init; } = 1;

        /// <summary>Entsteht der Bericht auf Englisch? Bestimmt auch die Sprache der Meldungen.</summary>
        public bool Englisch { get; init; }

        /// <summary>Die gewählte Ausgabe.</summary>
        public Vorlagenausgabe Ausgabe { get; init; } = Vorlagenausgabe.Word;

        /// <summary>Der Dateiname der Vorlage (für die Endung); <c>null</c> = unbekannt.</summary>
        public string Dateiname { get; init; }

        /// <summary>Der Kontext eines Berichtslaufs aus seiner Konfiguration.</summary>
        public static Pruefkontext Aus(BerichtsKonfiguration konfig, bool englisch, int sicht = 1, string dateiname = null)
        {
            return new Pruefkontext
            {
                AnzahlVarianten = konfig?.VariantenIds?.Count ?? 0,
                Sicht = sicht == 2 ? 2 : 1,
                Englisch = englisch,
                Ausgabe = AusgabeAus(konfig?.Ausgabe),
                Dateiname = dateiname,
            };
        }

        /// <summary>Die Ausgabe aus der Schreibweise der Berichtskonfiguration („Word“, „Excel“, „Beide“).</summary>
        public static Vorlagenausgabe AusgabeAus(string ausgabe)
        {
            switch ((ausgabe ?? "").Trim().ToLowerInvariant())
            {
                case "excel": return Vorlagenausgabe.Excel;
                case "beide": return Vorlagenausgabe.Beide;
                default: return Vorlagenausgabe.Word;
            }
        }

        /// <summary>Derselbe Kontext mit einem Dateinamen.</summary>
        internal Pruefkontext MitDateiname(string dateiname)
        {
            return new Pruefkontext
            {
                AnzahlVarianten = AnzahlVarianten,
                Sicht = Sicht,
                Englisch = Englisch,
                Ausgabe = Ausgabe,
                Dateiname = dateiname,
            };
        }
    }

    /// <summary>
    /// <b>Eine Meldung des Vorlagenprüfers</b> (Konzept 6.8): Gewicht, Text, menschlicher Fundort
    /// und „Was tun“ in der Sprache des Berichts, dazu eine Kennung für „erklären lassen“
    /// (<c>KiMeldungskennung</c>) — sie ist der Ressourcenschlüssel des Meldungstexts.
    /// </summary>
    public sealed class Pruefmeldung
    {
        internal Pruefmeldung(Befundstufe stufe, string kennung, string text, string fundort, string wasTun,
                              string marke, string vorschlag)
        {
            Stufe = stufe;
            Kennung = kennung ?? "";
            Text = text ?? "";
            Fundort = fundort ?? "";
            WasTun = wasTun ?? "";
            Marke = marke;
            Vorschlag = vorschlag;
        }

        /// <summary>Fehler, Warnung oder Hinweis.</summary>
        public Befundstufe Stufe { get; }

        /// <summary>Die Kennung der Regel, etwa <c>VF_PRUEF_UNBEKANNT</c> — zugleich der Ressourcenschlüssel des Texts.</summary>
        public string Kennung { get; }

        /// <summary>Der Meldungstext, etwa „Unbekannter Platzhalter {{projekt.kundename}}“.</summary>
        public string Text { get; }

        /// <summary>Der Fundort, etwa „Tabelle 3, Zeile 2, Zelle 1 beginnt mit „Wärme““.</summary>
        public string Fundort { get; }

        /// <summary>Was der Anwender tun kann.</summary>
        public string WasTun { get; }

        /// <summary>Der betroffene Platzhalter in Normalform; <c>null</c> bei Meldungen zur ganzen Datei.</summary>
        public string Marke { get; }

        /// <summary>Ein Ersatz für die Marke (unbekannter Schlüssel, unbekannte Angabe); <c>null</c> ohne.</summary>
        public string Vorschlag { get; }

        /// <inheritdoc/>
        public override string ToString() { return Stufe + " " + Kennung + ": " + Text + " — " + Fundort; }
    }

    /// <summary>Ein gefundener Platzhalter mit seinem Ort und — wenn bekannt — seinem Katalogeintrag.</summary>
    public sealed class Vorlagenfund
    {
        internal Vorlagenfund(Platzhalter platzhalter, Vorlagenort ort, Fundquelle quelle, Vorlagenabsatz absatz,
                              Vorlagensteuerelement steuerelement, Vorlagenbild bild)
        {
            Platzhalter = platzhalter;
            Ort = ort;
            Quelle = quelle;
            Absatz = absatz;
            Steuerelement = steuerelement;
            Bild = bild;
        }

        /// <summary>Die erkannte Marke.</summary>
        public Platzhalter Platzhalter { get; }

        /// <summary>Der Fundort.</summary>
        public Vorlagenort Ort { get; }

        /// <summary>Text, Inhaltssteuerelement oder Bild.</summary>
        public Fundquelle Quelle { get; }

        /// <summary>Der Absatz bei <see cref="Fundquelle.Text"/>; sonst <c>null</c>.</summary>
        public Vorlagenabsatz Absatz { get; }

        /// <summary>Das Steuerelement bei <see cref="Fundquelle.Steuerelement"/>; sonst <c>null</c>.</summary>
        public Vorlagensteuerelement Steuerelement { get; }

        /// <summary>Das Bild bei <see cref="Fundquelle.Bild"/>; sonst <c>null</c>.</summary>
        public Vorlagenbild Bild { get; }

        /// <summary>Der Katalogeintrag eines Felds (auch über einen Alias); <c>null</c> = unbekannt oder Blockmarke.</summary>
        public Vorlagenfeld Feld { get; internal set; }

        /// <summary>Zählt der Fund mit (nicht im Ersatzzweig eines <c>mc:AlternateContent</c>)?</summary>
        public bool ZaehltMit { get { return Ort == null || Ort.Zweig != Vorlagenzweig.Ersatz; } }

        /// <inheritdoc/>
        public override string ToString() { return Platzhalter + " @ " + Ort; }
    }

    /// <summary>
    /// <b>Der Befund einer Prüfung</b> — Meldungen und was die Oberfläche über die Vorlage wissen
    /// muss: Zahl der Platzhalter, genutzte Schlüssel, Katalogfassung und Sprache aus
    /// <c>custom.xml</c>, Kapitel und Wirtschaftlichkeit, Prüfsumme der geprüften Bytes. Eine
    /// geänderte <see cref="Pruefsumme"/> ersetzt die Meldungsliste (Konzept 6.8).
    /// </summary>
    public sealed class Pruefbefund
    {
        internal Pruefbefund(Pruefstufe stufe, IReadOnlyList<Pruefmeldung> meldungen, IReadOnlyList<Vorlagenfund> funde,
                             int anzahlPlatzhalter, IReadOnlyList<string> schluessel, IReadOnlyList<string> unbekannte,
                             int? katalogfassung, string sprache, bool spracheAbweichend, bool hatKapitel,
                             bool hatWirtschaftlichkeit, IReadOnlyList<string> bausteine, string pruefsumme,
                             bool istLesbar, int kommentare, IReadOnlyDictionary<string, string> kapitelstellen = null,
                             bool deckblattAusPlatzhaltern = false)
        {
            Kapitelstellen = kapitelstellen ?? new Dictionary<string, string>(StringComparer.Ordinal);
            DeckblattAusPlatzhaltern = deckblattAusPlatzhaltern;
            Stufe = stufe;
            Meldungen = Ordne(meldungen ?? Array.Empty<Pruefmeldung>());
            Funde = funde ?? Array.Empty<Vorlagenfund>();
            AnzahlPlatzhalter = anzahlPlatzhalter;
            Schluessel = schluessel ?? Array.Empty<string>();
            UnbekannteSchluessel = unbekannte ?? Array.Empty<string>();
            Katalogfassung = katalogfassung;
            Sprache = sprache;
            SpracheAbweichend = spracheAbweichend;
            HatKapitel = hatKapitel;
            HatWirtschaftlichkeit = hatWirtschaftlichkeit;
            Bausteine = bausteine ?? Array.Empty<string>();
            Pruefsumme = pruefsumme ?? "";
            IstLesbar = istLesbar;
            Kommentare = kommentare;
            Fehleranzahl = Meldungen.Count(m => m.Stufe == Befundstufe.Fehler);
            Warnungen = Meldungen.Count(m => m.Stufe == Befundstufe.Warnung);
            Hinweise = Meldungen.Count(m => m.Stufe == Befundstufe.Hinweis);
        }

        /// <summary>Die Stufe, in der geprüft wurde.</summary>
        public Pruefstufe Stufe { get; }

        /// <summary>Die Meldungen: Fehler, dann Warnungen, dann Hinweise, je in Dokumentfolge.</summary>
        public IReadOnlyList<Pruefmeldung> Meldungen { get; }

        /// <summary>Alle gefundenen Platzhalter (auch die Doppel der Ersatzzweige, siehe <see cref="Vorlagenfund.ZaehltMit"/>).</summary>
        public IReadOnlyList<Vorlagenfund> Funde { get; }

        /// <summary>Die Zahl der Platzhalter — Felder, Blockmarken, Tags, Bildschlüssel —, jedes Doppel einmal.</summary>
        public int AnzahlPlatzhalter { get; }

        /// <summary>Die genutzten Schlüssel, normiert, wie geschrieben (ein Alias bleibt Alias), ohne Doppel, sortiert.</summary>
        public IReadOnlyList<string> Schluessel { get; }

        /// <summary>Die Schlüssel, die der Katalog nicht kennt.</summary>
        public IReadOnlyList<string> UnbekannteSchluessel { get; }

        /// <summary>Die Katalogfassung der Vorlage (<c>custom.xml</c>, <c>EPOS.Katalogfassung</c>); <c>null</c> = keine Angabe.</summary>
        public int? Katalogfassung { get; }

        /// <summary>Die Sprache der Vorlage (<c>custom.xml</c>, <c>EPOS.Sprache</c>), etwa „de“ oder „en“; <c>null</c> = keine Angabe.</summary>
        public string Sprache { get; }

        /// <summary>Weicht die Sprache der Vorlage von der des Berichts ab (Anlass der Rückfrage vor dem Start)?</summary>
        public bool SpracheAbweichend { get; }

        /// <summary>Nutzt die Vorlage Kapitel (Kapitelart, <c>kapitel.*</c>, <c>baustein.*</c>)?</summary>
        public bool HatKapitel { get; }

        /// <summary>
        /// Führt die Vorlage einen Schlüssel der Wirtschaftlichkeit — aus den Bereichen
        /// <c>wirtschaft.</c>, <c>stand.wirtschaft.</c>, <c>stand.bandbreite.</c>, <c>bild.wirtschaft.</c>,
        /// <c>tabelle.wirtschaft.</c> oder über <see cref="Vorlagenfeld.Deckt"/> (Konzept 10.2)?
        /// </summary>
        public bool HatWirtschaftlichkeit { get; }

        /// <summary>
        /// Die Häkchen (<c>BerichtsKonfiguration.B_*</c>, in Berichtsfolge), deren Kapitel die Vorlage führt
        /// — einzeln an gültiger Stelle oder über den Sammelanker <c>{{bericht.inhalt}}</c> (alle); eine
        /// Vorlage ohne Platzhalter bekommt den Sammelanker ans Ende und führt damit alle. Der Anhang E
        /// zählt zum Häkchen „Wirtschaftlichkeit“. Die übrigen Häkchen graut die Hülle aus („in dieser
        /// Vorlage nicht enthalten“, Konzept 10.2).
        /// </summary>
        public IReadOnlyList<string> Bausteine { get; }

        /// <summary>
        /// Die Stelle jedes Kapitels im Bericht, den die Vorlage baut (Konzept 11 Nr. 3), je
        /// <see cref="Berichtskapitel.Stellenschluessel"/>: die Überschrift vor dem Anker — der
        /// Kapitelkopf der Vorlage, sonst die eigene Überschrift des Bausteins —, <c>null</c>, wenn die
        /// Vorlage das Kapitel nicht führt. Ohne Rücksicht auf die Häkchen: die berücksichtigt
        /// <see cref="BerichtCtrl.KapitelstellenDerVorlage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string> Kapitelstellen { get; }

        /// <summary>
        /// Trägt der Rumpf Deckblattangaben aus Platzhaltern (<c>{{bericht.titel}}</c> …)? Dann steht ein
        /// Deckblatt im Bericht, auch ohne das Kapitel Deckblatt und ohne sein Häkchen.
        /// </summary>
        public bool DeckblattAusPlatzhaltern { get; }

        /// <summary>Die Prüfsumme der geprüften Bytes (SHA-256, hexadezimal, klein).</summary>
        public string Pruefsumme { get; }

        /// <summary>Ließ sich die Vorlage öffnen und durchlaufen?</summary>
        public bool IstLesbar { get; }

        /// <summary>Die Zahl der Kommentare der Vorlage.</summary>
        public int Kommentare { get; }

        /// <summary>Die Zahl der Fehler.</summary>
        public int Fehleranzahl { get; }

        /// <summary>Die Zahl der Warnungen.</summary>
        public int Warnungen { get; }

        /// <summary>Die Zahl der Hinweise.</summary>
        public int Hinweise { get; }

        /// <summary>Keine Meldung.</summary>
        public bool OhneBefund { get { return Meldungen.Count == 0; } }

        /// <summary>Mindestens ein Fehler.</summary>
        public bool HatFehler { get { return Fehleranzahl > 0; } }

        /// <summary>Derselbe Befund mit einer weiteren Meldung (etwa „in Word geöffnet“ vom Controller).</summary>
        internal Pruefbefund MitMeldung(Pruefmeldung meldung)
        {
            if (meldung == null) return this;
            var alle = new List<Pruefmeldung>(Meldungen) { meldung };
            return new Pruefbefund(Stufe, alle, Funde, AnzahlPlatzhalter, Schluessel, UnbekannteSchluessel, Katalogfassung,
                                   Sprache, SpracheAbweichend, HatKapitel, HatWirtschaftlichkeit, Bausteine, Pruefsumme,
                                   IstLesbar, Kommentare, Kapitelstellen, DeckblattAusPlatzhaltern);
        }

        /// <summary>Ein Befund ohne lesbare Vorlage: allein die übergebene Meldung.</summary>
        internal static Pruefbefund Unlesbar(Pruefstufe stufe, Pruefmeldung meldung, string pruefsumme)
        {
            return new Pruefbefund(stufe, meldung == null ? null : new[] { meldung }, null, 0, null, null, null, null,
                                   false, false, false, null, pruefsumme, false, 0);
        }

        /// <summary>Fehler, Warnungen, Hinweise — je in der Folge des Eintrags (stabil).</summary>
        private static IReadOnlyList<Pruefmeldung> Ordne(IEnumerable<Pruefmeldung> meldungen)
        {
            return meldungen.Where(m => m != null).OrderBy(m => (int)m.Stufe).ToList();
        }
    }

    /// <summary>Die Sicherheitsgrenzen vor dem Öffnen (Konzept 8.5).</summary>
    internal sealed class Pruefgrenzen
    {
        internal static readonly Pruefgrenzen Standard =
            new Pruefgrenzen(Vorlagenpruefer.GRENZE_DATEI, Vorlagenpruefer.GRENZE_ENTPACKT);

        internal Pruefgrenzen(long datei, long entpackt)
        {
            Datei = datei;
            Entpackt = entpackt;
        }

        /// <summary>Höchstgröße der Datei in Byte.</summary>
        internal long Datei { get; }

        /// <summary>Höchstgröße aller Teile entpackt in Byte; zugleich die Zeichengrenze je Teil.</summary>
        internal long Entpackt { get; }
    }

    /// <summary>
    /// Der Katalog, gegen den der Prüfer prüft — im Betrieb der <see cref="Vorlagenfeldkatalog"/>;
    /// Tests reichen eigene Einträge herein, um Regeln zu prüfen, deren Arten Katalog v1 noch
    /// nicht führt (Tabelle, Bild, Schalter, Stand, Gebäude).
    /// </summary>
    internal sealed class Vorlagenkatalogsicht
    {
        private static Vorlagenkatalogsicht _standard;

        private readonly Dictionary<string, Vorlagenfeld> _index = new Dictionary<string, Vorlagenfeld>(StringComparer.Ordinal);

        /// <summary>Die gebildeten Einträge nach Position (<see cref="Vorlagenfeldkatalog.Positionsfeld"/>).</summary>
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Vorlagenfeld> _positionen =
            new System.Collections.Concurrent.ConcurrentDictionary<string, Vorlagenfeld>(StringComparer.Ordinal);

        internal Vorlagenkatalogsicht(IEnumerable<Vorlagenfeld> alle, int fassung)
        {
            Alle = (alle ?? Enumerable.Empty<Vorlagenfeld>()).Where(f => f != null).ToList();
            Fassung = fassung;
            foreach (Vorlagenfeld f in Alle)
            {
                _index.TryAdd(f.Schluessel, f);
                foreach (string alias in f.Aliasse) _index.TryAdd(alias, f);
            }
        }

        /// <summary>Der laufende Platzhalterkatalog.</summary>
        internal static Vorlagenkatalogsicht Standard
        {
            get
            {
                return _standard ??= new Vorlagenkatalogsicht(Vorlagenfeldkatalog.Alle, Vorlagenfeldkatalog.KATALOGFASSUNG);
            }
        }

        /// <summary>Alle Einträge in Katalogfolge.</summary>
        internal IReadOnlyList<Vorlagenfeld> Alle { get; }

        /// <summary>Die laufende Katalogfassung.</summary>
        internal int Fassung { get; }

        /// <summary>Der Eintrag zu einem Schlüssel oder Alias (normiert); <c>null</c> = unbekannt.</summary>
        internal Vorlagenfeld Finde(string schluessel)
        {
            string normiert = Platzhaltersyntax.NormiereSchluessel(schluessel);
            if (normiert.Length == 0) return null;
            if (_index.TryGetValue(normiert, out Vorlagenfeld f)) return f;
            // BV-E9: ein Schlüssel nach Position, gebildet aus dem Vorbild dieser Sicht (ab Fassung 7).
            return Vorlagenfeldkatalog.Positionsfeld(normiert, s => _index.TryGetValue(s, out Vorlagenfeld v) ? v : null,
                                                     Fassung, _positionen);
        }

        /// <summary>
        /// Der nächste Eintrag nach Editierabstand über Schlüssel und Aliasse, höchstens
        /// <paramref name="grenze"/>; bei Gleichstand der erste in Katalogfolge. Ein Schlüssel, der
        /// einen Katalogschlüssel verlängert oder verkürzt (<c>projekt.kundename</c> →
        /// <c>projekt.kunde</c>), zählt wie der größte noch zulässige Abstand — ein echter Tippfehler
        /// geht vor. <c>null</c> = keiner.
        /// </summary>
        internal Vorlagenfeld Naechster(string schluessel, int grenze)
        {
            string normiert = Platzhaltersyntax.NormiereSchluessel(schluessel);
            if (normiert.Length == 0) return null;
            Vorlagenfeld bester = null;
            int besterAbstand = grenze + 1;
            foreach (Vorlagenfeld f in Alle)
            {
                foreach (string kandidat in new[] { f.Schluessel }.Concat(f.Aliasse))
                {
                    int abstand = Vorlagenpruefer.Abstand(normiert, kandidat, grenze);
                    if (abstand > grenze && IstVerlaengerung(normiert, kandidat)) abstand = grenze;
                    if (abstand < besterAbstand)
                    {
                        besterAbstand = abstand;
                        bester = f;
                    }
                }
            }
            return besterAbstand <= grenze ? bester : null;
        }

        /// <summary>Beginnt der eine Schlüssel mit dem anderen, und trägt der kürzere mindestens einen Punkt und sechs Zeichen?</summary>
        private static bool IstVerlaengerung(string a, string b)
        {
            string kurz = a.Length <= b.Length ? a : b;
            string lang = a.Length <= b.Length ? b : a;
            return kurz.Length >= 6 && kurz.IndexOf('.') > 0 && lang.Length > kurz.Length &&
                   lang.StartsWith(kurz, StringComparison.Ordinal);
        }
    }
}
