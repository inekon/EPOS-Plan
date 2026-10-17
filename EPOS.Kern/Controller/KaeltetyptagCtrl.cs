using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>KBP-4a</b> — Lesen, Schreiben, Kopieren und Löschen der Typtage und ihrer Matrix an EINER Projektkopie des
    /// Kältebedarfs (<see cref="KaeltetyptagSchema"/>, Konzept Kältebedarfsprofile 3.4 und 8.2). Alle Zugriffe über
    /// <see cref="DataRepository"/> mit <c>?</c>-Parametern. Ohne die Tabellen (Schritt noch nicht gelaufen) liest der
    /// Controller eine leere Eingabe und lehnt das Schreiben benannt ab.
    ///
    /// <para><b>Mitziehen:</b> Duplizieren, Projektpaket und <c>.wpx</c> nehmen beide Tabellen über den generischen Plan
    /// von <see cref="ProjektDuplizierenCtrl"/> mit (Projektspalte bzw. Fremdschlüssel auf die Projektkopie); die Kopie
    /// bei Bearbeitung (<see cref="KaeltebedarfStammCtrl.ProjektkopieAbspalten"/>) ruft <see cref="Kopieren"/>.</para>
    /// </summary>
    public static class KaeltetyptagCtrl
    {
        // Die Namen kommen ueber Q() und nicht als Konstante: Bis zur Hebung der Testdatenbank (Schritt GEPARKT) kennt sie
        // die Tabellen nicht - der SQL-Dialekt-Pruefer prueft diese Texte deshalb als dynamisch (Syntax und Muster).
        private static string TAB_T => Q(KaeltetyptagSchema.TAB_TYPTAG);
        private static string TAB_M => Q(KaeltetyptagSchema.TAB_MATRIX);

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Stehen die Tabellen?</summary>
        public static bool Verfuegbar() => KaeltetyptagSchema.TabellenVorhanden();

        /// <summary>
        /// Die Typtag-Eingabe der Projektkopie <paramref name="idKaeltebedarf"/>; leer, wenn es keine gibt oder die
        /// Tabellen fehlen. Eine Matrixzelle, deren Typtag zu einer anderen Kopie gehört, gilt als leer.
        /// </summary>
        public static KaeltetyptagEingabe Lies(int idKaeltebedarf)
        {
            var e = new KaeltetyptagEingabe();
            if (idKaeltebedarf <= 0 || !Verfuegbar()) return e;

            DataTable t = DataRepository.GetDataTable("SELECT * FROM " + TAB_T + " WHERE \"ID_Kaeltebedarf\" = ? ORDER BY \"Nr\"",
                                                      new DbParam("@kb", idKaeltebedarf));
            var nrJeId = new Dictionary<int, int>();
            if (t != null)
                foreach (DataRow r in t.Rows)
                {
                    var tag = new Kaeltetyptag
                    {
                        Nr = Convert.ToInt32(r[KaeltetyptagSchema.SPALTE_NR], CultureInfo.InvariantCulture),
                        Bezeichner = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "",
                        Tagesmenge = Zahl(r[KaeltetyptagSchema.SPALTE_TAGESMENGE]),
                    };
                    for (int h = 1; h <= KaeltetyptagSchema.STUNDEN; h++)
                        tag.Stunden[h - 1] = Zahl(r[KaeltetyptagSchema.Stunde(h)]);
                    e.Typtage.Add(tag);
                    nrJeId[Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture)] = tag.Nr;
                }

            DataTable m = DataRepository.GetDataTable(
                "SELECT \"Monat\", \"Tagesart\", \"ID_Kaeltetyptag\" FROM " + TAB_M + " WHERE \"ID_Kaeltebedarf\" = ?",
                new DbParam("@kb", idKaeltebedarf));
            if (m != null)
                foreach (DataRow r in m.Rows)
                {
                    int monat = Convert.ToInt32(r["Monat"], CultureInfo.InvariantCulture);
                    int art = KaeltetyptagSchema.TagesartIndex(Convert.ToString(r["Tagesart"], CultureInfo.InvariantCulture));
                    int idTag = Convert.ToInt32(r["ID_Kaeltetyptag"], CultureInfo.InvariantCulture);
                    if (monat < 1 || monat > KaeltetyptagSchema.MONATE || art < 0) continue;
                    if (nrJeId.TryGetValue(idTag, out int nr)) e.Matrix[monat - 1, art] = nr;
                }
            return e;
        }

        /// <summary>
        /// Ersetzt die Typtag-Eingabe der Projektkopie <paramref name="idKaeltebedarf"/> des Projekts
        /// <paramref name="idProjekt"/> durch <paramref name="eingabe"/> — in EINEM Vorgang (als Unterpunkt, wenn der
        /// Aufrufer einen hält). <c>false</c> mit Grund, wenn die Tabellen fehlen, die Kopie nicht zum Projekt gehört
        /// oder die Eingabe gegen eine Regel verstößt.
        /// </summary>
        public static bool Schreiben(int idProjekt, int idKaeltebedarf, KaeltetyptagEingabe eingabe, out string grund)
        {
            grund = null;
            if (eingabe == null) eingabe = new KaeltetyptagEingabe();
            if (!Verfuegbar())
            {
                grund = "Typtage lassen sich erst nach Schemaschritt " +
                        KaeltetyptagSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) + " speichern.";
                return false;
            }
            grund = eingabe.Pruefen();
            if (grund != null) return false;

            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + KaeltebedarfSchema.TAB_KOPF + "\" WHERE \"ID\" = ? AND \"ID_Projekt\" = ?",
                new DbParam("@kb", idKaeltebedarf), new DbParam("@p", idProjekt));
            if (n == null || n == DBNull.Value || Convert.ToInt32(n, CultureInfo.InvariantCulture) == 0)
            {
                grund = "Die Projektkopie " + idKaeltebedarf.ToString(CultureInfo.InvariantCulture) +
                        " gehoert nicht zum Projekt " + idProjekt.ToString(CultureInfo.InvariantCulture) + ".";
                return false;
            }

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    LoeschenIn(v, idKaeltebedarf);
                    var idJeNr = new Dictionary<int, int>();
                    string einfuegen = InsertTyptag();
                    foreach (Kaeltetyptag t in eingabe.Typtage)
                    {
                        var p = new List<DbParam>
                        {
                            new DbParam("@p", idProjekt), new DbParam("@kb", idKaeltebedarf),
                            new DbParam("@nr", t.Nr), new DbParam("@bez", t.Bezeichner.Trim()),
                        };
                        for (int h = 0; h < KaeltetyptagSchema.STUNDEN; h++)
                            p.Add(new DbParam("@h" + (h + 1).ToString(CultureInfo.InvariantCulture), t.Stunden[h]));
                        p.Add(new DbParam("@g", t.Tagesmenge));
                        idJeNr[t.Nr] = v.EinfuegenUndId(einfuegen, p.ToArray());
                    }
                    for (int m = 0; m < KaeltetyptagSchema.MONATE; m++)
                        for (int a = 0; a < 3; a++)
                        {
                            int nr = eingabe.Matrix[m, a];
                            if (nr == 0) continue;
                            v.Ausfuehren("INSERT INTO " + TAB_M + " (\"ID_Kaeltebedarf\", \"Monat\", \"Tagesart\", \"ID_Kaeltetyptag\") " +
                                         "VALUES (?, ?, ?, ?)",
                                         new DbParam("@kb", idKaeltebedarf), new DbParam("@m", m + 1),
                                         new DbParam("@a", KaeltetyptagSchema.TAGESARTEN[a]), new DbParam("@t", idJeNr[nr]));
                        }
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    grund = ex.Message;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Kopiert die Typtag-Eingabe der Kopie <paramref name="idQuelle"/> an die Kopie <paramref name="idZiel"/> des
        /// Projekts <paramref name="idProjektZiel"/> (Kopie bei Bearbeitung). Ohne Tabellen oder ohne Eingabe der Quelle
        /// ist nichts zu tun (<c>true</c>).
        /// </summary>
        public static bool Kopieren(int idQuelle, int idZiel, int idProjektZiel)
        {
            if (!Verfuegbar()) return true;
            KaeltetyptagEingabe e = Lies(idQuelle);
            if (e.Leer) return true;
            if (Schreiben(idProjektZiel, idZiel, e, out string grund)) return true;
            DataRepository.FehlerMelden("Die Typtage des Kaeltebedarfs liessen sich nicht kopieren: " + grund);
            return false;
        }

        /// <summary>Löscht Typtage und Matrix der Projektkopie <paramref name="idKaeltebedarf"/>.</summary>
        public static bool Loeschen(int idKaeltebedarf)
        {
            if (!Verfuegbar()) return true;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    LoeschenIn(v, idKaeltebedarf);
                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    DataRepository.FehlerMelden("Die Typtage des Kaeltebedarfs liessen sich nicht loeschen: " + ex.Message);
                    return false;
                }
            }
        }

        private static void LoeschenIn(DbVorgang v, int idKaeltebedarf)
        {
            v.Ausfuehren("DELETE FROM " + TAB_M + " WHERE \"ID_Kaeltebedarf\" = ?", new DbParam("@kb", idKaeltebedarf));
            v.Ausfuehren("DELETE FROM " + TAB_T + " WHERE \"ID_Kaeltebedarf\" = ?", new DbParam("@kb", idKaeltebedarf));
        }

        private static string InsertTyptag()
        {
            var spalten = new StringBuilder("\"ID_Projekt\", \"ID_Kaeltebedarf\", \"Nr\", \"Bezeichner\"");
            var werte = new StringBuilder("?, ?, ?, ?");
            for (int h = 1; h <= KaeltetyptagSchema.STUNDEN; h++)
            {
                spalten.Append(", \"").Append(KaeltetyptagSchema.Stunde(h)).Append('"');
                werte.Append(", ?");
            }
            spalten.Append(", \"Tagesmenge\"");
            werte.Append(", ?");
            return "INSERT INTO " + TAB_T + " (" + spalten + ") VALUES (" + werte + ")";
        }

        private static double Zahl(object o) =>
            o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
    }
}
