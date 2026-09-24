using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Was der Anwender zum Einlesen einer Messreihe mitgibt</b> (Stufe Z5): Bezeichnung und
    /// Quelle der Reihe, wahlweise die gemessene Größe (sonst entscheidet die Einheit in der
    /// Kopfzeile) und die Schwelle, ab der ein Lückenanteil die Datei ablehnt statt sie zu nennen.
    ///
    /// <para>Die Schwelle steht als Parameter im freien Paketteil
    /// (<see cref="ZapfParameter.VALIDIERUNG_LUECKENANTEIL"/>); die Vorgabe hier gilt, wenn kein
    /// Parametersatz da ist (ein Test, ein Werkzeug).</para>
    /// </summary>
    internal sealed record Messreihenoptionen
    {
        /// <summary>Die Bezeichnung der Reihe; leer = der Dateiname.</summary>
        public string Bezeichnung { get; init; } = "";

        /// <summary>Die Quelle der Reihe (Zähler, Objekt, Zeitraum); leer = der Dateiname.</summary>
        public string Quelle { get; init; } = "";

        /// <summary>Die gemessene Größe; <c>null</c> = aus der Einheit der Kopfzeile lesen.</summary>
        public ZapfMessgroesse? Groesse { get; init; }

        /// <summary>Höchster zugelassener Anteil gefüllter Lücken [-]; darüber eine Ablehnung.</summary>
        public double LueckenanteilHoechstens { get; init; } = 0.05;
    }

    /// <summary>
    /// <b>Der Leser einer gemessenen Reihe</b> (Umsetzungskonzept Zapfprofilgenerator 4.8 und
    /// Kapitel 7 Zeile Z5; Stufe Z5): Er liest eine CSV-Datei des Anwenders aus einem
    /// <see cref="Stream"/> zu einer <see cref="Messreihe"/> — oder lehnt sie benannt ab.
    ///
    /// <para><b>Die Datei wählt die Hülle</b> über <c>Dienste.Datei</c>, nie der Kern; hier steht
    /// allein das Lesen aus einem Strom (Regel des Kerns: keine Pfadlogik, kein
    /// <c>SpecialFolder</c>).</para>
    ///
    /// <para><b>Das Format</b> (bewusst eng, damit nichts still falsch gelesen wird):</para>
    /// <list type="bullet">
    /// <item><b>Kopfzeile</b> mit Spaltennamen. Erkannt werden ein Zeitstempel
    /// (<see cref="NAMEN_ZEITSTEMPEL"/>) oder Datum und Uhrzeit getrennt
    /// (<see cref="NAMEN_DATUM"/>, <see cref="NAMEN_UHRZEIT"/>) und die Wertspalte
    /// (<see cref="NAMEN_WERT"/>) — Groß- und Kleinschreibung spielt keine Rolle.</item>
    /// <item><b>Trenner</b> <c>;</c>, Tabulator oder <c>,</c> — der häufigste der Kopfzeile gewinnt.
    /// <b>Dezimalkomma</b> ist erlaubt, SOLANGE der Trenner nicht das Komma ist; sonst wäre
    /// „1,5" zwei Felder. Kultur invariant, kein Tausendertrenner.</item>
    /// <item><b>Zeitstempel</b> ISO (<c>2025-01-01T00:00</c>, auch mit Leerzeichen und Sekunden)
    /// oder deutsch (<c>01.01.2025 00:00</c>), Datum allein für eine Tagesreihe.</item>
    /// <item><b>Einheit</b> im Kopf der Wertspalte in Klammern — <c>kWh</c>, <c>m³</c>/<c>m3</c>,
    /// <c>kW</c>; sie bestimmt die Größe, wenn die Optionen keine nennen.</item>
    /// </list>
    ///
    /// <para><b>Die Auflösung</b> misst der Leser aus den Zeitstempeln: der kleinste Abstand zweier
    /// Zeilen, der in <see cref="RASTER"/> stehen muss. Jeder größere Abstand ist ein Vielfaches
    /// davon und wird als <b>Lücke</b> mit 0 gefüllt und gezählt; über
    /// <see cref="Messreihenoptionen.LueckenanteilHoechstens"/> lehnt der Leser die Datei ab statt
    /// sie zu nennen. Ein Abstand, der kein Vielfaches ist, ein Rückschritt und ein doppelter
    /// Zeitstempel sind benannte Ablehnungen mit Datei und Zeile.</para>
    ///
    /// <para><b>Plausibilität:</b> kein negativer Wert (eine Zapfung zählt nie rückwärts), keine
    /// NaN, eine Menge über 0, höchstens <see cref="HOECHSTENS_ZEILEN"/> Zeilen und
    /// <see cref="HOECHSTENS_BYTE"/> Bytes. Ein leeres Wertfeld ist eine Lücke, kein Fehler.</para>
    ///
    /// <para><b>Jede Ablehnung und jeder Hinweis ist ein <see cref="ZapfSatz"/></b> — Kennung und
    /// Werte, kein fertiger Satz (N11 (k)); die Muster stehen in beiden Sprachen.</para>
    /// </summary>
    internal static class Messreihenleser
    {
        /// <summary>Die zugelassenen Zeitraster [min] — 1, 5, 10, 15, 60 und der Tag.</summary>
        internal static readonly IReadOnlyList<int> RASTER = Array.AsReadOnly(new[] { 1, 5, 10, 15, 60, 1440 });

        /// <summary>
        /// Höchstzahl der Datenzeilen (numerische Setzung): ein Jahr in Minutenschritten sind
        /// 525 600 Zeilen; die Grenze lässt ein Schaltjahr und einen kleinen Vorlauf zu und hält
        /// eine versehentlich gewählte Riesendatei benannt auf.
        /// </summary>
        internal const int HOECHSTENS_ZEILEN = 600000;

        /// <summary>Höchstgröße der Datei [Byte] (numerische Setzung, wie im Normformvektorleser: 64 MiB).</summary>
        internal const long HOECHSTENS_BYTE = 64L * 1024 * 1024;

        /// <summary>Kleinste Zahl der Datenzeilen — unter zwei lässt sich keine Auflösung messen.</summary>
        internal const int MINDESTENS_ZEILEN = 2;

        /// <summary>Die erkannten Namen einer Zeitstempelspalte (klein geschrieben).</summary>
        internal static readonly IReadOnlyList<string> NAMEN_ZEITSTEMPEL = Array.AsReadOnly(new[]
        {
            "zeitstempel", "zeitpunkt", "timestamp", "datetime", "datum_zeit", "datumzeit", "datum/zeit"
        });

        /// <summary>Die erkannten Namen einer Datumsspalte (klein geschrieben).</summary>
        internal static readonly IReadOnlyList<string> NAMEN_DATUM = Array.AsReadOnly(new[] { "datum", "date", "tag" });

        /// <summary>Die erkannten Namen einer Uhrzeitspalte (klein geschrieben).</summary>
        internal static readonly IReadOnlyList<string> NAMEN_UHRZEIT = Array.AsReadOnly(new[] { "uhrzeit", "zeit", "time" });

        /// <summary>Die erkannten Namen der Wertspalte (klein geschrieben).</summary>
        internal static readonly IReadOnlyList<string> NAMEN_WERT = Array.AsReadOnly(new[]
        {
            "wert", "value", "menge", "verbrauch", "leistung", "energie", "volumen"
        });

        /// <summary>Die Trenner in der Reihenfolge, in der ein Gleichstand entschieden wird.</summary>
        internal static readonly IReadOnlyList<char> TRENNER = Array.AsReadOnly(new[] { ';', '\t', ',' });

        private static readonly string[] ZeitstempelFormate =
        {
            "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd",
            "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy"
        };

        private static readonly string[] Datumsformate = { "yyyy-MM-dd", "dd.MM.yyyy" };

        private static readonly string[] Uhrzeitformate = { "HH:mm:ss", "HH:mm", "H:mm", "HH", "H" };

        // =================================================================================
        //  Die beiden Eingänge
        // =================================================================================

        /// <summary>
        /// <b>Liest eine Messreihe aus einem Strom</b> (der Weg der Hülle: Datei über
        /// <c>Dienste.Datei</c> wählen, Strom öffnen, hier hereingeben). Gelesen wird UTF-8 (BOM
        /// erlaubt). Ergebnis ist die Reihe oder <c>null</c> samt benanntem
        /// <paramref name="fehler"/>; <paramref name="hinweise"/> nimmt auf, was die Rechnung nicht
        /// entscheidet.
        /// </summary>
        /// <param name="strom">Der offene Strom; er wird nicht geschlossen.</param>
        /// <param name="datei">Der Name der Datei — nur für die Sätze und als Vorgabe von
        /// Bezeichnung und Quelle; der Kern öffnet keinen Pfad.</param>
        internal static Messreihe AusStrom(Stream strom, string datei, Messreihenoptionen optionen,
                                           out ZapfSatz fehler, ICollection<ZapfSatz> hinweise = null)
        {
            fehler = null;
            string name = Name(datei);
            if (strom == null)
            {
                fehler = ZapfSatz.Neu("MESSREIHE_DATEI_UNLESBAR", name, "kein Strom");
                return null;
            }
            if (strom.CanSeek && strom.Length > HOECHSTENS_BYTE)
            {
                fehler = ZapfSatz.Neu("MESSREIHE_ZU_GROSS", name, strom.Length, HOECHSTENS_BYTE);
                return null;
            }
            try
            {
                using (var leser = new StreamReader(strom, Encoding.UTF8, true, 64 * 1024, leaveOpen: true))
                    return Lesen(leser, name, optionen, out fehler, hinweise);
            }
            catch (Exception ex) when (ex is IOException || ex is NotSupportedException
                                       || ex is ObjectDisposedException || ex is ArgumentException)
            {
                fehler = ZapfSatz.Neu("MESSREIHE_DATEI_UNLESBAR", name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// <b>Liest eine Messreihe aus schon gelesenem Text</b> (ein Testhelfer, ein Paket) —
        /// dieselben Prüfungen wie <see cref="AusStrom"/>.
        /// </summary>
        internal static Messreihe AusText(string text, string datei, Messreihenoptionen optionen,
                                          out ZapfSatz fehler, ICollection<ZapfSatz> hinweise = null)
        {
            fehler = null;
            string name = Name(datei);
            if (text == null)
            {
                fehler = ZapfSatz.Neu("MESSREIHE_DATEI_UNLESBAR", name, "kein Text");
                return null;
            }
            using (var leser = new StringReader(text))
                return Lesen(leser, name, optionen, out fehler, hinweise);
        }

        // =================================================================================
        //  Der Leseweg
        // =================================================================================

        /// <summary>
        /// Der gemeinsame Leseweg: Kopfzeile, Spaltenwahl, Zeilen, Auflösung, Lücken, Plausibilität.
        /// Eine Ablehnung reist als <see cref="Leseabbruch"/> durch die Hilfsschritte und kommt als
        /// <paramref name="fehler"/> heraus — nie als Ausnahme nach außen.
        /// </summary>
        private static Messreihe Lesen(TextReader leser, string datei, Messreihenoptionen optionen,
                                       out ZapfSatz fehler, ICollection<ZapfSatz> hinweise)
        {
            fehler = null;
            Messreihenoptionen o = optionen ?? new Messreihenoptionen();
            try
            {
                string kopf = NaechsteZeile(leser);
                if (kopf == null) throw Abbruch(ZapfSatz.Neu("MESSREIHE_KOPFZEILE_FEHLT", datei));

                char trenner = Trenner(kopf, datei);
                string[] spalten = Zerlegen(kopf, trenner);
                Spaltenwahl wahl = Spalten(spalten, datei);
                ZapfMessgroesse groesse = o.Groesse ?? Einheit(spalten[wahl.Wert], datei);

                var zeitpunkte = new List<DateTime>();
                var werte = new List<double>();
                var leerstellen = new List<int>();
                int nummer = 1;
                for (string zeile = NaechsteZeile(leser); zeile != null; zeile = NaechsteZeile(leser))
                {
                    nummer++;
                    if (zeile.Trim().Length == 0) continue;                 // eine Leerzeile trennt nur
                    string[] felder = Zerlegen(zeile, trenner);
                    // GENAU so viele Felder wie die Kopfzeile Spalten hat. Streng mit Absicht: Ein
                    // Dezimalkomma bei Komma-Trenner ergaebe ein Feld zu viel, und die Wertspalte
                    // truege dann still nur den Vorkommateil ("1,5" -> 1).
                    if (felder.Length != spalten.Length)
                        throw Abbruch(ZapfSatz.Neu("MESSREIHE_FELDZAHL", datei, nummer, felder.Length, spalten.Length));

                    zeitpunkte.Add(Zeitpunkt(felder, wahl, datei, nummer));
                    string roh = felder[wahl.Wert].Trim();
                    if (roh.Length == 0)
                    {
                        leerstellen.Add(werte.Count);
                        werte.Add(0.0);
                    }
                    else
                    {
                        werte.Add(Zahl(roh, spalten[wahl.Wert], trenner, datei, nummer));
                    }
                    if (werte.Count > HOECHSTENS_ZEILEN)
                        throw Abbruch(ZapfSatz.Neu("MESSREIHE_ZU_VIELE_ZEILEN", datei, werte.Count, HOECHSTENS_ZEILEN));
                }

                if (werte.Count == 0) throw Abbruch(ZapfSatz.Neu("MESSREIHE_OHNE_WERTE", datei));
                if (werte.Count < MINDESTENS_ZEILEN)
                    throw Abbruch(ZapfSatz.Neu("MESSREIHE_ZU_KURZ", datei, werte.Count, MINDESTENS_ZEILEN));

                int aufloesung = Aufloesung(zeitpunkte, datei);
                double[] gefuellt = Fuellen(zeitpunkte, werte, aufloesung, datei, out int luecken);
                luecken += leerstellen.Count;

                double anteil = (double)luecken / gefuellt.Length;
                if (luecken > 0 && anteil > o.LueckenanteilHoechstens)
                    throw Abbruch(ZapfSatz.Neu("MESSREIHE_LUECKEN_ZU_GROSS", datei, luecken, gefuellt.Length,
                                               anteil, o.LueckenanteilHoechstens));
                if (luecken > 0)
                    hinweise?.Add(ZapfSatz.Neu("MESSREIHE_LUECKEN", datei, luecken, gefuellt.Length, anteil));

                var reihe = new Messreihe(Ersatz(o.Bezeichnung, datei), groesse, aufloesung, zeitpunkte[0],
                                          gefuellt, Ersatz(o.Quelle, datei), luecken,
                                          Schalttage(zeitpunkte[0], gefuellt.Length, aufloesung));
                if (reihe.Menge <= 0.0) throw Abbruch(ZapfSatz.Neu("MESSREIHE_OHNE_MENGE", datei));

                if (reihe.Schalttage > 0) hinweise?.Add(ZapfSatz.Neu("MESSREIHE_SCHALTTAG", reihe.Schalttage));
                if (reihe.Tage > Zapfkalender.TAGE) hinweise?.Add(ZapfSatz.Neu("MESSREIHE_UEBER_EIN_JAHR", reihe.Tage));
                if (reihe.Beginn.Minute != 0 || reihe.Beginn.Second != 0)
                    hinweise?.Add(ZapfSatz.Neu("MESSREIHE_BEGINN_NICHT_STUNDE", datei, reihe.Beginn.Minute));
                if (!reihe.StundenweiseTauglich)
                    hinweise?.Add(ZapfSatz.Neu("MESSREIHE_OHNE_STUNDENWERTE", aufloesung));
                return reihe;
            }
            catch (Leseabbruch a)
            {
                fehler = a.Satz;
                return null;
            }
        }

        // =================================================================================
        //  Kopfzeile, Trenner, Spalten, Einheit
        // =================================================================================

        /// <summary>
        /// Der Trenner: der häufigste aus <see cref="TRENNER"/> in der Kopfzeile. Kein Treffer ist
        /// eine benannte Ablehnung — eine Kopfzeile ohne Trenner hat nur eine Spalte, und dann
        /// fehlt entweder der Zeitstempel oder der Wert.
        /// </summary>
        private static char Trenner(string kopf, string datei)
        {
            char beste = '\0';
            int meiste = 0;
            foreach (char c in TRENNER)
            {
                int n = kopf.Count(z => z == c);
                if (n > meiste) { meiste = n; beste = c; }
            }
            if (meiste == 0) throw Abbruch(ZapfSatz.Neu("MESSREIHE_TRENNER_UNBEKANNT", datei));
            return beste;
        }

        /// <summary>Welche Spalte trägt was — und welche ist die hinterste, die eine Zeile führen muss.</summary>
        private readonly struct Spaltenwahl
        {
            internal Spaltenwahl(int zeitstempel, int datum, int uhrzeit, int wert)
            {
                Zeitstempel = zeitstempel; Datum = datum; Uhrzeit = uhrzeit; Wert = wert;
            }

            /// <summary>Die Spalte des vollen Zeitstempels; −1 = keine (dann Datum und Uhrzeit).</summary>
            internal int Zeitstempel { get; }

            /// <summary>Die Spalte des Datums; −1 = keine.</summary>
            internal int Datum { get; }

            /// <summary>Die Spalte der Uhrzeit; −1 = keine (dann gilt 00:00).</summary>
            internal int Uhrzeit { get; }

            /// <summary>Die Spalte des Werts.</summary>
            internal int Wert { get; }

            /// <summary>Der größte benutzte Spaltenindex — so viele Felder braucht jede Zeile.</summary>
            internal int Groesster => Math.Max(Math.Max(Zeitstempel, Datum), Math.Max(Uhrzeit, Wert));
        }

        /// <summary>
        /// Die Spaltenwahl aus der Kopfzeile. Fehlt der Zeitbezug oder der Wert, ist das eine
        /// benannte Ablehnung, die die erkannten Namen mitnennt — der Anwender soll die Kopfzeile
        /// berichtigen können, ohne die Anleitung zu suchen.
        /// </summary>
        private static Spaltenwahl Spalten(string[] spalten, string datei)
        {
            int zeitstempel = -1, datum = -1, uhrzeit = -1, wert = -1;
            for (int i = 0; i < spalten.Length; i++)
            {
                string n = Schluessel(spalten[i]);
                if (n.Length == 0) continue;
                if (zeitstempel < 0 && NAMEN_ZEITSTEMPEL.Contains(n, StringComparer.Ordinal)) { zeitstempel = i; continue; }
                if (datum < 0 && NAMEN_DATUM.Contains(n, StringComparer.Ordinal)) { datum = i; continue; }
                if (uhrzeit < 0 && NAMEN_UHRZEIT.Contains(n, StringComparer.Ordinal)) { uhrzeit = i; continue; }
                if (wert < 0 && NAMEN_WERT.Contains(n, StringComparer.Ordinal)) wert = i;
            }
            if (zeitstempel < 0 && datum < 0)
                throw Abbruch(ZapfSatz.Neu("MESSREIHE_SPALTE_ZEIT_FEHLT", datei,
                                           NAMEN_ZEITSTEMPEL.Concat(NAMEN_DATUM).ToArray()));
            if (wert < 0)
                throw Abbruch(ZapfSatz.Neu("MESSREIHE_SPALTE_WERT_FEHLT", datei, NAMEN_WERT.ToArray()));
            return new Spaltenwahl(zeitstempel, datum, uhrzeit, wert);
        }

        /// <summary>
        /// Die gemessene Größe aus der Einheit im Kopf der Wertspalte: der Text in Klammern
        /// (<c>[kWh]</c>, <c>(m³)</c>) oder hinter dem letzten Leerzeichen. Keine erkennbare
        /// Einheit ist eine benannte Ablehnung — still eine Größe zu raten hieße, kWh und m³ zu
        /// verwechseln.
        /// </summary>
        private static ZapfMessgroesse Einheit(string kopf, string datei)
        {
            string t = (kopf ?? "").Trim();
            string einheit = "";
            int auf = t.LastIndexOfAny(new[] { '[', '(' });
            int zu = t.LastIndexOfAny(new[] { ']', ')' });
            if (auf >= 0 && zu > auf) einheit = t.Substring(auf + 1, zu - auf - 1);
            else
            {
                int leer = t.LastIndexOf(' ');
                if (leer > 0) einheit = t.Substring(leer + 1);
            }
            switch (einheit.Trim().ToLowerInvariant())
            {
                case "kwh": return ZapfMessgroesse.Energie;
                case "m³":
                case "m3":
                case "cbm": return ZapfMessgroesse.Volumen;
                case "kw": return ZapfMessgroesse.Leistung;
                default: throw Abbruch(ZapfSatz.Neu("MESSREIHE_EINHEIT_UNBEKANNT", datei, t));
            }
        }

        // =================================================================================
        //  Zeitstempel, Zahl, Auflösung, Lücken
        // =================================================================================

        private static DateTime Zeitpunkt(string[] felder, Spaltenwahl wahl, string datei, int nummer)
        {
            if (wahl.Zeitstempel >= 0)
            {
                string t = felder[wahl.Zeitstempel].Trim();
                if (DateTime.TryParseExact(t, ZeitstempelFormate, CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out DateTime z))
                    return z;
                throw Abbruch(ZapfSatz.Neu("MESSREIHE_ZEITSTEMPEL_UNGUELTIG", datei, nummer, t));
            }

            string d = felder[wahl.Datum].Trim();
            if (!DateTime.TryParseExact(d, Datumsformate, CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out DateTime tag))
                throw Abbruch(ZapfSatz.Neu("MESSREIHE_ZEITSTEMPEL_UNGUELTIG", datei, nummer, d));
            if (wahl.Uhrzeit < 0) return tag;

            string u = felder[wahl.Uhrzeit].Trim();
            if (u.Length == 0) return tag;
            if (!DateTime.TryParseExact(u, Uhrzeitformate, CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out DateTime zeit))
                throw Abbruch(ZapfSatz.Neu("MESSREIHE_ZEITSTEMPEL_UNGUELTIG", datei, nummer, u));
            return tag.Add(zeit.TimeOfDay);
        }

        /// <summary>
        /// Eine Zahl in invarianter Kultur. Ist der Trenner nicht das Komma, gilt ein Komma als
        /// Dezimalzeichen; ist er es, wäre „1,5" zwei Felder — dann bleibt allein der Punkt.
        /// Ein Tausendertrenner scheitert benannt, statt still eine andere Zahl zu ergeben.
        /// </summary>
        private static double Zahl(string roh, string spalte, char trenner, string datei, int nummer)
        {
            string t = roh;
            if (trenner != ',') t = t.Replace(',', '.');
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double w)
                && !double.IsNaN(w) && !double.IsInfinity(w))
            {
                if (w < 0.0) throw Abbruch(ZapfSatz.Neu("MESSREIHE_WERT_NEGATIV", datei, nummer, w));
                return w;
            }
            throw Abbruch(ZapfSatz.Neu("MESSREIHE_KEINE_ZAHL", datei, nummer, (spalte ?? "").Trim(), roh));
        }

        /// <summary>
        /// Die Auflösung [min]: der kleinste Abstand zweier aufeinanderfolgender Zeitstempel. Ein
        /// Abstand ≤ 0 (Rückschritt oder doppelter Zeitstempel) und eine Auflösung außerhalb
        /// <see cref="RASTER"/> sind benannte Ablehnungen.
        /// </summary>
        private static int Aufloesung(IReadOnlyList<DateTime> zeitpunkte, string datei)
        {
            double kleinster = double.MaxValue;
            for (int i = 1; i < zeitpunkte.Count; i++)
            {
                double minuten = (zeitpunkte[i] - zeitpunkte[i - 1]).TotalMinutes;
                if (minuten <= 0.0) throw Abbruch(ZapfSatz.Neu("MESSREIHE_ZEITSTEMPEL_FOLGE", datei, i + 1));
                if (minuten < kleinster) kleinster = minuten;
            }
            int gerundet = (int)Math.Round(kleinster);
            if (Math.Abs(kleinster - gerundet) > 1e-9 || !RASTER.Contains(gerundet))
                throw Abbruch(ZapfSatz.Neu("MESSREIHE_AUFLOESUNG_UNBEKANNT", datei, kleinster, RASTER.ToArray()));
            return gerundet;
        }

        /// <summary>
        /// Die Reihe auf ein lückenloses Raster bringen: Jeder Abstand muss ein ganzes Vielfaches
        /// der Auflösung sein; fehlende Zeitschritte werden mit 0 gefüllt und gezählt. Ein Abstand,
        /// der kein Vielfaches ist (eine Zeitumstellung, ein fremdes Raster), ist eine benannte
        /// Ablehnung mit Zeile und gemessenem Abstand.
        /// </summary>
        private static double[] Fuellen(IReadOnlyList<DateTime> zeitpunkte, IReadOnlyList<double> werte,
                                        int aufloesung, string datei, out int luecken)
        {
            luecken = 0;
            var reihe = new List<double>(werte.Count) { werte[0] };
            for (int i = 1; i < zeitpunkte.Count; i++)
            {
                double minuten = (zeitpunkte[i] - zeitpunkte[i - 1]).TotalMinutes;
                double schritte = minuten / aufloesung;
                int ganz = (int)Math.Round(schritte);
                if (Math.Abs(schritte - ganz) > 1e-9 || ganz < 1)
                    throw Abbruch(ZapfSatz.Neu("MESSREIHE_ZEITSCHRITT_UNGLEICH", datei, i + 1, minuten, aufloesung));
                for (int k = 1; k < ganz; k++) { reihe.Add(0.0); luecken++; }
                reihe.Add(werte[i]);
                if (reihe.Count > HOECHSTENS_ZEILEN)
                    throw Abbruch(ZapfSatz.Neu("MESSREIHE_ZU_VIELE_ZEILEN", datei, reihe.Count, HOECHSTENS_ZEILEN));
            }
            return reihe.ToArray();
        }

        /// <summary>
        /// Wie viele 29. Februare die Reihe überstreicht. Der Rechenkern rechnet 365 Tage ohne
        /// Schaltjahr; der Hinweis sagt dem Anwender, dass ein Tag der Messung in keinem
        /// Vergleichstag steht.
        /// </summary>
        private static int Schalttage(DateTime beginn, int schritte, int aufloesung)
        {
            DateTime ende = beginn.AddMinutes((double)schritte * aufloesung);
            int n = 0;
            for (int jahr = beginn.Year; jahr <= ende.Year; jahr++)
            {
                if (!DateTime.IsLeapYear(jahr)) continue;
                var tag = new DateTime(jahr, 2, 29);
                if (tag >= beginn.Date && tag < ende) n++;
            }
            return n;
        }

        // =================================================================================
        //  Kleinkram
        // =================================================================================

        /// <summary>Eine Zeile ohne ihr Zeilenende; <c>null</c> am Ende der Datei.</summary>
        private static string NaechsteZeile(TextReader leser) => leser.ReadLine();

        /// <summary>
        /// Eine Zeile in Felder zerlegen. Ein Feld in geraden Anführungszeichen darf den Trenner
        /// tragen (RFC-4180-Kern); zwei Anführungszeichen hintereinander sind eines.
        /// </summary>
        private static string[] Zerlegen(string zeile, char trenner)
        {
            var felder = new List<string>();
            var feld = new StringBuilder();
            bool inAnfuehrung = false;
            for (int i = 0; i < zeile.Length; i++)
            {
                char c = zeile[i];
                if (inAnfuehrung)
                {
                    if (c == '"')
                    {
                        if (i + 1 < zeile.Length && zeile[i + 1] == '"') { feld.Append('"'); i++; }
                        else inAnfuehrung = false;
                    }
                    else feld.Append(c);
                    continue;
                }
                if (c == '"') { inAnfuehrung = true; continue; }
                if (c == trenner) { felder.Add(feld.ToString()); feld.Clear(); continue; }
                if (c == '\r') continue;
                feld.Append(c);
            }
            felder.Add(feld.ToString());
            return felder.ToArray();
        }

        /// <summary>
        /// Der Vergleichsschlüssel eines Spaltennamens: klein geschrieben, ohne Einheit in Klammern,
        /// ohne Rand. Aus „Wert [kWh]" wird „wert".
        /// </summary>
        private static string Schluessel(string kopf)
        {
            string t = (kopf ?? "").Trim();
            int auf = t.IndexOfAny(new[] { '[', '(' });
            if (auf > 0) t = t.Substring(0, auf);
            return t.Trim().ToLowerInvariant();
        }

        /// <summary>Der Dateiname ohne Ordneranteil; leer, wenn keiner da ist.</summary>
        private static string Name(string datei)
        {
            string t = datei ?? "";
            int strich = t.LastIndexOfAny(new[] { '/', '\\' });
            return strich >= 0 ? t.Substring(strich + 1) : t;
        }

        /// <summary>Der Text, sonst der Dateiname, sonst ein Strich — nie leer.</summary>
        private static string Ersatz(string text, string datei)
        {
            if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
            return string.IsNullOrWhiteSpace(datei) ? "-" : datei.Trim();
        }

        /// <summary>Die Ablehnung als Ausnahme, damit jeder Hilfsschritt sie werfen kann.</summary>
        private static Leseabbruch Abbruch(ZapfSatz satz) => new Leseabbruch(satz);

        /// <summary>Trägt eine benannte Ablehnung durch die Hilfsschritte; sie endet in <see cref="Lesen"/>.</summary>
        private sealed class Leseabbruch : Exception
        {
            internal Leseabbruch(ZapfSatz satz) : base(satz?.Klartext ?? "") { Satz = satz; }

            internal ZapfSatz Satz { get; }
        }
    }
}
