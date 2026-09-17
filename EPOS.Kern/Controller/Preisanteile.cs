using System;
using System.Collections.Generic;
using System.Data;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DER EINE BAUPLAN BEIDER PREISZERLEGUNGEN.
    //
    // Strom (StrompreisZerlegungCtrl) und Brennstoff (BrennstoffBestandteilCtrl)
    // zerlegen seit dem Anwenderentscheid zum Anteil_Modus DENSELBEN Preis nach
    // DERSELBEN Regel: (Wert, Aktiv)-Paare in energy_project_settings, Summe der
    // aktiven Anteile, Rest gegen den Arbeitspreis der Traegerkarte. Was daran
    // beide Traeger gemeinsam haben, steht hier EINMAL.
    //
    // WAS HIER STEHT - und nur das:
    //   * die Vorsorge (Spalten anlegen, wenn die Migration noch nicht lief),
    //   * das Lesen eines (Wert, Aktiv)-Paares in seinen zwei Spielarten,
    //   * das SET-Fragment und der NULL-taugliche Zahlenparameter der Schreibseite,
    //   * Summe und Rest in der Abrechnungseinheit ct/kWh.
    //
    // WAS HIER NICHT STEHT: die Spaltennamen, die Komponentenliste und der
    // Zuschnitt des Satzes (Umlagen einzeln oder als Summe). Das ist je Traeger
    // verschieden und bleibt in seinem Controller - eine Abstraktion auf Vorrat
    // waere hier genau der Fehler, den der Auftrag ausschliesst.
    //
    // DIE ZWEI SPIELARTEN DES LESENS sind kein Schoenheitsfehler, sondern die
    // fachliche Unterscheidung:
    //   Paar(...)        Strom: NULL laesst den VORSCHLAG des Modells stehen,
    //                    inaktiv. Eine per ADD COLUMN angelegte YESNO-Spalte steht
    //                    ueberall auf 0; der DOUBLE-Wert ist das verlaessliche
    //                    Kennzeichen dafuer, ob ueberhaupt gepflegt wurde.
    //   PaarNullbar(...) Brennstoff: NULL BLEIBT null - „kein Anteil erfasst".
    //                    Hier gibt es keinen Vorschlag zu verteidigen, und die
    //                    Kohaerenzpruefung (BW2) haengt an genau dieser
    //                    Unterscheidung (E5-Falle, Konzept BHKW § 5.1).
    // ---------------------------------------------------------------------------
    public static class Preisanteile
    {
        // =====================================================================
        // Vorsorge
        // =====================================================================

        /// <summary>
        /// Legt die genannten Spalten an, soweit sie fehlen — die tolerante
        /// Rückfallebene für eine Datenbank, deren Migration noch nicht gelaufen ist.
        /// </summary>
        /// <param name="herkunft">
        /// Aufrufer für das Protokoll (z. B. <c>"StrompreisZerlegungCtrl"</c>).
        /// </param>
        /// <param name="spalten">Die Sollspalten, gern aus mehreren Schritten.</param>
        /// <remarks>
        /// <para>
        /// <b>Bewusst OHNE jede Vorbelegung und ohne Faltung.</b> Hier entstehen nur
        /// die Spalten, damit ein Lesezugriff nicht scheitert; die Leseseite
        /// entscheidet danach je Träger, was ein leeres Feld bedeutet. Eine Faltung
        /// ändert Geldwerte und gehört in einen Migrationsschritt, nicht beiläufig in
        /// das Öffnen eines Dialogs.
        /// </para>
        /// <para>
        /// <b>JE TABELLE prüfen.</b> Das Schema wird je Tabelle einmal gelesen und
        /// gemerkt — sonst griffe die Existenzprüfung für die Spalten der zweiten
        /// Tabelle nie, und das <c>ALTER TABLE</c> liefe bei jedem Öffnen erneut.
        /// Dasselbe Vorgehen wie in <c>SchemaMigration.SpaltenAnlegen</c>.
        /// </para>
        /// <para>
        /// <b>Ohne Dialog.</b> Eine Vorsorge ist kein Bedienschritt. Das DDL läuft
        /// deshalb über <see cref="StilleDb"/> statt über
        /// <c>DataRepository.ExecuteSQL</c>, das seine Fehler selbst als Dialog zeigt
        /// und damit am umschliessenden <c>try/catch</c> vorbeikäme. Echte Fehler
        /// bleiben sichtbar: Scheitert das Anlegen wirklich (Datei schreibgeschützt,
        /// Datenbank exklusiv geöffnet), meldet der nachfolgende Zugriff über
        /// <c>DataRepository</c> ganz regulär.
        /// </para>
        /// </remarks>
        public static void SpaltenSicherstellen(string herkunft, IEnumerable<SchemaSpalte> spalten)
        {
            if (spalten == null) return;

            try
            {
                // Schema je Tabelle - einmal gelesen, dann gemerkt.
                Dictionary<string, HashSet<string>> schemaJeTabelle =
                    new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (SchemaSpalte s in spalten)
                {
                    if (s == null) continue;

                    HashSet<string> vorhanden;
                    if (!schemaJeTabelle.TryGetValue(s.Tabelle, out vorhanden))
                    {
                        vorhanden = StilleDb.SpaltenNamen(s.Tabelle);
                        schemaJeTabelle[s.Tabelle] = vorhanden;
                    }

                    // null = Tabelle gibt es (noch) nicht. Sie hier anzulegen ist nicht
                    // Aufgabe dieser Vorsorge - das erledigen die Migration bzw. das
                    // Kostenmodul, das die Pflichtfelder kennt.
                    if (vorhanden == null) continue;
                    if (vorhanden.Contains(s.Name)) continue;

                    // Protokoll statt Dialog - siehe <remarks>. StilleDb.NonQuery
                    // liefert -1 statt zu werfen.
                    if (StilleDb.NonQuery(StilleDb.AlterTableAddColumn(
                            s.Tabelle, s.Name, s.TypDefinition)) < 0)
                        Protokoll(herkunft,
                                  s.Tabelle + "." + s.Name + ": Spalte konnte nicht angelegt werden.");
                    else
                        vorhanden.Add(s.Name);
                }
            }
            catch (Exception ex)
            {
                // Keine Verbindung, kein Schema - der eigentliche Zugriff meldet es.
                Protokoll(herkunft, ex.Message);
            }
        }

        /// <summary>Protokolliert einen Vorsorge-Fehlschlag, ohne den Anwender zu stören.</summary>
        private static void Protokoll(string herkunft, string meldung)
        {
            try { Console.WriteLine(herkunft + ".StelleSpaltenSicher: " + meldung); }
            catch { }
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Übernimmt Wert UND Aktiv-Schalter eines Anteils — aber nur, wenn der WERT
        /// gepflegt ist. NULL lässt den Vorschlag des Modells stehen, inaktiv.
        /// </summary>
        /// <remarks>
        /// <b>Warum der Wert über den Schalter entscheidet.</b> Eine per
        /// <c>ADD COLUMN … YESNO</c> angelegte Spalte steht in jeder bestehenden Zeile
        /// sofort auf 0. Würde der Schalter für sich gelesen, stände jede Zeile, deren
        /// Spalten die stille Rückfallebene angelegt hat, auf „alle Anteile inaktiv" —
        /// auch eine gepflegte Zeile, deren Wert die Migration erst noch anlegt. Der
        /// DOUBLE-Wert dagegen ist NULL, solange nichts gepflegt wurde, und ist damit
        /// das verlässliche Kennzeichen. Ist er gepflegt, ist auch der Schalter gepflegt.
        /// </remarks>
        public static void Paar(DataTable dt, DataRow r, string spalte,
                                ref double wert, ref bool aktiv)
        {
            if (dt == null || r == null) return;
            if (!dt.Columns.Contains(spalte)) return;

            object v = r[spalte];
            if (v == null || v == DBNull.Value) return;   // nicht gepflegt -> Vorschlag bleibt, inaktiv

            wert = Convert.ToDouble(v);
            Schalter(dt, r, spalte, ref aktiv);
        }

        /// <summary>
        /// Übernimmt Wert UND Aktiv-Schalter eines Anteils; ein nicht gepflegter Wert
        /// bleibt <c>null</c> — „kein Anteil erfasst".
        /// </summary>
        /// <remarks>
        /// <b>Warum der Schalter hier eigenständig gelesen wird.</b> <c>False</c> ist
        /// hier genau die richtige Aussage („Anteil nicht ausgewiesen"), es gibt keine
        /// Vorgabe zu verteidigen. Der Schalter wird deshalb gelesen, wie er dasteht;
        /// ein aktiver Schalter ohne Wert trägt 0 bei und ist damit ehrlich abgebildet.
        /// </remarks>
        public static void PaarNullbar(DataTable dt, DataRow r, string spalte,
                                       ref double? wert, ref bool aktiv)
        {
            if (dt == null || r == null) return;
            if (!dt.Columns.Contains(spalte)) return;

            object v = r[spalte];
            if (v != null && v != DBNull.Value) wert = Convert.ToDouble(v);

            Schalter(dt, r, spalte, ref aktiv);
        }

        /// <summary>Liest die Aktiv-Spalte eines Anteils, wenn sie da und gepflegt ist.</summary>
        private static void Schalter(DataTable dt, DataRow r, string spalte, ref bool aktiv)
        {
            string name = spalte + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX;
            if (!dt.Columns.Contains(name)) return;

            object s = r[name];
            if (s == null || s == DBNull.Value) return;
            aktiv = Convert.ToBoolean(s);
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Wert- und Aktiv-Spalte eines Anteils als SET-Fragment — zwei
        /// <c>?</c>-Parameter, nie ein zusammengesetzter SQL-Text.
        /// </summary>
        public static string SetzPaar(string spalte)
        {
            return "[" + spalte + "] = ?, [" + spalte + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX + "] = ?, ";
        }

        /// <summary>
        /// Ein DOUBLE-Parameter, der <c>null</c> als <c>DBNull</c> weitergibt.
        /// </summary>
        /// <remarks>
        /// <b>null wird DBNull, nicht 0.</b> Eine 0 wäre die Aussage „der Anteil ist
        /// null ct/kWh"; NULL ist die Aussage „es ist keiner erfasst". Der Unterschied
        /// ist genau der, den die Kohärenzprüfung braucht, und er muss deshalb auch den
        /// Weg in die Datenbank überstehen.
        /// </remarks>
        public static DbParam Wert(string name, double? wert)
        {
            return new DbParam(name, DbParamTyp.Double)
            {
                Wert = wert.HasValue ? (object)wert.Value : DBNull.Value
            };
        }

        /// <summary>Ein Ja/Nein-Parameter eines Aktiv-Schalters.</summary>
        public static DbParam Aktiv(string name, bool wert)
        {
            return new DbParam(name, DbParamTyp.Boolean) { Wert = wert };
        }

        // =====================================================================
        // Rechnen — die eine Stelle für Summe und Rest
        // =====================================================================

        /// <summary>
        /// Summe der AKTIVEN Anteile [ct/kWh] — die Zahl, die in der Summenzeile steht
        /// und gegen den Arbeitspreis gehalten wird.
        /// </summary>
        /// <remarks>
        /// Gerechnet wird in der Engine (<see cref="Preiszerlegung.SummeAktivCtKwh"/>,
        /// sequenzielle Summation): Der Wert erscheint auf dem Bildschirm UND geht in
        /// den Geldwert ein; zwei Summationsreihenfolgen ergäben zwei Zahlen für
        /// dieselbe Grösse.
        /// </remarks>
        public static double SummeCtKwh(Preiszerlegung satz)
        {
            if (satz == null) throw new ArgumentNullException(nameof(satz));
            return satz.SummeAktivCtKwh;
        }

        /// <summary>
        /// Der nicht aufgeschlüsselte Rest [ct/kWh]: Arbeitspreis minus Summe der
        /// aktiven Anteile.
        /// </summary>
        /// <remarks>
        /// Ein NEGATIVER Rest heisst: Die ausgewiesenen Anteile sind zusammen teurer
        /// als der Preis. Das wird benannt (Warnfarbe), nicht geglättet — der Rest wird
        /// deshalb hier auch nicht bei 0 abgeschnitten.
        /// </remarks>
        public static double RestCtKwh(double arbeitspreisCtKwh, Preiszerlegung satz)
        {
            return arbeitspreisCtKwh - SummeCtKwh(satz);
        }
    }
}
