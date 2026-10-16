using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER WASSERPREIS WANDERT IN DIE TRAEGERKARTE - Welle KB-7a (Anwenderentscheide vom
    // 16.10.2026, Entwurf Dialoge/2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md,
    // Abschnitt 3; Grundsatz 13.10.2026 "eine Kostenbasis, keine Doppelhaltung").
    //
    // WOZU. Der Rueckkuehlwerk-Schritt (214) legte an Tab_Energieanlagen den Wasserpreis
    // Wasserpreis_EUR_m3 an (Frage KD-Q8): ein Preis an der Anlage, neben der Traegerkarte,
    // die alle uebrigen Preise haelt. Der Entscheid fuehrt Wasser als Traeger der Karte mit
    // einem Arbeitspreis je m3 (Abwasser im Preis enthalten, kein Grundpreis), EIN Preis je
    // Projekt (energy_project_settings.custom_price_work des Wassertraegers).
    //
    // WAS, IN DIESER REIHENFOLGE:
    //   a. TRAEGER. Fehlt ein Traeger mit pricing_model 'WATER', wird er angelegt: Name, Code
    //      und Gruppe "Wasser", Abrechnungseinheit m3, kein Heizwert, Preise 0, keine
    //      Emissionsfaktoren, ID_Brennstoff 0, aktiv. ERKANNT WIRD ER AM PREISMODELL, NIE AM
    //      NAMEN - ein Anwender darf ihn umbenennen, der zweite Lauf legt keinen zweiten an.
    //      Der Katalogpreis bleibt 0: Eine Saat bewegt nie ein Ergebnis (Muster Schritt 227).
    //   a2. PREISMODELL. Fehlt die Nachschlagezeile pricing_model 'WATER', wird sie angelegt
    //      (Name "Wasser", alle vier Flaggen 0); einziger Leser ist die Traegerliste des Katalogs
    //      per LEFT JOIN - die Werte bleiben gleich, der Katalogstempel bleibt, wie er war.
    //   b. PREISUEBERNAHME. Je Projekt mit einem Wasserpreis > 0 an einer Anlagenzeile geht der
    //      Preis nach energy_project_settings.custom_price_work des Wassertraegers; die Zeile
    //      (Projekt, Traeger) wird angelegt, wenn sie fehlt. Ein dort schon gepflegter Preis
    //      (> 0) bleibt unangetastet.
    //   c. MEHRERE PREISE. Tragen Anlagen eines Projekts verschiedene Preise, gewinnt der Preis
    //      der Anlage mit der kleinsten ID; der Bericht nennt Projekt, Anlagen und die
    //      verworfenen Preise (Frage F2: ein Preis je Projekt).
    //   d. DROP COLUMN Tab_Energieanlagen.Wasserpreis_EUR_m3. Die Spalte traegt nur ihren
    //      eigenen Spalten-CHECK (>= 0), keinen Index, keinen Fremdschluessel, keine Sicht und
    //      keinen Ausloeser - SQLite (>= 3.35) entfernt sie ohne Tabellenneubau.
    //
    // DER RUECKKUEHLWERK-SCHRITT LEGT DIE SPALTE NICHT WIEDER AN. RueckkuehlwerkSchema fuehrt
    // in SPALTEN_ANLAGE nur noch den Verweis; ein erneuter Lauf von 214 nach diesem Schritt
    // findet nichts anzulegen (Muster 175/227: die Anlage eines Schritts darf den Entfall eines
    // spaeteren nicht zuruecknehmen).
    //
    // IDEMPOTENT. Der Traeger wird nur angelegt, wenn keiner mit dem Preismodell steht; die
    // Uebernahme greift nur, solange die Spalte steht, und nur in leere Kartenpreise; danach ist
    // die Spalte fort (Offen() = 0), der zweite Lauf hat nichts zu tun.
    //
    // ERGEBNISNEUTRAL. In der Testdatenbank traegt keine Anlagenzeile einen Wasserpreis, kein
    // Referenzprojekt rechnet ein Rueckkuehlwerk; der neue Traeger ist eine Katalogzeile ohne
    // Zuordnung zu einem Referenzprojekt. energy_carrier steht in keinem Pruefsummenregister -
    // keine neue Katalogfassung allein dafuer.
    //
    // DER NAME DER SPALTE STEHT NUR HIER, UND NUR ALS ARGUMENT (Muster StrompreisAltspalten,
    // Schritt 85): Gegen eine migrierte Datenbank gehalten waere eine ausgeschriebene
    // Anweisung zwangslaeufig "no such column".
    //
    // NUMMER. Angemeldet als 235 (Statuskopf); haengt endgueltig an 234 (KBP-5, Sitzung Dialoge/Cloud),
    // sobald 231-234 auf origin liegen. Bis dahin haengt die Kette vorlaeufig an 233.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Umformung: ein aelteres Paket uebergibt den Preis an
    // die Traegerkarte seines Projekts).
    // ====================================================================================

    /// <summary>
    /// <b>KB-7a</b> — der Wasserpreis wandert in die Trägerkarte: Träger „Wasser“ (Preismodell <c>WATER</c>,
    /// Einheit m³), Übernahme der Anlagenpreise in <c>energy_project_settings</c>, Entfall von
    /// <c>Tab_Energieanlagen.Wasserpreis_EUR_m3</c>. EINE Quelle für Migration, Werkzeug, Paketanhebung und Nachweis.
    /// </summary>
    public static class WasserpreisTraegerkarteSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — angemeldet als 235 (Statuskopf); hängt endgültig an 234 (KBP-5,
        /// Sitzung Dialoge/Cloud), sobald 231–234 auf origin liegen. Vorläufig hängt sie an
        /// <see cref="BauteilUHerkunftSchema.SCHRITT"/>.
        /// </summary>
        public const int SCHRITT = BauteilUHerkunftSchema.SCHRITT + 1;

        /// <summary>Der zentrale Trägerkatalog.</summary>
        public const string TAB_TRAEGER = "energy_carrier";

        /// <summary>Die Projektebene der Träger — Ziel der Preisübernahme.</summary>
        public const string TAB_EINSTELLUNG = "energy_project_settings";

        /// <summary>Die Anlagenzeilen, an denen der Preis stand.</summary>
        public const string TAB_ANLAGEN = "Tab_Energieanlagen";

        /// <summary>Die entfallende Preisspalte an der Anlagenzeile (Schritt 214, Frage KD-Q8).</summary>
        public const string ALTSPALTE = "Wasserpreis_EUR_m3";

        /// <summary>Der Trägerverweis der Projektebene.</summary>
        public const string SPALTE_TRAEGER = "ID_Energieträger";

        /// <summary>Der Arbeitspreis der Projektebene [€ je Abrechnungseinheit].</summary>
        public const string SPALTE_PREIS = "custom_price_work";

        /// <summary>Das Preismodell des Wasserträgers — sein Erkennungsmerkmal (nie der Name).</summary>
        public const string PREISMODELL_WASSER = "WATER";

        /// <summary>Die Nachschlagetabelle der Preismodelle (Flaggen Heizwert, Brennwert, Leistungspreis, Dichte).</summary>
        public const string TAB_PREISMODELL = "pricing_model";

        /// <summary>Name, Code und Gruppe des angelegten Trägers.</summary>
        public const string NAME_WASSER = "Wasser";

        /// <summary>Die Abrechnungseinheit des Wasserträgers.</summary>
        public const string EINHEIT_WASSER = DbWerte.EINHEIT_KUBIKMETER;

        /// <summary>Was ein Lauf getan hat.</summary>
        public readonly record struct Laufergebnis(int TraegerAngelegt, int Uebernommen, int Verworfen, int Behalten, int Entfernt,
                                                   int PreismodellAngelegt = 0)
        {
            /// <summary>Die Summe der Handgriffe — die Zahl, die die Leser melden.</summary>
            public int Handgriffe => TraegerAngelegt + Uebernommen + Entfernt + PreismodellAngelegt;
        }

        /// <summary>Ein Preis je Projekt nach Regel c: der Gewinner und die verworfenen Anlagenpreise.</summary>
        public sealed class Projektpreis
        {
            /// <summary>Das Projekt.</summary>
            public long IdProjekt { get; set; }

            /// <summary>Die Anlage mit der kleinsten ID — ihr Preis gilt.</summary>
            public long IdAnlage { get; set; }

            /// <summary>Der übernommene Preis [€/m³].</summary>
            public double Preis { get; set; }

            /// <summary>Die Anlagen mit abweichendem Preis (Anlage, Preis).</summary>
            public List<(long IdAnlage, double Preis)> Verworfen { get; } = new List<(long, double)>();
        }

        /// <summary>Die Tabellen, ohne die der Schritt nicht laufen kann.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return TAB_TRAEGER;
            yield return TAB_EINSTELLUNG;
            yield return TAB_ANLAGEN;
        }

        /// <summary>Steht die Preisspalte noch an der Anlagenzeile?</summary>
        public static bool SpalteVorhanden() =>
            DataRepository.TabelleVorhanden(TAB_ANLAGEN) && DataRepository.SpalteVorhanden(TAB_ANLAGEN, ALTSPALTE);

        /// <summary>Die Abfrage des Trägers über das Preismodell — der kleinste, falls mehrere stehen.</summary>
        public static string SqlTraegerId() =>
            "SELECT \"id\" FROM " + Q(TAB_TRAEGER) + " WHERE \"pricing_model\" = ? ORDER BY \"id\" LIMIT 1";

        /// <summary>Die ID des Wasserträgers; <c>null</c>, wenn keiner steht.</summary>
        public static long? TraegerId()
        {
            if (!DataRepository.TabelleVorhanden(TAB_TRAEGER)) return null;
            object o = DataRepository.ExecuteScalar(SqlTraegerId(), new DbParam("@m", PREISMODELL_WASSER));
            return o == null || o == DBNull.Value ? (long?)null : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Wie viele Teile stehen noch aus (fehlender Träger, stehende Spalte)? 0 = der Schritt ist gelaufen.</summary>
        public static int Offen() => (TraegerId().HasValue ? 0 : 1) + (SpalteVorhanden() ? 1 : 0) +
                                     (PreismodellVorhanden() ? 0 : 1);

        /// <summary>
        /// Steht die Nachschlagezeile <c>pricing_model</c> 'WATER'? Ohne die Tabelle gibt es nichts nachzuschlagen
        /// (<c>true</c>). Einziger Leser ist die Trägerliste des Katalogs (<c>KostenSummenCtrl</c>, LEFT JOIN) — die
        /// Zeile trägt alle vier Flaggen 0, wie der Join ohne sie; sie ändert keinen Wert.
        /// </summary>
        public static bool PreismodellVorhanden()
        {
            if (!DataRepository.TabelleVorhanden(TAB_PREISMODELL)) return true;
            object o = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + Q(TAB_PREISMODELL) + " WHERE \"code\" = ?",
                                                    new DbParam("@c", PREISMODELL_WASSER));
            return Convert.ToInt64(o, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>Steht der Schritt vollständig?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Die Abfrage der Anlagenpreise (ID, Projekt, Preis) über alle Anlagen mit Preis &gt; 0, nach Projekt und ID
        /// geordnet — dieselbe Abfrage liest Migration und Paketanhebung.
        /// </summary>
        public static string SqlPreiseLesen() =>
            "SELECT \"ID\", \"ID_Projekt\", " + Q(ALTSPALTE) + " FROM " + Q(TAB_ANLAGEN) + " WHERE IFNULL(" + Q(ALTSPALTE) +
            ", 0) > 0 AND \"ID_Projekt\" IS NOT NULL ORDER BY \"ID_Projekt\", \"ID\"";

        /// <summary>
        /// Regel c: je Projekt der Preis der Anlage mit der kleinsten ID; abweichende Preise der übrigen Anlagen
        /// sind verworfen. Die Zeilen kommen aus <see cref="SqlPreiseLesen"/> (Spalten 0 = Anlage, 1 = Projekt, 2 = Preis).
        /// </summary>
        public static List<Projektpreis> Projektpreise(DataTable zeilen)
        {
            var liste = new List<Projektpreis>();
            if (zeilen == null) return liste;
            var nachProjekt = new Dictionary<long, Projektpreis>();
            foreach (DataRow r in zeilen.Rows)
            {
                long anlage = Convert.ToInt64(r[0], CultureInfo.InvariantCulture);
                long projekt = Convert.ToInt64(r[1], CultureInfo.InvariantCulture);
                double preis = Convert.ToDouble(r[2], CultureInfo.InvariantCulture);
                if (!nachProjekt.TryGetValue(projekt, out Projektpreis p))
                {
                    p = new Projektpreis { IdProjekt = projekt, IdAnlage = anlage, Preis = preis };
                    nachProjekt[projekt] = p;
                    liste.Add(p);
                    continue;
                }
                if (anlage < p.IdAnlage)
                {
                    // Die Abfrage ordnet nach ID; eine fremde Reihenfolge darf die Regel nicht kippen.
                    if (preis != p.Preis) p.Verworfen.Add((p.IdAnlage, p.Preis));
                    p.IdAnlage = anlage;
                    p.Preis = preis;
                }
                else if (preis != p.Preis) p.Verworfen.Add((anlage, preis));
            }
            return liste;
        }

        /// <summary>Die Berichtszeile zu verworfenen Preisen eines Projekts (Regel c); <c>null</c>, wenn keine.</summary>
        public static string HinweisVerworfen(Projektpreis p)
        {
            if (p == null || p.Verworfen.Count == 0) return null;
            return "Projekt " + p.IdProjekt.ToString(CultureInfo.InvariantCulture) + ": Wasserpreis " + Zahl(p.Preis) +
                   " EUR/m3 der Anlage " + p.IdAnlage.ToString(CultureInfo.InvariantCulture) + " uebernommen, verworfen: " +
                   string.Join(", ", p.Verworfen.OrderBy(v => v.IdAnlage).Select(v =>
                       "Anlage " + v.IdAnlage.ToString(CultureInfo.InvariantCulture) + " (" + Zahl(v.Preis) + " EUR/m3)"));
        }

        /// <summary>Führt den Schritt aus. Rückgabe: die Zahl der Handgriffe. <b>Wiederholbar.</b></summary>
        public static int Ausfuehren(IList<string> bericht) => Lauf(bericht).Handgriffe;

        /// <summary><see cref="Ausfuehren"/> mit der Aufschlüsselung des Laufs — der Weg des Nachweises.</summary>
        public static Laufergebnis Lauf(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + SCHRITT.ToString(CultureInfo.InvariantCulture) +
                                                        ": Die Tabelle " + t + " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            int angelegt = TraegerAnlegen(bericht);
            int preismodell = PreismodellAnlegen(bericht);
            long traeger = TraegerId() ?? throw new InvalidOperationException("Der Wassertraeger fehlt nach dem Anlegen.");
            (int uebernommen, int verworfen, int behalten) = PreiseUebernehmen(traeger, bericht);
            int entfernt = 0;
            if (SpalteVorhanden())
            {
                DataRepository.ExecuteNonQuery("ALTER TABLE " + Q(TAB_ANLAGEN) + " DROP COLUMN " + Q(ALTSPALTE));
                entfernt = 1;
                bericht?.Add(TAB_ANLAGEN + "." + ALTSPALTE + " entfernt");
            }
            if (bericht != null && angelegt + uebernommen + entfernt + preismodell == 0)
                bericht.Add("steht bereits - Wassertraeger vorhanden, Preisspalte an der Anlage fort; nichts zu tun");
            return new Laufergebnis(angelegt, uebernommen, verworfen, behalten, entfernt, preismodell);
        }

        // =================================================================================
        //  a. Der Traeger
        // =================================================================================

        /// <summary>Die Anlage des Wasserträgers (Parameter: Name, Code, Gruppe, Preismodell, Einheit).</summary>
        public static string SqlTraegerAnlegen() =>
            "INSERT INTO " + Q(TAB_TRAEGER) + " (\"ID_Brennstoff\", \"name\", \"code\", \"group_code\", \"pricing_model\", " +
            "\"billing_unit\", \"hi_kwh_per_unit\", \"hs_kwh_per_unit\", \"price_work\", \"price_base\", \"price_power\", " +
            "\"co2\", \"so2\", \"nox\", \"is_active\") VALUES (0, ?, ?, ?, ?, ?, NULL, NULL, 0, 0, 0, 0, 0, 0, 1)";

        /// <summary>
        /// Legt den Träger an. DER KATALOGSTEMPEL BLEIBT, WIE ER WAR: Der Auslöser an <c>energy_carrier</c>
        /// (<see cref="KostenStempelSchema"/>) stempelt jede neue Zeile als Kostenänderung des Katalogs - eine Zeile mit
        /// Preis 0 ohne Zuordnung ändert aber keine Wirtschaftlichkeit. Der Stempel wird deshalb im selben Vorgang auf
        /// seinen Wert davor zurückgesetzt (Saat bewegt nie ein Ergebnis, auch nicht dessen Aktualitätsvermerk).
        /// </summary>
        private static int TraegerAnlegen(IList<string> bericht)
        {
            if (TraegerId().HasValue) return 0;
            MitStempelErhalt(v => v.Ausfuehren(SqlTraegerAnlegen(),
                new DbParam("@name", NAME_WASSER), new DbParam("@code", NAME_WASSER), new DbParam("@gruppe", NAME_WASSER),
                new DbParam("@modell", PREISMODELL_WASSER), new DbParam("@einheit", EINHEIT_WASSER)));
            bericht?.Add(TAB_TRAEGER + ": Traeger \"" + NAME_WASSER + "\" (" + PREISMODELL_WASSER + ", " + EINHEIT_WASSER +
                         ") angelegt");
            return 1;
        }

        /// <summary>
        /// Die Nachschlagezeile des Preismodells (Parameter: Code, Name, Code) — nur, wenn sie fehlt; alle Flaggen 0
        /// (kein Heizwert, kein Brennwert, kein Leistungspreis, keine Dichte).
        /// </summary>
        public static string SqlPreismodellAnlegen() =>
            "INSERT INTO " + Q(TAB_PREISMODELL) + " (\"code\", \"name\", \"has_hi\", \"has_hs\", \"has_powerprice\", \"has_density\") " +
            "SELECT ?, ?, 0, 0, 0, 0 WHERE NOT EXISTS (SELECT 1 FROM " + Q(TAB_PREISMODELL) + " WHERE \"code\" = ?)";

        /// <summary>
        /// Legt die Nachschlagezeile 'WATER' an, wenn sie fehlt. Der Katalogstempel bleibt wie beim Träger: Der
        /// Auslöser an <c>pricing_model</c> stempelt die neue Zeile, eine Flaggenzeile ändert aber keine Wirtschaftlichkeit.
        /// </summary>
        private static int PreismodellAnlegen(IList<string> bericht)
        {
            if (PreismodellVorhanden()) return 0;
            MitStempelErhalt(v => v.Ausfuehren(SqlPreismodellAnlegen(),
                new DbParam("@code", PREISMODELL_WASSER), new DbParam("@name", NAME_WASSER), new DbParam("@code2", PREISMODELL_WASSER)));
            bericht?.Add(TAB_PREISMODELL + ": Preismodell \"" + PREISMODELL_WASSER + "\" angelegt");
            return 1;
        }

        /// <summary>Führt die Schreibung in einem Vorgang aus und setzt den Katalogstempel danach auf seinen Wert davor.</summary>
        private static void MitStempelErhalt(Action<DbVorgang> schreiben)
        {
            string tabStempel = KostenStempelSchema.TAB_APPLIKATION, spStempel = KostenStempelSchema.SPALTE_KATALOG;
            bool mitStempel = DataRepository.TabelleVorhanden(tabStempel) && DataRepository.SpalteVorhanden(tabStempel, spStempel);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    DataTable stempel = mitStempel
                        ? v.Lese("SELECT rowid, " + Q(spStempel) + " FROM " + Q(tabStempel), Array.Empty<DbParam>())
                        : null;
                    schreiben(v);
                    if (stempel != null)
                        foreach (DataRow r in stempel.Rows)
                            v.Ausfuehren("UPDATE " + Q(tabStempel) + " SET " + Q(spStempel) + " = ? WHERE rowid = ?",
                                         new DbParam("@s", r[1]), new DbParam("@r", r[0]));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
        }

        // =================================================================================
        //  b./c. Die Preisuebernahme
        // =================================================================================

        private static (int Uebernommen, int Verworfen, int Behalten) PreiseUebernehmen(long traeger, IList<string> bericht)
        {
            if (!SpalteVorhanden()) return (0, 0, 0);
            List<Projektpreis> preise = Projektpreise(DataRepository.GetDataTable(SqlPreiseLesen()));
            int uebernommen = 0, verworfen = 0, behalten = 0;
            if (preise.Count == 0) return (0, 0, 0);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (Projektpreis p in preise)
                    {
                        object da = v.Skalar("SELECT COUNT(*) FROM \"Tab_Projekt\" WHERE \"ID\" = ?", new DbParam("@p", p.IdProjekt));
                        if (da == null || da == DBNull.Value || Convert.ToInt64(da, CultureInfo.InvariantCulture) == 0)
                        {
                            bericht?.Add("Projekt " + p.IdProjekt.ToString(CultureInfo.InvariantCulture) +
                                         " fehlt - Wasserpreis der Anlage " + p.IdAnlage.ToString(CultureInfo.InvariantCulture) +
                                         " nicht uebernommen");
                            continue;
                        }
                        string hinweis = HinweisVerworfen(p);
                        if (hinweis != null) { bericht?.Add(hinweis); verworfen += p.Verworfen.Count; }

                        DataTable dt = v.Lese("SELECT \"ID\", " + Q(SPALTE_PREIS) + " FROM " + Q(TAB_EINSTELLUNG) +
                                              " WHERE \"ID_Projekt\" = ? AND " + Q(SPALTE_TRAEGER) + " = ? ORDER BY \"ID\" LIMIT 1",
                                              new[] { new DbParam("@p", p.IdProjekt), new DbParam("@t", traeger) });
                        if (dt == null || dt.Rows.Count == 0)
                        {
                            v.Ausfuehren("INSERT INTO " + Q(TAB_EINSTELLUNG) + " (\"ID_Projekt\", " + Q(SPALTE_TRAEGER) + ", " +
                                         Q(SPALTE_PREIS) + ") VALUES (?, ?, ?)",
                                         new DbParam("@p", p.IdProjekt), new DbParam("@t", traeger), new DbParam("@w", p.Preis));
                        }
                        else
                        {
                            object alt = dt.Rows[0][1];
                            double gepflegt = alt == null || alt == DBNull.Value ? 0 : Convert.ToDouble(alt, CultureInfo.InvariantCulture);
                            if (gepflegt > 0)
                            {
                                behalten++;
                                bericht?.Add("Projekt " + p.IdProjekt.ToString(CultureInfo.InvariantCulture) +
                                             ": gepflegter Wasserpreis " + Zahl(gepflegt) + " EUR/m3 der Traegerkarte bleibt (Anlage: " +
                                             Zahl(p.Preis) + " EUR/m3)");
                                continue;
                            }
                            v.Ausfuehren("UPDATE " + Q(TAB_EINSTELLUNG) + " SET " + Q(SPALTE_PREIS) + " = ? WHERE \"ID\" = ?",
                                         new DbParam("@w", p.Preis),
                                         new DbParam("@id", Convert.ToInt64(dt.Rows[0][0], CultureInfo.InvariantCulture)));
                        }
                        uebernommen++;
                        bericht?.Add("Projekt " + p.IdProjekt.ToString(CultureInfo.InvariantCulture) + ": Wasserpreis " +
                                     Zahl(p.Preis) + " EUR/m3 in die Traegerkarte uebernommen");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return (uebernommen, verworfen, behalten);
        }

        private static string Zahl(double d) => d.ToString("0.####", CultureInfo.InvariantCulture);

        private static string Q(string name) => "\"" + name + "\"";
    }
}
