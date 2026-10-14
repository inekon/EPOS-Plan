using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER KATALOG TYPISCHER BETRIEBSWEISEN DER PROZESSWÄRME - Welle M3a, Entscheidungsvorlage
    // Modellgrenzen PW5 (Wellenplan vom Anwender freigegeben).
    //
    // WAS. Acht ausgelieferte Prozesswärmesätze: je ein Kopfsatz in Tab_Prozesswaerme_STAMM
    // (zwölf Monatswerte, Jahresmenge 100 MWh, Temperaturpaar als Vorbelegung nach PW1) und ein
    // gleichnamiges Wochenprofil in Tab_Prozesstyp_STAMM (168 Stundenwerte), beide ReadOnly = 1.
    // Neutral benannt, runde Werte, Vermerk „Schichtmodell, keine Messung" - keine Hersteller-
    // und keine Messdaten. Im Projekt wird die Menge über den Jahresverbrauch skaliert.
    //
    // DIE MONATSWERTE folgen den Monatsfaktoren, gewichtet mit den Kalendertagen des Monats:
    //   Q_M(m) = 100 MWh · f(m) · d(m) / Σ f(k) · d(k)
    // Die Wochenprofile tragen relative Last (1 = Volllast); die Skala ist frei, denn die
    // Profilroutine normiert je Monat auf den Monatswert.
    //
    // WIEDERHOLBAR UND NIE ÜBERSCHREIBEND (Muster Konditionierungsvorlagen-Saat): Schlüssel ist der
    // Bezeichner, je Tabelle für sich. Steht ein Satz schon, wird er übergangen; ein EIGENER
    // gleichnamiger Satz des Anwenders (ReadOnly = 0) bleibt und steht im Bericht. Je Satz ein
    // Vorgang (Kopfsatz und Wochenprofil zusammen oder gar nicht).
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt ordnet einen dieser Sätze zu; der Lauf liest nur
    // Projektkopien. Der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>Ein ausgelieferter Satz einer typischen Betriebsweise (PW5).</summary>
    public sealed class ProzesstypSaatsatz
    {
        internal ProzesstypSaatsatz(string name, string kurz, double vorlauf, double ruecklauf,
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

        /// <summary>Die Betriebsweise in einem Satz (ohne den Vermerk).</summary>
        public string Kurz { get; }

        /// <summary>Vorbelegung des Vorlaufs [°C].</summary>
        public double Vorlauf { get; }

        /// <summary>Vorbelegung des Rücklaufs [°C].</summary>
        public double Ruecklauf { get; }

        /// <summary>Das Wochenprofil, 168 Werte, Index 0 = Montag 0 Uhr.</summary>
        public double[] Woche { get; }

        /// <summary>Die zwölf Monatsfaktoren.</summary>
        public double[] Monatsfaktoren { get; }

        /// <summary>Die Beschreibung des Satzes samt Vermerk.</summary>
        public string Beschreibung => Kurz + " " + ProzesstypSaat.VERMERK;

        /// <summary>Die zwölf Monatswerte [MWh], zusammen <see cref="ProzesstypSaat.JAHRESMENGE_MWH"/>.</summary>
        public double[] Monatswerte()
        {
            double nenner = 0;
            for (int m = 0; m < 12; m++) nenner += Monatsfaktoren[m] * ProzesstypSaat.TAGE[m];
            var w = new double[12];
            for (int m = 0; m < 12; m++)
                w[m] = ProzesstypSaat.JAHRESMENGE_MWH * Monatsfaktoren[m] * ProzesstypSaat.TAGE[m] / nenner;
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
    /// <b>Die Saat der acht typischen Betriebsweisen der Prozesswärme</b> (PW5) — Werte und Regeln
    /// stehen im Kopf der Datei. Gerufen im Schemaschritt <see cref="ProzesswaermeTemperaturSchema"/>
    /// (Migration, Werkzeug, Testvorrichtung).
    /// </summary>
    public static class ProzesstypSaat
    {
        /// <summary>Der Vermerk jeder Beschreibung.</summary>
        public const string VERMERK = "Schichtmodell, keine Messung; Jahresmenge 100 MWh, im Projekt über den Jahresverbrauch skalieren.";

        /// <summary>Die Jahresmenge je Satz [MWh].</summary>
        public const double JAHRESMENGE_MWH = 100;

        /// <summary>Die Kalendertage der zwölf Monate eines Nicht-Schaltjahres.</summary>
        internal static readonly int[] TAGE = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Der Kopfkatalog.</summary>
        public const string TAB_KOPF = "Tab_Prozesswaerme_STAMM";

        /// <summary>Der Typkatalog der Wochenprofile.</summary>
        public const string TAB_TYP = "Tab_Prozesstyp_STAMM";

        private static readonly ProzesstypSaatsatz[] SAETZE = Bauen();

        /// <summary>Die acht Sätze in Auslieferungsreihenfolge.</summary>
        public static IReadOnlyList<ProzesstypSaatsatz> Alle => SAETZE;

        // =================================================================================
        // Die acht Betriebsweisen (Entscheidungsvorlage Modellgrenzen, Tafel PW5)
        // =================================================================================

        private static ProzesstypSaatsatz[] Bauen()
        {
            double[] ferien = Faktoren(1.0, (8, 0.4), (12, 0.8));
            double[] eins = Faktoren(1.0);

            return new[]
            {
                // Mo–Fr 6–14 Uhr Volllast, die Stunde davor 50 %.
                new ProzesstypSaatsatz("Einschicht 5 Tage",
                    "Einschichtbetrieb Montag bis Freitag 6 bis 14 Uhr, die Stunde davor halbe Last; August 0,4 (Betriebsferien), Dezember 0,8.",
                    60, 40, Woche(Werktage(5), (5, 0.5), (6, 14, 1.0)), ferien),
                // Mo–Fr 6–22 Uhr.
                new ProzesstypSaatsatz("Zweischicht 5 Tage",
                    "Zweischichtbetrieb Montag bis Freitag 6 bis 22 Uhr; August 0,4 (Betriebsferien), Dezember 0,8.",
                    70, 50, Woche(Werktage(5), (6, 22, 1.0)), ferien),
                // Mo 6 Uhr bis Sa 6 Uhr durchgehend.
                new ProzesstypSaatsatz("Dreischicht 5 Tage",
                    "Dreischichtbetrieb von Montag 6 Uhr bis Samstag 6 Uhr durchgehend; August 0,4 (Betriebsferien), Dezember 0,8.",
                    80, 60, Durchgehend(6, 5 * 24 + 6), ferien),
                // 7 × 24 h, Nacht (22–6 Uhr) 10 % unter dem Tag; ein Revisionsmonat 0,7 (August).
                new ProzesstypSaatsatz("Durchlaufbetrieb 7 Tage",
                    "Durchlaufbetrieb an sieben Tagen rund um die Uhr, nachts 22 bis 6 Uhr 10 % unter der Tageslast; Revisionsmonat August 0,7.",
                    90, 70, Woche(Werktage(7), (0, 6, 0.9), (6, 22, 1.0), (22, 24, 0.9)), Faktoren(1.0, (8, 0.7))),
                // Mo–Fr zwei Spitzen je Schichtende (14 und 22 Uhr).
                new ProzesstypSaatsatz("Reinigung/Spülen (CIP)",
                    "Reinigung im Kreislauf (CIP) Montag bis Freitag mit je einer Spitze am Schichtende 14 und 22 Uhr.",
                    75, 40, Woche(Werktage(5), (14, 1.0), (22, 1.0)), eins),
                // Mo–Fr 6–22 Uhr, Anfahrspitze 150 % in der ersten Stunde.
                new ProzesstypSaatsatz("Trocknung/Lackierung",
                    "Trocknung oder Lackierung Montag bis Freitag 6 bis 22 Uhr mit Anfahrspitze 150 % in der ersten Stunde.",
                    120, 90, Woche(Werktage(5), (6, 1.5), (7, 22, 1.0)), eins),
                // Mo–Fr 6–18 Uhr, Aufheizspitze 200 % in der ersten Stunde des Montags.
                new ProzesstypSaatsatz("Waschen/Bäder",
                    "Waschen oder Bäder (Galvanik, Wäscherei) Montag bis Freitag 6 bis 18 Uhr mit Aufheizspitze 200 % zum Wochenstart.",
                    60, 45, Mit(Woche(Werktage(5), (6, 18, 1.0)), 6, 2.0), eins),
                // Mo–Fr 5–20 Uhr; Winter (Oktober bis April) 1,0, Sommer (Mai bis September) 0,1.
                new ProzesstypSaatsatz("Raumlufttechnik Halle",
                    "Lufterwärmung einer Halle Montag bis Freitag 5 bis 20 Uhr; Winter Oktober bis April 1,0, Sommer Mai bis September 0,1.",
                    50, 30, Woche(Werktage(5), (5, 20, 1.0)), Faktoren(1.0, (5, 0.1), (6, 0.1), (7, 0.1), (8, 0.1), (9, 0.1))),
            };
        }

        /// <summary>Die Wochentage 0 (Montag) bis <paramref name="anzahl"/> − 1.</summary>
        private static int[] Werktage(int anzahl)
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

        /// <summary>Ein Wochenprofil aus Einzelstunden und Bändern.</summary>
        private static double[] Woche(int[] tage, (int Stunde, double Last) a, (int Von, int Bis, double Last) b)
            => Woche(tage, (a.Stunde, a.Stunde + 1, a.Last), b);

        /// <summary>Ein Wochenprofil aus zwei Einzelstunden.</summary>
        private static double[] Woche(int[] tage, (int Stunde, double Last) a, (int Stunde, double Last) b)
            => Woche(tage, (a.Stunde, a.Stunde + 1, a.Last), (b.Stunde, b.Stunde + 1, b.Last));

        /// <summary>Volllast durchgehend von Wochenstunde <paramref name="von"/> bis vor <paramref name="bis"/>.</summary>
        private static double[] Durchgehend(int von, int bis)
        {
            var w = new double[168];
            for (int j = von; j < bis; j++) w[j] = 1.0;
            return w;
        }

        /// <summary>Setzt eine Wochenstunde auf eine Last.</summary>
        private static double[] Mit(double[] woche, int wochenstunde, double last)
        {
            woche[wochenstunde] = last;
            return woche;
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
                => Koepfe.ToString(CultureInfo.InvariantCulture) + " Prozesswaermesatz/-saetze und " +
                   Profile.ToString(CultureInfo.InvariantCulture) + " Wochenprofil(e) von " +
                   Alle.Count.ToString(CultureInfo.InvariantCulture) + " Betriebsweisen gesaet (ReadOnly = 1)";
        }

        /// <summary>Steht jeder Satz unter seinem Namen in beiden Katalogen?</summary>
        public static bool Vollstaendig()
        {
            if (!DataRepository.SpalteVorhanden(TAB_KOPF, ProzesswaermeTemperaturSchema.SPALTE_VORLAUF)) return false;
            foreach (ProzesstypSaatsatz s in Alle)
            {
                DataRow kopf = Kopf(s.Name), typ = Typ(s.Name);
                if (Eigen(kopf) || Eigen(typ)) continue;      // ein eigener Satz belegt den Namen
                if (kopf == null || typ == null) return false;
            }
            return true;
        }

        /// <summary>Ein EIGENER Satz des Anwenders (<c>ReadOnly = 0</c>)?</summary>
        private static bool Eigen(DataRow r) => r != null && !Kennzeichen(r);

        /// <summary>
        /// Schreibt die fehlenden Sätze, je Satz in einem Vorgang. <b>Wiederholbar und nie
        /// überschreibend.</b> Setzt die Temperaturspalten des Schemaschritts voraus. Fehler werfen.
        /// </summary>
        public static Bericht Ausfuehren(IList<string> bericht)
        {
            if (!DataRepository.SpalteVorhanden(TAB_KOPF, ProzesswaermeTemperaturSchema.SPALTE_VORLAUF))
                throw new InvalidOperationException("Die Temperaturspalten an " + TAB_KOPF + " fehlen.");

            var b = new Bericht();
            foreach (ProzesstypSaatsatz s in Alle)
            {
                DataRow kopf = Kopf(s.Name);
                DataRow typ = Typ(s.Name);

                // Ein EIGENER Kopfsatz oder Typ des Anwenders belegt den Namen: Der ganze Satz bleibt
                // ungesät - sonst rechnete ein gesäter Kopf mit dem fremden Wochenprofil.
                if (Eigen(kopf) || Eigen(typ))
                {
                    b.Eigene.Add(s.Name);
                    bericht?.Add("Betriebsweise \"" + s.Name + "\" nicht gesaet: ein eigener Satz oder Typ traegt den Namen");
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

        private static DataRow Kopf(string name)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ReadOnly FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = ?", new DbParam("@b", name));
            return dt != null && dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private static DataRow Typ(string name)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ReadOnly FROM Tab_Prozesstyp_STAMM WHERE Bezeichner = ?", new DbParam("@b", name));
            return dt != null && dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private static bool Kennzeichen(DataRow r)
            => r["ReadOnly"] != DBNull.Value && Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture) != 0;

        /// <summary>Der Kopfsatz: Name, Typ = Name, Beschreibung, zwölf Monatswerte, Paar, ReadOnly = 1.</summary>
        private static void KopfSchreiben(DbVorgang v, ProzesstypSaatsatz s)
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
            v.Ausfuehren("INSERT INTO Tab_Prozesswaerme_STAMM (" + spalten + ") VALUES (" + werte + ")", p.ToArray());
        }

        /// <summary>Das Wochenprofil: Name, Beschreibung, 168 Werte, ReadOnly = 1.</summary>
        private static void TypSchreiben(DbVorgang v, ProzesstypSaatsatz s)
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
            v.Ausfuehren("INSERT INTO Tab_Prozesstyp_STAMM (" + spalten + ") VALUES (" + werte + ")", p.ToArray());
        }
    }
}
