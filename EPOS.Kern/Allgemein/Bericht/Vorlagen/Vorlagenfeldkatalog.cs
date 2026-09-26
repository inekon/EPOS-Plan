using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Platzhalterkatalog</b> (Konzept Berichtsvorlagen 5, Anhang A) — die EINE Liste der
    /// Schlüssel, die eine Berichtsvorlage verwenden darf, mit Art, Kontext, Quelle, Format und
    /// Beschreibung. Daten wie <c>EPOS.UI/Bausteine/Menuetabelle.cs</c>: eine statische Tabelle,
    /// keine Datenbank. Der Kern führt keine Orte der Oberfläche.
    ///
    /// <para><b>Katalog v1</b> (Etappe BV-E1): <c>bericht.*</c>, <c>text.*</c>, <c>ersteller.*</c>,
    /// <c>projekt.*</c> handgepflegt; <c>stamm.kennzahl.&lt;k&gt;</c>, <c>kennzahl.&lt;k&gt;.beschriftung</c>
    /// und <c>kennzahl.&lt;k&gt;.einheit</c> aus dem <see cref="KennzahlenKatalog"/> erzeugt — eine neue
    /// Kennzahl ist ohne Pflege ein Platzhalter. <b>Katalog v2</b> (<see cref="KATALOGFASSUNG"/> 2,
    /// Etappe BV-E2): je Kapitel <c>kapitel.&lt;name&gt;</c> mit <see cref="Vorlagenfeld.Deckt"/>, je
    /// Häkchen der Schalter <c>baustein.&lt;name&gt;</c>, je Kapitelkopf der Standardvorlage
    /// <c>text.kapitel_&lt;name&gt;</c> (<see cref="Berichtskapitel"/>) und das Logo
    /// <c>bild.ersteller.logo</c>. Die Fassung steigt mit jeder Etappe, die Einträge hinzufügt; die
    /// Schlüsselliste jeder Fassung ist eingefroren
    /// (<c>EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v&lt;n&gt;.txt</c>), und ein einmal
    /// ausgelieferter Schlüssel bleibt lebendig oder Alias (5.6).</para>
    ///
    /// <para><b>Deckt</b> (Konzept 5.1, 10.2, 12): Ein Kapitel nennt die Einzelschlüssel, deren Inhalt
    /// es schreibt, dazu seinen Schalter und seinen Kapitelkopf; <c>bericht.inhalt</c> nennt alle
    /// Kapitel. Was eine Vorlage damit zeigt, rechnet <see cref="Gedeckt"/>.</para>
    ///
    /// <para><b>Namen.</b> Beschreibungen handgepflegter Einträge stehen in <c>MyResource</c> unter
    /// <see cref="RessourcenName"/> (<c>projekt.kunde</c> → <c>VF_PROJEKT__KUNDE</c>), erzeugte
    /// nehmen ein Muster (<c>VF_MUSTER_*</c>, <c>{0}</c> = Beschriftung der Kennzahl). In Excel heißt
    /// ein Platzhalter <see cref="ExcelName"/> (<c>EPOS.projekt.kunde</c>); die Namen der
    /// Formelmappe sind reserviert (<see cref="ReservierteExcelNamen"/>). Beide Abbildungen sind
    /// eindeutig, weil das Schlüsselmuster nur einzelne Unterstriche erlaubt und nur
    /// Kleinbuchstaben kennt (Konzept 5.5).</para>
    /// </summary>
    public static partial class Vorlagenfeldkatalog
    {
        /// <summary>Die Katalogfassung; sie steigt mit jeder Etappe, die Einträge hinzufügt (Konzept 5.6).</summary>
        public const int KATALOGFASSUNG = 7;

        /// <summary>Die Fassung der Kapitel, Schalter, Kapitelköpfe und des Logos (Etappe BV-E2).</summary>
        private const int FASSUNG_KAPITEL = 2;

        /// <summary>Die Fassung der Blockgrundlage (Etappe BV-E4, Katalog v3).</summary>
        private const int FASSUNG_BLOECKE = 3;

        /// <summary>Das Logo des Erstellers als Bildplatzhalter (Anwenderentscheid BV-E2-1).</summary>
        public const string LOGO = "bild.ersteller.logo";

        /// <summary>Vorsilbe der Beschreibungsressourcen.</summary>
        public const string PRAEFIX_RESSOURCE = "VF_";

        /// <summary>Vorsilbe der Excel-Namen — ohne sie wären <c>CO2</c> oder <c>P1</c> Zellbezüge (Konzept 4.4).</summary>
        public const string PRAEFIX_EXCEL = "EPOS.";

        /// <summary>Musterschlüssel der Stammkennzahlen.</summary>
        public const string MUSTER_STAMM_KENNZAHL = "stamm.kennzahl.<k>";

        /// <summary>Musterschlüssel der Kennzahlbeschriftungen.</summary>
        public const string MUSTER_KENNZAHL_BESCHRIFTUNG = "kennzahl.<k>.beschriftung";

        /// <summary>Musterschlüssel der Kennzahleinheiten.</summary>
        public const string MUSTER_KENNZAHL_EINHEIT = "kennzahl.<k>.einheit";

        /// <summary>Kurzes Datum nach Kultur.</summary>
        private const string FORMAT_DATUM = "d";

        /// <summary>Langes Datum nach Kultur.</summary>
        private const string FORMAT_DATUM_LANG = "D";

        /// <summary>Kurzes Datum mit Uhrzeit nach Kultur.</summary>
        private const string FORMAT_DATUM_ZEIT = "g";

        /// <summary>Zahl ohne Katalogformat.</summary>
        private const string FORMAT_ZAHL = "N0";

        /// <summary>
        /// Die Texte, die die Auflösung in den Bericht schreibt — Festtexte, Rückfalltexte, Gründe
        /// (Konzept 4.10: zweisprachig in <c>MyResource</c>; die Katalogwache prüft sie).
        /// </summary>
        public static readonly IReadOnlyList<string> Textschluessel = new[]
        {
            nameof(R.BV_TEXT_SEITE), nameof(R.BV_TEXT_KUNDE), nameof(R.BV_TEXT_BEARBEITER),
            nameof(R.BV_TEXT_ERSTELLER), nameof(R.BV_TEXT_VARIANTEN), nameof(R.BV_TEXT_DATUM),
            nameof(R.BV_TEXT_ERSTELLT_MIT), nameof(R.BV_NUR_STAMMPROJEKT),
            nameof(R.BV_GRUND_KEIN_STAMM), nameof(R.BV_GRUND_KEINE_PROJEKTDATEN), nameof(R.BV_GRUND_KEIN_ERGEBNIS),
            nameof(R.BV_GRUND_LAUF_FEHLGESCHLAGEN), nameof(R.BV_GRUND_NICHT_VERFUEGBAR),
            nameof(R.BV_GRUND_KEIN_VDI6007), nameof(R.BV_GRUND_AUSNAHME), nameof(R.BV_GRUND_KEIN_LOGO),
            // Katalog v3 (BV-E4)
            nameof(R.BV_GRUND_KEIN_STAND), nameof(R.BV_GRUND_KEIN_PAAR), nameof(R.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT),
            nameof(R.BV_GRUND_ZEILE_FEHLT), nameof(R.BV_GRUND_REFERENZ), nameof(R.BV_GRUND_NUR_STAMM),
            nameof(R.BV_GRUND_IST_STAMM), nameof(R.BV_GRUND_KEIN_DELTA), nameof(R.BV_GRUND_KEIN_GEBAEUDE),
            nameof(R.BV_GRUND_KEIN_RISIKO), nameof(R.BV_GRUND_ZU_WENIG_STAENDE),
            // Katalog v4 (BV-E5, Tabellen)
            nameof(R.BV_GRUND_TABELLE_LEER),
            // Katalog v4 (BV-E5): Bilder
            nameof(R.BV_GRUND_KEINE_ZEITREIHEN), nameof(R.BV_GRUND_BILD_OHNE_DATEN), nameof(R.BV_GRUND_VERLAUF_NICHT_ERHOBEN),
            nameof(R.BV_GRUND_VERLAUF_ENTFAELLT), nameof(R.BV_GRUND_KEIN_VERLAUF), nameof(R.BV_GRUND_KEINE_LEITVERSION),
        };

        /// <summary>Die Beschreibungsmuster der erzeugten Einträge (<c>{0}</c> = Beschriftung der Kennzahl).</summary>
        public static readonly IReadOnlyList<string> Musterschluessel = new[]
        {
            nameof(R.VF_MUSTER_STAMM_KENNZAHL), nameof(R.VF_MUSTER_KENNZAHL_BESCHRIFTUNG), nameof(R.VF_MUSTER_KENNZAHL_EINHEIT),
            // Katalog v3 (BV-E4)
            nameof(R.VF_MUSTER_STAND_KENNZAHL), nameof(R.VF_MUSTER_STAND_DELTA), nameof(R.VF_MUSTER_STAND_DELTA_PROZENT),
            nameof(R.VF_MUSTER_VERGLEICH_SPANNE), nameof(R.VF_MUSTER_VERGLEICH_MINIMUM), nameof(R.VF_MUSTER_VERGLEICH_MAXIMUM),
            nameof(R.VF_MUSTER_STAMM_WIRTSCHAFT), nameof(R.VF_MUSTER_STAND_WIRTSCHAFT),
            nameof(R.VF_MUSTER_STAND_WIRTSCHAFT_GRUND), nameof(R.VF_MUSTER_BESTE_WIRTSCHAFT),
            nameof(R.VF_MUSTER_STAND_A), nameof(R.VF_MUSTER_STAND_B), nameof(R.VF_MUSTER_WIRTSCHAFT_PARAMETER),
            nameof(R.VF_MUSTER_SZENARIO_NAME), nameof(R.VF_MUSTER_SZENARIO_ANNAHMEN), nameof(R.VF_MUSTER_SZENARIO_TRAEGERPREISE),
            // Katalog v4 (BV-E5, Tabellen)
            nameof(R.VF_MUSTER_TABELLE_KENNDATEN), nameof(R.VF_MUSTER_TABELLE_VERGLEICH), nameof(R.VF_MUSTER_HAT_TABELLE),
            // Katalog v4 (BV-E5)
            nameof(R.VF_MUSTER_BILD_VERGLEICH_BALKEN), nameof(R.VF_MUSTER_HAT_BILD_VERGLEICH_BALKEN),
        };

        /// <summary>
        /// Die Namen der Formelmappe (<c>ExcelFormelmappe</c>: Zins, Zeitraum, Preissteigerungen,
        /// Risiko, je Szenario) — Ausgabe, nicht füllbar. Kein Platzhaltername darf auf sie fallen.
        /// </summary>
        public static readonly IReadOnlyList<string> ReservierteExcelNamen = BildeReservierteExcelNamen();

        private static readonly List<Vorlagenfeld> _alle;
        private static readonly Dictionary<string, Vorlagenfeld> _index;
        private static readonly Dictionary<string, Kennzahl> _kennzahlen;

        static Vorlagenfeldkatalog()
        {
            List<Kennzahl> kennzahlen = KennzahlenKatalog.Alle();
            _kennzahlen = new Dictionary<string, Kennzahl>(StringComparer.Ordinal);
            foreach (Kennzahl k in kennzahlen) _kennzahlen[k.Schluessel] = k;

            // Katalog v4 (BV-E5): Strukturtabellen, Mustertabelle und Schalter je Tabelle, die Bildplatzhalter und
            // ihre Schalter — vorab gebaut, weil die Kapitel sie in ihrem Deckt führen (Anwenderentscheid BV-E5-3);
            // im Katalog stehen sie weiter nach der Paarsicht, die Tabellen und Bilder je Stand haben keinen
            // Zwilling stand.a/b.
            List<Vorlagenfeld> tabellen = Tabellen().ToList();
            List<Vorlagenfeld> bilder = Bilder(kennzahlen).ToList();

            _alle = new List<Vorlagenfeld>(Handgepflegt());
            _alle.AddRange(Kapitel(kennzahlen, tabellen.Concat(bilder).ToList()));
            _alle.AddRange(Blockgrundlage());
            _alle.AddRange(Erzeugte(kennzahlen));
            // Katalog v3 (BV-E4): Standwerte, Wirtschaftlichkeit, Gruppe, Gebäude, Datenschalter — und je
            // Eintrag des Kontexts Stand sein Zwilling in der Paarsicht (stand.a.*, stand.b.*).
            _alle.AddRange(Standwerte(kennzahlen));
            _alle.AddRange(Paarsicht(_alle).ToList());
            _alle.AddRange(tabellen);
            _alle.AddRange(bilder);
            // Katalog v5 (BV-E7): die Blattmarken der Excel-Vorlage.
            _alle.AddRange(Blaetter());

            // Erster Eintrag gewinnt; Doppelungen meldet die Katalogwache, statt hier den
            // Typinitialisierer — und mit ihm jeden Bericht — scheitern zu lassen.
            _index = new Dictionary<string, Vorlagenfeld>(StringComparer.Ordinal);
            foreach (Vorlagenfeld f in _alle)
            {
                _index.TryAdd(f.Schluessel, f);
                foreach (string alias in f.Aliasse) _index.TryAdd(alias, f);
            }
        }

        /// <summary>Die laufende Katalogfassung (<see cref="KATALOGFASSUNG"/>).</summary>
        public static int Katalogfassung { get { return KATALOGFASSUNG; } }

        /// <summary>
        /// Die letzte Katalogfassung, die Einträge mit Ausgabe Word brachte (Katalog v4; v5 brachte nur die Blattmarken der
        /// Excel-Vorlage, BV-E7) — die Fassung, die die mitgelieferten Word-Vorlagen in <c>custom.xml</c> tragen. Eine
        /// Word-Vorlage kann keinen Schlüssel einer reinen Excel-Fassung nutzen; sie braucht darum keine neue Fassung.
        /// </summary>
        public static int KatalogfassungWord
        {
            get { return _alle.Where(f => (f.Ausgaben & Vorlagenausgabe.Word) != 0).Max(f => f.Seit); }
        }

        /// <summary>Alle Einträge in Katalogreihenfolge: handgepflegt, dann erzeugt.</summary>
        public static IReadOnlyList<Vorlagenfeld> Alle { get { return _alle; } }

        /// <summary>
        /// Der Eintrag zu einem Schlüssel oder Alias; der Schlüssel wird vorher normiert
        /// (<see cref="Platzhaltersyntax.NormiereSchluessel"/>). <c>null</c> = unbekannt. Ein Alias
        /// führt auf den Eintrag, dessen <see cref="Vorlagenfeld.Schluessel"/> sich dann vom
        /// gesuchten unterscheidet.
        /// </summary>
        public static Vorlagenfeld Finde(string schluessel)
        {
            string normiert = Platzhaltersyntax.NormiereSchluessel(schluessel);
            return normiert.Length > 0 && _index.TryGetValue(normiert, out Vorlagenfeld feld) ? feld : null;
        }

        /// <summary>Der Ressourcenschlüssel der Beschreibung: <c>VF_</c> + Schlüssel in Großbuchstaben,
        /// Punkt → <c>__</c> (<c>projekt.kunde</c> → <c>VF_PROJEKT__KUNDE</c>).</summary>
        public static string RessourcenName(string schluessel)
        {
            return PRAEFIX_RESSOURCE + (schluessel ?? "").ToUpperInvariant().Replace(".", "__");
        }

        /// <summary>
        /// Der Name der Excel-Tabelle eines Tabellenschlüssels (Konzept 4.4, 7.3): <c>EPOS_</c> und der Schlüssel mit <c>__</c>
        /// für den Punkt — <c>tabelle.varianten</c> → <c>EPOS_tabelle__varianten</c>.
        /// </summary>
        public static string ExcelTabellenname(string schluessel)
        {
            return Excelbereiche.Tabellenname(schluessel);
        }

        /// <summary>Der mappenweite Excel-Name eines Platzhalters: <c>EPOS.</c> + Schlüssel
        /// (Messprobe 1 von BV-E0: Punkte überstehen Speichern und Laden).</summary>
        public static string ExcelName(string schluessel)
        {
            return PRAEFIX_EXCEL + (schluessel ?? "");
        }

        /// <summary>
        /// Die Beschreibung eines Eintrags in der gewählten Sprache: handgepflegt aus der eigenen
        /// Ressource, erzeugt aus dem Muster mit der Beschriftung der Kennzahl als <c>{0}</c>.
        /// </summary>
        public static string Beschreibung(Vorlagenfeld feld, bool englisch)
        {
            if (feld == null) return "";
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            if (feld.Ableitung == null) return Ressource(feld.BeschreibungId, kultur);

            string muster = Ressource(feld.Ableitung.MusterId, kultur);
            string parameter = feld.Ableitung.Bezeichnung != null ? feld.Ableitung.Bezeichnung(kultur)
                : _kennzahlen.TryGetValue(feld.Ableitung.Parameter, out Kennzahl k) ? k.Label(englisch)
                : feld.Ableitung.Parameter;
            string zusatz = feld.Ableitung.Zusatz != null ? feld.Ableitung.Zusatz(kultur) : "";
            try { return string.Format(kultur, muster, parameter, zusatz); }
            catch (FormatException) { return muster; }
        }

        // =====================================================================
        //  Auflösen
        // =====================================================================

        /// <summary>
        /// Löst einen Platzhalter auf dem Wertesatz auf (Konzept 4.8 bis 4.10). Formatangaben, die
        /// nicht zur Art passen oder unbekannt sind, übergeht die Auflösung (der Prüfer meldet sie)
        /// und nimmt das Katalogformat. Ohne Wert steht der <see cref="Vorlagenfeld.Leerwert"/> da
        /// — nie 0. Wirft die Quelle, wird daraus ein Leerwert mit Grund; der Bericht bricht an
        /// einem Einzelwert nicht ab.
        /// </summary>
        public static Platzhalterwert Loese(Vorlagenfeld feld, Berichtswerte werte, IReadOnlyList<Formatangabe> angaben)
        {
            if (feld == null) throw new ArgumentNullException(nameof(feld));
            if (werte == null) throw new ArgumentNullException(nameof(werte));

            List<Formatangabe> gueltig = (angaben ?? Array.Empty<Formatangabe>())
                .Where(a => a != null && a.PasstZu(feld.Art))
                .ToList();
            try
            {
                object roh = feld.Quelle != null ? feld.Quelle(werte) : null;
                return Forme(feld, werte, gueltig, roh);
            }
            catch (Exception ex)
            {
                return LeerMit(feld, gueltig, werte.Text(nameof(R.BV_GRUND_AUSNAHME)), ex.Message);
            }
        }

        /// <summary>
        /// Löst eine erkannte Marke auf: nur ein <see cref="Platzhalterart.Feld"/> mit bekanntem
        /// Schlüssel (auch Alias); sonst <c>null</c> — Blöcke füllt die Engine ab BV-E4, einen
        /// unbekannten Schlüssel lässt sie stehen und meldet ihn.
        /// </summary>
        public static Platzhalterwert Loese(Platzhalter platzhalter, Berichtswerte werte)
        {
            if (platzhalter == null || platzhalter.Art != Platzhalterart.Feld) return null;
            Vorlagenfeld feld = Finde(platzhalter.Schluessel);
            return feld == null ? null : Loese(feld, werte, platzhalter.Angaben);
        }

        private static Platzhalterwert Forme(Vorlagenfeld feld, Berichtswerte w, List<Formatangabe> angaben, object roh)
        {
            if (feld.Art == Vorlagenfeldart.Tabelle) return FormeTabelle(feld, w, roh);
            if (roh is Leergrund leer) return LeerMit(feld, angaben, leer.Grund, null);
            if (roh == null) return LeerMit(feld, angaben, null, null);

            switch (feld.Art)
            {
                case Vorlagenfeldart.Zahl: return FormeZahl(feld, w, angaben, roh);
                case Vorlagenfeldart.Datum: return FormeDatum(feld, w, angaben, roh);
                case Vorlagenfeldart.Text: return FormeText(feld, w, angaben, roh);
                case Vorlagenfeldart.Liste:
                    {
                        List<string> zeilen = Folge(roh, w.Kultur);
                        return zeilen.Count == 0 ? LeerMit(feld, angaben, null, null) : Platzhalterwert.MitZeilen(zeilen);
                    }
                case Vorlagenfeldart.Kapitel:
                    {
                        List<string> kapitel = Folge(roh, w.Kultur);
                        return kapitel.Count == 0 ? LeerMit(feld, angaben, null, null) : Platzhalterwert.MitKapiteln(kapitel);
                    }
                case Vorlagenfeldart.Schalter:
                    return roh is bool schalter
                        ? Platzhalterwert.MitSchalter(schalter)
                        : LeerMit(feld, angaben, w.Text(nameof(R.BV_GRUND_NICHT_VERFUEGBAR)), null);
                case Vorlagenfeldart.Bild:
                    if (roh is Bildinhalt bild) return Platzhalterwert.MitBild(bild);
                    if (roh is Diagrammbild diagramm) return Platzhalterwert.MitDiagramm(diagramm);
                    return LeerMit(feld, angaben, w.Text(nameof(R.BV_GRUND_NICHT_VERFUEGBAR)), null);
                case Vorlagenfeldart.Blatt:
                    // BV-E7: der heutige Name des Blattes — gefüllt wird die Marke durch das erzeugte Blatt.
                    {
                        string name = roh as string ?? Convert.ToString(roh, w.Kultur) ?? "";
                        return string.IsNullOrWhiteSpace(name) ? LeerMit(feld, angaben, null, null)
                                                               : Platzhalterwert.MitText(Vorlagenfeldart.Blatt, name);
                    }
                default:
                    return LeerMit(feld, angaben, w.Text(nameof(R.BV_GRUND_NICHT_VERFUEGBAR)), null);
            }
        }

        /// <summary>
        /// Eine Tabelle (BV-E5): mit Zeilen die <see cref="Berichtstabelle"/>, ohne Zeilen der Leerwert „—“ samt Grund —
        /// die Engine schreibt daraus den Leertext mit Grund („— (keine Zeilen für diese Tabelle)“, Konzept 4.10); eine
        /// leere Stelle im Bericht wäre keine Aussage.
        /// </summary>
        private static Platzhalterwert FormeTabelle(Vorlagenfeld feld, Berichtswerte w, object roh)
        {
            string grund = roh is Leergrund leer ? leer.Grund
                         : roh is Berichtstabelle t ? (t.IstLeer ? t.Leergrund ?? w.Text(nameof(R.BV_GRUND_TABELLE_LEER)) : null)
                         : w.Text(nameof(R.BV_GRUND_NICHT_VERFUEGBAR));
            if (grund == null) return Platzhalterwert.MitTabelle((Berichtstabelle)roh);
            return Platzhalterwert.Leer(Vorlagenfeldart.Tabelle, feld.Leerwert, grund, null);
        }

        private static Platzhalterwert FormeZahl(Vorlagenfeld feld, Berichtswerte w, List<Formatangabe> angaben, object roh)
        {
            double wert = roh is double d ? d : Convert.ToDouble(roh, CultureInfo.InvariantCulture);
            if (double.IsNaN(wert) || double.IsInfinity(wert))
                return LeerMit(feld, angaben, w.Text(nameof(R.BV_GRUND_NICHT_VERFUEGBAR)), null);

            string format = string.IsNullOrEmpty(feld.Format) ? FORMAT_ZAHL : feld.Format;
            bool mitEinheit = true;
            foreach (Formatangabe a in angaben)
            {
                if (a.Art == Formatangabeart.Stellen && a.Zahl.HasValue)
                    format = "N" + a.Zahl.Value.ToString(CultureInfo.InvariantCulture);
                else if (a.Art == Formatangabeart.OhneEinheit) mitEinheit = false;
                else if (a.Art == Formatangabeart.MitEinheit) mitEinheit = true;
            }

            string text = wert.ToString(format, w.Kultur);
            if (mitEinheit && HatEinheit(feld.Einheit)) text += " " + feld.Einheit;
            return Platzhalterwert.MitZahl(text, wert);
        }

        private static Platzhalterwert FormeDatum(Vorlagenfeld feld, Berichtswerte w, List<Formatangabe> angaben, object roh)
        {
            DateTime datum = roh is DateTime dt ? dt
                           : roh is DateTimeOffset dto ? dto.DateTime
                           : Convert.ToDateTime(roh, CultureInfo.InvariantCulture);
            if (datum == DateTime.MinValue) return LeerMit(feld, angaben, null, null);

            string format = string.IsNullOrEmpty(feld.Format) ? FORMAT_DATUM : feld.Format;
            foreach (Formatangabe a in angaben)
            {
                if (a.Art == Formatangabeart.Datum) format = FORMAT_DATUM;
                else if (a.Art == Formatangabeart.DatumLang) format = FORMAT_DATUM_LANG;
                else if (a.Art == Formatangabeart.DatumMitZeit) format = FORMAT_DATUM_ZEIT;
            }
            return Platzhalterwert.MitDatum(datum.ToString(format, w.Kultur), datum);
        }

        private static Platzhalterwert FormeText(Vorlagenfeld feld, Berichtswerte w, List<Formatangabe> angaben, object roh)
        {
            string text = roh as string ?? Convert.ToString(roh, w.Kultur) ?? "";
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            if (string.IsNullOrWhiteSpace(text)) return LeerMit(feld, angaben, null, null);
            return Platzhalterwert.MitText(Vorlagenfeldart.Text, text);
        }

        /// <summary>Der Leerwert des Eintrags nach den Angaben: <c>|leer statt strich</c> leert ihn,
        /// <c>|mit grund</c> hängt den Grund an („— (Lauf fehlgeschlagen)“).</summary>
        private static Platzhalterwert LeerMit(Vorlagenfeld feld, List<Formatangabe> angaben, string grund, string ausnahme)
        {
            bool leerStattStrich = angaben.Any(a => a.Art == Formatangabeart.LeerStattStrich);
            bool mitGrund = angaben.Any(a => a.Art == Formatangabeart.MitGrund);

            string text = leerStattStrich ? "" : (feld.Leerwert ?? "");
            if (mitGrund && !string.IsNullOrEmpty(grund))
                text = text.Length > 0 ? text + " (" + grund + ")" : "(" + grund + ")";
            return Platzhalterwert.Leer(feld.Art, text, string.IsNullOrEmpty(grund) ? null : grund, ausnahme);
        }

        /// <summary>Eine Folge von Einträgen (Liste, Kapitel) ohne leere; Zeilenenden als <c>\n</c>.</summary>
        private static List<string> Folge(object roh, CultureInfo kultur)
        {
            IEnumerable<object> folge = roh is string einzeln ? new object[] { einzeln }
                                      : roh is IEnumerable viele ? viele.Cast<object>()
                                      : new[] { roh };
            return folge
                .Select(o => (o as string ?? Convert.ToString(o, kultur) ?? "").Replace("\r\n", "\n").Replace('\r', '\n'))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        /// <summary>Wird eine Einheit angehängt? Nicht bei keiner und nicht beim Strich der
        /// dimensionslosen Kennzahlen (<c>eff.jaz</c> trägt „–“).</summary>
        private static bool HatEinheit(string einheit)
        {
            if (string.IsNullOrWhiteSpace(einheit)) return false;
            string e = einheit.Trim();
            return e != "–" && e != "-" && e != Vorlagenfeld.STRICH;
        }

        private static string Ressource(string schluessel, CultureInfo kultur)
        {
            if (string.IsNullOrEmpty(schluessel)) return "";
            try { return R.ResourceManager.GetString(schluessel, kultur) ?? ""; }
            catch { return ""; }
        }

        // =====================================================================
        //  Die Tabelle — handgepflegte Einträge (Anhang A, Etappe E1)
        // =====================================================================

        private static IEnumerable<Vorlagenfeld> Handgepflegt()
        {
            return new[]
            {
                // ---------------- bericht.* — Kontext Bericht ----------------
                new Vorlagenfeld("bericht.titel", Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht,
                    w => w.Daten.Stammprojektname),
                new Vorlagenfeld("bericht.untertitel", Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht,
                    w => BerichtTexte.T(DeckblattBaustein.UNTERTITEL, w.Englisch)),
                new Vorlagenfeld("bericht.datum", Vorlagenfeldart.Datum, Vorlagenfeldkontext.Bericht,
                    w => w.Daten.ErstelltAm) { Format = FORMAT_DATUM },
                new Vorlagenfeld("bericht.varianten.liste", Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht,
                    VariantenListe),
                new Vorlagenfeld("bericht.varianten.anzahl", Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Bericht,
                    w => (double)w.Varianten.Count) { Format = FORMAT_ZAHL },
                new Vorlagenfeld("bericht.gebaeudemodell.ausweis", Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht,
                    Gebaeudemodellausweis),
                new Vorlagenfeld("bericht.emissionsmodus", Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht,
                    w => EmissionsAusweis.Groesse(w.Emissionsmodus, w.Englisch)),
                new Vorlagenfeld("bericht.warnungen", Vorlagenfeldart.Liste, Vorlagenfeldkontext.Bericht,
                    w => w.Daten.Warnungen),
                new Vorlagenfeld("bericht.inhalt", Vorlagenfeldart.Kapitel, Vorlagenfeldkontext.Bericht,
                    w => w.AktiveKapitel)
                {
                    // Der Sammelanker deckt jedes Kapitel; einzeln geführte setzt er nicht noch einmal ein.
                    Deckt = Berichtskapitel.Alle.Select(k => k.Schluessel).ToArray(),
                    // BV-E3: der Bedarf aller Kapitel — im Lauf nur der angehakten (Berichtsbedarf.AusVorlage).
                    Bedarf = Berichtskapitel.Alle.Aggregate(Vorlagenbedarf.Keiner, (b, k) => b | k.Bedarf),
                },

                // ---------------- text.* — Festtexte der Standardvorlage (4.9) ----------------
                Festtext("text.seite", nameof(R.BV_TEXT_SEITE)),
                Festtext("text.kunde", nameof(R.BV_TEXT_KUNDE)),
                Festtext("text.bearbeiter", nameof(R.BV_TEXT_BEARBEITER)),
                Festtext("text.ersteller", nameof(R.BV_TEXT_ERSTELLER)),
                Festtext("text.varianten", nameof(R.BV_TEXT_VARIANTEN)),
                Festtext("text.datum", nameof(R.BV_TEXT_DATUM)),
                Festtext("text.erstellt_mit", nameof(R.BV_TEXT_ERSTELLT_MIT)),

                // ---------------- ersteller.* — Kontext Installation (BV-Q8) ----------------
                new Vorlagenfeld("ersteller.firma", Vorlagenfeldart.Text, Vorlagenfeldkontext.Installation,
                    w => w.Ersteller.Firma) { Leerwert = "" },
                new Vorlagenfeld("ersteller.programm", Vorlagenfeldart.Text, Vorlagenfeldkontext.Installation,
                    w => w.Ersteller.Programm),
                new Vorlagenfeld("ersteller.version", Vorlagenfeldart.Text, Vorlagenfeldkontext.Installation,
                    w => w.Ersteller.Version) { Aliasse = new[] { "bericht.programmversion" } },

                // ---------------- projekt.* — Stammdaten des Stammprojekts ----------------
                new Vorlagenfeld("projekt.name", Vorlagenfeldart.Text, Vorlagenfeldkontext.Stamm,
                    ProjektName),
                new Vorlagenfeld("projekt.kunde", Vorlagenfeldart.Text, Vorlagenfeldkontext.Stamm,
                    w => ProjektFeld(w, p => p.m_szKunde)) { Leerwert = "" },
                new Vorlagenfeld("projekt.bearbeiter", Vorlagenfeldart.Text, Vorlagenfeldkontext.Stamm,
                    w => ProjektFeld(w, p => p.m_szBearbeiter)) { Leerwert = "" },
                new Vorlagenfeld("projekt.beschreibung", Vorlagenfeldart.Text, Vorlagenfeldkontext.Stamm,
                    w => ProjektFeld(w, p => p.m_szBeschreibung)) { Leerwert = "" },
                new Vorlagenfeld("projekt.klimaregion", Vorlagenfeldart.Text, Vorlagenfeldkontext.Stamm,
                    Klimaregion),
                new Vorlagenfeld("projekt.angelegt", Vorlagenfeldart.Datum, Vorlagenfeldkontext.Stamm,
                    w => ProjektFeld(w, p => p.m_Erstelldatum)) { Format = FORMAT_DATUM },
                new Vorlagenfeld("projekt.geaendert", Vorlagenfeldart.Datum, Vorlagenfeldkontext.Stamm,
                    w => ProjektFeld(w, p => p.m_Aenderungsdatum)) { Format = FORMAT_DATUM },
                new Vorlagenfeld("projekt.simulationsstand", Vorlagenfeldart.Datum, Vorlagenfeldkontext.Stamm,
                    Simulationsstand) { Format = FORMAT_DATUM_ZEIT },
            };
        }

        // =====================================================================
        //  Katalog v2 — Kapitel, Schalter, Kapitelköpfe, Logo (Etappe BV-E2)
        // =====================================================================

        /// <summary>
        /// Die Einträge der Fassung 2, handgepflegt (je eine eigene Beschreibung) und aus
        /// <see cref="Berichtskapitel.Alle"/> gebildet: je Kapitel <c>kapitel.&lt;name&gt;</c> (Art
        /// Kapitel, nur Word) mit seinem <see cref="Vorlagenfeld.Deckt"/>, je Häkchen der Schalter
        /// <c>baustein.&lt;name&gt;</c> (wirkt ab BV-E4 in <c>{{#wenn}}</c>), je Kapitelkopf der
        /// Festtext <c>text.kapitel_&lt;name&gt;</c> (die eigene Überschrift des Kapitels in der
        /// Berichtssprache) und das Logo <see cref="LOGO"/>.
        /// </summary>
        private static IEnumerable<Vorlagenfeld> Kapitel(List<Kennzahl> kennzahlen, List<Vorlagenfeld> tabellenUndBilder)
        {
            foreach (Berichtskapitel k in Berichtskapitel.Alle)
            {
                Berichtskapitel kapitel = k;
                yield return new Vorlagenfeld(k.Schluessel, Vorlagenfeldart.Kapitel, Vorlagenfeldkontext.Bericht,
                    w => kapitel.IstAktiv(w.Konfiguration) ? new[] { kapitel.Name } : Array.Empty<string>())
                {
                    Seit = FASSUNG_KAPITEL,
                    Deckt = DecktVon(k, kennzahlen, tabellenUndBilder),
                    // BV-E3: das Kapitel trägt den Bedarf seines Bausteins (Berichtskapitel.Bedarf).
                    Bedarf = k.Bedarf,
                };
            }
            foreach (Berichtskapitel k in Berichtskapitel.Alle.Where(k => k.Schalter != null))
            {
                Berichtskapitel kapitel = k;
                yield return new Vorlagenfeld(k.Schalter, Vorlagenfeldart.Schalter, Vorlagenfeldkontext.Bericht,
                    w => kapitel.IstAktiv(w.Konfiguration))
                {
                    Seit = FASSUNG_KAPITEL,
                };
            }
            foreach (Berichtskapitel k in Berichtskapitel.Alle.Where(k => k.Kopfschluessel != null))
            {
                Berichtskapitel kapitel = k;
                yield return new Vorlagenfeld(k.Kopfschluessel, Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht,
                    w => kapitel.Ueberschrift(w.Englisch))
                {
                    Seit = FASSUNG_KAPITEL,
                };
            }
            yield return new Vorlagenfeld(LOGO, Vorlagenfeldart.Bild, Vorlagenfeldkontext.Installation, Logo)
            {
                Seit = FASSUNG_KAPITEL,
                Ausgaben = Vorlagenausgabe.Word,
            };
        }

        // =====================================================================
        //  Blockgrundlage (Katalog v3): die vier Einträge, auf denen Blöcke und Bedingungen
        //  (Engine, Prüfer) aufsetzen. Die übrigen stand.*, vergleich.*, wirtschaft.*,
        //  gebaeude.* und hat.* stehen in Vorlagenfeldkatalog.Standwerte.cs.
        // =====================================================================

        /// <summary>
        /// Die Blockgrundlage (Konzept 4.5, 4.7, 4.11, Anhang A): <c>stand.anzeige</c> — der Name des
        /// laufenden Stands, beim Stamm der Stammprojektname statt „Stamm“; <c>stand.ist_stamm</c> — ist
        /// der laufende Stand der Stamm; <c>gebaeude.name</c> — der Name des laufenden Gebäudes;
        /// <c>hat.varianten</c> — trägt der Bericht mindestens eine Variante.
        /// </summary>
        private static IEnumerable<Vorlagenfeld> Blockgrundlage()
        {
            yield return new Vorlagenfeld("stand.anzeige", Vorlagenfeldart.Text, Vorlagenfeldkontext.Stand, StandAnzeige)
            {
                Seit = FASSUNG_BLOECKE,
            };
            yield return new Vorlagenfeld("stand.ist_stamm", Vorlagenfeldart.Schalter, Vorlagenfeldkontext.Stand,
                w => w.LaufenderStand == null ? null : (object)w.LaufenderStand.IstStamm)
            {
                Seit = FASSUNG_BLOECKE,
            };
            yield return new Vorlagenfeld("gebaeude.name", Vorlagenfeldart.Text, Vorlagenfeldkontext.Gebaeude,
                w => w.LaufendesGebaeude == null ? null : ProjektDetails.S(w.LaufendesGebaeude, "Gebaeudename"))
            {
                Seit = FASSUNG_BLOECKE,
            };
            yield return new Vorlagenfeld("hat.varianten", Vorlagenfeldart.Schalter, Vorlagenfeldkontext.Bericht,
                w => w.Varianten.Count > 0)
            {
                Seit = FASSUNG_BLOECKE,
            };
        }

        /// <summary>
        /// Der Anzeigename des laufenden Stands wie in der Variantenliste (<see cref="VariantenDaten.Anzeige"/>);
        /// beim Stamm der Stammprojektname statt „Stamm“ (Konzept 4.5, BD:403-406).
        /// </summary>
        private static object StandAnzeige(Berichtswerte w)
        {
            VariantenDaten s = w.LaufenderStand;
            if (s == null) return null;
            if (!s.IstStamm) return s.Anzeige;
            string name = s.Projekt?.m_szProjektname;
            if (string.IsNullOrWhiteSpace(name)) name = s.Projektname;
            if (string.IsNullOrWhiteSpace(name)) name = w.Daten.Stammprojektname;
            return string.IsNullOrWhiteSpace(name) ? s.Anzeige : name;
        }

        /// <summary>
        /// Was ein Kapitel deckt (Konzept 5.1, 12): die Einzelschlüssel, deren Inhalt sein Baustein
        /// schreibt, dazu sein Kapitelkopf und sein Schalter. Das Deckblatt deckt Titel, Untertitel,
        /// Kunde, Bearbeiter, Varianten, Datum, Ausweis des Gebäudemodells und die Erstellerangaben;
        /// die Projektbeschreibung alle <c>projekt.*</c>; Ergebnisse und Vergleich die Kennzahlen samt
        /// Beschriftung und Einheit, der Vergleich dazu Emissionsmodus und Zahl der Varianten; der
        /// Anhang die Warnungen des Laufs.
        /// <para><b>Katalog v4</b> (Anwenderentscheid BV-E5-3): dazu die Strukturtabellen und Bilder, die der Baustein
        /// des Kapitels schreibt, je mit ihrem Schalter <c>hat.tabelle.*</c> bzw. <c>hat.bild.*</c>
        /// (<see cref="KapitelDerTabelleOderDesBildes"/>).</para>
        /// </summary>
        private static string[] DecktVon(Berichtskapitel k, List<Kennzahl> kennzahlen, List<Vorlagenfeld> tabellenUndBilder)
        {
            var deckt = new List<string>();
            IEnumerable<string> Kennzahlen() =>
                kennzahlen.Select(z => "stamm.kennzahl." + z.Schluessel)
                          .Concat(kennzahlen.SelectMany(z => new[] { "kennzahl." + z.Schluessel + ".beschriftung",
                                                                      "kennzahl." + z.Schluessel + ".einheit" }));
            switch (k.Name)
            {
                case Berichtskapitel.DECKBLATT:
                    deckt.AddRange(new[]
                    {
                        "bericht.titel", "bericht.untertitel", "projekt.kunde", "projekt.bearbeiter", "bericht.varianten.liste",
                        "bericht.datum", "bericht.gebaeudemodell.ausweis", "ersteller.firma", "ersteller.programm",
                        "ersteller.version",
                    });
                    break;
                case Berichtskapitel.PROJEKT:
                    deckt.AddRange(new[]
                    {
                        "projekt.name", "projekt.kunde", "projekt.bearbeiter", "projekt.beschreibung", "projekt.klimaregion",
                        "projekt.angelegt", "projekt.geaendert", "projekt.simulationsstand",
                    });
                    break;
                case Berichtskapitel.ERGEBNISSE:
                    deckt.AddRange(Kennzahlen());
                    break;
                case Berichtskapitel.VERGLEICH:
                    deckt.AddRange(Kennzahlen());
                    deckt.Add("bericht.emissionsmodus");
                    deckt.Add("bericht.varianten.anzahl");
                    break;
                case Berichtskapitel.ANHANG:
                    deckt.Add("bericht.warnungen");
                    break;
            }

            // Katalog v4: die Tabellen und Bilder des Bausteins, je gefolgt von ihrem Schalter.
            var schluessel = new HashSet<string>(tabellenUndBilder.Select(f => f.Schluessel), StringComparer.Ordinal);
            foreach (Vorlagenfeld f in tabellenUndBilder)
            {
                if (f.Art != Vorlagenfeldart.Tabelle && f.Art != Vorlagenfeldart.Bild) continue;
                if (KapitelDerTabelleOderDesBildes(f.Schluessel) != k.Name) continue;
                deckt.Add(f.Schluessel);
                string schalter = f.Art == Vorlagenfeldart.Tabelle ? SchalterDerTabelle(f.Schluessel) : SchalterDesBildes(f.Schluessel);
                if (schluessel.Contains(schalter)) deckt.Add(schalter);
            }

            if (k.Kopfschluessel != null) deckt.Add(k.Kopfschluessel);
            if (k.Schalter != null) deckt.Add(k.Schalter);
            return deckt.ToArray();
        }

        /// <summary>
        /// Die Tabellen und Bilder des Katalogs v4, die ein Baustein mit genau diesem Namen schreibt — sonst gehören
        /// sie zu keinem Kapitel.
        /// </summary>
        private static readonly Dictionary<string, string> _kapitelDerTabellenUndBilder = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Projektbeschreibung
            ["tabelle.kaelteerzeuger"] = Berichtskapitel.PROJEKT,
            ["tabelle.speichertemperaturen"] = Berichtskapitel.PROJEKT,
            ["tabelle.gebaeude.ergebnis"] = Berichtskapitel.PROJEKT,
            ["stamm.bild.speichertemperaturen"] = Berichtskapitel.PROJEKT,
            // Komponenten & Varianten — die Variantenliste gehört zum Kapitel, das die Varianten nennt
            ["tabelle.varianten"] = Berichtskapitel.KOMPONENTEN,
            ["tabelle.komponenten.matrix"] = Berichtskapitel.KOMPONENTEN,
            ["stand.tabelle.abweichungen"] = Berichtskapitel.KOMPONENTEN,
            // Berechnungsergebnisse je Variante
            ["stand.tabelle.kennzahlen"] = Berichtskapitel.ERGEBNISSE,
            ["stand.bild.waerme_jahresverlauf"] = Berichtskapitel.ERGEBNISSE,
            ["stand.bild.waerme_dauerlinie"] = Berichtskapitel.ERGEBNISSE,
            ["stand.bild.strombilanz_monate"] = Berichtskapitel.ERGEBNISSE,
            ["stand.bild.speicherverlauf"] = Berichtskapitel.ERGEBNISSE,
            // Variantenvergleich — die Gesamttafel führt dieselben Gruppen in einer Tabelle
            ["tabelle.vergleich"] = Berichtskapitel.VERGLEICH,
            ["tabelle.vergleich.delta_prozent"] = Berichtskapitel.VERGLEICH,
            ["stand.bild.deckung_waerme"] = Berichtskapitel.VERGLEICH,
            ["stand.bild.deckung_strom"] = Berichtskapitel.VERGLEICH,
            ["stand.tabelle.erzeuger"] = Berichtskapitel.VERGLEICH,
            ["stand.tabelle.brennstoffmengen"] = Berichtskapitel.VERGLEICH,
            // Wirtschaftlichkeit
            ["stand.bild.zahlungsstrom"] = Berichtskapitel.WIRTSCHAFTLICHKEIT,
            ["stand.tabelle.mehrjahres"] = Berichtskapitel.WIRTSCHAFTLICHKEIT,
            ["stand.tabelle.vermiedene_kosten"] = Berichtskapitel.WIRTSCHAFTLICHKEIT,
            ["stand.tabelle.kwkg_module"] = Berichtskapitel.WIRTSCHAFTLICHKEIT,
            ["stand.tabelle.betriebskosten"] = Berichtskapitel.WIRTSCHAFTLICHKEIT,
            ["stand.tabelle.strommengen"] = Berichtskapitel.WIRTSCHAFTLICHKEIT,
            ["stand.tabelle.emissionsbilanz"] = Berichtskapitel.WIRTSCHAFTLICHKEIT,
            ["stand.tabelle.sensitivitaet"] = Berichtskapitel.WIRTSCHAFTLICHKEIT,
            // Anhang und Anhang E
            ["tabelle.anhang.simulationsstaende"] = Berichtskapitel.ANHANG,
            ["tabelle.anhang_e.checkliste"] = Berichtskapitel.ANHANG_E,
        };

        /// <summary>Die erzeugten Tabellen und Bilder v4 nach Vorsilbe (je Gewerk, Kennzahlgruppe, Kennzahl, Szenario).</summary>
        private static readonly (string Vorsilbe, string Kapitel)[] _kapitelNachVorsilbe =
        {
            ("tabelle.komponenten.kenndaten.", Berichtskapitel.KOMPONENTEN),
            ("tabelle.vergleich.", Berichtskapitel.VERGLEICH),
            ("bild.vergleich.balken.", Berichtskapitel.VERGLEICH),
            ("tabelle.wirtschaft.", Berichtskapitel.WIRTSCHAFTLICHKEIT),
            ("bild.wirtschaft.", Berichtskapitel.WIRTSCHAFTLICHKEIT),
        };

        /// <summary>
        /// Das Kapitel (<see cref="Berichtskapitel.Name"/>), dessen Baustein eine Tabelle oder ein Bild des Katalogs
        /// v4 schreibt (Anwenderentscheid BV-E5-3) — dieselbe Tafel, dasselbe Bild wie im Bausteinweg; <c>null</c>
        /// für einen Schlüssel ohne Kapitel (<c>muster.tabelle</c>, das Logo). Die Kapitelwache hält die Zuordnung
        /// vollständig.
        /// </summary>
        internal static string KapitelDerTabelleOderDesBildes(string schluessel)
        {
            if (schluessel == null) return null;
            if (_kapitelDerTabellenUndBilder.TryGetValue(schluessel, out string kapitel)) return kapitel;
            foreach ((string vorsilbe, string k) in _kapitelNachVorsilbe)
                if (schluessel.StartsWith(vorsilbe, StringComparison.Ordinal)) return k;
            return null;
        }

        /// <summary>Das Logo des Erstellers; ohne Logo der Grund „kein Logo eingestellt“.</summary>
        private static object Logo(Berichtswerte w)
        {
            Bildinhalt bild = Bildinhalt.Aus(w.Ersteller?.Logo, w.Ersteller?.LogoDateiname);
            return bild != null ? (object)bild : Grund(w, nameof(R.BV_GRUND_KEIN_LOGO));
        }

        /// <summary>
        /// <b>Was eine Vorlage zeigt</b> (Konzept 5.1, 10.2, 12): die Schlüssel, die sie führt, dazu
        /// alles, was deren Kapitel decken — bis nichts mehr hinzukommt. Ein Kapitel, das die Vorlage
        /// nicht führt, gilt als gedeckt, wenn sie jeden Einzelschlüssel führt, den es schreiben würde
        /// (ohne Schalter und Kapitelkopf, und nur, wenn es solche Schlüssel hat) — so trägt die
        /// Standardvorlage ihr Deckblatt aus Platzhaltern, und <c>bericht.inhalt</c> gilt, sobald jedes
        /// Kapitel gilt. Aliasse zählen für ihren Eintrag; unbekannte Schlüssel bleiben außen vor.
        /// </summary>
        public static HashSet<string> Gedeckt(IEnumerable<string> schluessel)
        {
            return Gedeckt(schluessel, _alle, Finde);
        }

        /// <summary>Wie <see cref="Gedeckt(IEnumerable{string})"/> gegen einen übergebenen Katalog (Prüfer, Prüfstand).</summary>
        internal static HashSet<string> Gedeckt(IEnumerable<string> schluessel, IEnumerable<Vorlagenfeld> alle,
                                                Func<string, Vorlagenfeld> finde)
        {
            var gedeckt = new HashSet<string>(StringComparer.Ordinal);
            foreach (string s in schluessel ?? Enumerable.Empty<string>())
            {
                Vorlagenfeld f = finde(s);
                if (f != null) gedeckt.Add(f.Schluessel);
            }

            List<Vorlagenfeld> kapitel = (alle ?? Enumerable.Empty<Vorlagenfeld>()).Where(e => e.Art == Vorlagenfeldart.Kapitel).ToList();
            bool weiter = true;
            while (weiter)
            {
                weiter = false;
                foreach (Vorlagenfeld f in kapitel)
                {
                    if (gedeckt.Contains(f.Schluessel))
                    {
                        foreach (string d in f.Deckt)
                            if (gedeckt.Add(d)) weiter = true;
                        continue;
                    }
                    List<string> inhalt = f.Deckt.Where(d => !IstSchalterOderKopf(d)).ToList();
                    if (inhalt.Count > 0 && inhalt.All(gedeckt.Contains))
                    {
                        gedeckt.Add(f.Schluessel);
                        weiter = true;
                    }
                }
            }
            return gedeckt;
        }

        /// <summary>
        /// Die Deckblattangaben: was das Kapitel Deckblatt schreibt, ohne Schalter — trägt eine Vorlage
        /// eine davon im Rumpf, trägt sie ihr Deckblatt selbst (Stelle „Deckblatt“ der Anhang-E-Checkliste).
        /// </summary>
        public static ISet<string> Deckblattangaben
        {
            get
            {
                Vorlagenfeld deckblatt = Finde(Berichtskapitel.PRAEFIX_KAPITEL + Berichtskapitel.DECKBLATT);
                return new HashSet<string>((deckblatt?.Deckt ?? Array.Empty<string>()).Where(d => !IstSchalterOderKopf(d)),
                                           StringComparer.Ordinal);
            }
        }

        /// <summary>
        /// Schalter und Kapitelkopf zählen nicht als Inhalt eines Kapitels: der Häkchenschalter <c>baustein.*</c>,
        /// der Kapitelkopf <c>text.kapitel_*</c> und die Schalter je Tabelle und Bild (<c>hat.tabelle.*</c>,
        /// <c>hat.bild.*</c>, Katalog v4).
        /// </summary>
        private static bool IstSchalterOderKopf(string schluessel)
        {
            return schluessel.StartsWith(Berichtskapitel.PRAEFIX_SCHALTER, StringComparison.Ordinal) ||
                   schluessel.StartsWith(Berichtskapitel.PRAEFIX_KOPF, StringComparison.Ordinal) ||
                   schluessel.StartsWith(PRAEFIX_TABELLENSCHALTER, StringComparison.Ordinal) ||
                   schluessel.StartsWith(PRAEFIX_BILDSCHALTER, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ein Text mit Platzhaltern, aufgelöst: jede Marke eines bekannten Text-, Zahl- oder
        /// Datumsfelds (nicht je Stand oder Gebäude) durch ihren Wert, alles andere bleibt, wie es
        /// steht. So lesen Engine und Prüfer den Kapitelkopf der Vorlage (<c>{{text.kapitel_projekt}}</c>).
        /// </summary>
        internal static string LoeseImText(string text, Berichtswerte werte)
        {
            if (string.IsNullOrEmpty(text) || werte == null) return text ?? "";
            var sb = new System.Text.StringBuilder();
            int pos = 0;
            foreach (Platzhalter p in Platzhaltersyntax.Finde(text))
            {
                if (p.Position < pos) continue;
                sb.Append(text, pos, p.Position - pos);
                Vorlagenfeld f = p.Art == Platzhalterart.Feld ? Finde(p.Schluessel) : null;
                bool einfach = f != null && f.Kontext != Vorlagenfeldkontext.Stand && f.Kontext != Vorlagenfeldkontext.Gebaeude &&
                               (f.Art == Vorlagenfeldart.Text || f.Art == Vorlagenfeldart.Zahl || f.Art == Vorlagenfeldart.Datum);
                sb.Append(einfach ? Loese(f, werte, p.Angaben).Text : p.Roh);
                pos = p.Position + p.Laenge;
            }
            if (pos < text.Length) sb.Append(text, pos, text.Length - pos);
            return sb.ToString();
        }

        /// <summary>Die erzeugten Einträge (Konzept 5.2): je Kennzahl die Stammzahl, die Beschriftung
        /// und die Einheit, in der Reihenfolge des Kennzahlenkatalogs.</summary>
        private static IEnumerable<Vorlagenfeld> Erzeugte(List<Kennzahl> kennzahlen)
        {
            foreach (Kennzahl k in kennzahlen)
            {
                string s = k.Schluessel;
                yield return new Vorlagenfeld("stamm.kennzahl." + s, Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Stamm,
                    w => StammKennzahl(w, s))
                {
                    Format = k.Format,
                    Einheit = k.Einheit,
                    Ableitung = new Vorlagenfeldableitung(MUSTER_STAMM_KENNZAHL, nameof(R.VF_MUSTER_STAMM_KENNZAHL), s),
                    // Die Stunden mit Kühlbedarf zählt der Katalog an der Kanalreihe des Laufs.
                    Bedarf = s == KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN ? Vorlagenbedarf.Zeitreihen : Vorlagenbedarf.Keiner,
                };
            }
            foreach (Kennzahl k in kennzahlen)
            {
                string s = k.Schluessel;
                yield return new Vorlagenfeld("kennzahl." + s + ".beschriftung", Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht,
                    w => w.FindeKennzahl(s)?.Label(w.Englisch))
                {
                    Ableitung = new Vorlagenfeldableitung(MUSTER_KENNZAHL_BESCHRIFTUNG, nameof(R.VF_MUSTER_KENNZAHL_BESCHRIFTUNG), s),
                };
                yield return new Vorlagenfeld("kennzahl." + s + ".einheit", Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht,
                    w => w.FindeKennzahl(s)?.Einheit)
                {
                    Leerwert = "",
                    Ableitung = new Vorlagenfeldableitung(MUSTER_KENNZAHL_EINHEIT, nameof(R.VF_MUSTER_KENNZAHL_EINHEIT), s),
                };
            }
        }

        private static Vorlagenfeld Festtext(string schluessel, string ressource)
        {
            return new Vorlagenfeld(schluessel, Vorlagenfeldart.Text, Vorlagenfeldkontext.Bericht, w => w.Text(ressource));
        }

        private static IReadOnlyList<string> BildeReservierteExcelNamen()
        {
            string[] basis =
            {
                ExcelFormelmappe.ZINS, ExcelFormelmappe.ZINS_BASIS, ExcelFormelmappe.ZEITRAUM,
                ExcelFormelmappe.PREIS_E, ExcelFormelmappe.PREIS_B, ExcelFormelmappe.PREIS_I,
                ExcelFormelmappe.RISIKO_ZUSCHLAG, ExcelFormelmappe.RISIKO_VERLUST,
                ExcelFormelmappe.RISIKO_P, ExcelFormelmappe.RISIKO_ABZUG,
            };
            return basis
                .SelectMany(n => new[] { n, n + ExcelFormelmappe.ANHANG_GUENSTIG, n + ExcelFormelmappe.ANHANG_UNGUENSTIG })
                .ToList();
        }

        // =====================================================================
        //  Quellen — sie lesen nur den Wertesatz
        // =====================================================================

        private static Leergrund Grund(Berichtswerte w, string ressource)
        {
            return new Leergrund(w.Text(ressource));
        }

        /// <summary>Die Varianten wie auf dem Deckblatt (<see cref="VariantenDaten.Anzeige"/>),
        /// ohne Variante der Rückfall „— (nur Stammprojekt)“.</summary>
        private static object VariantenListe(Berichtswerte w)
        {
            string liste = string.Join(", ", w.Varianten.Select(v => v.Anzeige).Where(a => !string.IsNullOrWhiteSpace(a)));
            return liste.Length > 0 ? liste : w.Text(nameof(R.BV_NUR_STAMMPROJEKT));
        }

        /// <summary>Der Produktausweis nach VDI 6007 wie im Berichtskopf (A12), mit dem Satz zur
        /// Anlagenkopplung, sobald ein Gebäude gekoppelt rechnete.</summary>
        private static object Gebaeudemodellausweis(Berichtswerte w)
        {
            if (!w.Produktausweis) return Grund(w, nameof(R.BV_GRUND_KEIN_VDI6007));
            string satz = w.Text(nameof(R.GEB_PRODUKTAUSWEIS_VDI6007));
            return w.Anlagenkopplung ? satz + ". " + w.Text(nameof(R.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG)) : satz;
        }

        /// <summary>Der Name des Stammprojekts; ohne Projektdaten der Name des Berichtsbaums.</summary>
        private static object ProjektName(Berichtswerte w)
        {
            string name = w.Stamm?.Projekt?.m_szProjektname;
            if (string.IsNullOrWhiteSpace(name)) name = w.Daten.Stammprojektname;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        /// <summary>Ein Feld der Projektstammdaten (<c>Tab_Projekt</c>) des Stammprojekts; ein Datum
        /// ohne Wert (<see cref="DateTime.MinValue"/>) ist leer.</summary>
        private static object ProjektFeld(Berichtswerte w, Func<ProjektModel, object> feld)
        {
            if (w.Stamm == null) return Grund(w, nameof(R.BV_GRUND_KEIN_STAMM));
            if (w.Stamm.Projekt == null) return Grund(w, nameof(R.BV_GRUND_KEINE_PROJEKTDATEN));
            object wert = feld(w.Stamm.Projekt);
            return wert is DateTime d && d == DateTime.MinValue ? null : wert;
        }

        private static object Klimaregion(Berichtswerte w)
        {
            if (w.Stamm == null) return Grund(w, nameof(R.BV_GRUND_KEIN_STAMM));
            if (w.Stamm.Details == null) return Grund(w, nameof(R.BV_GRUND_KEINE_PROJEKTDATEN));
            return w.Stamm.Details.KlimaregionName;
        }

        private static object Simulationsstand(Berichtswerte w)
        {
            if (w.Stamm == null) return Grund(w, nameof(R.BV_GRUND_KEIN_STAMM));
            if (!w.Stamm.SimulationsStand.HasValue) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            return w.Stamm.SimulationsStand.Value;
        }

        /// <summary>
        /// Eine Kennzahl des Stammprojekts aus <see cref="VariantenDaten.Kennzahlen"/> — derselben
        /// Quelle wie die Kapitel. Ohne Wert der Grund: kein Stammprojekt, Lauf fehlgeschlagen,
        /// kein Simulationsergebnis oder für dieses Projekt nicht verfügbar.
        /// </summary>
        private static object StammKennzahl(Berichtswerte w, string schluessel)
        {
            VariantenDaten stamm = w.Stamm;
            if (stamm == null) return Grund(w, nameof(R.BV_GRUND_KEIN_STAMM));
            if (stamm.Kennzahlen != null && stamm.Kennzahlen.TryGetValue(schluessel, out double? wert) && wert.HasValue)
                return wert.Value;
            if (!string.IsNullOrEmpty(stamm.Fehler)) return Grund(w, nameof(R.BV_GRUND_LAUF_FEHLGESCHLAGEN));
            if (stamm.Ergebnis == null) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            return Grund(w, nameof(R.BV_GRUND_NICHT_VERFUEGBAR));
        }
    }
}
