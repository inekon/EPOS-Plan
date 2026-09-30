using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SAAT DER 14 AUSGELIEFERTEN KONDITIONIERUNGSVORLAGEN - Schemaschritt KP-S1b (Konzept
    // Konditionierungsprofile 3.5, 5.4, 5.7; Entscheid E56 F1 (b); Entwurf KP2 Abschnitt 4).
    //
    // WAS. 14 Vorlagen in Tab_Konditionierungsvorlage_STAMM mit ReadOnly = 1, ihre 46
    // Vorgabezeilen in Tab_Konditionierungsvorgabe und - bei Buero und Schule - je ein Kalender
    // ohne Standardwoche mit den neun Feiertagsregeln "wie Sonntag" (10 Kalender, 90 Perioden).
    // Werte und Herkunft: KonditionierungsvorlagenSaat.cs. Reines DML, keine Spalte, keine
    // Tabelle, kein Index; die STRICT-Zahl bleibt.
    //
    // SCHLUESSEL IST (GROESSE, NAME) mit OrdinalIgnoreCase - die Namensregel des Controllers
    // (KonditionierungsvorlageCtrl.Namenspruefung). Der NOCASE-Index idx_KondVorlage_Name faltet
    // nur ASCII und hielte "BUERO" (eigen) und "Buero" auseinander; die Saat vergleicht deshalb
    // selbst. Angelegt wird nur, was unter Groesse und Name fehlt; eine EIGENE gleichnamige Vorlage
    // des Anwenders bleibt, wie sie ist, und steht im Bericht. Nie ueberschrieben, feste Ids gibt
    // es nicht (AUTOINCREMENT).
    //
    // EIN VORGANG JE VORLAGE. Kopfsatz, Vorgabezeilen, Kalender und Perioden entstehen zusammen
    // oder gar nicht; scheitert eine Vorlage, bleiben die schon gesaeten stehen, und der Schritt
    // ist wiederholbar.
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt traegt eine Vorlage, und die Leser des Laufs filtern
    // ueber ID_Gebaeude (Konditionierungdatenweg) - Vorlagenzeilen tragen keins. Der Referenzlauf
    // bleibt byte-gleich; die Einfrierregeln greifen nicht, solange kein Referenzprojekt eine
    // Vorlage benutzt.
    // ====================================================================================

    /// <summary>
    /// <b>Der Schemaschritt der ausgelieferten Konditionierungsvorlagen</b> (KP-S1b, E56 F1 (b)) —
    /// EINE Quelle für die Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c>, die
    /// Testvorrichtung und den Nachweis. Die Saat steht in <see cref="KonditionierungsvorlagenSaattabelle"/>.
    /// </summary>
    public static class KonditionierungsvorlagenSaatSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Sie folgt lückenlos
        /// auf <see cref="KesselKennlinieSchema.SCHRITT"/>; wird der Schritt beim Zusammenführen
        /// umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = KesselKennlinieSchema.SCHRITT + 1;

        /// <summary>Die Tabelle der Vorlagen.</summary>
        public const string TABELLE = KonditionierungVorlagenSchema.TAB_VORLAGE;

        /// <summary>Die 14 Vorlagen (<see cref="KonditionierungsvorlagenSaattabelle.Alle"/>).</summary>
        public static IReadOnlyList<KonditionierungsvorlagenSaat> Saat => KonditionierungsvorlagenSaattabelle.Alle;

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Zahl der in diesem Lauf angelegten Vorlagen.</summary>
            public int Gesaet { get; internal set; }

            /// <summary>Vorlagen (<c>GROESSE/Name</c>), unter deren Größe und Name schon eine stand — übergangen.</summary>
            public List<string> Vorhanden { get; } = new List<string>();

            /// <summary>Vorlagen (<c>GROESSE/Name</c>), unter deren Größe und Name eine EIGENE des Anwenders steht (<c>ReadOnly = 0</c>).</summary>
            public List<string> Eigene { get; } = new List<string>();

            /// <summary>Die Zeile für Protokoll und Werkzeug.</summary>
            public string Zeile()
                => Gesaet.ToString(CultureInfo.InvariantCulture) + " von " + Saat.Count.ToString(CultureInfo.InvariantCulture) +
                   " Konditionierungsvorlage(n) gesaet (ReadOnly = 1), " +
                   Vorhanden.Count.ToString(CultureInfo.InvariantCulture) + " stand(en) bereits";
        }

        /// <summary>
        /// Steht jede Vorlage unter ihrer Größe und ihrem Namen (<see cref="StringComparison.OrdinalIgnoreCase"/>)?
        /// Eine eigene gleichnamige Vorlage zählt als vorhanden — sie belegt den Namen, und die Saat
        /// überschreibt sie nie.
        /// </summary>
        public static bool Vollstaendig()
        {
            if (!Tabellenbereit()) return false;
            foreach (KonditionierungsvorlagenSaat s in Saat)
                if (Treffer(s).Rows.Count == 0) return false;
            return true;
        }

        /// <summary>
        /// Schreibt die fehlenden Vorlagen, <b>je Vorlage in einem Vorgang</b>. <b>Wiederholbar und nie
        /// überschreibend:</b> Steht unter Größe und Name schon eine Vorlage, wird die Saat übergangen;
        /// eine eigene des Anwenders kommt ins Protokoll (<paramref name="bericht"/>, darf <c>null</c>
        /// sein). Setzt die Tabellen der Schritte <see cref="KonditionierungSchema.SCHRITT"/> und
        /// <see cref="KonditionierungVorlagenSchema.SCHRITT"/> voraus. Fehler werfen — der Aufrufer
        /// meldet sie.
        /// </summary>
        public static Bericht Ausfuehren(IList<string> bericht)
        {
            if (!Tabellenbereit())
                throw new InvalidOperationException(
                    "Die Tabellen der Konditionierung fehlen - Schritt " +
                    KonditionierungVorlagenSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) + " steht nicht.");

            var b = new Bericht();
            foreach (KonditionierungsvorlagenSaat s in Saat)
            {
                DataTable treffer = Treffer(s);
                if (treffer.Rows.Count > 0)
                {
                    b.Vorhanden.Add(s.ToString());
                    foreach (DataRow r in treffer.Rows)
                    {
                        if (Zahl(r["ReadOnly"]) != 0) continue;
                        b.Eigene.Add(s.ToString());
                        bericht?.Add("Konditionierungsvorlage \"" + s.Bezeichner + "\" (" + s.Kennwort +
                                     ") nicht gesaet: eine eigene Vorlage \"" + Convert.ToString(r["Bezeichner"],
                                     CultureInfo.InvariantCulture) + "\" traegt den Namen");
                        break;
                    }
                    continue;
                }

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        Schreiben(v, s);
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                b.Gesaet++;
            }
            bericht?.Add(b.Zeile());
            return b;
        }

        /// <summary>
        /// Schreibt EINE Vorlage im laufenden Vorgang: Kopfsatz, Vorgabezeilen und — mit Feiertagen —
        /// den Kalender samt neun Perioden. Alle Werte als <c>?</c>-Parameter; die Texte stehen am
        /// Aufruf, damit der <c>SqlDialektPruefer</c> sie gegen die Testdatenbank hält.
        /// </summary>
        /// <returns>Die Id der neuen Vorlage.</returns>
        internal static long Schreiben(DbVorgang v, KonditionierungsvorlagenSaat s)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (s == null) throw new ArgumentNullException(nameof(s));

            long id = v.EinfuegenUndId(
                "INSERT INTO \"Tab_Konditionierungsvorlage_STAMM\" (\"Groesse\", \"Bezeichner\", \"Beschreibung\", " +
                "\"Nutzung\", \"ReadOnly\") VALUES (?, ?, ?, ?, 1)",
                new[]
                {
                    new DbParam("@gr", s.Kennwort),
                    new DbParam("@bz", s.Bezeichner),
                    new DbParam("@be", s.Beschreibung),
                    new DbParam("@nu", s.Nutzung),
                });
            if (id <= 0) throw new InvalidOperationException("Der Kopfsatz der Vorlage " + s + " entstand nicht.");

            foreach (KonditionierungsvorlagenSaatzeile z in s.Zeilen)
                v.Ausfuehren(
                    "INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Vorlage\", \"Groesse\", \"Zeile\", \"Wert\", " +
                    "\"Aus\", \"Von\", \"Bis\") VALUES (?, ?, ?, ?, ?, ?, ?)",
                    new DbParam("@v", id),
                    new DbParam("@gr", s.Kennwort),
                    new DbParam("@ze", z.Zeile),
                    new DbParam("@we", Oder(z.Wert)),
                    new DbParam("@au", z.Aus ? 1 : 0),
                    new DbParam("@vo", Oder(z.Von)),
                    new DbParam("@bi", Oder(z.Bis)));

            if (!s.Feiertage) return id;

            KonditionierungsvorlagenSaatzeile grund = s.Kalendergrund
                ?? throw new InvalidOperationException("Die Vorlage " + s + " traegt Feiertage, aber keine Wochenendzeile.");
            long kalender = v.EinfuegenUndId(
                "INSERT INTO \"Tab_Konditionierungskalender\" (\"ID_Vorlage\", \"Groesse\", \"Wert\", \"Aus\") " +
                "VALUES (?, ?, ?, ?)",
                new[]
                {
                    new DbParam("@v", id),
                    new DbParam("@gr", s.Kennwort),
                    new DbParam("@we", Oder(grund.Wert)),
                    new DbParam("@au", grund.Aus ? 1 : 0),
                });
            if (kalender <= 0) throw new InvalidOperationException("Der Kalender der Vorlage " + s + " entstand nicht.");

            for (int k = 0; k < DbWerte.KOND_FEIERTAGE.Count; k++)
                v.Ausfuehren(
                    "INSERT INTO \"Tab_Konditionierungsperiode\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", " +
                    "\"Feiertagsregel\", \"Aus\", \"WieWochentag\") VALUES (?, ?, ?, ?, ?, 0, ?)",
                    new DbParam("@k", kalender),
                    new DbParam("@r", Standardfahrplan.RANG_FEIERTAG + k),
                    new DbParam("@ar", DbWerte.KOND_ART_FEIERTAG),
                    new DbParam("@bz", KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN[k]),
                    new DbParam("@ft", DbWerte.KOND_FEIERTAGE[k]),
                    new DbParam("@ww", KonditionierungsvorlagenSaattabelle.WIE_WOCHENTAG));
            return id;
        }

        /// <summary>Stehen die Vorlagen-, Vorgabe-, Kalender- und Periodentabelle?</summary>
        private static bool Tabellenbereit()
            => DataRepository.TabelleVorhanden(TABELLE)
               && DataRepository.TabelleVorhanden(KonditionierungSchema.TAB_VORGABE)
               && DataRepository.TabelleVorhanden(KonditionierungSchema.TAB_KALENDER)
               && DataRepository.TabelleVorhanden(KonditionierungSchema.TAB_PERIODE);

        /// <summary>
        /// Die Vorlagen der Größe von <paramref name="s"/>, deren Name dem der Saat gleicht — getrimmt
        /// und ohne Unterschied von Groß- und Kleinschreibung (<see cref="StringComparison.OrdinalIgnoreCase"/>).
        /// </summary>
        private static DataTable Treffer(KonditionierungsvorlagenSaat s)
        {
            DataTable alle = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Bezeichner\", \"ReadOnly\" FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"Groesse\" = ?",
                new DbParam("@gr", s.Kennwort));
            var treffer = new DataTable();
            treffer.Columns.Add("ID", typeof(long));
            treffer.Columns.Add("Bezeichner", typeof(string));
            treffer.Columns.Add("ReadOnly", typeof(long));
            if (alle == null) return treffer;
            foreach (DataRow r in alle.Rows)
            {
                string name = (Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "").Trim();
                if (string.Equals(name, s.Bezeichner, StringComparison.OrdinalIgnoreCase))
                    treffer.Rows.Add(Zahl(r["ID"]), name, Zahl(r["ReadOnly"]));
            }
            return treffer;
        }

        private static object Oder(double? wert) => wert.HasValue ? (object)wert.Value : DBNull.Value;

        private static object Oder(int? wert) => wert.HasValue ? (object)wert.Value : DBNull.Value;

        private static long Zahl(object o)
            => o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
    }
}
