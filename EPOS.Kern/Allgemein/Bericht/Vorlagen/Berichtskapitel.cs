using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Kapitel des Berichts</b> (Konzept Berichtsvorlagen 5.3, Anhang A; Etappe BV-E2) — an EINER
    /// Stelle die Zuordnung von Kapitelplatzhalter <c>{{kapitel.&lt;name&gt;}}</c>, Häkchen
    /// (<see cref="BerichtsKonfiguration"/>, <c>B_*</c>), Schalter <c>{{baustein.&lt;name&gt;}}</c>,
    /// Kapitelkopf <c>{{text.kapitel_&lt;name&gt;}}</c> und <see cref="IBerichtsBaustein"/>, in der Folge
    /// des heutigen Berichts (<see cref="WordBerichtGenerator.AktiveBausteine"/> liest sie hier).
    ///
    /// <para><b>Anhang E</b> ist ein eigenes Kapitel (<c>kapitel.anhang_e</c>) ohne eigenes Häkchen: Es
    /// hängt am Häkchen „Wirtschaftlichkeit“ (<see cref="BerichtsKonfiguration.B_WIRTSCHAFT"/>), wie
    /// sein Baustein denselben Schlüssel trägt. Deshalb unterscheidet der <see cref="Name"/> die
    /// Kapitel, nicht der Bausteinschlüssel.</para>
    ///
    /// <para><b>Die eigene Überschrift</b> (<see cref="Ueberschrift"/>) ist der Text, den der Baustein
    /// heute als Überschrift 1 druckt — über <see cref="BerichtTexte.T(string, bool)"/> wie im Bericht, beim
    /// Anhang E aus <c>MyResource</c>. Sie liefert <c>{{text.kapitel_&lt;name&gt;}}</c> und ist die Stelle
    /// der Anhang-E-Checkliste, wenn die Vorlage keinen Kapitelkopf führt. Beim Deckblatt, das keine
    /// Überschrift trägt, steht dort der Titel des Häkchens.</para>
    /// </summary>
    public sealed class Berichtskapitel
    {
        /// <summary>Kapitel Deckblatt.</summary>
        public const string DECKBLATT = "deckblatt";

        /// <summary>Kapitel Inhaltsverzeichnis.</summary>
        public const string INHALT = "inhalt";

        /// <summary>Kapitel Projektbeschreibung.</summary>
        public const string PROJEKT = "projekt";

        /// <summary>Kapitel Komponenten &amp; Varianten.</summary>
        public const string KOMPONENTEN = "komponenten";

        /// <summary>Kapitel Berechnungsergebnisse je Variante.</summary>
        public const string ERGEBNISSE = "ergebnisse";

        /// <summary>Kapitel Variantenvergleich.</summary>
        public const string VERGLEICH = "vergleich";

        /// <summary>Kapitel Wirtschaftlichkeit.</summary>
        public const string WIRTSCHAFTLICHKEIT = "wirtschaftlichkeit";

        /// <summary>Kapitel Anhang.</summary>
        public const string ANHANG = "anhang";

        /// <summary>Kapitel Anhang E (Checkliste nach DIN EN 17463) — am Häkchen „Wirtschaftlichkeit“.</summary>
        public const string ANHANG_E = "anhang_e";

        /// <summary>Vorsilbe der Kapitelplatzhalter.</summary>
        public const string PRAEFIX_KAPITEL = "kapitel.";

        /// <summary>Vorsilbe der Schalter je Häkchen.</summary>
        public const string PRAEFIX_SCHALTER = "baustein.";

        /// <summary>Vorsilbe der Kapitelköpfe (Festtexte der Standardvorlage).</summary>
        public const string PRAEFIX_KOPF = "text.kapitel_";

        private readonly Func<IBerichtsBaustein> _neu;
        private readonly Type _typ;
        private readonly Func<bool, string> _ueberschrift;

        private Berichtskapitel(string name, string baustein, bool eigenerSchalter, bool mitKopf,
                                Func<IBerichtsBaustein> neu, Type typ, Func<bool, string> ueberschrift,
                                Vorlagenbedarf bedarf = Vorlagenbedarf.Keiner)
        {
            Name = name;
            Baustein = baustein;
            Bedarf = bedarf;
            Schalter = eigenerSchalter ? PRAEFIX_SCHALTER + name : null;
            Kopfschluessel = mitKopf ? PRAEFIX_KOPF + name : null;
            _neu = neu;
            _typ = typ;
            _ueberschrift = ueberschrift;
        }

        /// <summary>Der Name, wie ihn <c>{{kapitel.&lt;name&gt;}}</c> führt, etwa <c>projekt</c> oder <c>anhang_e</c>.</summary>
        public string Name { get; }

        /// <summary>Der Kapitelplatzhalter, etwa <c>kapitel.projekt</c>.</summary>
        public string Schluessel { get { return PRAEFIX_KAPITEL + Name; } }

        /// <summary>Der Schlüssel des Häkchens, das das Kapitel schaltet (<c>BerichtsKonfiguration.B_*</c>).</summary>
        public string Baustein { get; }

        /// <summary>Der Schalter des Häkchens (<c>baustein.&lt;name&gt;</c>); beim Anhang E <c>null</c> — er hängt am Häkchen „Wirtschaftlichkeit“.</summary>
        public string Schalter { get; }

        /// <summary>Der Kapitelkopf der Standardvorlage (<c>text.kapitel_&lt;name&gt;</c>); bei Deckblatt und Inhaltsverzeichnis <c>null</c>.</summary>
        public string Kopfschluessel { get; }

        /// <summary>
        /// <b>Was der Sammler für das Kapitel zusätzlich erheben muss</b> (Konzept Berichtsvorlagen 5.1, 8.5;
        /// Etappe BV-E3) — abgelesen am Baustein: „Ergebnisse je Variante“ zeichnet die Ganglinien aus den
        /// Stundenreihen (<see cref="Vorlagenbedarf.Zeitreihen"/>), „Wirtschaftlichkeit“ den Kapitalwertverlauf
        /// samt Brücke und Mehrjahrestafeln und die Emissionsbilanz (<see cref="Vorlagenbedarf.Verlauf"/>,
        /// <see cref="Vorlagenbedarf.Emissionsbilanz"/>). Die übrigen lesen nur, was jeder Lauf erhebt — die
        /// Speichertemperaturen der Projektbeschreibung sind eine Beigabe, wenn die Stundenreihen ohnehin da
        /// sind, kein Bedarf; so bleibt die Standardvorlage beim Bedarf von heute. Der Katalog gibt den Bedarf
        /// an <c>{{kapitel.&lt;name&gt;}}</c> weiter; <see cref="Berichtsbedarf.Vorgabe"/> vereinigt ihn über die
        /// angehakten Kapitel.
        /// </summary>
        public Vorlagenbedarf Bedarf { get; }

        /// <summary>
        /// Der Schlüssel des Kapitels in den Kapitelstellen (<see cref="BerichtCtrl.KapitelstellenDerVorlage"/>):
        /// der Bausteinschlüssel, beim Anhang E sein Name — er teilt den Bausteinschlüssel mit der
        /// Wirtschaftlichkeit.
        /// </summary>
        public string Stellenschluessel { get { return Name == ANHANG_E ? ANHANG_E : Baustein; } }

        /// <summary>Ein neuer Baustein des Kapitels.</summary>
        public IBerichtsBaustein NeuerBaustein() { return _neu(); }

        /// <summary>
        /// Die eigene Überschrift des Kapitels in der Berichtssprache: der Text, den der Baustein als
        /// Überschrift 1 druckt; beim Deckblatt der Titel des Häkchens.
        /// </summary>
        public string Ueberschrift(bool englisch) { return _ueberschrift(englisch); }

        /// <inheritdoc/>
        public override string ToString() { return Schluessel; }

        /// <summary>Alle Kapitel in der Folge des heutigen Berichts.</summary>
        public static IReadOnlyList<Berichtskapitel> Alle { get; } = new[]
        {
            new Berichtskapitel(DECKBLATT, BerichtsKonfiguration.B_DECKBLATT, true, false,
                                () => new DeckblattBaustein(), typeof(DeckblattBaustein),
                                e => BerichtsKonfiguration.Titel(BerichtsKonfiguration.B_DECKBLATT, e)),
            new Berichtskapitel(INHALT, BerichtsKonfiguration.B_INHALT, true, false,
                                () => new InhaltsverzeichnisBaustein(), typeof(InhaltsverzeichnisBaustein),
                                e => BerichtTexte.T(InhaltsverzeichnisBaustein.UEBERSCHRIFT, e)),
            new Berichtskapitel(PROJEKT, BerichtsKonfiguration.B_PROJEKT, true, true,
                                () => new ProjektbeschreibungBaustein(), typeof(ProjektbeschreibungBaustein),
                                e => BerichtTexte.T(ProjektbeschreibungBaustein.UEBERSCHRIFT, e)),
            new Berichtskapitel(KOMPONENTEN, BerichtsKonfiguration.B_KOMPONENTEN, true, true,
                                () => new KomponentenBaustein(), typeof(KomponentenBaustein),
                                e => BerichtTexte.T(KomponentenBaustein.UEBERSCHRIFT, e)),
            new Berichtskapitel(ERGEBNISSE, BerichtsKonfiguration.B_ERGEBNISSE, true, true,
                                () => new ErgebnisseBaustein(), typeof(ErgebnisseBaustein),
                                e => BerichtTexte.T(ErgebnisseBaustein.UEBERSCHRIFT, e),
                                Vorlagenbedarf.Zeitreihen),
            new Berichtskapitel(VERGLEICH, BerichtsKonfiguration.B_VERGLEICH, true, true,
                                () => new VergleichBaustein(), typeof(VergleichBaustein),
                                e => BerichtTexte.T(VergleichBaustein.UEBERSCHRIFT, e)),
            new Berichtskapitel(WIRTSCHAFTLICHKEIT, BerichtsKonfiguration.B_WIRTSCHAFT, true, true,
                                () => new WirtschaftlichkeitBaustein(), typeof(WirtschaftlichkeitBaustein),
                                e => BerichtTexte.T(WirtschaftlichkeitBaustein.UEBERSCHRIFT, e),
                                Vorlagenbedarf.Verlauf | Vorlagenbedarf.Emissionsbilanz),
            new Berichtskapitel(ANHANG, BerichtsKonfiguration.B_ANHANG, true, true,
                                () => new AnhangBaustein(), typeof(AnhangBaustein),
                                e => BerichtTexte.T(AnhangBaustein.UEBERSCHRIFT, e)),
            new Berichtskapitel(ANHANG_E, BerichtsKonfiguration.B_WIRTSCHAFT, false, true,
                                () => new AnhangEChecklisteBaustein(), typeof(AnhangEChecklisteBaustein),
                                e => Ressource(nameof(R.WIRT_AE_TITEL), e)),
        };

        /// <summary>Das Kapitel zu einem Namen (<c>projekt</c>) oder Kapitelplatzhalter (<c>kapitel.projekt</c>); <c>null</c> = keins.</summary>
        public static Berichtskapitel Finde(string nameOderSchluessel)
        {
            string n = (nameOderSchluessel ?? "").Trim();
            if (n.StartsWith(PRAEFIX_KAPITEL, StringComparison.Ordinal)) n = n.Substring(PRAEFIX_KAPITEL.Length);
            return Alle.FirstOrDefault(k => string.Equals(k.Name, n, StringComparison.Ordinal));
        }

        /// <summary>Das Kapitel eines Bausteins (nach seiner Klasse); <c>null</c> = keins.</summary>
        public static Berichtskapitel Von(IBerichtsBaustein baustein)
        {
            return baustein == null ? null : Alle.FirstOrDefault(k => k._typ == baustein.GetType());
        }

        /// <summary>Ist das Kapitel in dieser Konfiguration angehakt? <c>null</c> = alle Bausteine (wie
        /// <see cref="WordBerichtGenerator.AktiveBausteine"/>).</summary>
        public bool IstAktiv(BerichtsKonfiguration konfig)
        {
            return konfig == null || konfig.IstAktiv(Baustein);
        }

        /// <summary>
        /// Die Stellen der Kapitel, wie der bisherige Weg sie druckt — jede eigene Überschrift, sofern
        /// das Häkchen gesetzt ist; sonst <c>null</c> („nicht im Bericht“). Die Schlüssel sind die
        /// <see cref="Stellenschluessel"/>.
        /// </summary>
        public static IReadOnlyDictionary<string, string> EigeneStellen(BerichtsKonfiguration konfig, bool englisch)
        {
            var stellen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Berichtskapitel k in Alle)
                stellen[k.Stellenschluessel] = k.IstAktiv(konfig) ? k.Ueberschrift(englisch) : null;
            return stellen;
        }

        private static string Ressource(string schluessel, bool englisch)
        {
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            try { return R.ResourceManager.GetString(schluessel, kultur) ?? schluessel; }
            catch (Exception) { return schluessel; }
        }

        // =====================================================================
        //  Kapitelkopf und Überschrift vor einem Anker (Engine und Prüfer)
        // =====================================================================

        /// <summary>Der Namensraum von WordprocessingML.</summary>
        internal const string NS_W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        /// <summary>
        /// Der Kapitelkopf unmittelbar vor <paramref name="bezug"/> (Konzept 5.3 „Entfall“): der
        /// vorhergehende Geschwisterabsatz im Format „EPOS Kapitelkopf“ (<paramref name="kopfstilId"/>,
        /// aufgelöst über <c>w:name</c>), der keine Abschnittsangabe trägt; sonst <c>null</c>. Gelesen über
        /// Namen — Engine und Prüfer sehen dieselbe Regel.
        /// </summary>
        internal static OpenXmlElement KapitelkopfVor(OpenXmlElement bezug, string kopfstilId)
        {
            if (bezug == null || string.IsNullOrEmpty(kopfstilId)) return null;
            OpenXmlElement vorher = bezug.PreviousSibling();
            if (!IstAbsatz(vorher) || HatAbschnitt(vorher)) return null;
            return string.Equals(Stil(vorher), kopfstilId, StringComparison.Ordinal) ? vorher : null;
        }

        /// <summary>
        /// Die nächste Überschrift vor <paramref name="bezug"/> auf derselben Ebene: ein Absatz, dessen
        /// Stil in <paramref name="ueberschriftIds"/> steht (Kapitelkopf, Überschrift 1 bis 9); sonst <c>null</c>.
        /// </summary>
        internal static OpenXmlElement UeberschriftVor(OpenXmlElement bezug, ICollection<string> ueberschriftIds)
        {
            if (bezug == null || ueberschriftIds == null || ueberschriftIds.Count == 0) return null;
            for (OpenXmlElement e = bezug.PreviousSibling(); e != null; e = e.PreviousSibling())
                if (IstAbsatz(e) && ueberschriftIds.Contains(Stil(e) ?? "")) return e;
            return null;
        }

        /// <summary>Die Stil-IDs der Überschriften eines Dokuments: Kapitelkopf und Überschrift 1 bis 9, soweit vorhanden.</summary>
        internal static HashSet<string> UeberschriftIds(WordVorlagenstile stile)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (stile == null) return ids;
            string kopf = stile.Finde(WordVorlagenstile.KAPITELKOPF);
            if (kopf != null) ids.Add(kopf);
            for (int n = 1; n <= WordVorlagenstile.EBENE_MAX; n++)
            {
                string id = stile.Finde(WordVorlagenstile.Ueberschrift(n));
                if (id != null) ids.Add(id);
            }
            return ids;
        }

        /// <summary>
        /// Die Gliederungsebene je Überschriftenstil eines Dokuments: der Kapitelkopf 0 (er steht über allen Überschriften),
        /// Überschrift n die Ebene n (1 bis 9), soweit vorhanden. Eine kleinere Zahl heißt eine höhere Ebene.
        /// </summary>
        internal static Dictionary<string, int> UeberschriftEbenen(WordVorlagenstile stile)
        {
            var ebenen = new Dictionary<string, int>(StringComparer.Ordinal);
            if (stile == null) return ebenen;
            string kopf = stile.Finde(WordVorlagenstile.KAPITELKOPF);
            if (kopf != null) ebenen.TryAdd(kopf, 0);
            for (int n = 1; n <= WordVorlagenstile.EBENE_MAX; n++)
            {
                string id = stile.Finde(WordVorlagenstile.Ueberschrift(n));
                if (id != null) ebenen.TryAdd(id, n);
            }
            return ebenen;
        }

        private static bool IstAbsatz(OpenXmlElement e)
        {
            return e != null && e.LocalName == "p" && e.NamespaceUri == NS_W;
        }

        private static OpenXmlElement Kind(OpenXmlElement e, string name)
        {
            return e?.ChildElements.FirstOrDefault(c => c.LocalName == name && c.NamespaceUri == NS_W);
        }

        private static string Stil(OpenXmlElement absatz)
        {
            OpenXmlElement stil = Kind(Kind(absatz, "pPr"), "pStyle");
            if (stil == null) return null;
            foreach (OpenXmlAttribute a in stil.GetAttributes())
                if (a.LocalName == "val" && a.NamespaceUri == NS_W) return a.Value;
            return null;
        }

        private static bool HatAbschnitt(OpenXmlElement absatz)
        {
            return Kind(Kind(absatz, "pPr"), "sectPr") != null;
        }
    }
}
