using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using R = WindowsFormsApplication1.MyResource.Resource;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Vorlagenprüfer</b> (Konzept Berichtsvorlagen 4.3, 4.7, 4.8, 4.10, 4.11, 5.6, 6.1,
    /// 6.7, 6.8, 8.5; Etappe BV-E1) — prüft eine Word-Vorlage, bevor sie gefüllt wird, und sagt
    /// zu jedem Befund, wo er steht und was zu tun ist.
    ///
    /// <para><b>Zwei Stufen derselben Regeln.</b> Die <see cref="Pruefstufe.Schnell"/>e Prüfung
    /// sieht nur auf die Platzhalter (Schlüssel, Ort, Kontext, Formatangaben, Blöcke) und auf
    /// Felder, Kommentare, Sprache und Katalogfassung; die <see cref="Pruefstufe.Voll"/>e dazu auf
    /// das Paket: Format, Makros, nachverfolgte Änderungen, externe Beziehungen,
    /// Überschriftenstile. Die Größengrenzen gelten in BEIDEN Stufen, weil beide das Paket öffnen
    /// — sie sind der Schutz vor einer Zip-Bombe, keine Stilregel.</para>
    ///
    /// <para><b>Gelesen wird aus Bytes</b> (Konzept 6.8): Die Vorlage wird einmal gelesen; genau
    /// diese Bytes prüft und füllt EPOS-Plan. Der Prüfer schreibt nichts, auch nicht nach
    /// <c>custom.xml</c>, und er läuft ohne Faden und ohne Dateiüberwachung.</para>
    ///
    /// <para><b>Meldungstexte</b> stehen zweisprachig in <c>MyResource</c> unter <c>VF_PRUEF_*</c>;
    /// die Kennung einer Meldung ist ihr Ressourcenschlüssel. Blöcke prüft er nach den Regeln der
    /// Engine (<c>VorlagenprueferBloecke.cs</c>, BV-E4): Paare, Ebenen, Orte und die Kontexte der
    /// Werte je Stand und je Gebäude.</para>
    /// </summary>
    public static partial class Vorlagenpruefer
    {
        /// <summary>Höchstgröße der Vorlagendatei: 20 MB (Konzept 8.5).</summary>
        public const long GRENZE_DATEI = 20L * 1024 * 1024;

        /// <summary>Höchstgröße aller Teile entpackt: 100 MB (Konzept 8.5).</summary>
        public const long GRENZE_ENTPACKT = 100L * 1024 * 1024;

        /// <summary>Die Dokumenteigenschaft mit der Katalogfassung der Vorlage (Konzept 5.6).</summary>
        public const string EIGENSCHAFT_KATALOGFASSUNG = "EPOS.Katalogfassung";

        /// <summary>Die Dokumenteigenschaft mit der Sprache der Vorlage (Konzept 4.9).</summary>
        public const string EIGENSCHAFT_SPRACHE = "EPOS.Sprache";

        /// <summary>Größter Editierabstand, bis zu dem ein Vorschlag gemacht wird.</summary>
        public const int VORSCHLAG_ABSTAND = 3;

        /// <summary>Der Sammelanker, den die Engine an eine Vorlage ohne Platzhalter anhängt (Konzept 6.1).</summary>
        public const string SAMMELANKER = "{{bericht.inhalt}}";

        /// <summary>Das Datum des Berichts — der Ersatz für DATE- und TIME-Felder (Konzept 6.7).</summary>
        public const string BERICHTSDATUM = "{{bericht.datum}}";

        /// <summary>Die Bereiche des Schlüsselschemas (Konzept 4.5). Ein Tag oder Alternativtext gilt nur
        /// dann als Platzhalter, wenn er mit einem von ihnen beginnt.</summary>
        public static readonly IReadOnlyList<string> Bereiche = new[]
        {
            "bericht", "text", "ersteller", "projekt", "stamm", "stand", "gebaeude", "vergleich", "wirtschaft",
            "kennzahl", "tabelle", "bild", "kapitel", "baustein", "hat", "muster", "blatt",
        };

        /// <summary>Die Warnlisten der Gültigkeitsregel (Konzept 4.11).</summary>
        public static readonly IReadOnlyList<string> Warnlisten = new[]
        {
            "bericht.warnungen", "wirtschaft.warnungen", "stand.wirtschaft.warnungen",
        };

        /// <summary>Die Bereiche der Wirtschaftlichkeit (Konzept 10.2, zweiter Einstieg).</summary>
        public static readonly IReadOnlyList<string> Wirtschaftsbereiche = new[]
        {
            "wirtschaft.", "stand.wirtschaft.", "stand.bandbreite.", "bild.wirtschaft.", "tabelle.wirtschaft.",
            // BV-E5: das Zahlungsstrombild je Stand und die Schalter der Bilder
            "stand.bild.zahlungsstrom", "hat.bild.wirtschaft.", "hat.bild.zahlungsstrom",
        };

        /// <summary>Die Bereiche, die ohne Warnliste die Gültigkeitswarnung auslösen (Konzept 4.11).</summary>
        private static readonly string[] Gueltigkeitsbereiche = { "stand.wirtschaft.", "stand.kennzahl.", "wirtschaft." };

        /// <summary>Die Elemente nachverfolgter Änderungen (Konzept 6.1).</summary>
        private static readonly HashSet<string> Aenderungselemente = new HashSet<string>(StringComparer.Ordinal)
        {
            "ins", "del", "moveFrom", "moveTo", "rPrChange", "pPrChange", "sectPrChange", "tblPrChange", "tcPrChange",
            "trPrChange", "tblGridChange", "tblPrExChange", "numberingChange", "cellIns", "cellDel", "cellMerge",
        };

        /// <summary>Endungen, die als Word-Vorlage abgelehnt werden (Konzept 6.1).</summary>
        private static readonly string[] AbgelehnteEndungen = { ".doc", ".dot", ".docm", ".dotm", ".rtf", ".odt" };

        private static readonly Regex Zahl = new Regex(@"\d+", RegexOptions.CultureInvariant);

        /// <summary>Prüft eine Word-Vorlage gegen den laufenden Platzhalterkatalog.</summary>
        /// <param name="vorlage">Die Bytes der Vorlage, einmal gelesen.</param>
        /// <param name="stufe">Schnell oder voll.</param>
        /// <param name="kontext">Die Umstände des Laufs; <c>null</c> = deutsch, eine Sicht, Word.</param>
        public static Pruefbefund Pruefe(byte[] vorlage, Pruefstufe stufe, Pruefkontext kontext)
        {
            return Pruefe(vorlage, stufe, kontext, Vorlagenkatalogsicht.Standard, Pruefgrenzen.Standard);
        }

        /// <summary>Prüft gegen einen übergebenen Katalog und übergebene Grenzen (Prüfstand).</summary>
        internal static Pruefbefund Pruefe(byte[] vorlage, Pruefstufe stufe, Pruefkontext kontext,
                                           Vorlagenkatalogsicht katalog, Pruefgrenzen grenzen)
        {
            var s = new Sitzung(stufe, kontext ?? new Pruefkontext(), katalog ?? Vorlagenkatalogsicht.Standard);
            grenzen ??= Pruefgrenzen.Standard;
            string summe = Pruefsumme(vorlage);

            if (stufe == Pruefstufe.Voll) s.PruefeEndung(s.Kontext.Dateiname);

            if (vorlage == null || vorlage.Length == 0)
                return s.Unlesbar(s.T(nameof(R.VF_PRUEF_GRUND_LEER)), summe);
            if (IstOle(vorlage))
            {
                s.Format(s.T(nameof(R.VF_PRUEF_FORMAT_DOC)));
                return s.Befund(summe, false);
            }
            if (IstRtf(vorlage))
            {
                s.Format(s.T(nameof(R.VF_PRUEF_FORMAT_RTF)));
                return s.Befund(summe, false);
            }
            if (!IstZip(vorlage))
                return s.Unlesbar(s.T(nameof(R.VF_PRUEF_GRUND_KEIN_WORD)), summe);

            // Die Sicherheitsgrenzen VOR dem Öffnen (Konzept 8.5) — in beiden Stufen.
            if (vorlage.LongLength > grenzen.Datei)
            {
                s.Groesse(nameof(R.VF_PRUEF_GROESSE), vorlage.LongLength, grenzen.Datei);
                return s.Befund(summe, false);
            }
            long entpackt;
            bool makroImPaket;
            try { entpackt = Paketinhalt(vorlage, out makroImPaket); }
            catch (Exception ex) { return s.Unlesbar(ex.Message, summe); }
            if (entpackt > grenzen.Entpackt)
            {
                s.Groesse(nameof(R.VF_PRUEF_GROESSE_ENTPACKT), entpackt, grenzen.Entpackt);
                return s.Befund(summe, false);
            }

            using (var strom = new MemoryStream(vorlage, false))
            {
                WordprocessingDocument doc;
                try
                {
                    doc = WordprocessingDocument.Open(strom, false, new OpenSettings { MaxCharactersInPart = grenzen.Entpackt });
                }
                catch (Exception ex)
                {
                    return s.Unlesbar(ex.Message, summe);
                }

                using (doc)
                {
                    // Ein Zip ohne Word-Hauptteil öffnet das SDK klaglos — es ist trotzdem keine Vorlage.
                    bool mitRumpf;
                    try { mitRumpf = doc.MainDocumentPart?.RootElement != null; }
                    catch (Exception ex) { return s.Unlesbar(ex.Message, summe); }
                    if (!mitRumpf) return s.Unlesbar(s.T(nameof(R.VF_PRUEF_GRUND_KEIN_WORD)), summe);

                    Vorlagendurchlauf lauf;
                    try { lauf = Vorlagenteile.Durchlaufe(doc); }
                    catch (Exception ex) { return s.Unlesbar(ex.Message, summe); }

                    s.LiesEigenschaften(doc);
                    s.PruefePlatzhalter(lauf);
                    s.PruefeRahmen(lauf);
                    s.PruefeKapitel(doc, lauf);
                    s.PruefeMustertabellen(doc);
                    if (stufe == Pruefstufe.Voll)
                    {
                        s.PruefeBildrahmen();
                        s.PruefeFormat(doc, makroImPaket);
                        s.PruefeAenderungen(doc, lauf);
                        s.PruefeExtern(doc);
                        s.PruefeUeberschriften(doc);
                    }
                    return s.Befund(summe, true);
                }
            }
        }

        /// <summary>Die Prüfsumme von Bytes: SHA-256, hexadezimal, klein; leer bei <c>null</c>.</summary>
        public static string Pruefsumme(byte[] daten)
        {
            if (daten == null) return "";
            return Convert.ToHexString(SHA256.HashData(daten)).ToLowerInvariant();
        }

        /// <summary>
        /// Ist der Tag eines Inhaltssteuerelements als Platzhalter gemeint? Die Regel der Engine: genau
        /// eine Marke in doppelten Klammern (<c>{{projekt.kunde}}</c>, <c>{{#je stand}}</c>), eine Blockmarke
        /// ohne Klammern (<c>#je stand</c>, <c>#wenn hat.varianten</c> — der Wiederholabschnitt, Konzept 6.6)
        /// oder ein Schlüssel mit Punkt (<c>projekt.kunde</c>). Andere Tags — Deckblätter, Bausteine anderer
        /// Werkzeuge — bleiben unbeachtet und ohne Befund.
        /// </summary>
        public static bool IstPlatzhalterTag(string tag)
        {
            string t = (tag ?? "").Trim();
            if (t.Length == 0) return false;
            if (IstEineMarke(t, out Platzhalter marke)) return marke != null;
            Platzhalter p = Platzhaltersyntax.Lies(t);
            if (p.IstBlockmarke) return true;
            return p.Art == Platzhalterart.Feld && p.Schluessel.IndexOf('.') > 0;
        }

        /// <summary>
        /// Ist der Alternativtext eines Bildes ein Bildplatzhalter? Strenger als ein Tag, weil ein
        /// Alternativtext freier Text ist („Logo.png“): ein Feld in doppelten Klammern oder ein Schlüssel
        /// mit Punkt aus einem der <see cref="Bereiche"/>. Sonst kein Befund.
        /// </summary>
        public static bool IstBildschluessel(string beschreibung)
        {
            string t = (beschreibung ?? "").Trim();
            if (t.Length == 0) return false;
            if (IstEineMarke(t, out Platzhalter marke)) return marke != null && marke.Art == Platzhalterart.Feld;
            Platzhalter p = Platzhaltersyntax.Lies(t);
            if (p.Art != Platzhalterart.Feld || !p.SchluesselGueltig || p.Schluessel.IndexOf('.') < 0) return false;
            return Bereiche.Contains(p.Schluessel.Substring(0, p.Schluessel.IndexOf('.')));
        }

        /// <summary>Steht der Text in doppelten Klammern? Dann ist <paramref name="marke"/> die eine Marke, die ihn ganz füllt, sonst <c>null</c>.</summary>
        private static bool IstEineMarke(string text, out Platzhalter marke)
        {
            marke = null;
            if (!text.StartsWith("{{", StringComparison.Ordinal) || !text.EndsWith("}}", StringComparison.Ordinal)) return false;
            List<Platzhalter> marken = Platzhaltersyntax.Finde(text).ToList();
            if (marken.Count == 1 && marken[0].Laenge == text.Length) marke = marken[0];
            return true;
        }

        /// <summary>Der Editierabstand (Levenshtein), abgebrochen über <paramref name="grenze"/> (dann <c>grenze + 1</c>).</summary>
        internal static int Abstand(string a, string b, int grenze)
        {
            a ??= "";
            b ??= "";
            if (Math.Abs(a.Length - b.Length) > grenze) return grenze + 1;
            int[] vorher = new int[b.Length + 1];
            int[] jetzt = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) vorher[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                jetzt[0] = i;
                int zeilenMinimum = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int kosten = a[i - 1] == b[j - 1] ? 0 : 1;
                    jetzt[j] = Math.Min(Math.Min(vorher[j] + 1, jetzt[j - 1] + 1), vorher[j - 1] + kosten);
                    if (jetzt[j] < zeilenMinimum) zeilenMinimum = jetzt[j];
                }
                if (zeilenMinimum > grenze) return grenze + 1;
                int[] tausch = vorher;
                vorher = jetzt;
                jetzt = tausch;
            }
            return Math.Min(vorher[b.Length], grenze + 1);
        }

        // =====================================================================
        //  Paketprobe
        // =====================================================================

        /// <summary>OLE-Verbunddatei: Word 97–2003 oder ein verschlüsseltes OOXML-Dokument.</summary>
        private static bool IstOle(byte[] b)
        {
            return b.Length >= 8 && b[0] == 0xD0 && b[1] == 0xCF && b[2] == 0x11 && b[3] == 0xE0 &&
                   b[4] == 0xA1 && b[5] == 0xB1 && b[6] == 0x1A && b[7] == 0xE1;
        }

        private static bool IstRtf(byte[] b)
        {
            return b.Length >= 5 && b[0] == (byte)'{' && b[1] == (byte)'\\' && b[2] == (byte)'r' && b[3] == (byte)'t' && b[4] == (byte)'f';
        }

        private static bool IstZip(byte[] b)
        {
            return b.Length >= 4 && b[0] == (byte)'P' && b[1] == (byte)'K' && b[2] == 3 && b[3] == 4;
        }

        /// <summary>Die Summe der entpackten Größen aus dem Zentralverzeichnis; dazu, ob ein VBA-Projekt im Paket liegt.</summary>
        private static long Paketinhalt(byte[] vorlage, out bool makro)
        {
            makro = false;
            long summe = 0;
            using (var strom = new MemoryStream(vorlage, false))
            using (var zip = new ZipArchive(strom, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry eintrag in zip.Entries)
                {
                    summe += Math.Max(0L, eintrag.Length);
                    if (eintrag.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase)) makro = true;
                }
            }
            return summe;
        }

        // =====================================================================
        //  Die Sitzung einer Prüfung
        // =====================================================================

        private sealed partial class Sitzung
        {
            private readonly Pruefstufe _stufe;
            private readonly Vorlagenkatalogsicht _katalog;
            private readonly CultureInfo _kultur;
            private readonly List<Pruefmeldung> _meldungen = new List<Pruefmeldung>();
            private readonly HashSet<string> _gesehen = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<Vorlagenfund> _funde = new List<Vorlagenfund>();
            private int? _fassung;
            private string _sprache;
            private bool _spracheAbweichend;
            private bool _formatGemeldet;
            private int _kommentare;
            private List<string> _bausteine;
            private Dictionary<string, string> _kapitelstellen;
            private bool _deckblattAusPlatzhaltern;

            public Sitzung(Pruefstufe stufe, Pruefkontext kontext, Vorlagenkatalogsicht katalog)
            {
                _stufe = stufe;
                Kontext = kontext;
                _katalog = katalog;
                _kultur = BerichtTexte.KulturFuer(kontext.Englisch);
            }

            public Pruefkontext Kontext { get; }

            private List<Vorlagenfund> Gezaehlt { get { return _funde.Where(f => f.ZaehltMit).ToList(); } }

            // ------------------------------------------------------------ Texte

            /// <summary>Ein Text aus <c>MyResource</c> in der Sprache des Berichts; mit Argumenten formatiert.</summary>
            public string T(string schluessel, params object[] argumente)
            {
                string muster = null;
                try { muster = R.ResourceManager.GetString(schluessel, _kultur); }
                catch { muster = null; }
                muster ??= schluessel;
                if (argumente == null || argumente.Length == 0) return muster;
                try { return string.Format(_kultur, muster, argumente); }
                catch (FormatException) { return muster; }
            }

            private void Melde(Befundstufe stufe, string kennung, string text, string fundort, string wasTun,
                               string marke = null, string vorschlag = null)
            {
                string schluessel = kennung + "|" + text + "|" + fundort;
                if (!_gesehen.Add(schluessel)) return;
                _meldungen.Add(new Pruefmeldung(stufe, kennung, text, fundort, wasTun, marke, vorschlag));
            }

            private string Datei { get { return T(nameof(R.VF_PRUEF_ORT_DATEI)); } }

            /// <summary>Der Name eines Teils, etwa „Kopfzeile 2“ oder „Textfeld 1 in Haupttext“.</summary>
            private string Teilname(Vorlagenteilart art, int nummer, Vorlagenteilart? wirt, int wirtsnummer)
            {
                switch (art)
                {
                    case Vorlagenteilart.Kopfzeile: return T(nameof(R.VF_PRUEF_TEIL_KOPFZEILE), nummer);
                    case Vorlagenteilart.Fusszeile: return T(nameof(R.VF_PRUEF_TEIL_FUSSZEILE), nummer);
                    case Vorlagenteilart.Fussnote: return T(nameof(R.VF_PRUEF_TEIL_FUSSNOTE), nummer);
                    case Vorlagenteilart.Endnote: return T(nameof(R.VF_PRUEF_TEIL_ENDNOTE), nummer);
                    case Vorlagenteilart.Textfeld:
                        return T(nameof(R.VF_PRUEF_TEIL_TEXTFELD), nummer,
                                 Teilname(wirt ?? Vorlagenteilart.Rumpf, wirtsnummer, null, 0));
                    default: return T(nameof(R.VF_PRUEF_TEIL_RUMPF));
                }
            }

            /// <summary>Der menschliche Fundort einer Stelle; im Haupttext ohne den Teilnamen.</summary>
            public string Fundort(Vorlagenort o)
            {
                if (o == null) return Datei;
                string stelle = null;
                if (o.InTabelle)
                    stelle = o.Anfang.Length > 0
                        ? T(nameof(R.VF_PRUEF_ORT_ZELLE), o.Tabelle.Value, o.Zeile, o.Zelle, o.Anfang)
                        : T(nameof(R.VF_PRUEF_ORT_ZELLE_LEER), o.Tabelle.Value, o.Zeile, o.Zelle);
                else if (o.Absatz > 0)
                    stelle = o.Anfang.Length > 0
                        ? T(nameof(R.VF_PRUEF_ORT_ABSATZ), o.Absatz, o.Anfang)
                        : T(nameof(R.VF_PRUEF_ORT_ABSATZ_LEER), o.Absatz);

                string teil = Teilname(o.Teil, o.Teilnummer, o.Wirt, o.Wirtsnummer);
                if (o.Teil == Vorlagenteilart.Rumpf) return stelle ?? teil;
                return stelle == null ? teil : teil + ", " + stelle;
            }

            private string Fundort(Vorlagenfund f)
            {
                string basis = Fundort(f.Ort);
                switch (f.Quelle)
                {
                    case Fundquelle.Steuerelement:
                        return T(nameof(R.VF_PRUEF_ORT_STEUERELEMENT), basis,
                                 string.IsNullOrWhiteSpace(f.Steuerelement.Alias) ? f.Steuerelement.Tag : f.Steuerelement.Alias);
                    case Fundquelle.Bild:
                        return T(nameof(R.VF_PRUEF_ORT_BILD), basis,
                                 f.Bild.Name.Length > 0 ? f.Bild.Name : f.Bild.Kennung);
                    default:
                        return basis;
                }
            }

            private string ArtName(Vorlagenfeldart art)
            {
                switch (art)
                {
                    case Vorlagenfeldart.Zahl: return T(nameof(R.VF_PRUEF_ART_ZAHL));
                    case Vorlagenfeldart.Datum: return T(nameof(R.VF_PRUEF_ART_DATUM));
                    case Vorlagenfeldart.Tabelle: return T(nameof(R.VF_PRUEF_ART_TABELLE));
                    case Vorlagenfeldart.Bild: return T(nameof(R.VF_PRUEF_ART_BILD));
                    case Vorlagenfeldart.Liste: return T(nameof(R.VF_PRUEF_ART_LISTE));
                    case Vorlagenfeldart.Kapitel: return T(nameof(R.VF_PRUEF_ART_KAPITEL));
                    case Vorlagenfeldart.Schalter: return T(nameof(R.VF_PRUEF_ART_SCHALTER));
                    case Vorlagenfeldart.Blatt: return T(nameof(R.VF_PRUEF_ART_BLATT));
                    default: return T(nameof(R.VF_PRUEF_ART_TEXT));
                }
            }

            // ------------------------------------------------------------ Paket

            public Pruefbefund Unlesbar(string grund, string summe)
            {
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_UNLESBAR), T(nameof(R.VF_PRUEF_UNLESBAR), grund), Datei,
                      T(nameof(R.VF_PRUEF_UNLESBAR_TUN)));
                return Befund(summe, false);
            }

            public void Format(string format)
            {
                if (_formatGemeldet) return;
                _formatGemeldet = true;
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_FORMAT), T(nameof(R.VF_PRUEF_FORMAT), format), Datei,
                      T(nameof(R.VF_PRUEF_FORMAT_TUN)));
            }

            public void PruefeEndung(string dateiname)
            {
                if (string.IsNullOrWhiteSpace(dateiname)) return;
                string endung;
                try { endung = Path.GetExtension(dateiname.Trim()).ToLowerInvariant(); }
                catch (ArgumentException) { return; }
                if (AbgelehnteEndungen.Contains(endung)) Format(endung);
            }

            public void Groesse(string kennung, long groesse, long grenze)
            {
                Melde(Befundstufe.Fehler, kennung, T(kennung, Megabyte(groesse), Megabyte(grenze)), Datei,
                      T(nameof(R.VF_PRUEF_GROESSE_TUN)));
            }

            private string Megabyte(long bytes)
            {
                double mb = bytes / (1024.0 * 1024.0);
                return mb.ToString(mb >= 10 ? "0" : "0.0", _kultur);
            }

            public void LiesEigenschaften(WordprocessingDocument doc)
            {
                try
                {
                    OpenXmlElement wurzel = doc.CustomFilePropertiesPart?.RootElement;
                    if (wurzel == null) return;
                    foreach (OpenXmlElement eigenschaft in wurzel.ChildElements)
                    {
                        string name = (Vorlagenteile.Attribut(eigenschaft, "name", "") ?? "").Trim();
                        string wert = (eigenschaft.InnerText ?? "").Trim();
                        if (string.Equals(name, EIGENSCHAFT_KATALOGFASSUNG, StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(wert, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f)) _fassung = f;
                        }
                        else if (string.Equals(name, EIGENSCHAFT_SPRACHE, StringComparison.OrdinalIgnoreCase))
                        {
                            _sprache = wert.Length > 0 ? wert : null;
                        }
                    }
                }
                catch
                {
                    // Eine unlesbare Eigenschaftsdatei ist kein Grund, die Vorlage abzulehnen.
                }
            }

            public void PruefeFormat(WordprocessingDocument doc, bool makroImPaket)
            {
                if (doc.DocumentType == WordprocessingDocumentType.MacroEnabledDocument) Format(".docm");
                else if (doc.DocumentType == WordprocessingDocumentType.MacroEnabledTemplate) Format(".dotm");

                if (makroImPaket || doc.MainDocumentPart?.VbaProjectPart != null)
                    Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_MAKROS), T(nameof(R.VF_PRUEF_MAKROS)), Datei,
                          T(nameof(R.VF_PRUEF_MAKROS_TUN)));
            }

            public void PruefeAenderungen(WordprocessingDocument doc, Vorlagendurchlauf lauf)
            {
                MainDocumentPart main = doc.MainDocumentPart;
                if (main == null) return;
                var teile = new List<(string Name, OpenXmlPart Teil)>
                {
                    (T(nameof(R.VF_PRUEF_TEIL_RUMPF)), main),
                };
                for (int i = 0; i < lauf.Kopfzeilen.Count; i++)
                    teile.Add((T(nameof(R.VF_PRUEF_TEIL_KOPFZEILE), i + 1), lauf.Kopfzeilen[i]));
                for (int i = 0; i < lauf.Fusszeilen.Count; i++)
                    teile.Add((T(nameof(R.VF_PRUEF_TEIL_FUSSZEILE), i + 1), lauf.Fusszeilen[i]));
                teile.Add((T(nameof(R.VF_PRUEF_TEIL_FUSSNOTEN)), main.FootnotesPart));
                teile.Add((T(nameof(R.VF_PRUEF_TEIL_ENDNOTEN)), main.EndnotesPart));
                teile.Add((T(nameof(R.VF_PRUEF_ORT_FORMATVORLAGEN)), main.StyleDefinitionsPart));
                teile.Add((T(nameof(R.VF_PRUEF_TEIL_NUMMERIERUNG)), main.NumberingDefinitionsPart));

                foreach ((string name, OpenXmlPart teil) in teile)
                {
                    OpenXmlElement wurzel = teil?.RootElement;
                    if (wurzel == null) continue;
                    int zahl = wurzel.Descendants().Count(e => e.NamespaceUri == Vorlagenteile.NS_W &&
                                                               Aenderungselemente.Contains(e.LocalName));
                    if (zahl > 0)
                        Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_AENDERUNGEN), T(nameof(R.VF_PRUEF_AENDERUNGEN), name, zahl),
                              name, T(nameof(R.VF_PRUEF_AENDERUNGEN_TUN)));
                }
            }

            public void PruefeExtern(WordprocessingDocument doc)
            {
                int zahl = 0;
                var verweise = new List<ExternalRelationship>(doc.ExternalRelationships);
                foreach (OpenXmlPart teil in doc.GetAllParts()) verweise.AddRange(teil.ExternalRelationships);
                foreach (ExternalRelationship verweis in verweise)
                {
                    if ((verweis.RelationshipType ?? "").EndsWith("/attachedTemplate", StringComparison.OrdinalIgnoreCase))
                        Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_VORLAGENVERWEIS),
                              T(nameof(R.VF_PRUEF_VORLAGENVERWEIS), Dateiname(verweis.Uri)), Datei,
                              T(nameof(R.VF_PRUEF_VORLAGENVERWEIS_TUN)));
                    else
                        zahl++;
                }
                if (zahl > 0)
                    Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_EXTERN), T(nameof(R.VF_PRUEF_EXTERN), zahl), Datei,
                          T(nameof(R.VF_PRUEF_EXTERN_TUN)));
            }

            private static string Dateiname(Uri uri)
            {
                string text = uri?.OriginalString ?? "";
                int schnitt = Math.Max(text.LastIndexOf('/'), text.LastIndexOf('\\'));
                string name = schnitt >= 0 ? text.Substring(schnitt + 1) : text;
                try { name = Uri.UnescapeDataString(name); } catch { }
                return name.Length > 0 ? name : text;
            }

            /// <summary>
            /// Überschrift 1 bis 3 GENAU wie in der Engine (Konzept 6.2): gesucht wird über
            /// <see cref="WordVorlagenstile.Finde"/>, dieselbe Rollenauflösung, die beim Füllen die
            /// Kapitelüberschriften setzt — über <c>w:name</c> „heading n“ ohne Rücksicht auf Groß- und
            /// Kleinschreibung, sonst über die ID <c>Heading n</c> (ein deutsches Word übersetzt nur die
            /// ID). Eine zweite Suche hier liefe der Engine davon: Was der Prüfer fände, die Engine aber
            /// nicht, legte sie beim Füllen neu an, und die Prüfzeile hätte geschwiegen. „Fehlt“ heißt:
            /// die Engine findet den Stil nicht und legt ihn an; „ohne Gliederungsebene“ heißt: der
            /// gefundene Stil trägt kein <c>w:outlineLvl</c>. Geprüft nur, wenn die Vorlage Kapitel
            /// einsetzt oder gar keinen Platzhalter hat (dann hängt die Engine den Sammelanker an) —
            /// sonst setzt EPOS-Plan keine Überschrift in sie.
            /// </summary>
            public void PruefeUeberschriften(WordprocessingDocument doc)
            {
                List<Vorlagenfund> gezaehlt = Gezaehlt;
                if (!(HatKapitel(gezaehlt) || gezaehlt.Count == 0)) return;
                MainDocumentPart main = doc.MainDocumentPart;
                if (main == null) return;

                // Nur lesen: Finde legt nichts an (das tut allein Id beim Füllen).
                var rollen = new WordVorlagenstile(main);
                List<W.Style> stile = main.StyleDefinitionsPart?.Styles?.Elements<W.Style>().ToList() ?? new List<W.Style>();
                string[] ueberschriften = { WordVorlagenstile.UEBERSCHRIFT1, WordVorlagenstile.UEBERSCHRIFT2, WordVorlagenstile.UEBERSCHRIFT3 };
                var befunde = new List<string>();
                for (int n = 1; n <= ueberschriften.Length; n++)
                {
                    string id = rollen.Finde(ueberschriften[n - 1]);
                    W.Style stil = id == null ? null : stile.FirstOrDefault(s => string.Equals(s.StyleId?.Value, id, StringComparison.Ordinal));
                    string bezeichnung = T(nameof(R.VF_PRUEF_STIL_UEBERSCHRIFT), n);
                    if (stil == null)
                        befunde.Add(T(nameof(R.VF_PRUEF_STIL_FEHLT), bezeichnung));
                    else if (stil.StyleParagraphProperties?.OutlineLevel == null)
                        befunde.Add(T(nameof(R.VF_PRUEF_STIL_OHNE_EBENE), bezeichnung));
                }
                if (befunde.Count > 0)
                    Melde(Befundstufe.Warnung, nameof(R.VF_PRUEF_UEBERSCHRIFTEN),
                          T(nameof(R.VF_PRUEF_UEBERSCHRIFTEN), string.Join("; ", befunde)),
                          T(nameof(R.VF_PRUEF_ORT_FORMATVORLAGEN)), T(nameof(R.VF_PRUEF_UEBERSCHRIFTEN_TUN)));
            }

            // ------------------------------------------------------------ Platzhalter

            public void PruefePlatzhalter(Vorlagendurchlauf lauf)
            {
                _kommentare = lauf.Kommentare;

                // Erkannt wird wie in der Engine: im Erkennungstext, dessen Trennstellen (Tabulator,
                // Umbruch, Feldzeichen, Bild, Behältergrenze) kein Platzhalter überspannt. Was dort
                // zerrissen ist, bleibt im Bericht still als Text stehen — deshalb die Warnung.
                foreach (Vorlagenabsatz a in lauf.Absaetze)
                {
                    string text = a.Erkennungstext;
                    foreach (int stelle in Platzhaltersyntax.OffeneKlammern(text))
                    {
                        string rest = text.Substring(stelle);
                        int ende = rest.IndexOfAny(new[] { '\n', '\r' });
                        if (ende >= 0) rest = rest.Substring(0, ende);
                        if (rest.Length > 30) rest = rest.Substring(0, 30) + "…";
                        Melde(Befundstufe.Warnung, nameof(R.VF_PRUEF_KLAMMER_OFFEN),
                              T(nameof(R.VF_PRUEF_KLAMMER_OFFEN), rest, "{{", "}}"),
                              Fundort(a.Ort), T(nameof(R.VF_PRUEF_KLAMMER_OFFEN_TUN)));
                    }
                    foreach (Platzhalter p in Platzhaltersyntax.Finde(text))
                        _funde.Add(new Vorlagenfund(p, a.Ort, Fundquelle.Text, a, null, null));
                }
                foreach (Vorlagensteuerelement st in lauf.Steuerelemente)
                    if (IstPlatzhalterTag(st.Tag))
                        _funde.Add(new Vorlagenfund(Platzhaltersyntax.Lies(st.Tag), st.Ort, Fundquelle.Steuerelement, null, st, null));
                foreach (Vorlagenbild b in lauf.Bilder)
                    if (IstBildschluessel(b.Beschreibung))
                        _funde.Add(new Vorlagenfund(Platzhaltersyntax.Lies(b.Beschreibung), b.Ort, Fundquelle.Bild, null, null, b));

                // Erst die Blöcke (Paare und Bereiche), dann jede Marke in ihrem Kontext (Konzept 4.7).
                PruefeBloecke();
                foreach (Vorlagenfund f in _funde)
                {
                    switch (f.Platzhalter.Art)
                    {
                        case Platzhalterart.Feld:
                            PruefeFeld(f);
                            break;
                        case Platzhalterart.Unbekannt:
                            Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_MARKE_UNBEKANNT),
                                  T(nameof(R.VF_PRUEF_MARKE_UNBEKANNT), MarkeVon(f)), Fundort(f),
                                  T(nameof(R.VF_PRUEF_MARKE_UNBEKANNT_TUN)), MarkeVon(f));
                            break;
                        default:
                            PruefeBlockmarke(f);
                            break;
                    }
                }
                PruefeTiefe();
            }

            /// <summary>Die Marke, wie der Anwender sie in der Meldung wiedererkennt: die Normalform,
            /// bei einer unbekannten Marke der Wortlaut.</summary>
            private static string MarkeVon(Vorlagenfund f)
            {
                Platzhalter p = f.Platzhalter;
                if (p.Art != Platzhalterart.Unbekannt) return p.Normalform;
                string roh = p.Roh.Trim();
                return roh.StartsWith("{{", StringComparison.Ordinal) ? roh : "{{" + roh + "}}";
            }

            private void PruefeFeld(Vorlagenfund f)
            {
                Platzhalter p = f.Platzhalter;
                Vorlagenfeld feld = _katalog.Finde(p.Schluessel);
                f.Feld = feld;
                if (feld == null)
                {
                    Vorlagenfeld naechster = _katalog.Naechster(p.Schluessel, VORSCHLAG_ABSTAND);
                    string vorschlag = naechster == null ? null
                        : "{{" + naechster.Schluessel + string.Concat(p.Angaben.Select(a => "|" + a.Normalform)) + "}}";

                    // Werte je Variante und je Gebäude führt der Katalog schrittweise: Außerhalb ihres Blocks
                    // ist ein Schlüssel dieser Bereiche ein Kontextfehler, im Block ein unbekannter.
                    // BV-E5: ein vorgemerktes App-Diagramm ist kein Tippfehler (VorlagenprueferBilder.cs).
                    if (PruefeVorgemerkt(f)) return;

                    Vorlagenfeldkontext? bereich = BereichOhneEintrag(p.Schluessel);
                    if (bereich.HasValue && !ImKontext(f, bereich.Value, p.Schluessel))
                    {
                        Kontextfehler(f, bereich.Value, vorschlag);
                        PruefeAngaben(f, null);
                        return;
                    }

                    Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_UNBEKANNT), T(nameof(R.VF_PRUEF_UNBEKANNT), p.Normalform),
                          Fundort(f),
                          vorschlag != null ? T(nameof(R.VF_PRUEF_VORSCHLAG_TUN), vorschlag) : T(nameof(R.VF_PRUEF_UNBEKANNT_TUN)),
                          p.Normalform, vorschlag);
                    PruefeAngaben(f, null);
                    return;
                }

                PruefeOrt(f, feld);
                if (!ImKontext(f, feld.Kontext, feld.Schluessel))
                    Kontextfehler(f, feld.Kontext, null);
                PruefePaarsicht(f, feld.Schluessel);
                PruefeAngaben(f, feld);

                if (f.Quelle == Fundquelle.Text && !p.IstNormalform)
                    Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_NORMALFORM),
                          T(nameof(R.VF_PRUEF_NORMALFORM), p.Roh, p.Normalform), Fundort(f),
                          T(nameof(R.VF_PRUEF_NORMALFORM_TUN), p.Normalform), p.Normalform, p.Normalform);
            }

            /// <summary>
            /// Ein Wert je Variante oder je Gebäude außerhalb seines Blocks (Konzept 4.7). Der Vorschlag
            /// nennt, wenn es ihn gibt, den nahen Wert des Stammprojekts.
            /// </summary>
            private void Kontextfehler(Vorlagenfund f, Vorlagenfeldkontext kontext, string vorschlag)
            {
                bool gebaeude = kontext == Vorlagenfeldkontext.Gebaeude;
                string kennung = gebaeude ? nameof(R.VF_PRUEF_KONTEXT_GEBAEUDE) : nameof(R.VF_PRUEF_KONTEXT_STAND);
                string block = gebaeude ? "{{#je gebaeude}}" : "{{#je stand}}";
                Melde(Befundstufe.Fehler, kennung, T(kennung, f.Platzhalter.Normalform, block), Fundort(f),
                      T(nameof(R.VF_PRUEF_KONTEXT_TUN), block, "{{/je}}"), f.Platzhalter.Normalform, vorschlag);
            }

            /// <summary>Der Kontext eines Schlüssels ohne Katalogeintrag aus seinem Bereich: <c>stand.</c> oder <c>gebaeude.</c>; sonst <c>null</c>.</summary>
            private static Vorlagenfeldkontext? BereichOhneEintrag(string schluessel)
            {
                if (schluessel.StartsWith("stand.", StringComparison.Ordinal)) return Vorlagenfeldkontext.Stand;
                if (schluessel.StartsWith("gebaeude.", StringComparison.Ordinal)) return Vorlagenfeldkontext.Gebaeude;
                return null;
            }

            /// <summary>Die Stelle eines Teils, die für Tabellen, Listen und Kapitel verboten ist; <c>null</c> im Rumpf.</summary>
            private static string Teilstelle(Vorlagenort o)
            {
                switch (o.Teil)
                {
                    case Vorlagenteilart.Kopfzeile:
                    case Vorlagenteilart.Fusszeile:
                        return nameof(R.VF_PRUEF_STELLE_KOPFFUSS);
                    case Vorlagenteilart.Fussnote:
                    case Vorlagenteilart.Endnote:
                        return nameof(R.VF_PRUEF_STELLE_NOTE);
                    case Vorlagenteilart.Textfeld:
                        return nameof(R.VF_PRUEF_STELLE_TEXTFELD);
                    default:
                        return null;
                }
            }

            private static bool InNote(Vorlagenort o)
            {
                Vorlagenteilart teil = o.Teil == Vorlagenteilart.Textfeld ? (o.Wirt ?? Vorlagenteilart.Rumpf) : o.Teil;
                return teil == Vorlagenteilart.Fussnote || teil == Vorlagenteilart.Endnote;
            }

            /// <summary>Steht die Marke allein in ihrem Absatz?</summary>
            private static bool IstAllein(Vorlagenfund f)
            {
                if (f.Absatz == null) return true;
                Platzhalter p = f.Platzhalter;
                string text = f.Absatz.Erkennungstext;
                if (p.Position < 0 || p.Position + p.Laenge > text.Length) return false;
                string rest = text.Remove(p.Position, p.Laenge).Replace("­", "");
                return string.IsNullOrWhiteSpace(rest);
            }

            /// <summary>Die Ortsregeln (Konzept 4.3, 4.2, 6.6) für ein bekanntes Feld.</summary>
            private void PruefeOrt(Vorlagenfund f, Vorlagenfeld feld)
            {
                Vorlagenort o = f.Ort;
                if ((feld.Ausgaben & Vorlagenausgabe.Word) == 0)
                {
                    OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_WORD), T(nameof(R.VF_PRUEF_ORT_TUN_EXCEL)));
                    return;
                }
                // BV-E5: {{muster.tabelle}} gilt nur als Alternativtext oder Titel einer Tabelle (Konzept 6.4 Nr. 2).
                if (string.Equals(feld.Schluessel, Vorlagenfeldkatalog.MUSTER_TABELLE, System.StringComparison.Ordinal))
                {
                    OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_MUSTER), T(nameof(R.VF_PRUEF_ORT_TUN_MUSTER)));
                    return;
                }
                if (feld.Art == Vorlagenfeldart.Schalter)
                {
                    OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_OHNE_WENN),
                              T(nameof(R.VF_PRUEF_ORT_TUN_WENN), "{{#wenn " + feld.Schluessel + "}}", "{{/wenn}}"));
                    return;
                }
                if (f.Quelle == Fundquelle.Bild)
                {
                    if (feld.Art != Vorlagenfeldart.Bild)
                        OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_BILD), T(nameof(R.VF_PRUEF_ORT_TUN_BILD)));
                    else if (InNote(o))
                        OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_NOTE), T(nameof(R.VF_PRUEF_ORT_TUN_HAUPTTEXT)));
                    return;
                }

                // Inhaltssteuerelemente um Tabellenzeilen oder -zellen füllt die Engine nicht.
                if (f.Quelle == Fundquelle.Steuerelement &&
                    (f.Steuerelement.Ebene == Steuerelementebene.Zeile || f.Steuerelement.Ebene == Steuerelementebene.Zelle))
                {
                    OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_SDT_TABELLE), T(nameof(R.VF_PRUEF_ORT_TUN_SDT_TABELLE)));
                    return;
                }

                // Das Logo gibt es nur als Alternativtext eines Bildes (Entscheid BV-E2-1): als Text oder Tag bliebe
                // die Stelle gelb stehen. Ein Diagramm (BV-E5) gilt als Text allein im Absatz — im Satz ein Fehler.
                if (feld.Art == Vorlagenfeldart.Bild)
                {
                    if (string.Equals(feld.Schluessel, Vorlagenfeldkatalog.LOGO, StringComparison.Ordinal))
                        Spaeter(f, Befundstufe.Fehler, T(nameof(R.VF_PRUEF_SPAETER_TUN_BILD), "{{" + feld.Schluessel + "}}"));
                    else if (PruefeDiagrammAlsText(f, feld))
                        return;
                }

                bool einfach = feld.Art == Vorlagenfeldart.Text || feld.Art == Vorlagenfeldart.Zahl ||
                               feld.Art == Vorlagenfeldart.Datum;
                if (einfach) return;

                if (f.Quelle == Fundquelle.Steuerelement && f.Steuerelement.Ebene == Steuerelementebene.Satz)
                {
                    OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_SDT_SATZ), T(nameof(R.VF_PRUEF_ORT_TUN_SDT)));
                    return;
                }
                if (f.Quelle == Fundquelle.Text && !IstAllein(f))
                {
                    OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_SATZ), T(nameof(R.VF_PRUEF_ORT_TUN_ABSATZ)));
                    return;
                }

                switch (feld.Art)
                {
                    case Vorlagenfeldart.Tabelle:
                    case Vorlagenfeldart.Liste:
                        {
                            string stelle = Teilstelle(o);
                            if (stelle != null) OrtFehler(f, feld.Art, stelle, T(nameof(R.VF_PRUEF_ORT_TUN_HAUPTTEXT)));
                            break;
                        }
                    case Vorlagenfeldart.Kapitel:
                        {
                            string stelle = Teilstelle(o);
                            if (stelle != null) OrtFehler(f, feld.Art, stelle, T(nameof(R.VF_PRUEF_ORT_TUN_HAUPTTEXT)));
                            else if (o.InTabelle) OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_ZELLE), T(nameof(R.VF_PRUEF_ORT_TUN_ZELLE)));
                            break;
                        }
                    case Vorlagenfeldart.Bild:
                        if (InNote(o)) OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_NOTE), T(nameof(R.VF_PRUEF_ORT_TUN_HAUPTTEXT)));
                        break;
                }
            }

            private void OrtFehler(Vorlagenfund f, Vorlagenfeldart art, string stelle, string wasTun)
            {
                OrtFehler(f, ArtName(art), stelle, wasTun);
            }

            /// <summary>
            /// „Erst in einer späteren Programmfassung“ — ein Bild als Text oder Tag (ab BV-E5) als Fehler: Es
            /// bliebe gelb stehen.
            /// </summary>
            private void Spaeter(Vorlagenfund f, Befundstufe stufe, string wasTun)
            {
                string marke = MarkeVon(f);
                string text = T(nameof(R.VF_PRUEF_SPAETER), marke);
                if (stufe == Befundstufe.Fehler)
                    Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_SPAETER), text, Fundort(f), wasTun, marke);
                else
                    Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_SPAETER), text, Fundort(f), wasTun, marke);
            }

            private void OrtFehler(Vorlagenfund f, string artName, string stelle, string wasTun)
            {
                string marke = MarkeVon(f);
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_ORT), T(nameof(R.VF_PRUEF_ORT), marke, artName, T(stelle)),
                      Fundort(f), wasTun, marke);
            }

            /// <summary>Unbekannte und unpassende Formatangaben (Konzept 4.8).</summary>
            private void PruefeAngaben(Vorlagenfund f, Vorlagenfeld feld)
            {
                Platzhalter p = f.Platzhalter;
                foreach (Formatangabe a in p.Angaben)
                {
                    if (!a.IstBekannt)
                    {
                        IReadOnlyList<string> erlaubt = feld != null ? PassendeAngaben(feld.Art) : Platzhaltersyntax.AngabenMuster;
                        UnbekannteAngabe(f, a, erlaubt);
                    }
                    else if (feld != null && !a.PasstZu(feld.Art))
                    {
                        UnpassendeAngabe(f, a, ArtName(feld.Art), PassendeAngaben(feld.Art));
                    }
                }
            }

            private void UnbekannteAngabe(Vorlagenfund f, Formatangabe a, IReadOnlyList<string> erlaubt)
            {
                Platzhalter p = f.Platzhalter;
                if (erlaubt == null || erlaubt.Count == 0) erlaubt = Platzhaltersyntax.AngabenMuster;
                string vorschlag = AngabeVorschlag(a, erlaubt);
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_ANGABE_UNBEKANNT),
                      T(nameof(R.VF_PRUEF_ANGABE_UNBEKANNT), a.Roh.Trim(), p.Normalform), Fundort(f),
                      vorschlag != null ? T(nameof(R.VF_PRUEF_ANGABE_TUN_VORSCHLAG), vorschlag)
                                        : T(nameof(R.VF_PRUEF_ANGABE_TUN_LISTE), string.Join(", ", erlaubt)),
                      p.Normalform, vorschlag);
            }

            private void UnpassendeAngabe(Vorlagenfund f, Formatangabe a, string artName, IReadOnlyList<string> passende)
            {
                Platzhalter p = f.Platzhalter;
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_ANGABE_UNPASSEND),
                      T(nameof(R.VF_PRUEF_ANGABE_UNPASSEND), a.Normalform, p.Normalform, artName), Fundort(f),
                      passende.Count > 0 ? T(nameof(R.VF_PRUEF_ANGABE_UNPASSEND_TUN), string.Join(", ", passende))
                                         : T(nameof(R.VF_PRUEF_ANGABE_TUN_ENTFERNEN)),
                      p.Normalform);
            }

            /// <summary>Die Angaben (Muster mit „n“), die für eine Art gelten.</summary>
            private static IReadOnlyList<string> PassendeAngaben(Vorlagenfeldart art)
            {
                return Platzhaltersyntax.AngabenMuster
                    .Where(m => Platzhaltersyntax.LiesAngabe(MitZahl(m, "1")).PasstZu(art))
                    .ToList();
            }

            /// <summary>Ein Muster mit eingesetzter Zahl: „stellen n“ → „stellen 2“.</summary>
            private static string MitZahl(string muster, string zahl)
            {
                return muster.EndsWith(" n", StringComparison.Ordinal) ? muster.Substring(0, muster.Length - 1) + zahl : muster;
            }

            /// <summary>Die nächste bekannte Angabe nach Editierabstand (mit der Zahl der Vorlage); <c>null</c> = keine.</summary>
            private static string AngabeVorschlag(Formatangabe a, IReadOnlyList<string> erlaubt)
            {
                string normiert = Platzhaltersyntax.Normiere(a.Roh);
                if (normiert.Length == 0) return null;
                Match zahl = Zahl.Match(normiert);
                string bester = null;
                int besterAbstand = VORSCHLAG_ABSTAND + 1;
                foreach (string muster in erlaubt)
                {
                    string kandidat = MitZahl(muster, zahl.Success ? zahl.Value : "n");
                    int abstand = Math.Min(Abstand(normiert, kandidat, VORSCHLAG_ABSTAND),
                                           Abstand(normiert.Replace(" ", ""), kandidat.Replace(" ", ""), VORSCHLAG_ABSTAND));
                    if (abstand < besterAbstand)
                    {
                        besterAbstand = abstand;
                        bester = kandidat;
                    }
                }
                return bester;
            }

            /// <summary>Blockanfang, -ende und Bedingung: Ort, Bereich, Angaben, Schalter und Kontext (Konzept 4.2, 4.3, 4.7, 4.8).</summary>
            private void PruefeBlockmarke(Vorlagenfund f)
            {
                Platzhalter p = f.Platzhalter;
                string marke = p.Normalform;
                string block = T(nameof(R.VF_PRUEF_ART_BLOCK));

                if (f.Quelle == Fundquelle.Text || f.Quelle == Fundquelle.Steuerelement)
                {
                    string stelle = Teilstelle(f.Ort);
                    if (stelle != null) OrtFehler(f, block, stelle, T(nameof(R.VF_PRUEF_ORT_TUN_HAUPTTEXT)));
                }
                if (f.Quelle == Fundquelle.Steuerelement) PruefeBlocksteuerelement(f);

                switch (p.Art)
                {
                    case Platzhalterart.BlockAnfang:
                        if (!Platzhaltersyntax.JeBereiche.Contains(p.Schluessel))
                            Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_BEREICH), T(nameof(R.VF_PRUEF_BLOCK_BEREICH), marke),
                                  Fundort(f), T(nameof(R.VF_PRUEF_BLOCK_BEREICH_TUN),
                                                string.Join(", ", Platzhaltersyntax.JeBereiche.Select(b => "{{#je " + b + "}}"))),
                                  marke);
                        foreach (Formatangabe a in p.Angaben)
                        {
                            if (!a.IstBekannt) UnbekannteAngabe(f, a, new[] { "block n" });
                            else if (!a.PasstZuBlock) UnpassendeAngabe(f, a, block, new[] { "block n" });
                            else if (!BlockangabeErlaubt(f)) UnpassendeAngabe(f, a, block, Array.Empty<string>());
                        }
                        break;
                    case Platzhalterart.WennAnfang:
                        if (p.Schluessel.Length == 0)
                        {
                            Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_WENN_OHNE_SCHALTER), T(nameof(R.VF_PRUEF_WENN_OHNE_SCHALTER), marke),
                                  Fundort(f), T(nameof(R.VF_PRUEF_WENN_TUN)), marke);
                        }
                        else
                        {
                            Vorlagenfeld schalter = _katalog.Finde(p.Schluessel);
                            if (schalter == null)
                            {
                                Vorlagenfeld naechster = _katalog.Naechster(p.Schluessel, VORSCHLAG_ABSTAND);
                                string vorschlag = naechster == null ? null
                                    : "{{#wenn " + (p.Verneint ? Platzhaltersyntax.WORT_NICHT + " " : "") + naechster.Schluessel + "}}";
                                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_UNBEKANNT), T(nameof(R.VF_PRUEF_UNBEKANNT), marke),
                                      Fundort(f),
                                      vorschlag != null ? T(nameof(R.VF_PRUEF_VORSCHLAG_TUN), vorschlag) : T(nameof(R.VF_PRUEF_UNBEKANNT_TUN)),
                                      marke, vorschlag);
                            }
                            else if (schalter.Art != Vorlagenfeldart.Schalter)
                            {
                                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_WENN_KEIN_SCHALTER),
                                      T(nameof(R.VF_PRUEF_WENN_KEIN_SCHALTER), "{{" + schalter.Schluessel + "}}"),
                                      Fundort(f), T(nameof(R.VF_PRUEF_WENN_TUN)), marke);
                            }
                            else
                            {
                                f.Feld = schalter;
                                // Ein Schalter je Stand oder je Gebäude gilt nur in seinem Block (Konzept 4.7).
                                if (!ImKontext(f, schalter.Kontext, schalter.Schluessel))
                                    Kontextfehler(f, schalter.Kontext, null);
                            }
                        }
                        AngabenAnEnde(f, block);
                        break;
                    default:
                        AngabenAnEnde(f, block);
                        break;
                }

                if (f.Quelle == Fundquelle.Text)
                {
                    if (!f.Ort.InTabelle && !IstAllein(f) && !InMusterzeile(f))
                        Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_ALLEIN), T(nameof(R.VF_PRUEF_BLOCK_ALLEIN), marke),
                              Fundort(f), T(nameof(R.VF_PRUEF_BLOCK_ALLEIN_TUN)), marke);
                    if (!p.IstNormalform)
                        Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_NORMALFORM), T(nameof(R.VF_PRUEF_NORMALFORM), p.Roh, marke),
                              Fundort(f), T(nameof(R.VF_PRUEF_NORMALFORM_TUN), marke), marke, marke);
                }
            }

            /// <summary>Bedingung und Blockenden tragen keine Angaben.</summary>
            private void AngabenAnEnde(Vorlagenfund f, string block)
            {
                foreach (Formatangabe a in f.Platzhalter.Angaben)
                {
                    if (!a.IstBekannt) UnbekannteAngabe(f, a, null);
                    else UnpassendeAngabe(f, a, block, Array.Empty<string>());
                }
            }

            /// <summary>
            /// Die Paare der getippten Blockmarken je Teil (Konzept 4.2, 6.4): jedes Ende hat seinen
            /// Anfang, nie über eine Tabellengrenze, in einer Wiederholzeile kein Zellverbund; die Ebenen
            /// zählt <see cref="PruefeTiefe"/>. Steuerelemente begrenzen ihren Block selbst und brauchen
            /// kein Ende. Jedes Paar wird ein Blockbereich für die Kontexte.
            /// </summary>
            private void PruefeBloecke()
            {
                IEnumerable<IGrouping<string, Vorlagenfund>> teile = _funde
                    .Where(f => f.Quelle == Fundquelle.Text && f.ZaehltMit && f.Platzhalter.IstBlockmarke)
                    .GroupBy(f => f.Ort.Teil + "/" + f.Ort.Teilnummer + "/" + f.Ort.Wirt + "/" + f.Ort.Wirtsnummer);
                foreach (IGrouping<string, Vorlagenfund> teil in teile)
                {
                    var stapel = new List<Vorlagenfund>();
                    foreach (Vorlagenfund f in teil)
                    {
                        Platzhalterart art = f.Platzhalter.Art;
                        if (art == Platzhalterart.BlockAnfang || art == Platzhalterart.WennAnfang)
                        {
                            stapel.Add(f);
                            continue;
                        }

                        Platzhalterart gesucht = art == Platzhalterart.BlockEnde ? Platzhalterart.BlockAnfang : Platzhalterart.WennAnfang;
                        int i = stapel.FindLastIndex(s => s.Platzhalter.Art == gesucht);
                        if (i < 0)
                        {
                            Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_ENDE), T(nameof(R.VF_PRUEF_BLOCK_ENDE), f.Platzhalter.Normalform),
                                  Fundort(f), T(nameof(R.VF_PRUEF_BLOCK_ENDE_TUN)), f.Platzhalter.Normalform);
                            continue;
                        }
                        for (int k = stapel.Count - 1; k > i; k--)
                        {
                            Offen(stapel[k]);
                            stapel.RemoveAt(k);
                        }
                        PruefePaar(stapel[i], f);
                        MerkeBereich(stapel[i], f);
                        stapel.RemoveAt(i);
                    }
                    foreach (Vorlagenfund offen in stapel) Offen(offen);
                }
            }

            private void Offen(Vorlagenfund anfang)
            {
                string ende = anfang.Platzhalter.Art == Platzhalterart.WennAnfang
                    ? "{{/" + Platzhaltersyntax.BLOCK_WENN + "}}" : "{{/" + Platzhaltersyntax.BLOCK_JE + "}}";
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_OFFEN), T(nameof(R.VF_PRUEF_BLOCK_OFFEN), anfang.Platzhalter.Normalform),
                      Fundort(anfang), T(nameof(R.VF_PRUEF_BLOCK_OFFEN_TUN), ende), anfang.Platzhalter.Normalform);
            }

            private void PruefePaar(Vorlagenfund anfang, Vorlagenfund ende)
            {
                Vorlagenort a = anfang.Ort, b = ende.Ort;
                if (!a.InTabelle && !b.InTabelle) return;
                if (a.InTabelle && b.InTabelle && a.Tabelle == b.Tabelle && a.Zeile == b.Zeile)
                {
                    if (a.Zelle == b.Zelle)
                    {
                        // Ein Block in einer Zelle: Anfang und Ende je in einem eigenen Absatz (wie im Rumpf) —
                        // außer die Zeile hat nur diese Zelle, dann ist sie eine Musterzeile.
                        if (b.ZellenInZeile > 1 && (!IstAllein(anfang) || !IstAllein(ende)))
                            Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_ALLEIN),
                                  T(nameof(R.VF_PRUEF_BLOCK_ALLEIN), anfang.Platzhalter.Normalform), Fundort(anfang),
                                  T(nameof(R.VF_PRUEF_BLOCK_ALLEIN_TUN)), anfang.Platzhalter.Normalform);
                        return;
                    }
                    if (a.Zelle == 1 && b.Zelle == b.ZellenInZeile)
                    {
                        if (a.ZeileVerbunden)
                            Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_VERBUNDEN),
                                  T(nameof(R.VF_PRUEF_BLOCK_VERBUNDEN), anfang.Platzhalter.Normalform), Fundort(anfang),
                                  T(nameof(R.VF_PRUEF_BLOCK_VERBUNDEN_TUN)), anfang.Platzhalter.Normalform);
                        return;
                    }
                }
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_TABELLE), T(nameof(R.VF_PRUEF_BLOCK_TABELLE), anfang.Platzhalter.Normalform),
                      Fundort(anfang), T(nameof(R.VF_PRUEF_BLOCK_TABELLE_TUN)), anfang.Platzhalter.Normalform);
            }

            // ------------------------------------------------------------ Rahmen

            /// <summary>Felder, Kommentare, leere Vorlage, Sprache, Katalogfassung, Gültigkeit.</summary>
            public void PruefeRahmen(Vorlagendurchlauf lauf)
            {
                foreach (Feldanweisung feld in lauf.Feldanweisungen)
                    if (feld.Feldname == "DATE" || feld.Feldname == "TIME")
                        Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_DATUMSFELD), T(nameof(R.VF_PRUEF_DATUMSFELD), feld.Feldname),
                              Fundort(feld.Ort), T(nameof(R.VF_PRUEF_DATUMSFELD_TUN), BERICHTSDATUM));

                if (lauf.Kommentare > 0)
                    Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_KOMMENTARE), T(nameof(R.VF_PRUEF_KOMMENTARE), lauf.Kommentare),
                          T(nameof(R.VF_PRUEF_ORT_KOMMENTARE)), T(nameof(R.VF_PRUEF_KOMMENTARE_TUN)));

                List<Vorlagenfund> gezaehlt = Gezaehlt;
                if (gezaehlt.Count == 0)
                    Melde(Befundstufe.Warnung, nameof(R.VF_PRUEF_OHNE_PLATZHALTER), T(nameof(R.VF_PRUEF_OHNE_PLATZHALTER), SAMMELANKER),
                          Datei, T(nameof(R.VF_PRUEF_OHNE_PLATZHALTER_TUN), SAMMELANKER));

                PruefeSprache();
                PruefeFassung(gezaehlt);
                PruefeGueltigkeit(gezaehlt);
            }

            private void PruefeSprache()
            {
                if (string.IsNullOrWhiteSpace(_sprache)) return;
                string s = _sprache.Trim().ToLowerInvariant();
                bool? vorlageEnglisch = s.StartsWith("en", StringComparison.Ordinal) ? true
                                      : s.StartsWith("de", StringComparison.Ordinal) ? false : (bool?)null;
                if (!vorlageEnglisch.HasValue || vorlageEnglisch.Value == Kontext.Englisch) return;
                _spracheAbweichend = true;
                // BV-Q7 b: kein Anhalten — der Bericht entsteht in der Sprache der Vorlage; die Prüfliste nennt es.
                Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_SPRACHE),
                      T(nameof(R.VF_PRUEF_SPRACHE), Sprachname(vorlageEnglisch.Value), Sprachname(Kontext.Englisch)),
                      T(nameof(R.VF_PRUEF_ORT_EIGENSCHAFTEN)), T(nameof(R.VF_PRUEF_SPRACHE_TUN)));
            }

            private string Sprachname(bool englisch)
            {
                return T(englisch ? nameof(R.VF_PRUEF_SPRACHE_EN) : nameof(R.VF_PRUEF_SPRACHE_DE));
            }

            /// <summary>
            /// Eine ältere Katalogfassung wird NUR gemeldet, wenn ein genutzter Schlüssel seither
            /// Alias ist, und neue Kapitel als Hinweis (Konzept 5.6); eine neuere nur, wenn
            /// Schlüssel unbekannt sind.
            /// </summary>
            private void PruefeFassung(List<Vorlagenfund> gezaehlt)
            {
                if (!_fassung.HasValue) return;
                int fassung = _fassung.Value;
                if (fassung < _katalog.Fassung)
                {
                    var gemeldet = new HashSet<string>(StringComparer.Ordinal);
                    foreach (Vorlagenfund f in gezaehlt)
                    {
                        if (f.Feld == null || f.Platzhalter.Art != Platzhalterart.Feld) continue;
                        string alias = f.Platzhalter.Schluessel;
                        // „Seither Alias“ nur, wenn der Eintrag jünger ist als die Vorlage — ein Alias, den
                        // schon ihre Fassung kannte (bericht.programmversion seit Fassung 1), ist kein Befund.
                        if (alias == f.Feld.Schluessel || f.Feld.Seit <= fassung || !gemeldet.Add(alias)) continue;
                        string alt = "{{" + alias + "}}", neu = "{{" + f.Feld.Schluessel + "}}";
                        Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_FASSUNG_ALT), T(nameof(R.VF_PRUEF_FASSUNG_ALT), fassung, alt, neu),
                              Fundort(f), T(nameof(R.VF_PRUEF_FASSUNG_ALT_TUN), neu, alt), alt, neu);
                    }
                    // Genutzt heißt auch gedeckt: Der Sammelanker führt jedes Kapitel, auch die neuen.
                    HashSet<string> genutzt = Vorlagenfeldkatalog.Gedeckt(
                        gezaehlt.Where(f => f.Feld != null).Select(f => f.Feld.Schluessel), _katalog.Alle, _katalog.Finde);
                    foreach (Vorlagenfeld kapitel in _katalog.Alle.Where(e => e.Art == Vorlagenfeldart.Kapitel && e.Seit > fassung))
                    {
                        if (genutzt.Contains(kapitel.Schluessel)) continue;
                        string marke = "{{" + kapitel.Schluessel + "}}";
                        Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_KAPITEL_NEU), T(nameof(R.VF_PRUEF_KAPITEL_NEU), marke),
                              Datei, T(nameof(R.VF_PRUEF_KAPITEL_NEU_TUN), marke), marke);
                    }
                }
                else if (fassung > _katalog.Fassung && gezaehlt.Any(f => f.Platzhalter.Art == Platzhalterart.Feld && f.Feld == null))
                {
                    Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_FASSUNG_NEU), T(nameof(R.VF_PRUEF_FASSUNG_NEU), fassung, _katalog.Fassung),
                          T(nameof(R.VF_PRUEF_ORT_EIGENSCHAFTEN)), T(nameof(R.VF_PRUEF_FASSUNG_NEU_TUN)));
                }
            }

            /// <summary>Werte der Wirtschaftlichkeit ohne Warnliste (Konzept 4.11).</summary>
            private void PruefeGueltigkeit(List<Vorlagenfund> gezaehlt)
            {
                List<string> bekannte = gezaehlt.Where(f => f.Feld != null).Select(f => f.Feld.Schluessel).ToList();
                bool nutzt = bekannte.Any(k => !Warnlisten.Contains(k) &&
                                               Gueltigkeitsbereiche.Any(b => k.StartsWith(b, StringComparison.Ordinal)));
                bool mitListe = bekannte.Any(k => Warnlisten.Contains(k));
                if (nutzt && !mitListe)
                    Melde(Befundstufe.Warnung, nameof(R.VF_PRUEF_GUELTIGKEIT), T(nameof(R.VF_PRUEF_GUELTIGKEIT)), Datei,
                          T(nameof(R.VF_PRUEF_GUELTIGKEIT_TUN), "{{" + Warnlisten[0] + "}}"));
            }

            // ------------------------------------------------------------ Kapitel (Konzept 5.3, 10.2, 11 Nr. 3)

            /// <summary>
            /// Die Kapitel der Vorlage, nach der Regel der Engine: Je Kapitel gilt die erste gültige Stelle
            /// (allein im Absatz des Rumpfs oder als Inhaltssteuerelement auf Blockebene; getippte
            /// Platzhalter in Dokumentfolge, danach die Steuerelemente) — jede weitere bekommt den Hinweis
            /// „doppelt“. Daraus die Häkchen, die die Vorlage schaltet (<see cref="Pruefbefund.Bausteine"/>),
            /// die Überschrift vor jedem Anker (<see cref="Pruefbefund.Kapitelstellen"/>) und, wenn die
            /// Vorlage den Anhang E führt, die Warnung für jedes Kapitel, auf das seine Checkliste verweist,
            /// das sie aber nicht führt. Eine Vorlage, die Kapitel bewusst weglässt, bekommt sonst keinen
            /// Befund; eine ohne jeden Platzhalter führt über den angehängten Sammelanker alle.
            /// </summary>
            public void PruefeKapitel(WordprocessingDocument doc, Vorlagendurchlauf lauf)
            {
                List<Vorlagenfund> gezaehlt = Gezaehlt;
                var orte = new Dictionary<string, Vorlagenfund>(StringComparer.Ordinal);
                Vorlagenfund sammel = null;
                foreach (Vorlagenfund f in gezaehlt.Where(f => f.Feld?.Art == Vorlagenfeldart.Kapitel && IstKapitelstelle(f)))
                {
                    bool sammelanker = string.Equals(f.Feld.Schluessel, WordVorlagenfueller.SAMMELANKER, StringComparison.Ordinal);
                    Berichtskapitel k = sammelanker ? null : Berichtskapitel.Finde(f.Feld.Schluessel);
                    if (!sammelanker && k == null) continue;
                    if (sammelanker ? sammel == null : !orte.ContainsKey(k.Name))
                    {
                        if (sammelanker) sammel = f;
                        else orte[k.Name] = f;
                        continue;
                    }
                    string marke = "{{" + f.Feld.Schluessel + "}}";
                    Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_KAPITEL_DOPPELT), T(nameof(R.VF_PRUEF_KAPITEL_DOPPELT), marke),
                          Fundort(f), T(nameof(R.VF_PRUEF_KAPITEL_DOPPELT_TUN)), MarkeVon(f));
                }

                bool alle = sammel != null || gezaehlt.Count == 0;
                _bausteine = BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel)
                    .Where(b => alle || Berichtskapitel.Alle.Any(k => k.Baustein == b && orte.ContainsKey(k.Name)))
                    .ToList();

                // Die Überschrift vor jedem Anker — wie die Engine sie im gefüllten Bericht findet.
                MainDocumentPart main = doc.MainDocumentPart;
                WordVorlagenstile stile = main == null ? null : new WordVorlagenstile(main);
                string kopfstil = stile?.Finde(WordVorlagenstile.KAPITELKOPF);
                HashSet<string> ueberschriften = Berichtskapitel.UeberschriftIds(stile);
                var absaetze = new Dictionary<OpenXmlElement, Vorlagenabsatz>();
                foreach (Vorlagenabsatz a in lauf.Absaetze)
                    if (a.Element != null && !absaetze.ContainsKey(a.Element)) absaetze[a.Element] = a;
                Berichtswerte werte = Berichtswerte.Aus(new BerichtsDaten(), null, Kontext.Englisch, null);
                ISet<string> deckblattangaben = Vorlagenfeldkatalog.Deckblattangaben;
                bool deckblatt = gezaehlt.Any(f => f.Ort?.Teil == Vorlagenteilart.Rumpf && f.Feld != null &&
                                                   deckblattangaben.Contains(f.Feld.Schluessel));
                _deckblattAusPlatzhaltern = deckblatt;

                _kapitelstellen = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (Berichtskapitel k in Berichtskapitel.Alle)
                {
                    string text = null;
                    if (orte.TryGetValue(k.Name, out Vorlagenfund f)) text = Kopftext(k, f, kopfstil, ueberschriften, absaetze, werte);
                    else if (alle) text = k.Ueberschrift(Kontext.Englisch);
                    if (text == null && k.Name == Berichtskapitel.DECKBLATT && deckblatt) text = k.Ueberschrift(Kontext.Englisch);
                    _kapitelstellen[k.Stellenschluessel] = text;
                }

                if (_kapitelstellen[Berichtskapitel.ANHANG_E] == null) return;
                foreach (string bezug in AnhangECheckliste.Kapitelbezuege)
                {
                    if (!_kapitelstellen.TryGetValue(bezug, out string stelle) || stelle != null) continue;
                    Berichtskapitel k = Berichtskapitel.Alle.First(x => x.Stellenschluessel == bezug);
                    string marke = "{{" + k.Schluessel + "}}";
                    Melde(Befundstufe.Warnung, nameof(R.VF_PRUEF_ANHANG_E_STELLE),
                          T(nameof(R.VF_PRUEF_ANHANG_E_STELLE), BerichtsKonfiguration.Titel(k.Baustein, Kontext.Englisch)),
                          Datei, T(nameof(R.VF_PRUEF_ANHANG_E_STELLE_TUN), marke), marke);
                }
            }

            /// <summary>Füllt die Engine an dieser Stelle ein Kapitel (Rumpf, keine Zelle, kein Textfeld; allein bzw. Block)?</summary>
            private static bool IstKapitelstelle(Vorlagenfund f)
            {
                Vorlagenort o = f.Ort;
                if (o == null || o.Teil != Vorlagenteilart.Rumpf || o.InTabelle) return false;
                if (f.Quelle == Fundquelle.Text) return IstAllein(f);
                return f.Quelle == Fundquelle.Steuerelement && f.Steuerelement?.Ebene == Steuerelementebene.Block;
            }

            /// <summary>
            /// Die Überschrift vor dem Anker eines einzeln geführten Kapitels: der Kapitelkopf unmittelbar
            /// davor, mit <c>|ohne titel</c> sonst die nächste Überschrift davor — ihr Text, Platzhalter
            /// darin aufgelöst (<c>{{text.kapitel_projekt}}</c>); ohne sie die eigene Überschrift des Bausteins.
            /// </summary>
            private string Kopftext(Berichtskapitel k, Vorlagenfund f, string kopfstil, HashSet<string> ueberschriften,
                                    Dictionary<OpenXmlElement, Vorlagenabsatz> absaetze, Berichtswerte werte)
            {
                OpenXmlElement bezug = f.Quelle == Fundquelle.Steuerelement ? f.Steuerelement?.Element : f.Absatz?.Element;
                OpenXmlElement kopf = Berichtskapitel.KapitelkopfVor(bezug, kopfstil);
                if (kopf == null && f.Platzhalter.Angaben.Any(a => a.Art == Formatangabeart.OhneTitel))
                    kopf = Berichtskapitel.UeberschriftVor(bezug, ueberschriften);
                if (kopf != null && absaetze.TryGetValue(kopf, out Vorlagenabsatz absatz))
                {
                    string text = Vorlagenfeldkatalog.LoeseImText(absatz.Text, werte).Trim();
                    if (text.Length > 0) return text;
                }
                return k.Ueberschrift(Kontext.Englisch);
            }

            // ------------------------------------------------------------ Befund

            private static bool HatKapitel(List<Vorlagenfund> gezaehlt)
            {
                return gezaehlt.Any(f => f.Feld?.Art == Vorlagenfeldart.Kapitel ||
                                         (f.Platzhalter.Art == Platzhalterart.Feld &&
                                          (f.Platzhalter.Schluessel.StartsWith("kapitel.", StringComparison.Ordinal) ||
                                           f.Platzhalter.Schluessel.StartsWith("baustein.", StringComparison.Ordinal))));
            }

            public Pruefbefund Befund(string summe, bool lesbar)
            {
                List<Vorlagenfund> gezaehlt = Gezaehlt;
                List<Vorlagenfund> felder = gezaehlt
                    .Where(f => f.Platzhalter.Art == Platzhalterart.Feld && f.Platzhalter.Schluessel.Length > 0)
                    .ToList();
                List<string> schluessel = felder.Select(f => f.Platzhalter.Schluessel)
                    .Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToList();
                List<string> unbekannte = felder.Where(f => f.Feld == null).Select(f => f.Platzhalter.Schluessel)
                    .Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToList();

                // Die Wirtschaftlichkeit: ein Schlüssel ihrer Bereiche oder ihr Kapitel — direkt oder gedeckt,
                // etwa über den Sammelanker (Konzept 10.2, zweiter Einstieg).
                HashSet<string> gedeckt = Vorlagenfeldkatalog.Gedeckt(
                    felder.Where(f => f.Feld != null).Select(f => f.Feld.Schluessel), _katalog.Alle, _katalog.Finde);
                string wirtschaftskapitel = Berichtskapitel.PRAEFIX_KAPITEL + Berichtskapitel.WIRTSCHAFTLICHKEIT;
                bool wirtschaft = gedeckt.Any(k => string.Equals(k, wirtschaftskapitel, StringComparison.Ordinal) ||
                                                   Wirtschaftsbereiche.Any(b => k.StartsWith(b, StringComparison.Ordinal)));

                return new Pruefbefund(_stufe, _meldungen, _funde.ToList(), gezaehlt.Count, schluessel, unbekannte,
                                       _fassung, _sprache, _spracheAbweichend, HatKapitel(gezaehlt), wirtschaft,
                                       _bausteine ?? new List<string>(), summe, lesbar, _kommentare, _kapitelstellen,
                                       _deckblattAusPlatzhaltern);
            }
        }
    }
}
