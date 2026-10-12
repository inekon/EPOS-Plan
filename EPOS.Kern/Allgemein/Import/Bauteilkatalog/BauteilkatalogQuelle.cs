using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BAUTEILKATALOG EINLESEN - der Leser des CSV-Exports der Quelltabellen (BK-3, Entscheid E133).
    //
    // FORMAT (Feldinventar, Abschnitt "Format des CSV-Exports"): UTF-8 ohne BOM, Trennzeichen ";",
    // Kopfzeile = SQL-Spaltennamen, RFC 4180 - gequotet nur bei ";", '"' oder Zeilenumbruch, '"'
    // verdoppelt, ein einzelnes LF darf im Feldinneren stehen, Datensaetze enden mit CRLF. Ein
    // zeilenweise lesender Parser scheitert daran; dieser Leser geht Zeichen fuer Zeichen.
    //
    // WERTE: InvariantCulture mit Exponentenschreibweise; NULL ist ein leeres Feld; die Platzhalter
    // -987654321.99 und -987654.32 und Betraege ueber 1e8 heissen "kein Wert" (Entwurf 2.3 Regel 1);
    // GUID in Grossbuchstaben, Leer-GUID (AAAAAAAA-...) und Null-GUID heissen "kein Verweis".
    //
    // PFLICHTSPALTEN: Fehlt eine, wird die ganze Quelle benannt abgelehnt - nie still mit
    // Leerwerten weitergerechnet. Uebrige Spalten sind Kann-Spalten (fehlt eine, bleibt das Feld leer).
    // ====================================================================================

    /// <summary>Eine gelesene Quelltabelle: Kopf (Spaltenname → Index) und Datenzeilen.</summary>
    public sealed class BauteilkatalogTabelle
    {
        internal BauteilkatalogTabelle(string datei, Dictionary<string, int> spalten, List<string[]> zeilen)
        {
            Datei = datei;
            Spalten = spalten;
            Zeilen = zeilen;
        }

        /// <summary>Der Dateiname (ohne Ordner).</summary>
        public string Datei { get; }

        /// <summary>Spaltenname → Index (Groß-/Kleinschreibung egal).</summary>
        internal Dictionary<string, int> Spalten { get; }

        /// <summary>Die Datenzeilen, je Zeile so viele Felder wie der Kopf.</summary>
        internal List<string[]> Zeilen { get; }

        /// <summary>Die Zahl der Datenzeilen.</summary>
        public int Anzahl => Zeilen.Count;

        /// <summary>Das rohe Feld; <c>null</c>, wenn die Spalte fehlt oder das Feld leer ist.</summary>
        internal string Roh(string[] z, string spalte)
        {
            if (!Spalten.TryGetValue(spalte, out int k)) return null;
            string s = z[k];
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        /// <summary>Ein Text (getrimmt); leer → <c>null</c>.</summary>
        internal string Text(string[] z, string spalte) => Roh(z, spalte)?.Trim();

        /// <summary>Eine Zahl nach Regel 1 (Platzhalter und Ausreißer → <c>null</c>).</summary>
        internal double? Zahl(string[] z, string spalte) => BauteilkatalogQuelle.Zahl(Roh(z, spalte));

        /// <summary>Eine ganze Zahl; leer oder nicht lesbar → <c>null</c>.</summary>
        internal int? Ganz(string[] z, string spalte)
        {
            string s = Roh(z, spalte);
            if (s == null) return null;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) return i;
            double? d = BauteilkatalogQuelle.Zahl(s);
            return d.HasValue && d.Value == Math.Floor(d.Value) && Math.Abs(d.Value) < int.MaxValue ? (int)d.Value : (int?)null;
        }

        /// <summary>Ein Schalter (bit): 1/0, auch true/false; sonst <c>null</c>.</summary>
        internal int? Schalter(string[] z, string spalte)
        {
            string s = Roh(z, spalte)?.Trim();
            if (s == null) return null;
            if (s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase)) return 1;
            if (s == "0" || s.Equals("false", StringComparison.OrdinalIgnoreCase)) return 0;
            return null;
        }

        /// <summary>Eine Kennung (GUID, Großbuchstaben); leer, Leer-GUID und Null-GUID → <c>null</c>.</summary>
        internal string Kennung(string[] z, string spalte) => BauteilkatalogQuelle.Kennung(Roh(z, spalte));

        /// <summary>Ein Zeitstempel (ISO); sonst <c>null</c>.</summary>
        internal DateTime? Datum(string[] z, string spalte)
        {
            string s = Roh(z, spalte)?.Trim();
            if (s == null) return null;
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime d) ? d : (DateTime?)null;
        }
    }

    /// <summary>
    /// <b>Der CSV-Export der Quelltabellen</b> (Ordner mit sechs Dateien, E133) — gelesen, aber noch nicht
    /// gedeutet; die Regeln wendet <see cref="BauteilkatalogImport"/> an. <see cref="Abgelehnt"/> nennt den
    /// Grund, wenn die Quelle als Ganzes nicht taugt.
    /// </summary>
    public sealed class BauteilkatalogQuelle
    {
        /// <summary>Die Dateinamen des Exports.</summary>
        public const string DATEI_BAUSTOFFE = "Baustoffe.csv";
        /// <summary>Siehe <see cref="DATEI_BAUSTOFFE"/>.</summary>
        public const string DATEI_AUFBAUTEN = "Aufbauten.csv";
        /// <summary>Siehe <see cref="DATEI_BAUSTOFFE"/>.</summary>
        public const string DATEI_SCHICHTEN = "Schichten.csv";
        /// <summary>Siehe <see cref="DATEI_BAUSTOFFE"/>.</summary>
        public const string DATEI_ABSCHNITTE = "Schichten_Abschnitte.csv";
        /// <summary>Siehe <see cref="DATEI_BAUSTOFFE"/>.</summary>
        public const string DATEI_FENSTER = "Fenster.csv";
        /// <summary>Siehe <see cref="DATEI_BAUSTOFFE"/>.</summary>
        public const string DATEI_BASIS = "Basis_Auszug.csv";

        /// <summary>Die Platzhalter der Quelle, wörtlich (Regel 1).</summary>
        public static readonly IReadOnlyList<string> PLATZHALTER = new[] { "-987654321.99", "-987654.32" };

        /// <summary>Beträge darüber sind Ausreißer und heißen „kein Wert“ (Regel 1).</summary>
        public const double AUSREISSER = 1e8;

        /// <summary>Die Leer-GUID der Quelle.</summary>
        public const string LEER_GUID = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA";

        /// <summary>Pflichtspalten je Datei; <c>true</c> = die Datei selbst ist Pflicht.</summary>
        public static readonly IReadOnlyList<(string Datei, bool Pflicht, string[] Spalten)> PFLICHT = new[]
        {
            (DATEI_BAUSTOFFE, true, new[] { "UId", "ShortDesc", "LongDesc", "HeatConductivity", "Density", "HeatCapacity" }),
            (DATEI_AUFBAUTEN, true, new[] { "UId", "BasisUId", "ShortDesc", "LongDesc", "UValue" }),
            (DATEI_SCHICHTEN, true, new[] { "UId", "DimensionUId", "SeqNum", "ShortDesc", "Thickness", "ThermalConductivity" }),
            (DATEI_ABSCHNITTE, false, new[] { "UId", "DimensionUId", "SectionNum", "SectionShareValve" }),
            (DATEI_FENSTER, false, new[] { "DimensionUId", "GlazingUValue", "GlazingGValue" }),
            (DATEI_BASIS, true, new[] { "UId", "GroupType", "SubType", "MaterialUId" })
        };

        /// <summary>Der Ordner der Quelle.</summary>
        public string Ordner { get; private set; }

        /// <summary>Grund der Ablehnung der ganzen Quelle; <c>null</c> = lesbar.</summary>
        public string Abgelehnt { get; private set; }

        /// <summary>Hinweise des Lesens (fehlende Kann-Dateien).</summary>
        public List<string> Hinweise { get; } = new List<string>();

        /// <summary>Die gelesenen Tabellen je Dateiname; eine fehlende Kann-Datei fehlt hier.</summary>
        public Dictionary<string, BauteilkatalogTabelle> Tabellen { get; } =
            new Dictionary<string, BauteilkatalogTabelle>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Die Tabelle einer Datei; <c>null</c>, wenn sie fehlt.</summary>
        public BauteilkatalogTabelle Tabelle(string datei) => Tabellen.TryGetValue(datei, out BauteilkatalogTabelle t) ? t : null;

        /// <summary>
        /// Liest den Ordner. Ein Pfad auf eine Datei (die Wahl von <c>Baustoffe.csv</c>) meint ihren Ordner.
        /// Wirft nicht: Ein Lesefehler steht in <see cref="Abgelehnt"/>.
        /// </summary>
        public static BauteilkatalogQuelle Lesen(string pfad)
        {
            var q = new BauteilkatalogQuelle();
            string ordner = pfad;
            if (!string.IsNullOrWhiteSpace(pfad) && File.Exists(pfad)) ordner = Path.GetDirectoryName(Path.GetFullPath(pfad));
            q.Ordner = ordner;
            if (string.IsNullOrWhiteSpace(ordner) || !Directory.Exists(ordner))
            {
                q.Abgelehnt = "Ordner nicht gefunden: " + (pfad ?? "");
                return q;
            }
            foreach ((string datei, bool pflicht, string[] spalten) in PFLICHT)
            {
                string voll = Path.Combine(ordner, datei);
                if (!File.Exists(voll))
                {
                    if (pflicht) { q.Abgelehnt = "Datei fehlt: " + datei; return q; }
                    q.Hinweise.Add("Datei fehlt, ohne sie weiter: " + datei);
                    continue;
                }
                string text;
                try { text = File.ReadAllText(voll, new UTF8Encoding(false)); }
                catch (Exception ex) { q.Abgelehnt = datei + ": nicht lesbar (" + ex.Message + ")"; return q; }
                string fehler = AusText(q, datei, text, spalten);
                if (fehler != null) { q.Abgelehnt = fehler; return q; }
            }
            return q;
        }

        /// <summary>Nimmt den Text einer Datei auf; Rückgabe: Ablehnungsgrund oder <c>null</c>.</summary>
        internal static string AusText(BauteilkatalogQuelle q, string datei, string text, IEnumerable<string> pflicht)
        {
            List<string[]> zeilen = Zerlegen(text, out string fehler);
            if (zeilen == null) return datei + ": " + fehler;
            if (zeilen.Count == 0) return datei + ": ohne Kopfzeile";
            string[] kopf = zeilen[0];
            var spalten = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < kopf.Length; i++)
            {
                string n = kopf[i].Trim();
                if (n.Length > 0 && !spalten.ContainsKey(n)) spalten[n] = i;
            }
            List<string> fehlt = pflicht.Where(p => !spalten.ContainsKey(p)).ToList();
            if (fehlt.Count > 0) return datei + ": Pflichtspalte fehlt (" + string.Join(", ", fehlt) + ")";
            var daten = new List<string[]>(zeilen.Count);
            for (int i = 1; i < zeilen.Count; i++)
            {
                string[] z = zeilen[i];
                if (z.Length == 1 && z[0].Length == 0) continue;
                if (z.Length != kopf.Length)
                    return datei + ": Datensatz " + i.ToString(CultureInfo.InvariantCulture) + " hat " +
                           z.Length.ToString(CultureInfo.InvariantCulture) + " statt " + kopf.Length.ToString(CultureInfo.InvariantCulture) + " Felder";
                daten.Add(z);
            }
            q.Tabellen[datei] = new BauteilkatalogTabelle(datei, spalten, daten);
            return null;
        }

        /// <summary>
        /// <b>RFC-4180-Zerlegung</b> mit Trennzeichen „;“: gequotete Felder dürfen „;“, '"' (verdoppelt) und
        /// Zeilenumbrüche tragen; ein Datensatz endet an CRLF, CR oder LF außerhalb der Anführungszeichen.
        /// Rückgabe <c>null</c> mit <paramref name="fehler"/> bei kaputtem Quoting.
        /// </summary>
        public static List<string[]> Zerlegen(string text, out string fehler)
        {
            fehler = null;
            var zeilen = new List<string[]>();
            if (string.IsNullOrEmpty(text)) return zeilen;
            var felder = new List<string>();
            var sb = new StringBuilder();
            int n = text.Length;
            int i = text[0] == '﻿' ? 1 : 0;
            int satz = 1;
            while (i < n)
            {
                string feld;
                if (text[i] == '"')
                {
                    sb.Clear();
                    i++;
                    while (true)
                    {
                        if (i >= n) { fehler = "Anführungszeichen in Datensatz " + satz.ToString(CultureInfo.InvariantCulture) + " nicht geschlossen"; return null; }
                        char d = text[i++];
                        if (d == '"')
                        {
                            if (i < n && text[i] == '"') { sb.Append('"'); i++; }
                            else break;
                        }
                        else sb.Append(d);
                    }
                    feld = sb.ToString();
                    if (i < n && text[i] != ';' && text[i] != '\r' && text[i] != '\n')
                    {
                        fehler = "Zeichen nach schließendem Anführungszeichen in Datensatz " + satz.ToString(CultureInfo.InvariantCulture);
                        return null;
                    }
                }
                else
                {
                    int s = i;
                    while (i < n && text[i] != ';' && text[i] != '\r' && text[i] != '\n') i++;
                    feld = text.Substring(s, i - s);
                }
                felder.Add(feld);
                if (i >= n) break;
                if (text[i] == ';')
                {
                    i++;
                    if (i >= n) felder.Add("");
                    continue;
                }
                if (text[i] == '\r') { i++; if (i < n && text[i] == '\n') i++; }
                else i++;
                zeilen.Add(felder.ToArray());
                felder.Clear();
                satz++;
            }
            if (felder.Count > 0) zeilen.Add(felder.ToArray());
            return zeilen;
        }

        /// <summary>Eine Zahl nach Regel 1: invariant, mit Exponent; Platzhalter, Ausreißer, NaN → <c>null</c>.</summary>
        public static double? Zahl(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            if (PLATZHALTER.Contains(s)) return null;
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return null;
            if (double.IsNaN(d) || double.IsInfinity(d) || Math.Abs(d) > AUSREISSER) return null;
            if (Math.Abs(d - (-987654321.99)) < 1e-6 || Math.Abs(d - (-987654.32)) < 1e-9) return null;
            return d;
        }

        /// <summary>Eine GUID in Großbuchstaben; leer, Leer-GUID, Null-GUID oder unlesbar → <c>null</c>.</summary>
        public static string Kennung(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim().Trim('{', '}');
            if (!Guid.TryParse(s, out Guid g) || g == Guid.Empty) return null;
            string k = g.ToString("D").ToUpperInvariant();
            return k == LEER_GUID ? null : k;
        }
    }
}
