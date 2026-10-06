using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>Die Art einer Zeilenmeldung des CSV-Imports der Nutzungsprofile (Konzept Nutzungsprofile 6.1, NP-F11).</summary>
    public enum RaumnutzungCsvArt
    {
        /// <summary>Der Wert steht im Profil.</summary>
        Uebernommen,

        /// <summary>Die Spalte oder der Wert wird bewusst nicht gelesen (unbekannte Spalte, abgeleitete Zahl, Vorrang).</summary>
        Ignoriert,

        /// <summary>Der Wert (oder die ganze Zeile) wird mit Grund nicht übernommen.</summary>
        Fehler,
    }

    /// <summary>
    /// Eine Zeilenmeldung: Zeile der Datei (die Kopfzeile ist 1), Spalte, Art und Grund. Bei <see cref="RaumnutzungCsvArt.Uebernommen"/>
    /// trägt der Grund den gelesenen Wert.
    /// </summary>
    public sealed record RaumnutzungCsvMeldung(int Zeile, string Spalte, RaumnutzungCsvArt Art, string Grund);

    /// <summary>Eine Profilzeile der Datei samt ihrem Stand.</summary>
    public sealed class RaumnutzungCsvZeile
    {
        /// <summary>Die Zeile der Datei, in der der Datensatz beginnt.</summary>
        public int Zeile { get; init; }

        /// <summary>Das gelesene Profil — auch bei einer abgelehnten Zeile, damit die Vorschau Nummer und Name zeigt.</summary>
        public Raumnutzungsprofil Profil { get; init; }

        /// <summary>Der Grund, aus dem die Datei die Zeile ablehnt; <c>null</c> = von der Datei her übernehmbar.</summary>
        public string Ablehnung { get; set; }

        /// <summary>Der Grund, aus dem die Zielkategorie die Zeile ablehnt (<c>RaumnutzungCtrl.CsvAbgleichen</c>); <c>null</c> = keiner.</summary>
        public string Zielablehnung { get; set; }

        /// <summary>Die Id des gleichnamigen Profils der Zielkategorie (Rückfrage ersetzen/überspringen); <c>null</c> = neu.</summary>
        public long? Vorhanden { get; set; }

        /// <summary>Wird die Zeile mit „Übernehmen" geschrieben (neu oder, nach der Rückfrage, ersetzend)?</summary>
        public bool Uebernehmbar => Ablehnung == null && Zielablehnung == null;
    }

    /// <summary>Das Ergebnis des Lesens: die Profilzeilen und die Zeilenmeldungen — oder der Abbruch, wenn die Datei als Ganzes nicht lesbar ist.</summary>
    public sealed class RaumnutzungCsvLesung
    {
        /// <summary>Warum die Datei als Ganzes nicht gelesen wird; <c>null</c> = gelesen.</summary>
        public string Abbruch { get; init; }

        /// <summary>Die Profilzeilen in Dateireihenfolge.</summary>
        public List<RaumnutzungCsvZeile> Zeilen { get; } = new List<RaumnutzungCsvZeile>();

        /// <summary>Die Meldungen der Datei (Kopfzeile und Werte).</summary>
        public List<RaumnutzungCsvMeldung> Meldungen { get; } = new List<RaumnutzungCsvMeldung>();

        /// <summary>Die Meldungen des Abgleichs mit der Zielkategorie; jeder Abgleich ersetzt sie.</summary>
        public List<RaumnutzungCsvMeldung> Zielmeldungen { get; } = new List<RaumnutzungCsvMeldung>();

        /// <summary>
        /// Alle Meldungen nach Zeile geordnet (stabil); eine Zeile, die das Ziel ablehnt, meldet keinen Wert mehr als übernommen.
        /// </summary>
        public IReadOnlyList<RaumnutzungCsvMeldung> AlleMeldungen
        {
            get
            {
                var abgelehnt = new HashSet<int>(Zeilen.Where(z => !z.Uebernehmbar).Select(z => z.Zeile));
                return Meldungen.Concat(Zielmeldungen)
                    .Where(m => !(m.Art == RaumnutzungCsvArt.Uebernommen && abgelehnt.Contains(m.Zeile)))
                    .Select((m, i) => (m, i)).OrderBy(x => x.m.Zeile).ThenBy(x => x.i).Select(x => x.m).ToList();
            }
        }

        /// <summary>Die Zeilen, die „Übernehmen" schreibt.</summary>
        public IEnumerable<RaumnutzungCsvZeile> Uebernehmbare => Zeilen.Where(z => z.Uebernehmbar);
    }

    /// <summary>
    /// <b>Das CSV-Format der Nutzungsprofile</b> (Konzept Nutzungsprofile 6.4, NP-F11, Stufe NP4): Leser und Schreiber, ohne
    /// Datenbank. UTF-8 (ein BOM wird toleriert, der Schreiber setzt eines für Tabellenprogramme), Semikolon, Dezimalpunkt,
    /// eine Kopfzeile mit den Spaltennamen aus 4.1, eine Zeile je Profil. Felder mit Semikolon, Anführungszeichen oder
    /// Zeilenumbruch stehen in Anführungszeichen (<c>""</c> für ein Anführungszeichen).
    ///
    /// <para><b>Spalten.</b> <c>Nummer</c>, <c>Bezeichner</c> (Pflicht), <c>Beschreibung</c> und die Kennwerte aus
    /// <see cref="RaumnutzungSchema.SPALTEN_KENNWERTE"/> ohne <c>Nutzungstage_Jahr</c> — die Zahl ist abgeleitet (E93), wird
    /// nicht geschrieben und beim Lesen benannt ignoriert. <c>Heiz_Absenkung_K</c> wird als Alternative zu
    /// <c>Heiz_Soll_Ausserhalb</c> angenommen (<c>Heiz_Soll − Absenkung</c>). Stundenprofile (NP-F9) stehen als
    /// <c>Stunden_&lt;Größe&gt;_&lt;Tagesart&gt;</c> mit 24 Werten, durch Leerzeichen getrennt (Größe als Kennwort
    /// <c>HEIZSOLL</c> … <c>PERSONEN</c>, Tagesart <c>WERKTAG</c>/<c>FREI</c>); das Zeilenbild (NP-F7) als
    /// <c>Zeile_&lt;Größe&gt;_&lt;Zeile&gt;</c> (<c>TAG</c>, <c>NACHT</c>, <c>WOCHENENDE</c>, <c>FERIEN</c>) mit dem Wert, <c>aus</c>
    /// oder <c>-</c> (nur Zeiten) und den Zusätzen <c>von=</c>, <c>bis=</c>, <c>bedingt=</c> — so ist der Rundlauf Export → Import
    /// vollständig. Unbekannte Spalten werden benannt ignoriert, fehlende heißen „leer".</para>
    ///
    /// <para><b>Meldungen.</b> Jeder gelesene Wert meldet sich als übernommen, ignoriert oder Fehler mit Grund (Grenzen aus
    /// 4.1 über <see cref="Konditionierungsgroessen"/>); ein Wert mit Fehler bleibt leer (NP-F11). Eine Zeile ohne Bezeichner,
    /// mit einem Namen oder einer Nummer, die die Datei schon führt (NP-F20, NP-F5), oder die <see cref="RaumnutzungCtrl.Profilpruefung"/>
    /// ablehnt, wird als Ganzes nicht übernommen.</para>
    /// </summary>
    public static class RaumnutzungCsv
    {
        /// <summary>Das Trennzeichen.</summary>
        public const char TRENNER = ';';

        /// <summary>Die Pflichtspalte.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Die Nummer in der Quelle (NP-F5).</summary>
        public const string SPALTE_NUMMER = "Nummer";

        /// <summary>Die Beschreibung.</summary>
        public const string SPALTE_BESCHREIBUNG = "Beschreibung";

        /// <summary>Abgeleitet (E93) — benannt ignoriert, nie geschrieben.</summary>
        public const string SPALTE_TAGE_JAHR = "Nutzungstage_Jahr";

        /// <summary>Die Absenkung in K als Alternative zu <c>Heiz_Soll_Ausserhalb</c>.</summary>
        public const string SPALTE_ABSENKUNG = "Heiz_Absenkung_K";

        /// <summary>Der Präfix der Stundenprofilspalten.</summary>
        public const string PRAEFIX_STUNDEN = "Stunden_";

        /// <summary>Der Präfix der Zeilenbildspalten.</summary>
        public const string PRAEFIX_ZEILE = "Zeile_";

        /// <summary>Die Werte eines Stundenprofils.</summary>
        public const int STUNDEN_JE_TAG = 24;

        /// <summary>„nur Zeiten" in einer Zeilenbildzelle.</summary>
        public const string OHNE_WERT = "-";

        /// <summary>Die Zeichenkodierung des Schreibers: UTF-8 mit BOM.</summary>
        public static readonly Encoding KODIERUNG = new UTF8Encoding(true);

        private static readonly Encoding STRENG = new UTF8Encoding(false, true);

        // =================================================================
        //  Die Spalten
        // =================================================================

        private enum Typ { Stunde, Woche, Bit, Soll, NichtNegativ, Positiv, Anteil, Einheit }

        private sealed record Kennwertspalte(string Name, Typ Typ, Konditionierungsgroesse Groesse,
                                             Func<Raumnutzungsprofil, object> Lesen, Action<Raumnutzungsprofil, object> Setzen);

        private static Kennwertspalte S(string n, Typ t, Func<Raumnutzungsprofil, object> l, Action<Raumnutzungsprofil, object> s,
                                        Konditionierungsgroesse g = Konditionierungsgroesse.Heizsoll)
            => new Kennwertspalte(n, t, g, l, s);

        /// <summary>Die Kennwerte in Schemareihenfolge, ohne <c>Nutzungstage_Jahr</c>.</summary>
        private static readonly IReadOnlyList<Kennwertspalte> KENNWERTE = new[]
        {
            S("Nutzung_Von", Typ.Stunde, p => p.Nutzung_Von, (p, w) => p.Nutzung_Von = (int?)w),
            S("Nutzung_Bis", Typ.Stunde, p => p.Nutzung_Bis, (p, w) => p.Nutzung_Bis = (int?)w),
            S("Betrieb_Von", Typ.Stunde, p => p.Betrieb_Von, (p, w) => p.Betrieb_Von = (int?)w),
            S("Betrieb_Bis", Typ.Stunde, p => p.Betrieb_Bis, (p, w) => p.Betrieb_Bis = (int?)w),
            S("Nutzungstage_Woche", Typ.Woche, p => p.Nutzungstage_Woche, (p, w) => p.Nutzungstage_Woche = (string)w),
            S("Feiertage_Wie_Sonntag", Typ.Bit, p => p.Feiertage_Wie_Sonntag, (p, w) => p.Feiertage_Wie_Sonntag = (bool?)w),
            S("Heiz_Soll", Typ.Soll, p => p.Heiz_Soll, (p, w) => p.Heiz_Soll = (double?)w, Konditionierungsgroesse.Heizsoll),
            S("Heiz_Soll_Ausserhalb", Typ.Soll, p => p.Heiz_Soll_Ausserhalb, (p, w) => p.Heiz_Soll_Ausserhalb = (double?)w, Konditionierungsgroesse.Heizsoll),
            S("Heiz_Aus_Ausserhalb", Typ.Bit, p => p.Heiz_Aus_Ausserhalb, (p, w) => p.Heiz_Aus_Ausserhalb = (bool?)w),
            S("Kuehl_Soll", Typ.Soll, p => p.Kuehl_Soll, (p, w) => p.Kuehl_Soll = (double?)w, Konditionierungsgroesse.Kuehlsoll),
            S("Kuehl_Soll_Ausserhalb", Typ.Soll, p => p.Kuehl_Soll_Ausserhalb, (p, w) => p.Kuehl_Soll_Ausserhalb = (double?)w, Konditionierungsgroesse.Kuehlsoll),
            S("Kuehl_Aus_Ausserhalb", Typ.Bit, p => p.Kuehl_Aus_Ausserhalb, (p, w) => p.Kuehl_Aus_Ausserhalb = (bool?)w),
            S("Aussenluft", Typ.NichtNegativ, p => p.Aussenluft, (p, w) => p.Aussenluft = (double?)w),
            S("Aussenluft_Einheit", Typ.Einheit, p => p.Aussenluft_Einheit, (p, w) => p.Aussenluft_Einheit = (string)w),
            S("Aussenluft_Ausserhalb", Typ.NichtNegativ, p => p.Aussenluft_Ausserhalb, (p, w) => p.Aussenluft_Ausserhalb = (double?)w),
            S("Personen_Flaeche", Typ.Positiv, p => p.Personen_Flaeche, (p, w) => p.Personen_Flaeche = (double?)w),
            S("Personen_Waerme", Typ.NichtNegativ, p => p.Personen_Waerme, (p, w) => p.Personen_Waerme = (double?)w),
            S("Personen_Anteil", Typ.Anteil, p => p.Personen_Anteil, (p, w) => p.Personen_Anteil = (double?)w),
            S("Personen_Anteil_Ausserhalb", Typ.Anteil, p => p.Personen_Anteil_Ausserhalb, (p, w) => p.Personen_Anteil_Ausserhalb = (double?)w),
            S("Geraete_Leistung", Typ.NichtNegativ, p => p.Geraete_Leistung, (p, w) => p.Geraete_Leistung = (double?)w),
            S("Geraete_Anteil", Typ.Anteil, p => p.Geraete_Anteil, (p, w) => p.Geraete_Anteil = (double?)w),
            S("Geraete_Anteil_Ausserhalb", Typ.Anteil, p => p.Geraete_Anteil_Ausserhalb, (p, w) => p.Geraete_Anteil_Ausserhalb = (double?)w),
            S("Beleuchtung_Leistung", Typ.NichtNegativ, p => p.Beleuchtung_Leistung, (p, w) => p.Beleuchtung_Leistung = (double?)w),
            S("Beleuchtung_Anteil", Typ.Anteil, p => p.Beleuchtung_Anteil, (p, w) => p.Beleuchtung_Anteil = (double?)w),
        };

        /// <summary>Die Namen der Kennwertspalten in der Reihenfolge des Schreibers (Prüfhilfe gegen das Schema).</summary>
        public static IReadOnlyList<string> Kennwertspalten => KENNWERTE.Select(k => k.Name).ToList();

        /// <summary>Der Spaltenname eines Stundenprofils.</summary>
        public static string Stundenspalte(Konditionierungsgroesse g, string tagesart)
            => PRAEFIX_STUNDEN + Konditionierungsgroessen.Kennwort(g) + "_" + tagesart;

        /// <summary>Der Spaltenname einer Zeile des Zeilenbilds.</summary>
        public static string Zeilenspalte(Konditionierungsgroesse g, string zeile)
            => PRAEFIX_ZEILE + Konditionierungsgroessen.Kennwort(g) + "_" + zeile;

        private static readonly string[] TAGESARTEN = { RaumnutzungSchema.TAGESART_WERKTAG, RaumnutzungSchema.TAGESART_FREI };

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>Schreibt die Profile als Bytes (UTF-8 mit BOM).</summary>
        public static byte[] SchreibenBytes(IEnumerable<Raumnutzungsprofil> profile)
            => KODIERUNG.GetPreamble().Concat(KODIERUNG.GetBytes(Schreiben(profile))).ToArray();

        /// <summary>
        /// Schreibt die Profile als Text: Kopf, Kennwerte, dann die Stunden- und Zeilenbildspalten, die mindestens ein Profil
        /// trägt (in der Ordnung Größe × Tagesart bzw. Größe × Zeile). Zeilenende CRLF.
        /// </summary>
        public static string Schreiben(IEnumerable<Raumnutzungsprofil> profile)
        {
            List<Raumnutzungsprofil> liste = (profile ?? Enumerable.Empty<Raumnutzungsprofil>()).Where(p => p != null).ToList();
            var stunden = new List<(Konditionierungsgroesse G, string T)>();
            var zeilen = new List<(Konditionierungsgroesse G, string Z)>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                string k = Konditionierungsgroessen.Kennwort(g);
                foreach (string t in TAGESARTEN)
                    if (liste.Any(p => (p.Stunden ?? new List<Raumnutzungsstunden>()).Any(s => s.Groesse == k && s.Tagesart == t)))
                        stunden.Add((g, t));
                foreach (string z in RaumnutzungSchema.ZEILEN)
                    if (liste.Any(p => (p.Zeilen ?? new List<Vorgabezeile>()).Any(v => v.Groesse == k && v.Zeile == z)))
                        zeilen.Add((g, z));
            }

            var kopf = new List<string> { SPALTE_NUMMER, SPALTE_BEZEICHNER, SPALTE_BESCHREIBUNG };
            kopf.AddRange(KENNWERTE.Select(k => k.Name));
            kopf.AddRange(stunden.Select(s => Stundenspalte(s.G, s.T)));
            kopf.AddRange(zeilen.Select(z => Zeilenspalte(z.G, z.Z)));

            var sb = new StringBuilder();
            sb.Append(string.Join(TRENNER.ToString(), kopf.Select(Feld))).Append("\r\n");
            foreach (Raumnutzungsprofil p in liste)
            {
                var felder = new List<string> { p.Nummer ?? "", p.Bezeichner ?? "", p.Beschreibung ?? "" };
                felder.AddRange(KENNWERTE.Select(k => Wert(k.Lesen(p))));
                foreach ((Konditionierungsgroesse g, string t) in stunden)
                {
                    string k = Konditionierungsgroessen.Kennwort(g);
                    Raumnutzungsstunden s = (p.Stunden ?? new List<Raumnutzungsstunden>()).FirstOrDefault(x => x.Groesse == k && x.Tagesart == t);
                    felder.Add(s == null ? "" : string.Join(" ", (s.Werte ?? "").Split(';').Select(x => x.Trim())));
                }
                foreach ((Konditionierungsgroesse g, string z) in zeilen)
                {
                    string k = Konditionierungsgroessen.Kennwort(g);
                    Vorgabezeile v = (p.Zeilen ?? new List<Vorgabezeile>()).FirstOrDefault(x => x.Groesse == k && x.Zeile == z);
                    felder.Add(v == null ? "" : Zelltext(v));
                }
                sb.Append(string.Join(TRENNER.ToString(), felder.Select(Feld))).Append("\r\n");
            }
            return sb.ToString();
        }

        /// <summary>Der Text einer Zeilenbildzelle: Wert, <c>aus</c> oder <c>-</c>, dazu <c>von=</c>, <c>bis=</c>, <c>bedingt=</c>.</summary>
        public static string Zelltext(Vorgabezeile v)
        {
            var teile = new List<string> { v.Aus ? DbWerte.KOND_WOCHE_AUS : v.Wert.HasValue ? Zahl(v.Wert.Value) : OHNE_WERT };
            if (v.Von.HasValue) teile.Add("von=" + v.Von.Value.ToString(CultureInfo.InvariantCulture));
            if (v.Bis.HasValue) teile.Add("bis=" + v.Bis.Value.ToString(CultureInfo.InvariantCulture));
            if (v.BedingtK.HasValue) teile.Add("bedingt=" + Zahl(v.BedingtK.Value));
            return string.Join(" ", teile);
        }

        private static string Wert(object w) => w switch
        {
            null => "",
            bool b => b ? "1" : "0",
            int i => i.ToString(CultureInfo.InvariantCulture),
            double d => Zahl(d),
            _ => Convert.ToString(w, CultureInfo.InvariantCulture),
        };

        private static string Zahl(double d) => d.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Ein Feld, wo nötig in Anführungszeichen.</summary>
        private static string Feld(string t)
        {
            t ??= "";
            bool quoten = t.IndexOfAny(new[] { TRENNER, '"', '\r', '\n' }) >= 0 || t.Length != t.Trim().Length;
            return quoten ? "\"" + t.Replace("\"", "\"\"") + "\"" : t;
        }

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Liest die Bytes einer Datei: UTF-8 (BOM toleriert), sonst benannter Abbruch.</summary>
        public static RaumnutzungCsvLesung Lesen(byte[] inhalt)
        {
            if (inhalt == null || inhalt.Length == 0) return new RaumnutzungCsvLesung { Abbruch = MyResource.Resource.RNP_CSV_MSG_LEER };
            string text;
            try { text = STRENG.GetString(inhalt); }
            catch (DecoderFallbackException) { return new RaumnutzungCsvLesung { Abbruch = MyResource.Resource.RNP_CSV_MSG_KEIN_UTF8 }; }
            return Lesen(text);
        }

        /// <summary>Liest den Text einer Datei (ein führendes BOM wird übergangen).</summary>
        public static RaumnutzungCsvLesung Lesen(string text)
        {
            text ??= "";
            if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);
            List<(int Zeile, List<string> Felder)> saetze = Datensaetze(text)
                .Where(s => s.Felder.Any(f => !string.IsNullOrWhiteSpace(f))).ToList();
            if (saetze.Count == 0) return new RaumnutzungCsvLesung { Abbruch = MyResource.Resource.RNP_CSV_MSG_LEER };

            (int kopfzeile, List<string> kopf) = saetze[0];
            if (kopf.Count == 1 && kopf[0].Contains(','))
                return new RaumnutzungCsvLesung { Abbruch = MyResource.Resource.RNP_CSV_MSG_TRENNER };

            var lesung = new RaumnutzungCsvLesung();
            // Spaltenindex → Bedeutung; null = ignoriert.
            var bedeutung = new Func<Zeilenarbeit, string, int, bool>[kopf.Count];
            var namen = new string[kopf.Count];
            var gesehen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int bezeichner = -1;
            for (int i = 0; i < kopf.Count; i++)
            {
                string n = kopf[i].Trim();
                namen[i] = n;
                if (n.Length == 0 && saetze.Skip(1).All(s => i >= s.Felder.Count || string.IsNullOrWhiteSpace(s.Felder[i])))
                    continue; // eine leere Spalte am Rand (Tabellenprogramm) meldet sich nicht
                if (!gesehen.Add(n))
                {
                    lesung.Meldungen.Add(new RaumnutzungCsvMeldung(kopfzeile, n, RaumnutzungCsvArt.Ignoriert, MyResource.Resource.RNP_CSV_GRUND_DOPPELSPALTE));
                    continue;
                }
                bedeutung[i] = Spaltenleser(n, out string kanonisch);
                if (bedeutung[i] != null)
                {
                    namen[i] = kanonisch;
                    if (kanonisch == SPALTE_BEZEICHNER) bezeichner = i;
                    continue;
                }
                string grund = string.Equals(n, SPALTE_TAGE_JAHR, StringComparison.OrdinalIgnoreCase)
                    ? MyResource.Resource.RNP_CSV_GRUND_TAGE_JAHR
                    : MyResource.Resource.RNP_CSV_GRUND_UNBEKANNT;
                lesung.Meldungen.Add(new RaumnutzungCsvMeldung(kopfzeile, n, RaumnutzungCsvArt.Ignoriert, grund));
            }
            if (bezeichner < 0)
                return new RaumnutzungCsvLesung { Abbruch = MyResource.Resource.RNP_CSV_MSG_OHNE_BEZEICHNER };

            var namenDerDatei = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var nummernDerDatei = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach ((int zeile, List<string> felder) in saetze.Skip(1))
            {
                var arbeit = new Zeilenarbeit(zeile);
                for (int i = 0; i < felder.Count; i++)
                {
                    if (i >= kopf.Count)
                    {
                        if (!string.IsNullOrWhiteSpace(felder[i]))
                            arbeit.Melden("#" + (i + 1).ToString(CultureInfo.InvariantCulture), RaumnutzungCsvArt.Ignoriert,
                                          MyResource.Resource.RNP_CSV_GRUND_UEBERZAEHLIG);
                        continue;
                    }
                    if (bedeutung[i] == null) continue;
                    string t = felder[i].Trim();
                    if (t.Length == 0 && i != bezeichner) continue;
                    bedeutung[i](arbeit, t, i);
                }
                if (felder.Count <= bezeichner) bedeutung[bezeichner](arbeit, "", bezeichner);
                arbeit.Abschliessen(namenDerDatei, nummernDerDatei);
                lesung.Zeilen.Add(arbeit.Ergebnis);
                lesung.Meldungen.AddRange(arbeit.Meldungen);
            }
            return lesung;
        }

        /// <summary>Der Leser einer Spalte — <c>null</c>, wenn die Spalte unbekannt ist.</summary>
        private static Func<Zeilenarbeit, string, int, bool> Spaltenleser(string name, out string kanonisch)
        {
            kanonisch = name;
            if (Gleich(name, SPALTE_BEZEICHNER)) { kanonisch = SPALTE_BEZEICHNER; return (a, t, _) => a.Bezeichner(t); }
            if (Gleich(name, SPALTE_NUMMER)) { kanonisch = SPALTE_NUMMER; return (a, t, _) => a.Nummer(t); }
            if (Gleich(name, SPALTE_BESCHREIBUNG)) { kanonisch = SPALTE_BESCHREIBUNG; return (a, t, _) => a.Beschreibung(t); }
            if (Gleich(name, SPALTE_ABSENKUNG)) { kanonisch = SPALTE_ABSENKUNG; return (a, t, _) => a.Absenkung(t); }
            foreach (Kennwertspalte k in KENNWERTE)
                if (Gleich(name, k.Name))
                {
                    kanonisch = k.Name;
                    Kennwertspalte spalte = k;
                    return (a, t, _) => a.Kennwert(spalte, t);
                }
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                foreach (string ta in TAGESARTEN)
                    if (Gleich(name, Stundenspalte(g, ta)))
                    {
                        kanonisch = Stundenspalte(g, ta);
                        Konditionierungsgroesse gg = g;
                        string tt = ta;
                        return (a, t, _) => a.Stunden(gg, tt, t);
                    }
                foreach (string z in RaumnutzungSchema.ZEILEN)
                    if (Gleich(name, Zeilenspalte(g, z)))
                    {
                        kanonisch = Zeilenspalte(g, z);
                        Konditionierungsgroesse gg = g;
                        string zz = z;
                        return (a, t, _) => a.Zeile(gg, zz, t);
                    }
            }
            return null;
        }

        private static bool Gleich(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        /// <summary>Die Datensätze samt Zeile ihres Beginns; Anführungszeichen dürfen Trenner und Zeilenumbrüche tragen.</summary>
        private static List<(int Zeile, List<string> Felder)> Datensaetze(string text)
        {
            var saetze = new List<(int, List<string>)>();
            var felder = new List<string>();
            var feld = new StringBuilder();
            int zeile = 1, beginn = 1;
            bool inQuote = false, gequotet = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuote)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { feld.Append('"'); i++; }
                        else inQuote = false;
                    }
                    else
                    {
                        if (c == '\n' || (c == '\r' && !(i + 1 < text.Length && text[i + 1] == '\n'))) zeile++;
                        feld.Append(c);
                    }
                    continue;
                }
                if (c == '"' && feld.ToString().Trim().Length == 0 && !gequotet)
                {
                    feld.Clear();
                    inQuote = gequotet = true;
                    continue;
                }
                if (c == TRENNER)
                {
                    felder.Add(feld.ToString());
                    feld.Clear();
                    gequotet = false;
                    continue;
                }
                if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    felder.Add(feld.ToString());
                    saetze.Add((beginn, felder));
                    felder = new List<string>();
                    feld.Clear();
                    gequotet = false;
                    zeile++;
                    beginn = zeile;
                    continue;
                }
                feld.Append(c);
            }
            if (feld.Length > 0 || felder.Count > 0)
            {
                felder.Add(feld.ToString());
                saetze.Add((beginn, felder));
            }
            return saetze;
        }

        // =================================================================
        //  Eine Zeile
        // =================================================================

        /// <summary>Die Arbeit an einer Profilzeile: Werte lesen, Meldungen sammeln, zum Schluss querprüfen.</summary>
        private sealed class Zeilenarbeit
        {
            private readonly int _zeile;
            private readonly Raumnutzungsprofil _p = new Raumnutzungsprofil();
            private readonly List<(string Spalte, string Text)> _gelesen = new List<(string, string)>();
            private double? _absenkung;
            private string _absenkungText;
            internal readonly List<RaumnutzungCsvMeldung> Meldungen = new List<RaumnutzungCsvMeldung>();
            private string _ablehnung;

            internal Zeilenarbeit(int zeile) { _zeile = zeile; }

            internal RaumnutzungCsvZeile Ergebnis { get; private set; }

            internal void Melden(string spalte, RaumnutzungCsvArt art, string grund)
                => Meldungen.Add(new RaumnutzungCsvMeldung(_zeile, spalte, art, grund ?? ""));

            private bool Fehler(string spalte, string grund)
            {
                Melden(spalte, RaumnutzungCsvArt.Fehler, grund);
                return false;
            }

            private void Ablehnen(string spalte, string grund)
            {
                if (_ablehnung != null) return;
                _ablehnung = grund;
                Melden(spalte, RaumnutzungCsvArt.Fehler, grund);
            }

            internal bool Bezeichner(string t)
            {
                if (t.Length == 0) { Ablehnen(SPALTE_BEZEICHNER, MyResource.Resource.RNP_CSV_GRUND_NAME_LEER); return false; }
                if (t.Length > RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN)
                {
                    Ablehnen(SPALTE_BEZEICHNER, Format(MyResource.Resource.RNP_CSV_GRUND_TEXT_ZEILE, RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN));
                    return false;
                }
                _p.Bezeichner = t;
                _gelesen.Add((SPALTE_BEZEICHNER, t));
                return true;
            }

            internal bool Nummer(string t)
            {
                if (t.Length > RaumnutzungSchema.NUMMER_MAX_ZEICHEN)
                {
                    Ablehnen(SPALTE_NUMMER, Format(MyResource.Resource.RNP_CSV_GRUND_TEXT_ZEILE, RaumnutzungSchema.NUMMER_MAX_ZEICHEN));
                    return false;
                }
                _p.Nummer = t;
                _gelesen.Add((SPALTE_NUMMER, t));
                return true;
            }

            internal bool Beschreibung(string t)
            {
                if (t.Length > RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN)
                    return Fehler(SPALTE_BESCHREIBUNG, Format(MyResource.Resource.RNP_CSV_GRUND_TEXT_LAENGE, RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN));
                _p.Beschreibung = t;
                _gelesen.Add((SPALTE_BESCHREIBUNG, t));
                return true;
            }

            internal bool Absenkung(string t)
            {
                if (!Zahl(SPALTE_ABSENKUNG, t, out double d)) return false;
                if (d < 0.0) return Fehler(SPALTE_ABSENKUNG, Grenzen(t, 0.0, double.PositiveInfinity));
                _absenkung = d;
                _absenkungText = t;
                return true;
            }

            internal bool Kennwert(Kennwertspalte k, string t)
            {
                object wert;
                switch (k.Typ)
                {
                    case Typ.Stunde:
                        if (t.Contains(',')) return Fehler(k.Name, Format(MyResource.Resource.RNP_CSV_GRUND_KOMMA, t));
                        if (!int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) || h < 0 || h > 24)
                            return Fehler(k.Name, Format(MyResource.Resource.RNP_CSV_GRUND_STUNDE, t));
                        wert = (int?)h;
                        break;
                    case Typ.Woche:
                        if (t.Length != 7 || t.Any(c => c != '0' && c != '1'))
                            return Fehler(k.Name, Format(MyResource.Resource.RNP_CSV_GRUND_WOCHE, t));
                        wert = t;
                        break;
                    case Typ.Bit:
                        if (t != "0" && t != "1") return Fehler(k.Name, Format(MyResource.Resource.RNP_CSV_GRUND_BIT, t));
                        wert = (bool?)(t == "1");
                        break;
                    case Typ.Einheit:
                        if (t != RaumnutzungSchema.EINHEIT_JE_STUNDE && t != RaumnutzungSchema.EINHEIT_JE_FLAECHE)
                            return Fehler(k.Name, Format(MyResource.Resource.RNP_CSV_GRUND_EINHEIT, t));
                        wert = t;
                        break;
                    default:
                        if (!Zahl(k.Name, t, out double d)) return false;
                        string grund = k.Typ switch
                        {
                            Typ.Soll => Konditionierungsgroessen.ImBereich(k.Groesse, d) ? null
                                : Grenzen(t, Konditionierungsgroessen.Min(k.Groesse), Konditionierungsgroessen.Max(k.Groesse)),
                            Typ.Anteil => d >= 0.0 && d <= 1.0 ? null : Grenzen(t, 0.0, 1.0),
                            Typ.Positiv => d > 0.0 ? null : Format(MyResource.Resource.RNP_CSV_GRUND_POSITIV, t),
                            _ => d >= 0.0 ? null : Grenzen(t, 0.0, double.PositiveInfinity),
                        };
                        if (grund != null) return Fehler(k.Name, grund);
                        wert = (double?)d;
                        break;
                }
                k.Setzen(_p, wert);
                _gelesen.Add((k.Name, t));
                return true;
            }

            internal bool Stunden(Konditionierungsgroesse g, string tagesart, string t)
            {
                string spalte = Stundenspalte(g, tagesart);
                string[] teile = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (teile.Any(x => x.Contains(','))) return Fehler(spalte, Format(MyResource.Resource.RNP_CSV_GRUND_KOMMA, teile.First(x => x.Contains(','))));
                if (teile.Length != STUNDEN_JE_TAG)
                    return Fehler(spalte, Format(MyResource.Resource.RNP_CSV_GRUND_STUNDEN_ZAHL, teile.Length));
                string werte = string.Join(";", teile);
                if (werte.Length > RaumnutzungSchema.STUNDENWERTE_MAX_ZEICHEN || Raumnutzungsgenerator.Stundenwerte(werte, g) == null)
                    return Fehler(spalte, MyResource.Resource.RNP_CSV_GRUND_STUNDEN);
                _p.Stunden.Add(new Raumnutzungsstunden(Konditionierungsgroessen.Kennwort(g), tagesart, werte));
                _gelesen.Add((spalte, t));
                return true;
            }

            internal bool Zeile(Konditionierungsgroesse g, string zeile, string t)
            {
                string spalte = Zeilenspalte(g, zeile);
                var v = new Vorgabezeile { Groesse = Konditionierungsgroessen.Kennwort(g), Zeile = zeile };
                string[] teile = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < teile.Length; i++)
                {
                    string x = teile[i];
                    if (x.Contains(',')) return Fehler(spalte, Format(MyResource.Resource.RNP_CSV_GRUND_KOMMA, x));
                    if (i == 0)
                    {
                        if (string.Equals(x, DbWerte.KOND_WOCHE_AUS, StringComparison.OrdinalIgnoreCase)) v.Aus = true;
                        else if (x == OHNE_WERT) { }
                        else if (double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) && double.IsFinite(w)) v.Wert = w;
                        else return Fehler(spalte, Format(MyResource.Resource.RNP_CSV_GRUND_ZEILE, t));
                        continue;
                    }
                    int gl = x.IndexOf('=');
                    string schluessel = gl > 0 ? x.Substring(0, gl) : "", rest = gl > 0 ? x.Substring(gl + 1) : "";
                    if (Gleich(schluessel, "von") && int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out int von)) v.Von = von;
                    else if (Gleich(schluessel, "bis") && int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out int bis)) v.Bis = bis;
                    else if (Gleich(schluessel, "bedingt") && double.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out double k) && double.IsFinite(k)) v.BedingtK = k;
                    else return Fehler(spalte, Format(MyResource.Resource.RNP_CSV_GRUND_ZEILE, t));
                }
                string f = Konditionierungsarbeit.Zellenpruefung(g, zeile, Konditionierungsstand.Zelle(v));
                if (f != null) return Fehler(spalte, Format(MyResource.Resource.RNP_CSV_GRUND_ZEILE, f));
                _p.Zeilen.Add(v);
                _gelesen.Add((spalte, t));
                return true;
            }

            /// <summary>Querprüfungen, Doppelte der Datei, die Profilprüfung und die Meldungen „übernommen".</summary>
            internal void Abschliessen(Dictionary<string, int> namen, Dictionary<string, int> nummern)
            {
                // Die Absenkung — Alternative zu Heiz_Soll_Ausserhalb.
                if (_absenkung.HasValue)
                {
                    if (_p.Heiz_Soll_Ausserhalb.HasValue)
                        Melden(SPALTE_ABSENKUNG, RaumnutzungCsvArt.Ignoriert, MyResource.Resource.RNP_CSV_GRUND_ABSENKUNG_VORRANG);
                    else if (!_p.Heiz_Soll.HasValue)
                        Fehler(SPALTE_ABSENKUNG, MyResource.Resource.RNP_CSV_GRUND_ABSENKUNG_OHNE_SOLL);
                    else
                    {
                        double aussen = Math.Round(_p.Heiz_Soll.Value - _absenkung.Value, 4, MidpointRounding.AwayFromZero);
                        if (!Konditionierungsgroessen.ImBereich(Konditionierungsgroesse.Heizsoll, aussen))
                            Fehler(SPALTE_ABSENKUNG, Grenzen(aussen.ToString("R", CultureInfo.InvariantCulture),
                                                              Konditionierungsgroessen.Min(Konditionierungsgroesse.Heizsoll),
                                                              Konditionierungsgroessen.Max(Konditionierungsgroesse.Heizsoll)));
                        else
                        {
                            _p.Heiz_Soll_Ausserhalb = aussen;
                            Melden(SPALTE_ABSENKUNG, RaumnutzungCsvArt.Uebernommen,
                                   Format(MyResource.Resource.RNP_CSV_GRUND_ABSENKUNG, aussen.ToString("R", CultureInfo.CurrentCulture)));
                        }
                    }
                }
                // Außenluft ohne Einheit: beide Werte bleiben leer.
                if ((_p.Aussenluft.HasValue || _p.Aussenluft_Ausserhalb.HasValue) && _p.Aussenluft_Einheit == null)
                {
                    _p.Aussenluft = null;
                    _p.Aussenluft_Ausserhalb = null;
                    Fehler("Aussenluft_Einheit", MyResource.Resource.RNP_CSV_GRUND_EINHEIT_FEHLT);
                }
                if (_ablehnung == null && string.IsNullOrEmpty(_p.Bezeichner))
                    Ablehnen(SPALTE_BEZEICHNER, MyResource.Resource.RNP_CSV_GRUND_NAME_LEER);
                if (_ablehnung == null)
                {
                    if (namen.TryGetValue(_p.Bezeichner, out int z))
                        Ablehnen(SPALTE_BEZEICHNER, Format(MyResource.Resource.RNP_CSV_GRUND_NAME_DOPPELT, _p.Bezeichner, z));
                    else if (_p.Nummer != null && nummern.TryGetValue(_p.Nummer, out int zn))
                        Ablehnen(SPALTE_NUMMER, Format(MyResource.Resource.RNP_CSV_GRUND_NUMMER_DOPPELT, _p.Nummer, zn));
                }
                if (_ablehnung == null)
                {
                    namen[_p.Bezeichner] = _zeile;
                    if (_p.Nummer != null) nummern[_p.Nummer] = _zeile;
                    string m = RaumnutzungCtrl.Profilpruefung(_p.Kopie());
                    if (m != null) Ablehnen(SPALTE_BEZEICHNER, Format(MyResource.Resource.RNP_CSV_GRUND_PROFIL, m));
                }
                if (_ablehnung == null)
                    foreach ((string spalte, string text) in _gelesen)
                        if (Noch(spalte))
                            Melden(spalte, RaumnutzungCsvArt.Uebernommen, text);
                Ergebnis = new RaumnutzungCsvZeile { Zeile = _zeile, Profil = _p, Ablehnung = _ablehnung };
            }

            /// <summary>Steht der gelesene Wert der Spalte nach den Querprüfungen noch im Profil?</summary>
            private bool Noch(string spalte)
                => spalte switch
                {
                    "Aussenluft" => _p.Aussenluft.HasValue,
                    "Aussenluft_Ausserhalb" => _p.Aussenluft_Ausserhalb.HasValue,
                    _ => true,
                };

            private bool Zahl(string spalte, string t, out double d)
            {
                d = 0;
                if (t.Contains(',')) return Fehler(spalte, Format(MyResource.Resource.RNP_CSV_GRUND_KOMMA, t));
                if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out d) || !double.IsFinite(d))
                    return Fehler(spalte, Format(MyResource.Resource.RNP_CSV_GRUND_ZAHL, t));
                return true;
            }

            private static string Grenzen(string t, double min, double max)
                => Format(MyResource.Resource.RNP_CSV_GRUND_GRENZEN, t, min.ToString("R", CultureInfo.CurrentCulture),
                          double.IsPositiveInfinity(max) ? "∞" : max.ToString("R", CultureInfo.CurrentCulture));
        }

        private static string Format(string muster, params object[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);
    }
}
