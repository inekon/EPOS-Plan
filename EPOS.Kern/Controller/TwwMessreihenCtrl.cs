using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Kopf einer eingespielten Messreihe — so, wie ihn eine Liste braucht, ohne ihre Werte zu
    /// laden: Bezeichnung, Größe, Auflösung, Beginn, Zahl der Zeitschritte, Menge in der Einheit
    /// der Größe, Quelle und Tag des Einspielens.
    /// </summary>
    internal sealed record TwwMessreihenkopf(string Bezeichnung, ZapfMessgroesse Groesse, int AufloesungMin,
                                             string Beginn, int Schritte, double Menge, string Quelle,
                                             string DatumImport)
    {
        /// <summary>Die Länge der Reihe in Tagen [d].</summary>
        internal double Tage => (double)Schritte * AufloesungMin / Messreihe.MINUTEN_JE_TAG;
    }

    /// <summary>
    /// Was ein Einspielen einer Messreihe zurückgibt: Zahl der geschriebenen Zeilen, die Zahl der
    /// Zeilen, die eine gleichnamige Reihe hatte und die ersetzt wurden, die gelesene Reihe, die
    /// benannten Hinweise des Lesers und — wenn die Datei nicht taugt — der <see cref="Abbruch"/>.
    /// Bei einem Abbruch ist <b>nichts</b> geändert.
    /// </summary>
    internal sealed class TwwMessreihenimportBericht
    {
        /// <summary>Taugte die Datei?</summary>
        internal bool Ok => Abbruch == null;

        /// <summary>Die Zahl der geschriebenen Zeilen; 0 bei einem Abbruch.</summary>
        internal int Zeilen { get; set; }

        /// <summary>Die Zahl der Zeilen, die das Einspielen ersetzt hat (eine gleichnamige Reihe).</summary>
        internal int Ersetzt { get; set; }

        /// <summary>Die gelesene Reihe; <c>null</c> bei einem Abbruch.</summary>
        internal Messreihe Reihe { get; set; }

        /// <summary>Benannte Hinweise des Lesers und des Schreibwegs — nie still.</summary>
        internal List<ZapfSatz> Hinweise { get; } = new List<ZapfSatz>();

        /// <summary>Der Grund, aus dem die Datei abgelehnt ist; <c>null</c> = eingespielt.</summary>
        internal ZapfSatz Abbruch { get; set; }
    }

    /// <summary>
    /// <b>Der Schreib- und Leseweg der gemessenen Reihen</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.8 und Kapitel 7 Zeile Z5; Schemaschritt T4 „Messreihen", Stufe Z5):
    /// Er spielt eine CSV-Datei des Anwenders in <c>Tab_TwwMessreihe</c> ein, listet die Köpfe
    /// eines Projekts, löscht eine Reihe und liest eine Reihe für den Vergleich zurück.
    ///
    /// <para><b>Eine Reihe je Bezeichnung und Projekt.</b> Ein Einspielen ERSETZT die gleichnamige
    /// Reihe desselben Projekts — Löschen und Schreiben in EINEM Vorgang. Geht dabei etwas schief,
    /// rollt der Vorgang zurück, und die frühere Reihe steht unverändert da (kein halber Stand).</para>
    ///
    /// <para><b>Bestandteil des Projekts</b> (Kapitel 9 K5): Die Zeilen tragen <c>ID_Projekt</c> und
    /// reisen mit einer Projektkopie und einem <c>.wpx</c>-Paket; sie tragen keinen <c>Status</c>
    /// und kein <c>ReadOnly</c>, und <c>Werkzeuge/Auslieferungsvorlage</c> leert die Tabelle —
    /// gemessene Daten gehören dem Objekt, nie der Auslieferung.</para>
    ///
    /// <para><b>Kein SQL-Text aus Daten.</b> Jeder Zugriff läuft über <see cref="DataRepository"/>
    /// mit <c>?</c>-Parametern; Tabellen- und Spaltennamen stehen als Konstanten in
    /// <see cref="TwwSchema"/>. <b>Die Datei wählt die Hülle</b> über <c>Dienste.Datei</c>; hier
    /// kommt ein <see cref="Stream"/> herein.</para>
    /// </summary>
    internal static class TwwMessreihenCtrl
    {
        /// <summary>Die Spalten der Tabelle in Schreibreihenfolge.</summary>
        private const string SPALTEN = "\"ID_Projekt\", \"Bezeichnung\", \"Groesse\", \"Aufloesung_min\", " +
                                       "\"Beginn\", \"Zeilenindex\", \"Wert\", \"Quelle\", \"Datum_Import\"";

        /// <summary>Das Format des Beginns in der Ablage: ISO mit Uhrzeit, invariant.</summary>
        internal const string FORMAT_BEGINN = "yyyy-MM-ddTHH:mm";

        /// <summary>
        /// <b>Prüfnaht des Schreibvorgangs</b> (Muster <c>TwwTyptagCtrl.Pruefnaht</c>): Sie läuft
        /// INNERHALB der Transaktion, nachdem die alten Zeilen entfernt und die ersten Werte
        /// geschrieben sind. Vorbelegt mit einer folgenlosen Handlung; allein die Probe des
        /// Rollbacks belegt sie und setzt sie danach zurück.
        /// </summary>
        internal static Action Pruefnaht = () => { };

        /// <summary>Führt die Datenbank die Tabelle der Messreihen (Stand ab Schritt 132)?</summary>
        internal static bool TabelleVorhanden() => DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_MESSREIHE);

        // =================================================================================
        //  Liste und Stand
        // =================================================================================

        /// <summary>
        /// <b>Die Köpfe der Messreihen eines Projekts</b>, nach Bezeichnung geordnet (die
        /// Reihenfolge ist wiederholbar). Ohne Tabelle (Stand vor 132) und ohne Zeile eine leere
        /// Liste — der Vergleich ist dann benannt nicht verfügbar, das entscheidet der Aufrufer.
        /// </summary>
        internal static IReadOnlyList<TwwMessreihenkopf> Liste(int idProjekt)
        {
            if (!TabelleVorhanden()) return new TwwMessreihenkopf[0];

            // Aggregiert in EINER Abfrage: der Kopf steht an jeder Zeile (eine Zeile je Wert), die
            // Menge braucht die Aufloesung (eine Leistungsreihe ist kW, nicht kWh) - deshalb
            // SUM(Wert) roh und die Umrechnung in C#, an EINER Stelle (Messreihe).
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Bezeichnung\", \"Groesse\", \"Aufloesung_min\", \"Beginn\", COUNT(*) AS \"Schritte\", " +
                "SUM(\"Wert\") AS \"Summe\", MIN(\"Quelle\") AS \"Quelle\", MIN(\"Datum_Import\") AS \"Datum_Import\" " +
                "FROM \"" + TwwSchema.TAB_TWW_MESSREIHE + "\" WHERE \"ID_Projekt\" = ? " +
                "GROUP BY \"Bezeichnung\", \"Groesse\", \"Aufloesung_min\", \"Beginn\" " +
                "ORDER BY \"Bezeichnung\"",
                new DbParam("@projekt", idProjekt));
            if (t == null || t.Rows.Count == 0) return new TwwMessreihenkopf[0];

            var liste = new List<TwwMessreihenkopf>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
            {
                ZapfMessgroesse groesse = Groesse(Text(r, "Groesse"));
                int aufloesung = Ganz(r, "Aufloesung_min");
                double summe = Zahl(r, "Summe");
                double menge = groesse == ZapfMessgroesse.Leistung
                    ? summe * aufloesung / Messreihe.MINUTEN_JE_STUNDE : summe;
                liste.Add(new TwwMessreihenkopf(Text(r, "Bezeichnung"), groesse, aufloesung, Text(r, "Beginn"),
                                                Ganz(r, "Schritte"), menge, Text(r, "Quelle"),
                                                Text(r, "Datum_Import")));
            }
            return liste;
        }

        // =================================================================================
        //  Lesen
        // =================================================================================

        /// <summary>
        /// <b>Eine Messreihe eines Projekts zurücklesen</b> — der Weg des Vergleichs. Fehlt die
        /// Tabelle oder die Reihe, ist das eine benannte Ablehnung, keine leere Reihe: Ein Vergleich
        /// gegen nichts wäre ein still falsches Ergebnis.
        /// </summary>
        internal static Messreihe Lesen(int idProjekt, string bezeichnung, out ZapfSatz fehler)
        {
            fehler = null;
            if (!TabelleVorhanden())
            {
                fehler = ZapfSatz.Neu("MESSREIHENIMPORT_TABELLE_FEHLT", ZapfSatz.Tabelle(TwwSchema.TAB_TWW_MESSREIHE));
                return null;
            }
            string name = (bezeichnung ?? "").Trim();
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Groesse\", \"Aufloesung_min\", \"Beginn\", \"Zeilenindex\", \"Wert\", \"Quelle\" " +
                "FROM \"" + TwwSchema.TAB_TWW_MESSREIHE + "\" WHERE \"ID_Projekt\" = ? AND \"Bezeichnung\" = ? " +
                "ORDER BY \"Zeilenindex\"",
                new DbParam("@projekt", idProjekt), new DbParam("@name", name));
            if (t == null || t.Rows.Count == 0)
            {
                fehler = ZapfSatz.Neu("MESSREIHENIMPORT_NICHT_GEFUNDEN", name);
                return null;
            }

            var werte = new double[t.Rows.Count];
            int i = 0;
            int luecken = 0;
            foreach (DataRow r in t.Rows)
            {
                // Der Zeilenindex MUSS lueckenlos von 0 aufwaerts laufen - sonst waere die Reihe
                // zeitlich verschoben. Eine Luecke ist ein benannter Abbruch, keine stille 0.
                if (Ganz(r, "Zeilenindex") != i)
                {
                    fehler = ZapfSatz.Neu("MESSREIHENIMPORT_ZEILENINDEX_LUECKE", name, i);
                    return null;
                }
                werte[i] = Zahl(r, "Wert");
                if (werte[i] == 0.0) luecken++;
                i++;
            }

            DataRow erste = t.Rows[0];
            if (!DateTime.TryParseExact(Text(erste, "Beginn"), FORMAT_BEGINN, CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out DateTime beginn)
                && !DateTime.TryParse(Text(erste, "Beginn"), CultureInfo.InvariantCulture,
                                      DateTimeStyles.None, out beginn))
            {
                fehler = ZapfSatz.Neu("MESSREIHENIMPORT_BEGINN_UNGUELTIG", name, Text(erste, "Beginn"));
                return null;
            }

            try
            {
                // Die Zahl der Luecken steht nicht in der Tabelle (jede Zeile traegt einen Wert);
                // zurueckgelesen gilt sie als 0 - der Bericht des Einspielens hat sie benannt.
                return new Messreihe(name, Groesse(Text(erste, "Groesse")), Ganz(erste, "Aufloesung_min"),
                                     beginn, werte, Text(erste, "Quelle"));
            }
            catch (ArgumentException ex)
            {
                fehler = ZapfSatz.Neu("MESSREIHENIMPORT_FEHLGESCHLAGEN", ex.Message);
                return null;
            }
        }

        // =================================================================================
        //  Einspielen
        // =================================================================================

        /// <summary>
        /// <b>Spielt eine Messreihe aus einem Strom ein</b> (der Weg der Hülle: Datei über
        /// <c>Dienste.Datei</c> wählen, Strom öffnen, hier hereingeben). Gelesen wird über
        /// <see cref="Messreihenleser.AusStrom"/>, geschrieben in EINEM Vorgang; ein Fehler rollt
        /// zurück und lässt eine frühere gleichnamige Reihe stehen.
        /// </summary>
        internal static TwwMessreihenimportBericht Importieren(int idProjekt, Stream strom, string datei,
                                                               Messreihenoptionen optionen, string datumImport = null)
        {
            var bericht = new TwwMessreihenimportBericht();
            if (!TabelleVorhanden())
            {
                bericht.Abbruch = ZapfSatz.Neu("MESSREIHENIMPORT_TABELLE_FEHLT",
                                               ZapfSatz.Tabelle(TwwSchema.TAB_TWW_MESSREIHE));
                return bericht;
            }
            Messreihe reihe = Messreihenleser.AusStrom(strom, datei, optionen, out ZapfSatz fehler, bericht.Hinweise);
            if (reihe == null)
            {
                bericht.Abbruch = fehler;
                return bericht;
            }
            return Schreiben(idProjekt, reihe, datumImport, bericht);
        }

        /// <summary>
        /// <b>Spielt eine schon gelesene Reihe ein</b> (ein Testhelfer, ein zweiter Leseweg) — sonst
        /// wie <see cref="Importieren(int, Stream, string, Messreihenoptionen, string)"/>.
        /// </summary>
        internal static TwwMessreihenimportBericht Importieren(int idProjekt, Messreihe reihe, string datumImport = null)
        {
            var bericht = new TwwMessreihenimportBericht();
            if (!TabelleVorhanden())
            {
                bericht.Abbruch = ZapfSatz.Neu("MESSREIHENIMPORT_TABELLE_FEHLT",
                                               ZapfSatz.Tabelle(TwwSchema.TAB_TWW_MESSREIHE));
                return bericht;
            }
            if (reihe == null)
            {
                bericht.Abbruch = ZapfSatz.Neu("MESSREIHENIMPORT_OHNE_REIHE");
                return bericht;
            }
            return Schreiben(idProjekt, reihe, datumImport, bericht);
        }

        /// <summary>
        /// Schreibt die Reihe: die gleichnamige weg, die neue hinein, alles in EINEM Vorgang. Eine
        /// Ausnahme rollt zurück und wird benannt zurückgegeben; die frühere Reihe bleibt.
        /// </summary>
        private static TwwMessreihenimportBericht Schreiben(int idProjekt, Messreihe reihe, string datumImport,
                                                           TwwMessreihenimportBericht bericht)
        {
            string datum = string.IsNullOrWhiteSpace(datumImport)
                ? DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : datumImport.Trim();
            string beginn = reihe.Beginn.ToString(FORMAT_BEGINN, CultureInfo.InvariantCulture);
            string groesse = Kennung(reihe.Groesse);
            int vorher = 0;
            int geschrieben = 0;

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        if (Anzahl(v, "SELECT COUNT(*) FROM \"Tab_Projekt\" WHERE \"ID\" = ?", idProjekt) == 0)
                            throw new InvalidOperationException(
                                ZapfSatz.Neu("MESSREIHENIMPORT_PROJEKT_FEHLT", idProjekt).Klartext);

                        vorher = v.Ausfuehren("DELETE FROM \"" + TwwSchema.TAB_TWW_MESSREIHE + "\" " +
                                              "WHERE \"ID_Projekt\" = ? AND \"Bezeichnung\" = ?",
                                              new DbParam("@projekt", idProjekt),
                                              new DbParam("@name", reihe.Bezeichnung));

                        IReadOnlyList<double> werte = reihe.Werte;
                        for (int i = 0; i < werte.Count; i++)
                        {
                            geschrieben += v.Ausfuehren(
                                "INSERT INTO \"" + TwwSchema.TAB_TWW_MESSREIHE + "\" (" + SPALTEN + ") " +
                                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                new DbParam("@projekt", idProjekt),
                                new DbParam("@name", reihe.Bezeichnung),
                                new DbParam("@groesse", groesse),
                                new DbParam("@aufloesung", reihe.AufloesungMin),
                                new DbParam("@beginn", beginn),
                                new DbParam("@index", i),
                                new DbParam("@wert", werte[i]),
                                new DbParam("@quelle", reihe.Quelle),
                                new DbParam("@datum", datum));
                            if (i == 0) Pruefnaht();
                        }

                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex) when (ex is not LesemodusException)
            {
                bericht.Zeilen = 0;
                bericht.Abbruch = ZapfSatz.Neu("MESSREIHENIMPORT_FEHLGESCHLAGEN", ex.Message);
                return bericht;
            }

            if (vorher > 0) bericht.Hinweise.Add(ZapfSatz.Neu("MESSREIHENIMPORT_ERSETZT", reihe.Bezeichnung, vorher));
            bericht.Zeilen = geschrieben;
            bericht.Ersetzt = vorher;
            bericht.Reihe = reihe;
            return bericht;
        }

        // =================================================================================
        //  Löschen
        // =================================================================================

        /// <summary>
        /// <b>Löscht eine Messreihe eines Projekts</b> und gibt die Zahl der entfernten Zeilen
        /// zurück; 0 heißt „es gab sie nicht" (kein Fehler — ein zweites Löschen ist folgenlos).
        /// Ohne Tabelle 0.
        /// </summary>
        internal static int Loeschen(int idProjekt, string bezeichnung)
        {
            if (!TabelleVorhanden()) return 0;
            return DataRepository.ExecuteNonQuery(
                "DELETE FROM \"" + TwwSchema.TAB_TWW_MESSREIHE + "\" WHERE \"ID_Projekt\" = ? AND \"Bezeichnung\" = ?",
                new DbParam("@projekt", idProjekt), new DbParam("@name", (bezeichnung ?? "").Trim()));
        }

        // =================================================================================
        //  Kleinkram
        // =================================================================================

        /// <summary>Die Kennung der Ablage zu einer Größe (die Wertemenge der DDL).</summary>
        internal static string Kennung(ZapfMessgroesse groesse)
        {
            switch (groesse)
            {
                case ZapfMessgroesse.Volumen: return TwwSchema.MESSGROESSE_VOLUMEN;
                case ZapfMessgroesse.Leistung: return TwwSchema.MESSGROESSE_LEISTUNG;
                default: return TwwSchema.MESSGROESSE_ENERGIE;
            }
        }

        /// <summary>Die Größe zu einer Kennung der Ablage; ein fremder Text gilt als Energie (CHECK schließt ihn aus).</summary>
        internal static ZapfMessgroesse Groesse(string kennung)
        {
            if (string.Equals(kennung, TwwSchema.MESSGROESSE_VOLUMEN, StringComparison.Ordinal)) return ZapfMessgroesse.Volumen;
            if (string.Equals(kennung, TwwSchema.MESSGROESSE_LEISTUNG, StringComparison.Ordinal)) return ZapfMessgroesse.Leistung;
            return ZapfMessgroesse.Energie;
        }

        private static long Anzahl(DbVorgang v, string sql, int wert)
        {
            object o = v.Skalar(sql, new DbParam("@wert", wert));
            return o == null || o == DBNull.Value ? 0L : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static string Text(DataRow r, string spalte)
        {
            object o = r.Table.Columns.Contains(spalte) ? r[spalte] : null;
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static int Ganz(DataRow r, string spalte)
        {
            object o = r.Table.Columns.Contains(spalte) ? r[spalte] : null;
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static double Zahl(DataRow r, string spalte)
        {
            object o = r.Table.Columns.Contains(spalte) ? r[spalte] : null;
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }
    }
}
