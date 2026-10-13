using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Normwerte der DIN einlesen</b> (Stufe NP7, Entscheid E131; Konzept Nutzungsprofile 6.5): füllt die ausgelieferten,
    /// leeren Profile der Kategorie DIN/TS 18599-10 in der Datenbank des Anwenders aus einer Quelle des Anwenders — einer CSV
    /// im Format 6.4 (<see cref="RaumnutzungCsv"/>) oder einer Projektdatei mit Nutzungsprofilen
    /// (<see cref="Projektdateiprofile"/>). Zugeordnet wird über die DIN-Nummer, die zwei Wohnprofile ohne Nummer über ihren
    /// Namen (<see cref="RaumnutzungNormkategorienSchema.WOHNPROFILE"/>) oder über die Nummern 70 und 71 der Projektdatei
    /// (<see cref="WohnprofilAusNummer"/>).
    ///
    /// <para><b>Was geschrieben wird:</b> allein die Kennwerte und die Außenluft je Person der ausgelieferten DIN-Profile samt
    /// Herkunft (<see cref="RaumnutzungNormwerteSchema.HERKUNFT_EINGELESEN"/>, Quelle, Tag). Nummer, Name, Beschreibung,
    /// Kategorie und <c>ReadOnly</c> bleiben; Zeilenbild und Stundenprofile der Quelle werden nicht übernommen. Die Vorschau
    /// (<see cref="Vorschau"/>) nennt Treffer, unbekannte Nummern, Abweichungen zu vorhandenen Werten und was abgelehnt
    /// wird; das Einlesen überschreibt vorhandene Werte nur auf ausdrücklichen Wunsch. <see cref="Entfernen"/> nimmt alle
    /// eingelesenen Werte zurück.</para>
    ///
    /// <para><b>Keine Normwerte im Programm:</b> Die Werte kommen allein aus der Quelle des Anwenders; ausgeliefert wird die
    /// Struktur ohne Werte, und die Auslieferungsvorlage nimmt eingelesene Werte heraus (NP-F21).</para>
    /// </summary>
    public static class RaumnutzungNormwerte
    {
        /// <summary>Ein Profil der Quelle, das ein DIN-Profil trifft.</summary>
        /// <param name="IdProfil">Das DIN-Profil der Datenbank.</param>
        /// <param name="Nummer">Seine Nummer (<c>null</c> bei den Wohnprofilen).</param>
        /// <param name="Bezeichner">Sein Name.</param>
        /// <param name="Werte">Das Profil mit den Werten der Quelle (Kopf des DIN-Profils).</param>
        /// <param name="HatWerte">Trägt das DIN-Profil schon Werte?</param>
        /// <param name="Eingelesen">Stammen die vorhandenen Werte aus einem früheren Einlesen?</param>
        /// <param name="Abweichungen">Die Spalten, deren vorhandener Wert vom neuen abweicht.</param>
        public sealed record Treffer(long IdProfil, string Nummer, string Bezeichner, Raumnutzungsprofil Werte, bool HatWerte,
                                     bool Eingelesen, IReadOnlyList<string> Abweichungen);

        /// <summary>Die Vorschau eines Einlesens; schreibt nichts.</summary>
        /// <param name="Ok">Lesbar und mindestens ein Treffer?</param>
        /// <param name="Meldung">Die benannte Ablehnung, sonst <c>null</c>.</param>
        /// <param name="Quelle">Der Name der Quelle (Dateiname), der als Herkunft geschrieben wird.</param>
        /// <param name="Treffer">Die zugeordneten Profile.</param>
        /// <param name="Unbekannt">Profile der Quelle ohne DIN-Profil (Nummer bzw. Name unbekannt).</param>
        /// <param name="Meldungen">Was beim Lesen, Zuordnen und Prüfen benannt wurde.</param>
        public sealed record Vorschau(bool Ok, string Meldung, string Quelle, IReadOnlyList<Treffer> Treffer,
                                      IReadOnlyList<string> Unbekannt, IReadOnlyList<string> Meldungen)
        {
            /// <summary>Die Meldungen zu Profilen der Quelle ohne einen Wert — ebenso in <see cref="Meldungen"/>.</summary>
            public IReadOnlyList<string> OhneWerte { get; init; } = Array.Empty<string>();

            /// <summary>Die Meldungen zu Nummern, die die Quelle mehr als einmal führt — ebenso in <see cref="Meldungen"/>.</summary>
            public IReadOnlyList<string> Doppelt { get; init; } = Array.Empty<string>();

            /// <summary>Treffer, deren DIN-Profil schon Werte trägt (Rückfrage „überschreiben“).</summary>
            public int MitWerten => Treffer.Count(t => t.HatWerte);

            /// <summary>Treffer, deren vorhandene Werte von den neuen abweichen.</summary>
            public int MitAbweichung => Treffer.Count(t => t.Abweichungen.Count > 0);

            internal static Vorschau Fehler(string quelle, string meldung, IReadOnlyList<string> meldungen = null)
                => new Vorschau(false, meldung ?? "", quelle, Array.Empty<Treffer>(), Array.Empty<string>(),
                                meldungen ?? Array.Empty<string>());
        }

        /// <summary>Was das Einlesen oder Entfernen geschrieben hat.</summary>
        public sealed record Bilanz(bool Ok, string Meldung, int Geschrieben, int Uebersprungen);

        // =================================================================
        //  Lesen der Quelle
        // =================================================================

        /// <summary>Liest eine CSV des Anwenders (Format 6.4 mit <c>Aussenluft_Person</c>) und gleicht sie mit der Kategorie DIN ab.</summary>
        public static Vorschau AusCsv(byte[] inhalt, string quelle)
        {
            RaumnutzungCsvLesung lesung = RaumnutzungCsv.Lesen(inhalt ?? Array.Empty<byte>());
            if (lesung.Abbruch != null) return Vorschau.Fehler(quelle, lesung.Abbruch);
            var meldungen = lesung.AlleMeldungen
                .Where(m => m.Art == RaumnutzungCsvArt.Fehler || m.Art == RaumnutzungCsvArt.Ignoriert)
                .Select(m => Zeilentext(m)).ToList();
            foreach (RaumnutzungCsvZeile z in lesung.Zeilen.Where(z => !z.Uebernehmbar))
                meldungen.Add(Text(MyResource.Resource.RNP_NW_MSG_ABGELEHNT, z.Profil?.Nummer, z.Profil?.Bezeichner,
                                   z.Ablehnung ?? z.Zielablehnung));
            return Abgleichen(lesung.Uebernehmbare.Select(z => z.Profil), quelle, meldungen);
        }

        /// <summary>Nimmt die Profile einer gelesenen Projektdatei (<see cref="SqprojRaumnutzung"/>) und gleicht sie mit der Kategorie DIN ab.</summary>
        public static Vorschau AusProjektdatei(Projektdateiprofile satz)
        {
            if (satz == null) throw new ArgumentNullException(nameof(satz));
            if (satz.Abgelehnt) return Vorschau.Fehler(satz.Dateiname, satz.Ablehnung);
            return Abgleichen(satz.Profile.Select(p => p.Profil), satz.Dateiname, satz.Meldungen);
        }

        /// <summary>
        /// <b>Gleicht Profile einer Quelle mit den ausgelieferten DIN-Profilen ab</b>: je Profil der Quelle die DIN-Nummer
        /// (führende Nullen gleich), ohne Nummer der Name eines Wohnprofils. Eine Nummer, die die Quelle zweimal führt, gilt
        /// beim ersten Mal; ein Profil ohne einen Wert und eines, das die Prüfung ablehnt, wird benannt und nicht übernommen.
        /// </summary>
        public static Vorschau Abgleichen(IEnumerable<Raumnutzungsprofil> quelle, string quellname, IEnumerable<string> meldungen = null)
        {
            string name = Quellname(quellname);
            var liste = new List<string>(meldungen ?? Enumerable.Empty<string>());
            if (!RaumnutzungCtrl.Lesbar() || !RaumnutzungNormwerteSchema.Lesbar())
                return Vorschau.Fehler(name, Schemafehlt(), liste);
            List<Raumnutzungsprofil> din = DinProfile();
            if (din.Count == 0) return Vorschau.Fehler(name, MyResource.Resource.RNP_NW_MSG_KEINE_DIN, liste);

            var treffer = new List<Treffer>();
            var unbekannt = new List<string>();
            var vergeben = new HashSet<long>();
            var ohneWerte = new List<string>();
            var doppelt = new List<string>();
            foreach (Raumnutzungsprofil q in (quelle ?? Enumerable.Empty<Raumnutzungsprofil>()).Where(x => x != null))
            {
                Raumnutzungsprofil ziel = Zuordnen(din, q);
                if (ziel == null)
                {
                    unbekannt.Add(Text(MyResource.Resource.RNP_NW_MSG_UNBEKANNT, q.Nummer, q.Bezeichner));
                    continue;
                }
                if (!vergeben.Add(ziel.Id))
                {
                    doppelt.Add(Text(MyResource.Resource.RNP_NW_MSG_DOPPELT, ziel.Nummer, ziel.Bezeichner));
                    liste.Add(doppelt[doppelt.Count - 1]);
                    continue;
                }
                Raumnutzungsprofil neu = MitWerten(ziel, q);
                if (neu.IstLeer)
                {
                    ohneWerte.Add(Text(MyResource.Resource.RNP_NW_MSG_OHNE_WERTE, ziel.Nummer, ziel.Bezeichner));
                    liste.Add(ohneWerte[ohneWerte.Count - 1]);
                    continue;
                }
                string pruefung = RaumnutzungCtrl.Profilpruefung(neu);
                if (pruefung != null)
                {
                    liste.Add(Text(MyResource.Resource.RNP_NW_MSG_ABGELEHNT, ziel.Nummer, ziel.Bezeichner, pruefung));
                    continue;
                }
                treffer.Add(new Treffer(ziel.Id, ziel.Nummer, ziel.Bezeichner, neu, !ziel.IstLeer, ziel.Eingelesen,
                                        ziel.IstLeer ? Array.Empty<string>() : Abweichungen(ziel, neu)));
            }
            if (treffer.Count == 0)
                return new Vorschau(false, MyResource.Resource.RNP_NW_MSG_NICHTS, name, treffer, unbekannt, liste)
                    { OhneWerte = ohneWerte, Doppelt = doppelt };
            return new Vorschau(true, null, name, treffer, unbekannt, liste) { OhneWerte = ohneWerte, Doppelt = doppelt };
        }

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>
        /// <b>Liest die Werte der Vorschau ein</b> — in einem Vorgang. Ein DIN-Profil, das schon Werte trägt, wird nur mit
        /// <paramref name="ueberschreiben"/> geschrieben, sonst übersprungen. Herkunft „eingelesen“, Quelle und Tag
        /// (<paramref name="tag"/>, ohne Angabe heute) stehen danach am Profil.
        /// </summary>
        public static Bilanz Einlesen(Vorschau vorschau, bool ueberschreiben, DateTime? tag = null)
        {
            if (vorschau == null) throw new ArgumentNullException(nameof(vorschau));
            if (!vorschau.Ok) return new Bilanz(false, vorschau.Meldung, 0, 0);
            if (!RaumnutzungNormwerteSchema.Lesbar()) return new Bilanz(false, Schemafehlt(), 0, 0);
            string datum = (tag ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string quelle = Quellname(vorschau.Quelle);
            int geschrieben = 0, uebersprungen = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (Treffer t in vorschau.Treffer)
                    {
                        if (t.HatWerte && !ueberschreiben)
                        {
                            uebersprungen++;
                            continue;
                        }
                        Raumnutzungsprofil p = t.Werte.Kopie();
                        p.Werte_Herkunft = RaumnutzungNormwerteSchema.HERKUNFT_EINGELESEN;
                        p.Werte_Quelle = quelle;
                        p.Werte_Datum = datum;
                        geschrieben += v.Ausfuehren(SQL_WERTE_SETZEN, WerteParameter(p, t.IdProfil));
                    }
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return new Bilanz(false, ex.Message, 0, 0);
                }
            }
            return new Bilanz(true, Text(MyResource.Resource.RNP_NW_MSG_ERGEBNIS, geschrieben, uebersprungen, vorschau.Unbekannt.Count),
                              geschrieben, uebersprungen);
        }

        /// <summary>
        /// <b>Nimmt alle eingelesenen Werte zurück</b>: jedes ausgelieferte Profil mit Herkunft „eingelesen“ steht danach wieder
        /// ohne Werte (Kennwerte, Außenluft je Person, Herkunft leer). Auch die Auslieferungsvorlage ruft das.
        /// </summary>
        public static Bilanz Entfernen()
        {
            if (!RaumnutzungNormwerteSchema.Lesbar()) return new Bilanz(false, Schemafehlt(), 0, 0);
            int n;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    n = v.Ausfuehren(SQL_ENTFERNEN, new DbParam("@h", RaumnutzungNormwerteSchema.HERKUNFT_EINGELESEN));
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return new Bilanz(false, ex.Message, 0, 0);
                }
            }
            return new Bilanz(true, Text(MyResource.Resource.RNP_NW_MSG_ENTFERNT, n), n, 0);
        }

        /// <summary>Die Zahl der ausgelieferten Profile mit eingelesenen Werten (0 ohne Schritt 220).</summary>
        public static int Eingelesene()
        {
            if (!RaumnutzungNormwerteSchema.Lesbar()) return 0;
            object o = DataRepository.ExecuteScalar(SQL_ZAHL_EINGELESEN, new DbParam("@h", RaumnutzungNormwerteSchema.HERKUNFT_EINGELESEN));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>Die CSV-Vorlage ohne Werte</b> (Weg 3): Kopfzeile des Formats 6.4 und je DIN-Profil Nummer und Name, jede
        /// Wertzelle leer — auch wenn die Datenbank schon eingelesene Werte trägt. Leer ohne Kategorie DIN.
        /// </summary>
        public static byte[] Vorlage()
            => RaumnutzungCsv.SchreibenBytes(DinProfile().Select(p => new Raumnutzungsprofil { Nummer = p.Nummer, Bezeichner = p.Bezeichner }));

        // =================================================================
        //  Helfer
        // =================================================================

        private static readonly string Q_PROFIL = "\"" + RaumnutzungSchema.TAB_PROFIL + "\"";
        private static readonly string Q_KATALOG = "\"" + RaumnutzungSchema.TAB_KATALOG + "\"";

        /// <summary>Die Spalten, die das Einlesen schreibt: die 25 Kennwerte, dann die vier Spalten des Schritts 220.</summary>
        internal static IReadOnlyList<string> Wertspalten()
            => RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => s.Spalte)
                   .Concat(RaumnutzungNormwerteSchema.SPALTEN.Select(s => s.Spalte)).ToList();

        private static readonly string BEDINGUNG_DIN =
            " AND \"ReadOnly\" = 1 AND \"ID_Katalog\" IN (SELECT k.\"ID\" FROM " + Q_KATALOG + " k WHERE k.\"Art\" = ? AND k.\"ReadOnly\" = 1)";

        private static readonly string SQL_WERTE_SETZEN =
            "UPDATE " + Q_PROFIL + " SET " + string.Join(", ", Wertspalten().Select(c => "\"" + c + "\" = ?")) +
            " WHERE \"ID\" = ?" + BEDINGUNG_DIN;

        private static readonly string SQL_ENTFERNEN =
            "UPDATE " + Q_PROFIL + " SET " + string.Join(", ", Wertspalten().Select(c => "\"" + c + "\" = NULL")) +
            " WHERE \"" + RaumnutzungNormwerteSchema.SPALTE_HERKUNFT + "\" = ? AND \"ReadOnly\" = 1";

        private static readonly string SQL_ZAHL_EINGELESEN =
            "SELECT COUNT(*) FROM " + Q_PROFIL + " WHERE \"" + RaumnutzungNormwerteSchema.SPALTE_HERKUNFT + "\" = ? AND \"ReadOnly\" = 1";

        private static readonly string SQL_DIN_KATALOG =
            "SELECT \"ID\" FROM " + Q_KATALOG + " WHERE \"Art\" = ? AND \"ReadOnly\" = 1 ORDER BY \"ID\"";

        private static DbParam[] WerteParameter(Raumnutzungsprofil p, long id)
        {
            var par = new List<DbParam>();
            IReadOnlyList<object> kennwerte = p.Kennwerte();
            for (int i = 0; i < kennwerte.Count; i++)
                par.Add(new DbParam("@w" + i.ToString(CultureInfo.InvariantCulture), kennwerte[i] is bool b ? (b ? 1 : 0) : kennwerte[i]));
            object[] n = RaumnutzungCtrl.Normwerte(p);
            for (int i = 0; i < n.Length; i++) par.Add(new DbParam("@n" + i.ToString(CultureInfo.InvariantCulture), n[i]));
            par.Add(new DbParam("@id", id));
            par.Add(new DbParam("@art", RaumnutzungSchema.ART_DIN_V_18599_10));
            return par.ToArray();
        }

        /// <summary>Die ausgelieferten Profile der Kategorie DIN (leer ohne Kategorie).</summary>
        internal static List<Raumnutzungsprofil> DinProfile()
        {
            if (!RaumnutzungCtrl.Lesbar()) return new List<Raumnutzungsprofil>();
            DataTable t = DataRepository.GetDataTable(SQL_DIN_KATALOG, new DbParam("@a", RaumnutzungSchema.ART_DIN_V_18599_10));
            if (t == null || t.Rows.Count == 0) return new List<Raumnutzungsprofil>();
            long id = Convert.ToInt64(t.Rows[0]["ID"], CultureInfo.InvariantCulture);
            return new RaumnutzungCtrl().Profile(id).Where(p => p.Ausgeliefert).ToList();
        }

        /// <summary>Die DIN-Nummer in Normalform: ganze Zahl ohne führende Nullen, sonst getrimmt; leer = <c>null</c>.</summary>
        public static string Nummernschluessel(string nummer)
        {
            string n = nummer?.Trim();
            if (string.IsNullOrEmpty(n)) return null;
            return int.TryParse(n, NumberStyles.None, CultureInfo.InvariantCulture, out int z) ? z.ToString(CultureInfo.InvariantCulture) : n;
        }

        /// <summary>Das DIN-Profil zu einem Profil der Quelle: über die Nummer, ohne Nummer über den Namen eines Wohnprofils.</summary>
        internal static Raumnutzungsprofil Zuordnen(IReadOnlyList<Raumnutzungsprofil> din, Raumnutzungsprofil q)
        {
            string nr = Nummernschluessel(q.Nummer);
            if (nr != null)
            {
                Raumnutzungsprofil ueberNummer = din.FirstOrDefault(p => string.Equals(Nummernschluessel(p.Nummer), nr, StringComparison.OrdinalIgnoreCase));
                if (ueberNummer != null) return ueberNummer;
                // Nur beim Einlesen: die Wohnzeilen der Projektdatei (Nummern 70/71) treffen die Wohnprofile ohne Nummer.
                string wohnprofil = WohnprofilAusNummer(nr);
                return wohnprofil == null ? null : din.FirstOrDefault(p => p.Nummer == null &&
                                                                           string.Equals(p.Bezeichner, wohnprofil, StringComparison.OrdinalIgnoreCase));
            }
            string name = q.Bezeichner?.Trim();
            if (string.IsNullOrEmpty(name)) return null;
            return din.FirstOrDefault(p => p.Nummer == null &&
                                           RaumnutzungNormkategorienSchema.WOHNPROFILE.Contains(p.Bezeichner, StringComparer.OrdinalIgnoreCase) &&
                                           string.Equals(p.Bezeichner, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Nummer der Projektdatei der Wohnzeile Einfamilienhaus (nur für das Einlesen der Normwerte).</summary>
        public const string NUMMER_WOHNEN_EFH = "70";

        /// <summary>Nummer der Projektdatei der Wohnzeile Mehrfamilienhaus (nur für das Einlesen der Normwerte).</summary>
        public const string NUMMER_WOHNEN_MFH = "71";

        /// <summary>
        /// Das Wohnprofil zu einer Nummer der Projektdatei: 70 = „Wohngebäude – Einfamilienhaus“, 71 = „– Mehrfamilienhaus“,
        /// sonst <c>null</c>. Gilt allein für das Einlesen der Normwerte; die Raumzuordnung auf das Muster Wohnen bleibt.
        /// </summary>
        public static string WohnprofilAusNummer(string nummer)
            => Nummernschluessel(nummer) switch
            {
                NUMMER_WOHNEN_EFH => RaumnutzungNormkategorienSchema.WOHNEN_EFH,
                NUMMER_WOHNEN_MFH => RaumnutzungNormkategorienSchema.WOHNEN_MFH,
                _ => null,
            };

        /// <summary>Kopf des DIN-Profils mit den Werten der Quelle (ohne Zeilenbild und Stunden, ohne Herkunft).</summary>
        internal static Raumnutzungsprofil MitWerten(Raumnutzungsprofil ziel, Raumnutzungsprofil q)
        {
            Raumnutzungsprofil p = q.Kopie();
            p.Id = ziel.Id;
            p.IdKatalog = ziel.IdKatalog;
            p.Nummer = ziel.Nummer;
            p.Bezeichner = ziel.Bezeichner;
            p.Beschreibung = ziel.Beschreibung;
            p.Ausgeliefert = true;
            p.Nutzungstage_Jahr = null;
            p.Zeilen = new List<Vorgabezeile>();
            p.Stunden = new List<Raumnutzungsstunden>();
            p.Werte_Herkunft = null;
            p.Werte_Quelle = null;
            p.Werte_Datum = null;
            return p;
        }

        /// <summary>Die Spalten, in denen ein vorhandener Wert vom neuen abweicht (ein leerer vorhandener Wert weicht nicht ab).</summary>
        internal static IReadOnlyList<string> Abweichungen(Raumnutzungsprofil alt, Raumnutzungsprofil neu)
        {
            var spalten = RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => s.Spalte)
                .Concat(new[] { RaumnutzungNormwerteSchema.SPALTE_AUSSENLUFT_PERSON }).ToList();
            object[] a = alt.Kennwerte().Concat(new object[] { alt.Aussenluft_Person }).ToArray();
            object[] b = neu.Kennwerte().Concat(new object[] { neu.Aussenluft_Person }).ToArray();
            var liste = new List<string>();
            for (int i = 0; i < spalten.Count; i++)
                if (a[i] != null && !Equals(a[i], b[i])) liste.Add(spalten[i]);
            return liste;
        }

        private static string Quellname(string quelle)
        {
            string n = GebaeudeQuelle.NurName(quelle ?? "").Trim();
            if (n.Length == 0) n = "?";
            return n.Length <= RaumnutzungNormwerteSchema.QUELLE_MAX_ZEICHEN ? n : n.Substring(0, RaumnutzungNormwerteSchema.QUELLE_MAX_ZEICHEN);
        }

        private static string Schemafehlt()
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_SCHEMA_FEHLT,
                             RaumnutzungNormwerteSchema.SCHRITT.ToString(CultureInfo.InvariantCulture));

        private static string Zeilentext(RaumnutzungCsvMeldung m)
            => string.Format(CultureInfo.CurrentCulture, "{0} {1}: {2}", m.Zeile, m.Spalte, m.Grund);

        private static string Text(string muster, params object[] werte)
            => string.Format(CultureInfo.CurrentCulture, muster, werte.Select(w => w ?? "—").ToArray());
    }
}
