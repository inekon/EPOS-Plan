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
    /// <para><b>Katalog v1</b> (<see cref="KATALOGFASSUNG"/> 1, Etappe BV-E1): <c>bericht.*</c>,
    /// <c>text.*</c>, <c>ersteller.*</c>, <c>projekt.*</c> handgepflegt; <c>stamm.kennzahl.&lt;k&gt;</c>,
    /// <c>kennzahl.&lt;k&gt;.beschriftung</c> und <c>kennzahl.&lt;k&gt;.einheit</c> aus dem
    /// <see cref="KennzahlenKatalog"/> erzeugt — eine neue Kennzahl ist ohne Pflege ein Platzhalter.
    /// Die Fassung steigt mit jeder Etappe, die Einträge hinzufügt; die Schlüsselliste jeder
    /// Fassung ist eingefroren (<c>EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v1.txt</c>), und
    /// ein einmal ausgelieferter Schlüssel bleibt lebendig oder Alias (5.6).</para>
    ///
    /// <para><b>Namen.</b> Beschreibungen handgepflegter Einträge stehen in <c>MyResource</c> unter
    /// <see cref="RessourcenName"/> (<c>projekt.kunde</c> → <c>VF_PROJEKT__KUNDE</c>), erzeugte
    /// nehmen ein Muster (<c>VF_MUSTER_*</c>, <c>{0}</c> = Beschriftung der Kennzahl). In Excel heißt
    /// ein Platzhalter <see cref="ExcelName"/> (<c>EPOS.projekt.kunde</c>); die Namen der
    /// Formelmappe sind reserviert (<see cref="ReservierteExcelNamen"/>). Beide Abbildungen sind
    /// eindeutig, weil das Schlüsselmuster nur einzelne Unterstriche erlaubt und nur
    /// Kleinbuchstaben kennt (Konzept 5.5).</para>
    /// </summary>
    public static class Vorlagenfeldkatalog
    {
        /// <summary>Die Katalogfassung; sie steigt mit jeder Etappe, die Einträge hinzufügt (Konzept 5.6).</summary>
        public const int KATALOGFASSUNG = 1;

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
            nameof(R.BV_GRUND_KEIN_VDI6007), nameof(R.BV_GRUND_AUSNAHME),
        };

        /// <summary>Die Beschreibungsmuster der erzeugten Einträge (<c>{0}</c> = Beschriftung der Kennzahl).</summary>
        public static readonly IReadOnlyList<string> Musterschluessel = new[]
        {
            nameof(R.VF_MUSTER_STAMM_KENNZAHL), nameof(R.VF_MUSTER_KENNZAHL_BESCHRIFTUNG), nameof(R.VF_MUSTER_KENNZAHL_EINHEIT),
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

            _alle = new List<Vorlagenfeld>(Handgepflegt());
            _alle.AddRange(Erzeugte(kennzahlen));

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
            string parameter = _kennzahlen.TryGetValue(feld.Ableitung.Parameter, out Kennzahl k)
                ? k.Label(englisch) : feld.Ableitung.Parameter;
            try { return string.Format(kultur, muster, parameter); }
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
                default:
                    // Tabelle, Bild, Schalter, Blatt kommen mit späteren Etappen; Katalog v1 führt keine.
                    return LeerMit(feld, angaben, w.Text(nameof(R.BV_GRUND_NICHT_VERFUEGBAR)), null);
            }
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
                    Deckt = BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel).ToArray(),
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
