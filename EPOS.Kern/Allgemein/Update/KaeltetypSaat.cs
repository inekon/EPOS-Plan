using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER KATALOG TYPISCHER KAELTEBEDARFE - Welle K1, Konzept Kaeltebedarf 3.3 (Entscheide E-K1, E-K3).
    //
    // WAS. Sechs ausgelieferte Kaeltebedarfssaetze: je ein Kopfsatz in Tab_Kaeltebedarf_STAMM (zwoelf
    // Monatswerte, Jahresmenge 100 MWh, Temperaturpaar als ANGABE ohne Wirkung auf die Erzeuger) und ein
    // gleichnamiges Wochenprofil in Tab_Kaeltetyp_STAMM (168 Stundenwerte), beide ReadOnly = 1. Neutral benannt,
    // runde Werte, Vermerk "Formmodell, keine Messung" - keine Hersteller- und keine Messdaten. Nur sensible Kaelte.
    //
    // DIE MONATSWERTE folgen den Monatsfaktoren, gewichtet mit den Kalendertagen des Monats:
    //   Q_M(m) = 100 MWh * f(m) * d(m) / Summe f(k) * d(k)
    // Die Wochenprofile tragen relative Last (1 = Spitze); die Profilroutine normiert je Monat auf den Monatswert.
    //
    // WIEDERHOLBAR UND NIE UEBERSCHREIBEND (Muster ProzesstypSaat): Schluessel ist der Bezeichner, je Tabelle fuer
    // sich. Ein EIGENER gleichnamiger Satz des Anwenders (ReadOnly = 0) bleibt und steht im Bericht. Je Satz ein
    // Vorgang (Kopfsatz und Wochenprofil zusammen oder gar nicht). Katalogschluessel und Pruefsumme vergibt
    // danach KatalogSchluesselSaat (Schritt KaeltebedarfSchema).
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt ordnet einen dieser Saetze zu; der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>Ein ausgelieferter Satz eines typischen Kältebedarfs (Konzept Kältebedarf 3.3).</summary>
    public sealed class KaeltetypSaatsatz
    {
        internal KaeltetypSaatsatz(string name, string kurz, double vorlauf, double ruecklauf,
                                   double[] woche, double[] monatsfaktoren)
        {
            Name = name;
            Kurz = kurz;
            Vorlauf = vorlauf;
            Ruecklauf = ruecklauf;
            Woche = woche;
            Monatsfaktoren = monatsfaktoren;
        }

        /// <summary>Der Bezeichner — derselbe im Kopfsatz, als Typ und im Wochenprofil.</summary>
        public string Name { get; }

        /// <summary>Der Bedarf in einem Satz (ohne den Vermerk).</summary>
        public string Kurz { get; }

        /// <summary>Vorlauf [°C] — Angabe ohne Wirkung.</summary>
        public double Vorlauf { get; }

        /// <summary>Rücklauf [°C] — Angabe ohne Wirkung.</summary>
        public double Ruecklauf { get; }

        /// <summary>Das Wochenprofil, 168 Werte, Index 0 = Montag 0 Uhr.</summary>
        public double[] Woche { get; }

        /// <summary>Die zwölf Monatsfaktoren.</summary>
        public double[] Monatsfaktoren { get; }

        /// <summary>Die Beschreibung des Satzes samt Vermerk.</summary>
        public string Beschreibung => Kurz + " " + KaeltetypSaat.VERMERK;

        /// <summary>Die zwölf Monatswerte [MWh], zusammen <see cref="KaeltetypSaat.JAHRESMENGE_MWH"/>.</summary>
        public double[] Monatswerte()
        {
            double nenner = 0;
            for (int m = 0; m < 12; m++) nenner += Monatsfaktoren[m] * KaeltetypSaat.TAGE[m];
            var w = new double[12];
            for (int m = 0; m < 12; m++)
                w[m] = KaeltetypSaat.JAHRESMENGE_MWH * Monatsfaktoren[m] * KaeltetypSaat.TAGE[m] / nenner;
            return w;
        }

        /// <summary>Die Summe der 168 Wochenwerte.</summary>
        public double Wochensumme()
        {
            double s = 0;
            foreach (double x in Woche) s += x;
            return s;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// <b>Die Saat der sechs typischen Kältebedarfe</b> (Konzept Kältebedarf 3.3) — Werte und Regeln stehen im Kopf
    /// der Datei. Gerufen im Schemaschritt <see cref="KaeltebedarfSchema"/> (Migration, Werkzeug, Testvorrichtung).
    /// </summary>
    public static class KaeltetypSaat
    {
        /// <summary>Der Vermerk jeder Beschreibung.</summary>
        public const string VERMERK = "Formmodell, keine Messung; nur sensible Kaelte; Jahresmenge 100 MWh, im Projekt über die Jahressumme skalieren.";

        /// <summary>Die Jahresmenge je Satz [MWh].</summary>
        public const double JAHRESMENGE_MWH = 100;

        /// <summary>Die Kalendertage der zwölf Monate eines Nicht-Schaltjahres.</summary>
        internal static readonly int[] TAGE = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Der Kopfkatalog.</summary>
        public const string TAB_KOPF = KaeltebedarfSchema.TAB_KOPF_STAMM;

        /// <summary>Der Typkatalog der Wochenprofile.</summary>
        public const string TAB_TYP = KaeltebedarfSchema.TAB_TYP_STAMM;

        private static readonly KaeltetypSaatsatz[] SAETZE = Bauen();

        /// <summary>Die sechs Sätze in Auslieferungsreihenfolge.</summary>
        public static IReadOnlyList<KaeltetypSaatsatz> Alle => SAETZE;

        // =================================================================================
        // Die sechs Bedarfe (Konzept Kaeltebedarf, Tafel 3.3)
        // =================================================================================

        private static KaeltetypSaatsatz[] Bauen()
        {
            int[] werktage = Tage(5), mitSamstag = Tage(6), alle = Tage(7);
            double[] gleich = Faktoren(1.0);

            return new[]
            {
                // Mo–Fr 7–19 Uhr, Spitze nachmittags 14–17 Uhr; Mai bis September, Spitze Juli/August.
                new KaeltetypSaatsatz("Raumkühlung Büro",
                    "Raumkühlung eines Bürobaus Montag bis Freitag 7 bis 19 Uhr mit Spitze nachmittags 14 bis 17 Uhr, Wochenende ohne Kühlung; Mai bis September, Spitze Juli und August.",
                    16, 19, Woche(werktage, (7, 12, 0.6), (12, 14, 0.8), (14, 17, 1.0), (17, 19, 0.7)),
                    Faktoren(0.0, (5, 0.3), (6, 0.7), (7, 1.0), (8, 1.0), (9, 0.5))),
                // Mo–Sa 8–21 Uhr, Sonntag 0; April bis Oktober, flacher als Büro.
                new KaeltetypSaatsatz("Raumkühlung Handel",
                    "Raumkühlung einer Verkaufsfläche Montag bis Samstag 8 bis 21 Uhr, Sonntag ohne Kühlung; April bis Oktober, flacher als im Büro.",
                    16, 19, Woche(mitSamstag, (8, 12, 0.7), (12, 18, 1.0), (18, 21, 0.8)),
                    Faktoren(0.0, (4, 0.3), (5, 0.6), (6, 0.9), (7, 1.0), (8, 1.0), (9, 0.8), (10, 0.4))),
                // 168 Stunden gleich, zwölf Monate gleich.
                new KaeltetypSaatsatz("Prozesskälte Dauerlast",
                    "Prozesskälte als Dauerlast an sieben Tagen rund um die Uhr, zwölf Monate gleich.",
                    6, 12, Woche(alle, (0, 24, 1.0)), gleich),
                // 168 Stunden, tags 6–20 Uhr leicht erhöht (Türöffnungen); leichter Sommeranstieg.
                new KaeltetypSaatsatz("Kühlraum",
                    "Kühlraum an sieben Tagen rund um die Uhr, tags 6 bis 20 Uhr leicht erhöht durch Türöffnungen und Einlagerung; leichter Sommeranstieg Mai bis September.",
                    -2, 4, Woche(alle, (0, 6, 0.85), (6, 20, 1.0), (20, 24, 0.85)),
                    Faktoren(1.0, (5, 1.05), (6, 1.1), (7, 1.15), (8, 1.15), (9, 1.05))),
                // 168 Stunden, tags leicht erhöht; leichter Sommeranstieg.
                new KaeltetypSaatsatz("Tiefkühlraum",
                    "Tiefkühlraum an sieben Tagen rund um die Uhr, tags 6 bis 20 Uhr leicht erhöht durch Türöffnungen und Einlagerung; leichter Sommeranstieg Juni bis September.",
                    -30, -24, Woche(alle, (0, 6, 0.9), (6, 20, 1.0), (20, 24, 0.9)),
                    Faktoren(1.0, (6, 1.05), (7, 1.1), (8, 1.1), (9, 1.05))),
                // 168 Stunden gleich, zwölf Monate gleich.
                new KaeltetypSaatsatz("Serverraum",
                    "Serverraum mit gleichbleibender innerer Last an sieben Tagen rund um die Uhr, zwölf Monate gleich.",
                    18, 24, Woche(alle, (0, 24, 1.0)), gleich),
            };
        }

        /// <summary>Die Wochentage 0 (Montag) bis <paramref name="anzahl"/> − 1.</summary>
        private static int[] Tage(int anzahl)
        {
            var t = new int[anzahl];
            for (int i = 0; i < anzahl; i++) t[i] = i;
            return t;
        }

        /// <summary>Ein Wochenprofil: an jedem genannten Tag die Stundenbänder [von, bis) mit ihrer Last.</summary>
        private static double[] Woche(int[] tage, params (int Von, int Bis, double Last)[] baender)
        {
            var w = new double[168];
            foreach (int tag in tage)
                foreach ((int von, int bis, double last) in baender)
                    for (int h = von; h < bis; h++) w[tag * 24 + h] = last;
            return w;
        }

        /// <summary>Zwölf Faktoren mit Grundwert und Ausnahmen (Monat 1–12).</summary>
        private static double[] Faktoren(double grund, params (int Monat, double Faktor)[] ausnahmen)
        {
            var f = new double[12];
            for (int m = 0; m < 12; m++) f[m] = grund;
            foreach ((int monat, double faktor) in ausnahmen) f[monat - 1] = faktor;
            return f;
        }

        // =================================================================================
        // Der Schreibweg
        // =================================================================================

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Zahl der in diesem Lauf angelegten Kopfsätze.</summary>
            public int Koepfe { get; internal set; }

            /// <summary>Zahl der in diesem Lauf angelegten Wochenprofile.</summary>
            public int Profile { get; internal set; }

            /// <summary>Namen, unter denen ein EIGENER Satz des Anwenders steht (übergangen).</summary>
            public List<string> Eigene { get; } = new List<string>();

            /// <summary>Die Zeile für Protokoll und Werkzeug.</summary>
            public string Zeile()
                => Koepfe.ToString(CultureInfo.InvariantCulture) + " Kaeltebedarfssatz/-saetze und " +
                   Profile.ToString(CultureInfo.InvariantCulture) + " Wochenprofil(e) von " +
                   Alle.Count.ToString(CultureInfo.InvariantCulture) + " Kaeltebedarfen gesaet (ReadOnly = 1)";
        }

        /// <summary>Steht jeder Satz unter seinem Namen in beiden Katalogen?</summary>
        public static bool Vollstaendig()
        {
            if (!DataRepository.TabelleVorhanden(TAB_KOPF) || !DataRepository.TabelleVorhanden(TAB_TYP)) return false;
            foreach (KaeltetypSaatsatz s in Alle)
            {
                DataRow kopf = Zeile(TAB_KOPF, s.Name), typ = Zeile(TAB_TYP, s.Name);
                if (Eigen(kopf) || Eigen(typ)) continue;      // ein eigener Satz belegt den Namen
                if (kopf == null || typ == null) return false;
            }
            return true;
        }

        private static bool Eigen(DataRow r) => r != null && !Kennzeichen(r);

        /// <summary>
        /// Schreibt die fehlenden Sätze, je Satz in einem Vorgang. <b>Wiederholbar und nie überschreibend.</b>
        /// Setzt die Tabellen des Schemaschritts voraus. Fehler werfen.
        /// </summary>
        public static Bericht Ausfuehren(IList<string> bericht)
        {
            if (!DataRepository.TabelleVorhanden(TAB_KOPF) || !DataRepository.TabelleVorhanden(TAB_TYP))
                throw new InvalidOperationException("Die Tabellen " + TAB_KOPF + " und " + TAB_TYP + " fehlen.");

            var b = new Bericht();
            foreach (KaeltetypSaatsatz s in Alle)
            {
                DataRow kopf = Zeile(TAB_KOPF, s.Name);
                DataRow typ = Zeile(TAB_TYP, s.Name);
                if (Eigen(kopf) || Eigen(typ))
                {
                    b.Eigene.Add(s.Name);
                    bericht?.Add("Kaeltebedarf \"" + s.Name + "\" nicht gesaet: ein eigener Satz oder Typ traegt den Namen");
                    continue;
                }
                if (kopf != null && typ != null) continue;

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        if (kopf == null) { KopfSchreiben(v, s); b.Koepfe++; }
                        if (typ == null) { TypSchreiben(v, s); b.Profile++; }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
            }
            bericht?.Add(b.Zeile());
            return b;
        }

        private static DataRow Zeile(string tabelle, string name)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ReadOnly FROM \"" + tabelle + "\" WHERE Bezeichner = ?", new DbParam("@b", name));
            return dt != null && dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private static bool Kennzeichen(DataRow r)
            => r["ReadOnly"] != DBNull.Value && Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture) != 0;

        /// <summary>Der Kopfsatz: Name, Typ = Name, Beschreibung, zwölf Monatswerte, Paar, ReadOnly = 1.</summary>
        private static void KopfSchreiben(DbVorgang v, KaeltetypSaatsatz s)
        {
            double[] monat = s.Monatswerte();
            var spalten = new StringBuilder("Bezeichner, Typ, Beschreibung");
            var werte = new StringBuilder("?, ?, ?");
            var p = new List<DbParam>
            {
                new DbParam("@bz", s.Name),
                new DbParam("@ty", s.Name),
                new DbParam("@be", s.Beschreibung)
            };
            for (int m = 0; m < 12; m++)
            {
                spalten.Append(", Monat_" + (m + 1).ToString(CultureInfo.InvariantCulture));
                werte.Append(", ?");
                p.Add(new DbParam("@m" + (m + 1).ToString("D2", CultureInfo.InvariantCulture), monat[m]));
            }
            spalten.Append(", Vorlauf, Ruecklauf, ReadOnly");
            werte.Append(", ?, ?, 1");
            p.Add(new DbParam("@vl", s.Vorlauf));
            p.Add(new DbParam("@rl", s.Ruecklauf));
            v.Ausfuehren("INSERT INTO Tab_Kaeltebedarf_STAMM (" + spalten + ") VALUES (" + werte + ")", p.ToArray());
        }

        /// <summary>Das Wochenprofil: Name, Beschreibung, 168 Werte, ReadOnly = 1.</summary>
        private static void TypSchreiben(DbVorgang v, KaeltetypSaatsatz s)
        {
            var spalten = new StringBuilder("Bezeichner, Beschreibung");
            var werte = new StringBuilder("?, ?");
            var p = new List<DbParam>
            {
                new DbParam("@bz", s.Name),
                new DbParam("@be", s.Beschreibung)
            };
            for (int j = 0; j < 168; j++)
            {
                spalten.Append(", \"" + (j + 1).ToString(CultureInfo.InvariantCulture) + "\"");
                werte.Append(", ?");
                p.Add(new DbParam("@w" + (j + 1).ToString("D3", CultureInfo.InvariantCulture), s.Woche[j]));
            }
            spalten.Append(", ReadOnly");
            werte.Append(", 1");
            v.Ausfuehren("INSERT INTO Tab_Kaeltetyp_STAMM (" + spalten + ") VALUES (" + werte + ")", p.ToArray());
        }
    }
}
