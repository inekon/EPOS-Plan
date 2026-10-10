using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

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
    }
}
