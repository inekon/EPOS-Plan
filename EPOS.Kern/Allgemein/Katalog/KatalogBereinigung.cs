using System;
using System.Collections.Generic;
using System.Data;
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

                // Eine benutzte Zeile eines Katalogs mit VerwendungSperrt ist unveraenderlich
                // (Zapfprofilgenerator 3.2) - auch dann, wenn sie wie eine leere Kopie aussieht.
                if (k.VerwendungSperrt)
                {
                    string sperre = Sperrgrund(k, dublette.Id);
                    if (sperre != null)
                    {
                        erg.Protokoll.Add(k.Tabelle + ", ID " + dublette.Id + " \"" + dublette.Name +
                            "\": gesperrt (" + sperre + ") - bleibt stehen.");
                        erg.Offen++;
                        continue;
                    }
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
        ///
        /// <para><b>In EINEM Vorgang.</b> Bloecke und Kopf fallen zusammen oder gar nicht:
        /// Scheitert der Kopf (etwa an einem Fremdschluessel, der auf ihn zeigt) oder gibt
        /// es ihn nicht, rollt der Vorgang zurueck, und die Bloecke stehen unveraendert da.
        /// Bei einem Katalog mit <see cref="KatalogDefinition.VerwendungSperrt"/> prueft
        /// derselbe Vorgang vorher die Sperre (<see cref="Sperrgrund(KatalogDefinition, int)"/>)
        /// und loescht eine benutzte oder ausgelieferte Zeile nicht.</para>
        /// </summary>
        /// <returns>true, wenn Kopf und Bloecke geloescht sind.</returns>
        public static bool SatzLoeschen(KatalogDefinition k, int id)
        {
            if (k == null) return false;
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    if (k.VerwendungSperrt && Sperrgrund(v, k, id) != null)
                    {
                        v.Rollback();
                        return false;
                    }

                    foreach (KatalogDatenblock b in k.Datenbloecke)
                    {
                        // Ein Block, dessen Tabelle diese Datenbank noch nicht fuehrt (etwa die
                        // Zapfkategorien vor Schritt 114), hat nichts zu loeschen.
                        if (!BlocktabelleDa(v, b)) continue;
                        v.Ausfuehren(
                            "DELETE FROM [" + b.Tabelle + "] WHERE [" + b.FkSpalte + "] = ?",
                            new DbParam("@fk", id));
                    }

                    int kopf = v.Ausfuehren(
                        "DELETE FROM [" + k.Tabelle + "] WHERE [" + k.IdSpalte + "] = ?",
                        new DbParam("@id", id));
                    if (kopf < 1)
                    {
                        v.Rollback();
                        return false;
                    }

                    v.Commit();
                    return true;
                }
            }
            catch (LesemodusException ex)
            {
                // Wie der Datenzugriff selbst: ein Satz fuer den Anwender, "nicht geloescht".
                DataRepository.FehlerMelden(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Katalogsatz nicht geloescht (" + k.Tabelle + ", ID " + id + "): " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Benennt einen Satz um (iU9-W14c.0g, Befund W14c-B45).
        ///
        /// <para><b>Warum das hier steht.</b> Es war der EINZIGE Schreibzugriff der
        /// Dublettenmaske ohne Controller: ein verketteter <c>UPDATE</c>-Text
        /// unmittelbar im Formular. Tabellen- und Spaltenname kommen aus der Registry
        /// (nicht vom Anwender), der Name selbst geht als <see cref="DbParam"/>.</para>
        ///
        /// <para>Die Namenspruefung - nicht leer, normalisiert nicht anderweitig
        /// vergeben - bleibt beim Aufrufer: Sie ist eine Bedienregel, keine
        /// Datenbankregel (<c>DublettenPruefung.VergebeneNamen</c>).</para>
        /// </summary>
        public static bool SatzUmbenennen(KatalogDefinition k, int id, string neu)
        {
            if (k == null || id <= 0) return false;

            // Eine benutzte oder ausgelieferte Zeile eines Katalogs mit VerwendungSperrt
            // behaelt ihren Namen - er ist Teil des natuerlichen Schluessels, ueber den der
            // Projektexport Katalogverweise zuordnet (Zapfprofilgenerator 3.2). Pruefung und
            // Schreiben in EINEM Vorgang.
            if (k.VerwendungSperrt)
            {
                try
                {
                    using (DbVorgang v = DataRepository.Vorgang())
                    {
                        if (Sperrgrund(v, k, id) != null)
                        {
                            v.Rollback();
                            return false;
                        }
                        int n = v.Ausfuehren(
                            "UPDATE [" + k.Tabelle + "] SET [" + k.NamensSpalte + "] = ? " +
                            "WHERE [" + k.IdSpalte + "] = ?",
                            new DbParam("@name", (object)(neu ?? "")),
                            new DbParam("@id", id));
                        if (n < 1)
                        {
                            v.Rollback();
                            return false;
                        }
                        v.Commit();
                        return true;
                    }
                }
                catch (LesemodusException ex)
                {
                    DataRepository.FehlerMelden(ex.Message);
                    return false;
                }
                catch { return false; }
            }

            try
            {
                return DataRepository.ExecuteSQL(
                    "UPDATE [" + k.Tabelle + "] SET [" + k.NamensSpalte + "] = ? " +
                    "WHERE [" + k.IdSpalte + "] = ?",
                    new DbParam("@name", (object)(neu ?? "")),
                    new DbParam("@id", id));
            }
            catch { return false; }
        }

        /// <summary>
        /// <b>Warum ein Satz gesperrt ist</b> — nur fuer Kataloge mit
        /// <see cref="KatalogDefinition.VerwendungSperrt"/> (Zapfprofilgenerator 3.2): der
        /// Grund als Text (<c>ReadOnly</c>, Fundstellen der Verwendung, eine gescheiterte
        /// Pruefung) oder <c>null</c>, wenn der Satz frei ist. Ein Katalog ohne den Schalter
        /// ist nie gesperrt. Eine Pruefung, die nicht laufen kann, SPERRT (Befund W14c-B44:
        /// ein Fehlschlag ist nicht "nicht verwendet").
        /// </summary>
        public static string Sperrgrund(KatalogDefinition k, int id)
        {
            if (k == null || !k.VerwendungSperrt) return null;
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    string grund = Sperrgrund(v, k, id);
                    v.Rollback();
                    return grund;
                }
            }
            catch (Exception ex)
            {
                return "Verwendungspruefung gescheitert: " + ex.Message;
            }
        }

        /// <summary>Die Sperrpruefung im laufenden Vorgang — Pruefung und Schreiben sehen denselben Stand.</summary>
        private static string Sperrgrund(DbVorgang v, KatalogDefinition k, int id)
        {
            if (k == null || !k.VerwendungSperrt) return null;

            object ro = v.Skalar("SELECT [ReadOnly] FROM [" + k.Tabelle + "] WHERE [" + k.IdSpalte + "] = ?",
                                 new DbParam("@id", id));
            if (ro != null && Convert.ToInt64(ro, CultureInfo.InvariantCulture) != 0)
                return "schreibgeschuetzt (ReadOnly)";

            // Ein Datenblock mit eigener Spalte ReadOnly (die Zapfkategorien einer Nutzungsart):
            // Eine ausgelieferte Blockzeile ist ebenso unveraenderlich wie ein ausgelieferter Kopf.
            foreach (KatalogDatenblock b in k.Datenbloecke)
            {
                if (!BlocktabelleDa(v, b)) continue;
                object mitSpalte = v.Skalar("SELECT COUNT(*) FROM pragma_table_info(?) WHERE name = 'ReadOnly'",
                                            new DbParam("@tabelle", b.Tabelle));
                if (mitSpalte == null || Convert.ToInt64(mitSpalte, CultureInfo.InvariantCulture) == 0) continue;
                object n = v.Skalar("SELECT COUNT(*) FROM [" + b.Tabelle + "] WHERE [" + b.FkSpalte + "] = ? AND [ReadOnly] <> 0",
                                    new DbParam("@fk", id));
                if (n == null) return "Verwendungspruefung " + b.Tabelle + " nicht lesbar";
                if (Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0)
                    return "schreibgeschuetzt (ReadOnly in " + b.Tabelle + ")";
            }

            object name = null;
            var treffer = new List<string>();
            foreach (VerwendungsPruefung vp in k.VerwendungsPruefungen)
            {
                object wert = id;
                if (vp.UeberName)
                {
                    if (name == null)
                        name = v.Skalar("SELECT [" + k.NamensSpalte + "] FROM [" + k.Tabelle + "] WHERE [" + k.IdSpalte + "] = ?",
                                        new DbParam("@id", id)) ?? "";
                    wert = name;
                }

                object anzahl = v.Skalar("SELECT COUNT(*) FROM [" + vp.Tabelle + "] WHERE [" + vp.Spalte + "] = ?",
                                         new DbParam("@wert", wert));
                if (anzahl == null) return "Verwendungspruefung " + vp.Tabelle + " nicht lesbar";
                long n = Convert.ToInt64(anzahl, CultureInfo.InvariantCulture);
                if (n > 0) treffer.Add(vp.Tabelle + " (" + n.ToString(CultureInfo.InvariantCulture) + ")");
            }
            return treffer.Count > 0 ? "verwendet: " + string.Join(", ", treffer) : null;
        }

        /// <summary>Fuehrt die Datenbank die Tabelle des Blocks? (Im laufenden Vorgang gefragt.)</summary>
        private static bool BlocktabelleDa(DbVorgang v, KatalogDatenblock b)
        {
            object n = v.Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                                new DbParam("@tabelle", b.Tabelle));
            return n != null && Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// Zaehlt die Verwendungen eines Satzes nach einer Registry-Pruefung
        /// (iU9-W14c.0f, Befund W14c-B44).
        ///
        /// <para><b>Ein Fehlschlag ist NICHT "nicht verwendet".</b> Der Vorlaeufer fing
        /// jede Ausnahme ab und lieferte 0 - eine fehlende Tabelle sah damit aus wie
        /// ein freier Satz, und die Loeschsperre griff nicht. Hier kommt der Fehler
        /// als <paramref name="fehler"/> zurueck, und der Aufrufer meldet ihn.</para>
        /// </summary>
        /// <returns>Die Trefferzahl; -1, wenn die Pruefung nicht laufen konnte.</returns>
        public static int VerwendungZaehlen(VerwendungsPruefung vp, KatalogSatz satz, out string fehler)
        {
            fehler = null;
            if (vp == null || satz == null) return 0;

            object anz;
            string[] still;

            // Der Zugriff meldet einen Fehler NICHT als Ausnahme, sondern ueber
            // DataRepository.FehlerMelden, und liefert null - deshalb der dialogfreie
            // Modus samt Abholen der Sammlung. Ein SELECT COUNT(*) liefert IMMER eine
            // Zeile; null heisst also: die Abfrage ist gescheitert.
            using (DataRepository.EngineModus())
            {
                try
                {
                    anz = DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM [" + vp.Tabelle + "] WHERE [" + vp.Spalte + "] = ?",
                        new DbParam("@wert", vp.UeberName ? (object)(satz.Name ?? "") : (object)satz.Id));
                }
                catch (Exception ex)
                {
                    DataRepository.StilleFehlerAbholen();
                    fehler = ex.Message;
                    return -1;
                }
                still = DataRepository.StilleFehlerAbholen();
            }

            if (anz == null || anz is DBNull)
            {
                fehler = still.Length > 0 ? still[0] : ("Tabelle " + vp.Tabelle + " nicht lesbar.");
                return -1;
            }

            try { return Convert.ToInt32(anz, CultureInfo.InvariantCulture); }
            catch (Exception ex) { fehler = ex.Message; return -1; }
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
