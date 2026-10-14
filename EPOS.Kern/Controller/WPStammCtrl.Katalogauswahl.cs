using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Wärmepumpe als Wirt der Katalogauswahl V1, Stufe 3</b> (Konzept Projektdialoge mit Katalogauswahl 4.6,
    /// 4.9): die Felder der Satzbearbeitung je Bereich und das Mehrfach-Bearbeiten in EINER Transaktion.
    /// </summary>
    /// <remarks>
    /// <para><b>Welche Felder.</b> Die Satzbearbeitung trägt nur, was für mehrere Geräte zugleich Sinn ergibt:
    /// Hersteller, Beschreibung und Modulkosten. Nennleistung, Heizstab, Kühlleistung, Typ und Regelung hängen an der
    /// Kennlinie des Geräts; sie und die Kennlinien selbst bearbeitet der Katalogeditor (ein Katalogsatz) bzw. die
    /// Anlagenseite „Anlage…" (die Projektkopie).</para>
    /// <para><b>Kopie oder Katalog</b> wählt <c>projektkopie</c>: <c>Tab_WP</c> bzw. <see cref="TABLE"/>. Der Name
    /// bleibt immer, ein gesperrter Katalogsatz wird nie geschrieben.</para>
    /// </remarks>
    partial class WPStammCtrl
    {
        /// <summary>Die Projektkopien der Wärmepumpen (alle Projekte, Spalte <c>ID_Projekt</c>).</summary>
        public const string TABELLE_PROJEKT = "Tab_WP";

        /// <summary><c>Tab_KostenKomponente.ID</c> der Wärmepumpe.</summary>
        public const int KOMPONENTE_KOSTEN = 1;

        private static string Satztabelle(bool projektkopie) => projektkopie ? TABELLE_PROJEKT : TABLE;

        /// <summary>Die Felder der Satzbearbeitung eines Geräts; <c>Modulkosten</c> leer = keine gepflegt.</summary>
        public sealed record Sammelfelder(string Firma, string Beschreibung, double? Modulkosten);

        /// <summary>Ein gelesener Satz der Satzbearbeitung: Name, Sperre (nur Katalog) und die Felder.</summary>
        public sealed record Sammelsatz(int Id, string Bezeichner, bool Gesperrt, Sammelfelder Felder);

        /// <summary>Die geänderten Felder eines Satzes, benannt über seine ID.</summary>
        public sealed record Sammelaenderung(int Id, Sammelfelder Felder);

        /// <summary>
        /// <b>Die Felder der Satzbearbeitung nach ID</b> — Projektkopie (<paramref name="projektkopie"/>) oder
        /// Katalogsatz, ohne die zweistufige Suche der Anlagenzeile: Die ID ist eindeutig, weil der Bereich die Tabelle
        /// nennt. <c>null</c>, wenn es die ID nicht gibt.
        /// </summary>
        public static Sammelsatz SammelsatzLesen(bool projektkopie, int id)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + Satztabelle(projektkopie) + "] WHERE ID = ?", new DbParam("@id", id));
            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            bool gesperrt = !projektkopie && dt.Columns.Contains("ReadOnly") && r["ReadOnly"] != DBNull.Value &&
                            Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture) != 0;
            double? kosten = r["Modulkosten"] == DBNull.Value
                ? (double?)null : Convert.ToDouble(r["Modulkosten"], CultureInfo.InvariantCulture);
            return new Sammelsatz(id, Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "", gesperrt,
                                  new Sammelfelder(Convert.ToString(r["Firma"], CultureInfo.InvariantCulture) ?? "",
                                                   Convert.ToString(r["Beschreibung"], CultureInfo.InvariantCulture) ?? "",
                                                   kosten));
        }

        /// <summary>
        /// <b>Schreibt alle geänderten Sätze einer Satzbearbeitung — alle oder keiner</b> (Konzept 4.6). Ein gesperrter
        /// Katalogsatz, eine fehlende ID oder negative Modulkosten rollen die ganze Transaktion zurück und nennen den Satz.
        /// Der Name, die Kennlinien und alle übrigen Spalten bleiben.
        /// </summary>
        public static SpeicherErgebnis SammelfelderSchreibenAlle(bool projektkopie, IReadOnlyList<Sammelaenderung> saetze)
        {
            if (saetze == null || saetze.Count == 0)
                return new SpeicherErgebnis(true, Text("KAT_MSG_SAMMEL_KEINE", "Keine Änderung."), "");
            string tabelle = Satztabelle(projektkopie);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    foreach (Sammelaenderung s in saetze)
                    {
                        if (s == null || s.Felder == null) continue;
                        DataTable dt = v.Lese("SELECT * FROM [" + tabelle + "] WHERE ID = ?", new DbParam("@id", s.Id));
                        if (dt == null || dt.Rows.Count == 0)
                        {
                            v.Rollback();
                            return new SpeicherErgebnis(false, string.Format(CultureInfo.CurrentCulture,
                                Text("KAT_MSG_SAMMEL_FEHLT", "Der Satz mit der Nummer {0} wurde nicht gefunden. Es wurde nichts gespeichert."),
                                s.Id), "");
                        }
                        DataRow r = dt.Rows[0];
                        string name = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "";
                        if (!projektkopie && dt.Columns.Contains("ReadOnly") && r["ReadOnly"] != DBNull.Value &&
                            Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture) != 0)
                        {
                            v.Rollback();
                            return new SpeicherErgebnis(false, string.Format(CultureInfo.CurrentCulture,
                                Text("KAT_MSG_SAMMEL_GESPERRT", "„{0}“ ist gesperrt. Es wurde nichts gespeichert."), name), name);
                        }
                        if (s.Felder.Modulkosten is double k && (k < 0 || double.IsNaN(k) || double.IsInfinity(k)))
                        {
                            v.Rollback();
                            return new SpeicherErgebnis(false, string.Format(CultureInfo.CurrentCulture,
                                Text("KAT_MSG_SAMMEL_VERSTOSS", "„{0}“: {1} Es wurde nichts gespeichert."), name,
                                Text("WPV_MSG_MODULKOSTEN_NEGATIV", "Die Modulkosten dürfen nicht negativ sein.")), name);
                        }
                        v.Ausfuehren("UPDATE [" + tabelle + "] SET Firma = ?, Beschreibung = ?, Modulkosten = ? WHERE ID = ?",
                                     new DbParam("@firma", (s.Felder.Firma ?? "").Trim()),
                                     new DbParam("@beschreibung", s.Felder.Beschreibung ?? ""),
                                     new DbParam("@kosten", s.Felder.Modulkosten.HasValue ? (object)s.Felder.Modulkosten.Value : DBNull.Value),
                                     new DbParam("@id", s.Id));
                    }
                    v.Commit();
                }
                return new SpeicherErgebnis(true, string.Format(CultureInfo.CurrentCulture,
                    Text("KAT_MSG_SAMMEL_GESPEICHERT", "{0} Sätze gespeichert."), saetze.Count), "");
            }
            catch (Exception)
            {
                // DbVorgang.Dispose rollt ohne Commit zurück.
                return new SpeicherErgebnis(false, Text("WPS_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");
            }
        }

        // =================================================================================
        // Teil b: der Rueckweg "In die Datenbank übernehmen…" (Konzept 5.2, KA-E-9) und das Loeschen (KA-E-16)
        // =================================================================================

        /// <summary>Die Kennlinie der Kopie (Wärme), ihr Katalogzwilling und die Kühlkennlinie samt Zwilling.</summary>
        public const string KENNLINIE_PROJEKT = "Tab_Kenndaten";
        public const string KENNLINIE_K_PROJEKT = "Tab_Kenndaten_Kuehlung";

        /// <summary>
        /// <b>Das Gewerk des Rückwegs</b> (Konzept Katalogauswahl 5.2, KA‑E‑9): Kopie <see cref="TABELLE_PROJEKT"/>, Katalog
        /// <see cref="TABLE"/>, Anlage über <c>ID_WP</c>, Kostenkomponente 1. <b>Zwei technische Kindtabellen</b> (Konzept 7
        /// Nr. 5): die Heizkennlinie <c>Tab_Kenndaten</c> → <see cref="CURVE"/> und die Kühlkennlinie
        /// <c>Tab_Kenndaten_Kuehlung</c> → <see cref="CURVE_K"/>, je mit allen Vorlauf-Stützstellen — bei „neu" als Kopie am
        /// neuen Satz, bei „überschreiben" als Ersatz. Mit der Schnittmenge gehen die Fachspalten samt Kühlkonfiguration,
        /// Taktwerten, den acht Gerätespalten der Übergabegrenze und <c>Modulkosten</c>. Anlagenbezogen und damit im
        /// Projekt bleiben die Anlagenzeile (Betriebsart, Temperaturen, Bivalenz, Heizstab, Sperrzeiten und Zeitprogramm,
        /// Quellfelder <c>WQ_*</c>, Einbindung, Vorwärmbetrieb, <c>Vorlauf_Max</c>) und die Senken. Prüfregel: Nennleistung
        /// und Modulkosten nicht negativ.
        /// </summary>
        public static Rueckweggewerk Rueckweg() => new Rueckweggewerk
        {
            Kopietabelle = TABELLE_PROJEKT,
            Katalogtabelle = TABLE,
            Anlagenverweis = "ID_WP",
            KomponentenId = KOMPONENTE_KOSTEN,
            Kinder = new[]
            {
                (KENNLINIE_PROJEKT, CURVE, "ID_WP"),
                (KENNLINIE_K_PROJEKT, CURVE_K, "ID_WP"),
            },
            Pruefung = zeile =>
            {
                double Zahl(string spalte) => zeile.Table.Columns.Contains(spalte) && zeile[spalte] != DBNull.Value
                    ? Convert.ToDouble(zeile[spalte], CultureInfo.InvariantCulture) : 0;
                if (Zahl("Nennleistung") < 0) return Text("WPV_MSG_NENNLEISTUNG_NEGATIV", "Die Nennleistung darf nicht negativ sein.");
                if (Zahl("Modulkosten") < 0) return Text("WPV_MSG_MODULKOSTEN_NEGATIV", "Die Modulkosten dürfen nicht negativ sein.");
                return null;
            },
        };

        /// <summary>Die Zeilen der Rückfrage zu den Projektkopien <paramref name="idsKopie"/> (<see cref="Katalogrueckweg.Vorschau"/>).</summary>
        public static IReadOnlyList<Rueckwegzeile> RueckwegVorschau(IReadOnlyList<int> idsKopie)
            => Katalogrueckweg.Vorschau(Rueckweg(), idsKopie);

        /// <summary>
        /// <b>„In die Datenbank übernehmen…"</b> — die Projektkopien als neue Katalogsätze oder als Ersatz ihres Ursprungs,
        /// alles oder nichts (<see cref="Katalogrueckweg.Uebernehmen"/>): Fachspalten, Heiz- und Kühlkennlinie mit allen
        /// Stützstellen, Betriebs- und Investitionspositionen der Anlage als Satzvorlagen. Der Name der Kopie bleibt (KA‑E‑15),
        /// eine neue Kopie bekommt den Satz als Ursprung (<c>ID_Stamm</c>).
        /// </summary>
        public static Rueckwegergebnis AusProjektUebernehmen(IReadOnlyList<Rueckwegauftrag> auftraege)
            => Katalogrueckweg.Uebernehmen(Rueckweg(), auftraege);

        /// <summary>Ist der Name im Wärmepumpenkatalog vergeben?</summary>
        public static bool RueckwegNameBelegt(string name) => Katalogrueckweg.NameBelegt(Rueckweg(), name);

        /// <summary>Ausgang von <see cref="KatalogsatzLoeschen"/>.</summary>
        public sealed record KatalogsatzLoeschung(bool Ok, Satzvorlagenabbau Vorlage, string Meldung);

        /// <summary>
        /// <b>Löscht den Katalogsatz <paramref name="id"/> samt Kennlinien und Satzvorlagen</b> (KA‑E‑16, beide Verweise,
        /// <see cref="Katalogrueckweg.SatzvorlageBeimLoeschen"/>) in EINEM Vorgang — scheitert eines, bleibt alles. Die Heiz-
        /// und die Kühlkennlinie des Satzes gehen ausdrücklich mit (nicht erst über den Fremdschlüssel). Ein gesperrter Satz
        /// wird nicht gelöscht. Die Meldung nennt eine Vorlage, die Projektzeilen noch brauchen.
        /// </summary>
        public static KatalogsatzLoeschung KatalogsatzLoeschen(int id)
        {
            string[] verweise = new[] { KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE,
                                        KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION }
                .Where(sp => DataRepository.SpalteVorhanden(TABLE, sp)).ToArray();
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    DataTable satz = v.Lese("SELECT \"Bezeichner\", \"ReadOnly\"" + string.Concat(verweise.Select(sp => ", \"" + sp + "\"")) +
                                            " FROM \"" + TABLE + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
                    if (satz == null || satz.Rows.Count == 0) return new KatalogsatzLoeschung(false, Satzvorlagenabbau.KeineVorlage, "");
                    DataRow z = satz.Rows[0];
                    if (z["ReadOnly"] != DBNull.Value && Convert.ToInt64(z["ReadOnly"], CultureInfo.InvariantCulture) != 0)
                        return new KatalogsatzLoeschung(false, Satzvorlagenabbau.KeineVorlage, "");
                    string name = Convert.ToString(z["Bezeichner"], CultureInfo.InvariantCulture) ?? "";
                    int? Lies(string sp) => satz.Columns.Contains(sp) && z[sp] != DBNull.Value
                        ? Convert.ToInt32(z[sp], CultureInfo.InvariantCulture) : (int?)null;
                    v.Ausfuehren("DELETE FROM \"" + CURVE + "\" WHERE \"ID_WP\" = ?", new DbParam("@id", id));
                    v.Ausfuehren("DELETE FROM \"" + CURVE_K + "\" WHERE \"ID_WP\" = ?", new DbParam("@id", id));
                    v.Ausfuehren("DELETE FROM \"" + TABLE + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
                    Satzvorlagenabbau abbau = Katalogrueckweg.SatzvorlageBeimLoeschen(
                        v, Lies(KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE),
                        Lies(KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION));
                    v.Commit();
                    return new KatalogsatzLoeschung(true, abbau, Katalogrueckweg.SatzvorlagenMeldung(abbau, name));
                }
            }
            catch (Exception)
            {
                // DbVorgang.Dispose rollt ohne Commit zurück.
                return new KatalogsatzLoeschung(false, Satzvorlagenabbau.KeineVorlage, "");
            }
        }
    }
}
