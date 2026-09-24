using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>„Schloss setzen…" und „Schloss aufheben…" — das Auslieferungskennzeichen eines
    /// Katalogsatzes umschalten</b> (Konzept Administrationsdialoge, Entscheid <b>AD-Q15</b>
    /// vom 23.09.2026; löst AD-Q11 ab, nach dem ein Auslieferungssatz nur über
    /// „Duplizieren…" zu einem eigenen Satz wurde).
    ///
    /// <para><b>Was das Kennzeichen ist.</b> Die Spalte <c>ReadOnly</c> (0/1) des Kopfsatzes
    /// einer <c>Tab_*_STAMM</c>-Tabelle. Mit ihr ist ein Satz in der Verwaltung nur lesbar,
    /// „Speichern" und „Löschen" sind gesperrt (Oberfläche und Stamm-Controller). Die
    /// Kindtabellen (Kennlinien, Ganglinienwerte) führen die Spalte ebenfalls, werten sie
    /// aber nicht aus — geschaltet wird deshalb NUR der Kopfsatz.</para>
    ///
    /// <para><b>Was das Umschalten NICHT tut.</b> Es ändert keinen Wert des Satzes, auch
    /// nicht beim Setzen: Ein entsperrter und geänderter Satz behält seine Werte, wenn er
    /// wieder gesperrt wird. Setup, <c>Erstbereitstellung</c> und <c>SchemaMigration</c>
    /// überschreiben Katalogsätze nie (ADR-001) — die ausgelieferten Werte eines geänderten
    /// Satzes gibt es danach nur noch in einer Datenbanksicherung.</para>
    ///
    /// <para><b>EINE Regel für alle Kataloge der Verwaltungen.</b> Tabelle, Id-Spalte und
    /// Name kommen aus der <see cref="KatalogRegistry"/>; je Stamm-Controller steht nur ein
    /// Einzeiler mit dem Schlüssel (Muster <see cref="Katalogkopie"/>). Zwei Besonderheiten
    /// trägt die Registry selbst: <see cref="KatalogDefinition.SchlossGegenspalte"/> (der
    /// Gebäudetyp führt das Schloss zusätzlich umgekehrt in <c>Veraenderbar</c>) und
    /// <see cref="KatalogDefinition.SchlossAusStatus"/> (die Tww-Kataloge, deren
    /// <c>ReadOnly</c> ihrem Freigabestatus folgt — benannt abgelehnt).</para>
    ///
    /// <para><b>Eine Transaktion.</b> Alle genannten Sätze werden in EINEM Vorgang
    /// geschaltet; fehlt einer oder scheitert ein Schritt, bleibt der Katalog, wie er war.
    /// Im Lizenz-Lesemodus lehnt der Weg vorher benannt ab (<see cref="Schreibnaht"/>).</para>
    ///
    /// <para><b>Kein Weg für den Hilfe-Assistenten</b> (AD-Q15): Wie das Löschen ist das
    /// Umschalten eine Handlung, die der Anwender selbst bestätigt; keine Maske meldet sie
    /// dem Assistenten an.</para>
    /// </summary>
    public static class Auslieferungskennzeichen
    {
        /// <summary>Die Spalte des Kennzeichens im Kopfsatz.</summary>
        public const string SPALTE = "ReadOnly";

        /// <summary>
        /// Der Ausgang von <see cref="Setzen(string, IEnumerable{int}, bool)"/>: bei Erfolg je
        /// ID, ob sie geschaltet wurde oder schon im Zielzustand stand; sonst der Grund in der
        /// Oberflächensprache — dann ist nichts geschrieben.
        /// </summary>
        public sealed record Ergebnis(bool Ok, string Meldung,
                                      IReadOnlyList<int> Geaendert, IReadOnlyList<int> Unveraendert);

        /// <summary>
        /// <b>Setzt oder hebt das Schloss der Sätze <paramref name="ids"/></b> im Katalog
        /// <paramref name="schluessel"/> (<see cref="KatalogDefinition.Schluessel"/>) —
        /// <paramref name="gesperrt"/> = <c>true</c> macht sie zu Auslieferungssätzen,
        /// <c>false</c> zu eigenen Sätzen.
        /// </summary>
        public static Ergebnis Setzen(string schluessel, IEnumerable<int> ids, bool gesperrt)
        {
            KatalogDefinition def = KatalogRegistry.Finde(schluessel);
            if (def == null)
                return Abgelehnt(string.Format(CultureInfo.CurrentCulture,
                                               MyResource.Resource.ADM_SCHLOSS_KATALOG_UNBEKANNT, schluessel ?? ""));
            return Setzen(def, ids, gesperrt);
        }

        /// <summary>
        /// Wie <see cref="Setzen(string, IEnumerable{int}, bool)"/>, der Katalog über seine
        /// Kopftabelle (<see cref="KatalogRegistry.FindeTabelle"/>) — der Einzeiler der
        /// Stamm-Controller, die ihre Tabelle als <c>TABLE</c> führen (Muster
        /// <see cref="Katalogkopie.Duplizieren(string, int, string, Katalogkopie.Kindtabelle[])"/>).
        /// </summary>
        public static Ergebnis SetzenInTabelle(string tabelle, IEnumerable<int> ids, bool gesperrt)
        {
            KatalogDefinition def = KatalogRegistry.FindeTabelle(tabelle);
            if (def == null)
                return Abgelehnt(string.Format(CultureInfo.CurrentCulture,
                                               MyResource.Resource.ADM_SCHLOSS_KATALOG_UNBEKANNT, tabelle ?? ""));
            return Setzen(def, ids, gesperrt);
        }

        /// <summary>
        /// Wie <see cref="Setzen(string, IEnumerable{int}, bool)"/>, mit der Beschreibung
        /// selbst — für Prüfstände, die eine Tabelle ohne Kennzeichen vorführen.
        /// </summary>
        internal static Ergebnis Setzen(KatalogDefinition def, IEnumerable<int> ids, bool gesperrt)
        {
            if (def == null)
                return Abgelehnt(string.Format(CultureInfo.CurrentCulture,
                                               MyResource.Resource.ADM_SCHLOSS_KATALOG_UNBEKANNT, ""));

            // Die Tww-Kataloge: ReadOnly folgt dem Freigabestatus (AD-Q15).
            if (def.SchlossAusStatus)
                return Abgelehnt(MyResource.Resource.ADM_SCHLOSS_STATUS);

            // Jede ID einmal, in der Reihenfolge des Aufrufers.
            var liste = new List<int>();
            var gesehen = new HashSet<int>();
            foreach (int id in ids ?? Array.Empty<int>())
                if (gesehen.Add(id)) liste.Add(id);

            var geaendert = new List<int>();
            var unveraendert = new List<int>();
            if (liste.Count == 0) return new Ergebnis(true, "", geaendert, unveraendert);

            // Der Lesemodus der Lizenz sperrt jedes Schreiben - hier benannt, nicht erst als
            // Ausnahme der Schreibnaht mitten im Vorgang.
            if (!Schreibnaht.DarfSchreiben())
                return Abgelehnt(MyResource.Resource.LIZ_LESEMODUS_SPERRE);

            // Tabelle und Spalten kommen aus der Registry, nie aus einer Eingabe; die Werte
            // gehen als ?-Parameter hinein.
            string tabelle = "[" + def.Tabelle + "]";
            string idSpalte = "[" + def.IdSpalte + "]";

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    if (!HatSpalte(v, def.Tabelle, SPALTE))
                    {
                        v.Rollback();
                        return Abgelehnt(string.Format(CultureInfo.CurrentCulture,
                                                       MyResource.Resource.ADM_SCHLOSS_OHNE_KENNZEICHEN,
                                                       KatalogRegistry.Anzeige(def.Schluessel)));
                    }

                    string gegen = def.SchlossGegenspalte ?? "";
                    bool mitGegen = gegen.Length > 0 && HatSpalte(v, def.Tabelle, gegen);

                    string lesen = "SELECT [" + SPALTE + "]" + (mitGegen ? ", [" + gegen + "]" : "") +
                                   " FROM " + tabelle + " WHERE " + idSpalte + " = ?";
                    string schreiben = "UPDATE " + tabelle + " SET [" + SPALTE + "] = ?" +
                                       (mitGegen ? ", [" + gegen + "] = ?" : "") +
                                       " WHERE " + idSpalte + " = ?";

                    foreach (int id in liste)
                    {
                        DataTable dt = v.Lese(lesen, new DbParam("@id", id));
                        if (dt == null || dt.Rows.Count == 0)
                        {
                            // Ein Satz fehlt: nichts wird geschaltet, auch nicht die
                            // Saetze davor - der Vorgang rollt zurueck.
                            v.Rollback();
                            return Abgelehnt(string.Format(CultureInfo.CurrentCulture,
                                                           MyResource.Resource.ADM_SCHLOSS_SATZ_FEHLT, id));
                        }

                        DataRow r = dt.Rows[0];
                        bool jetzt = Kennzeichen(r[SPALTE]) || (mitGegen && !Kennzeichen(r[gegen]));
                        if (jetzt == gesperrt)
                        {
                            unveraendert.Add(id);
                            continue;
                        }

                        var parameter = new List<DbParam> { new DbParam("@ro", gesperrt ? 1 : 0) };
                        if (mitGegen) parameter.Add(new DbParam("@gegen", gesperrt ? 0 : 1));
                        parameter.Add(new DbParam("@id", id));
                        v.Ausfuehren(schreiben, parameter.ToArray());
                        geaendert.Add(id);
                    }

                    v.Commit();
                }
            }
            catch (Exception ex)
            {
                // DbVorgang.Dispose rollt zurueck, wenn kein Commit gesehen wurde.
                return Abgelehnt(MyResource.Resource.ADM_SCHLOSS_FEHLER + " " + ex.Message);
            }

            return new Ergebnis(true, "", geaendert, unveraendert);
        }

        /// <summary>Führt die Tabelle die Spalte? (<c>pragma_table_info</c>, ohne Groß-/Kleinschreibung.)</summary>
        private static bool HatSpalte(DbVorgang v, string tabelle, string spalte)
        {
            DataTable dt = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle));
            if (dt == null) return false;
            foreach (DataRow r in dt.Rows)
                if (string.Equals(Convert.ToString(r["name"], CultureInfo.InvariantCulture), spalte,
                                  StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static bool Kennzeichen(object wert)
            => wert != null && wert != DBNull.Value && Convert.ToInt64(wert, CultureInfo.InvariantCulture) != 0;

        private static Ergebnis Abgelehnt(string meldung)
            => new Ergebnis(false, meldung ?? "", Array.Empty<int>(), Array.Empty<int>());
    }
}
