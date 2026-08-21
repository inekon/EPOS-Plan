using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Ergebnis einer Katalog-Bereinigung (Zahlen plus Protokollzeilen).</summary>
    public class BereinigungsErgebnis
    {
        public int Geloescht;
        public int Offen;
        public List<string> Protokoll = new List<string>();
    }

    // ====================================================================================
    // Bereinigung "leerer Kopien" nach der Regel von Migrationsschritt 24, erweitert um
    // die Datenblock-Bedingung (Konzept 5.2/7.1): Je Namensgruppe behaelt die kleinste
    // ID den Platz; eine Dublette wird nur entfernt, wenn sie (a) nicht ReadOnly ist,
    // (b) in keiner Kopfspalte einen EIGENEN, nicht leeren Wert traegt, den der
    // behaltene Satz nicht hat, und (c) ihre Datenbloecke leer oder mit denen des
    // behaltenen Satzes identisch sind. Gefuellte, abweichende Saetze bleiben stehen
    // und werden gemeldet - deren Aufloesung ist Handarbeit im Admin-Dialog.
    //
    // Genutzt von der Admin-Dublettensuche (Form_KatalogDubletten). Der
    // Migrationsschritt 30 wendet DIESELBE fachliche Regel in seiner eigenen, in das
    // Migrationsprotokoll eingebetteten Fassung an
    // (SchemaMigration.KatalogBereinigenMitBloecken) - eine Regelaenderung muss
    // deshalb immer beide Stellen treffen. Gemeinsame Basis bleiben KatalogRegistry
    // und DublettenPruefung (BlockHashes, Kanonisch).
    // ====================================================================================
    public static class KatalogBereinigung
    {
        /// <summary>Wendet die Leerkopien-Regel auf einen ganzen Katalog an.</summary>
        public static BereinigungsErgebnis LeereKopienBereinigen(KatalogDefinition k)
        {
            BereinigungsErgebnis erg = new BereinigungsErgebnis();

            ScanErgebnis scan = DublettenPruefung.ScanKatalog(k);
            if (scan.Fehler != null)
            {
                erg.Protokoll.Add(k.Tabelle + ": " + scan.Fehler);
                return erg;
            }

            foreach (DublettenGruppe g in scan.Namensgruppen)
                GruppeBereinigen(k, g, erg);

            return erg;
        }

        /// <summary>Wendet die Leerkopien-Regel auf EINE Namensgruppe an.</summary>
        public static void GruppeBereinigen(KatalogDefinition k, DublettenGruppe g, BereinigungsErgebnis erg)
        {
            if (g == null || g.Saetze.Count < 2) return;

            // ScanKatalog laedt nach Name+Id sortiert - der erste Satz ist die kleinste ID.
            KatalogSatz behalten = g.Saetze[0];
            List<string> behaltenBloecke = null;   // erst bei Bedarf ermitteln

            for (int i = 1; i < g.Saetze.Count; i++)
            {
                KatalogSatz dublette = g.Saetze[i];

                // Auslieferungsbestand nie anfassen - dieselbe Zusage wie ueberall.
                if (dublette.ReadOnly)
                {
                    erg.Protokoll.Add(k.Tabelle + ", ID " + dublette.Id + " \"" + dublette.Name +
                        "\": schreibgeschuetzt (ReadOnly) - bleibt trotz doppeltem Namen stehen.");
                    erg.Offen++;
                    continue;
                }

                string eigenerWert = ErsteEigeneSpalte(k, behalten.Zeile, dublette.Zeile);
                if (eigenerWert != null)
                {
                    erg.Protokoll.Add(k.Tabelle + ", ID " + dublette.Id + " \"" + dublette.Name +
                        "\": traegt in " + eigenerWert + " einen eigenen Wert, den ID " + behalten.Id +
                        " nicht hat - das koennten zwei verschiedene Geraete sein. Bleibt stehen.");
                    erg.Offen++;
                    continue;
                }

                if (k.Datenbloecke.Length > 0)
                {
                    if (behaltenBloecke == null)
                        behaltenBloecke = DublettenPruefung.BlockHashes(k, behalten.Id);
                    List<string> dubBloecke = DublettenPruefung.BlockHashes(k, dublette.Id);

                    string abweichenderBlock = null;
                    for (int b = 0; b < k.Datenbloecke.Length; b++)
                        if (dubBloecke[b].Length > 0 &&
                            !string.Equals(dubBloecke[b], behaltenBloecke[b], StringComparison.Ordinal))
                        {
                            abweichenderBlock = k.Datenbloecke[b].Tabelle;
                            break;
                        }

                    if (abweichenderBlock != null)
                    {
                        erg.Protokoll.Add(k.Tabelle + ", ID " + dublette.Id + " \"" + dublette.Name +
                            "\": traegt in " + abweichenderBlock + " eigene Datenblockwerte - " +
                            "bleibt stehen und muss von Hand entschieden werden.");
                        erg.Offen++;
                        continue;
                    }
                }

                if (!SatzLoeschen(k, dublette.Id))
                {
                    erg.Protokoll.Add(k.Tabelle + ", ID " + dublette.Id + " \"" + dublette.Name +
                        "\": Das Loeschen schlug fehl - die Zeile bleibt unveraendert stehen.");
                    erg.Offen++;
                    continue;
                }

                erg.Protokoll.Add(k.Tabelle + ", ID " + dublette.Id + " \"" + dublette.Name +
                    "\": entfernt - reine Wiederholung von ID " + behalten.Id + ".");
                erg.Geloescht++;
            }
        }

        /// <summary>
        /// Loescht einen Kopfsatz samt seiner Datenbloecke (Kaskade, Konzept 7.1 -
        /// bei der WP haengen die Kennlinien an ID_WP, bei Ganglinien die Werte an
        /// ID_Ganglinie). Reihenfolge: erst die Bloecke, dann der Kopf.
        /// </summary>
        public static bool SatzLoeschen(KatalogDefinition k, int id)
        {
            foreach (KatalogDatenblock b in k.Datenbloecke)
                DataRepository.ExecuteSQL(
                    "DELETE FROM [" + b.Tabelle + "] WHERE [" + b.FkSpalte + "] = ?",
                    new OleDbParameter("@fk", id));

            return DataRepository.ExecuteSQL(
                "DELETE FROM [" + k.Tabelle + "] WHERE [" + k.IdSpalte + "] = ?",
                new OleDbParameter("@id", id));
        }

        /// <summary>
        /// Der Name der ersten Kopfspalte, in der die Dublette einen EIGENEN, nicht
        /// leeren Wert traegt, den der behaltene Satz nicht hat - oder null, wenn die
        /// Dublette nichts beisteuert (Leerwert-Regel aus Migrationsschritt 24:
        /// NULL/""/0/FALSE zaehlen als leer).
        /// </summary>
        public static string ErsteEigeneSpalte(KatalogDefinition k, DataRow behalten, DataRow dublette)
        {
            if (behalten == null || dublette == null) return null;

            foreach (DataColumn c in dublette.Table.Columns)
            {
                if (string.Equals(c.ColumnName, k.IdSpalte, StringComparison.OrdinalIgnoreCase)) continue;

                object a = behalten[c.ColumnName];
                object b = dublette[c.ColumnName];

                if (string.Equals(DublettenPruefung.Kanonisch(a), DublettenPruefung.Kanonisch(b),
                                  StringComparison.Ordinal)) continue;    // gleich
                if (Leerwert(b)) continue;                                // Dublette leer

                return c.ColumnName;
            }
            return null;
        }

        /// <summary>Leerwert im Sinne der Bereinigungsregel: NULL, Leertext, 0, FALSE.</summary>
        public static bool Leerwert(object v)
        {
            if (v == null || v is DBNull) return true;
            if (v is string) return ((string)v).Trim().Length == 0;
            if (v is bool) return !(bool)v;
            try
            {
                return Math.Abs(Convert.ToDouble(v, CultureInfo.InvariantCulture)) < 1e-12;
            }
            catch
            {
                return false;
            }
        }
    }
}
